/*
 * EFT DMA Radar Lite
 * Brought to you by Lone (Lone DMA)
 * 
MIT License

Copyright (c) 2025 Lone DMA

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 *
*/

using EftDmaRadarLite.DMA;
using EftDmaRadarLite.Misc.Cache;
using EftDmaRadarLite.Tarkov.Data.ProfileApi.Providers;
using EftDmaRadarLite.Tarkov.Player;
using LiteDB;
using System.Threading.Channels;

namespace EftDmaRadarLite.Tarkov.Data.ProfileApi
{
    internal static class EFTProfileService
    {
        #region Fields / Constructor
        private static readonly Lock _syncRoot = new();
        private static readonly Channel<PlayerProfile> _channel = Channel.CreateUnbounded<PlayerProfile>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });
        private static CancellationTokenSource _cts = new();

        static EFTProfileService()
        {
            RuntimeHelpers.RunClassConstructor(typeof(EftApiTechProvider).TypeHandle);
            RuntimeHelpers.RunClassConstructor(typeof(TarkovDevProvider).TypeHandle);
            MemDMA.ProcessStopped += MemDMA_ProcessStopped;
            _ = Task.Run(WorkerRoutineAsync);
        }

        private static void MemDMA_ProcessStopped(object sender, EventArgs e)
        {
            lock (_syncRoot)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = new();
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Attempt to register a Profile for lookup.
        /// </summary>
        public static void RegisterProfile(PlayerProfile profile)
        {
            _ = _channel.Writer.TryWrite(profile);
        }

        #endregion

        #region Internal API

        private static async Task WorkerRoutineAsync()
        {
            while (true)
            {
                try
                {
                    CancellationToken ct;
                    lock (_syncRoot)
                    {
                        ct = _cts.Token;
                    }

                    // Wait until we are actually in-raid before starting to read/process.
                    while (!Memory.InRaid)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), ct);
                    }

                    var cache = LocalCache.GetProfileCollection();

                    await foreach (var profile in _channel.Reader.ReadAllAsync(ct))
                    {
                        await ProcessProfileAsync(profile, cache, ct);
                        await Task.Yield();
                    }
                }
                catch (OperationCanceledException)
                {
                    // Cancellation expected when ProcessStopped triggers; loop will restart with new token.
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[EFTProfileService] Unhandled Exception: {ex}");
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250));
                }
            }
        }

        /// <summary>
        /// Get profile data for a particular Account ID.
        /// </summary>
        private static async Task ProcessProfileAsync(PlayerProfile profile, ILiteCollection<CachedPlayerProfile> cache, CancellationToken ct)
        {
            if (!long.TryParse(profile.AccountID, out var acctIdLong))
                return; // Skip invalid Account IDs

            var validProviders = IProfileApiProvider.AllProviders.Where(IsValidProvider);
            if (!validProviders.Any())
                return; // No valid providers, don't ever try again

            var provider = validProviders
                .Where(x => x.CanRun)
                .OrderBy(x => x.Priority)
                .FirstOrDefault();

            if (provider is null) // None available right now
            {
                await RetryProfileAsync(); // Eligible for retry
                return;
            }

            var result = await provider.GetProfileAsync(profile.AccountID, ct);
            if (result is not null) // Success
            {
                var cachedProfile = cache.FindById(acctIdLong);
                if (cachedProfile is not null && result.LastUpdated < cachedProfile.Updated)
                {
                    try
                    {
                        profile.Data ??= cachedProfile.ToProfileData(); // Use newer cached data
                        return; // Don't overwrite with older data
                    }
                    catch
                    {
                        // Corrupted cache, proceed to overwrite
                    }
                }

                profile.Data ??= result.Data;
                if (result.Raw is null)
                    return; // Cannot cache without raw data

                cachedProfile ??= new CachedPlayerProfile
                {
                    Id = acctIdLong,
                };
                cachedProfile.Data = result.Raw;
                cachedProfile.Updated = result.LastUpdated;
                cachedProfile.CachedAt = DateTimeOffset.UtcNow;
                _ = cache.Upsert(cachedProfile);
            }
            else
            {
                await RetryProfileAsync(); // Retry later
            }

            bool IsValidProvider(IProfileApiProvider p) => p.IsEnabled && p.CanLookup(profile.AccountID);
            async Task RetryProfileAsync()
            {
                if (IProfileApiProvider.AllProviders.Any(IsValidProvider))
                {
                    await _channel.Writer.WriteAsync(profile, ct); // Put back for retry
                }
            }
        }

        #endregion
    }
}
