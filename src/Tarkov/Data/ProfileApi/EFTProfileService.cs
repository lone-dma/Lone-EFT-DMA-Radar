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
using EftDmaRadarLite.Tarkov.Data.ProfileApi.Schema;
using EftDmaRadarLite.Tarkov.Player;
using LiteDB;

namespace EftDmaRadarLite.Tarkov.Data.ProfileApi
{
    internal static class EFTProfileService
    {
        #region Fields / Constructor
        private static readonly Lock _syncRoot = new();
        private static readonly ConcurrentQueue<PlayerProfile> _profiles = new();
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
        /// <param name="accountId">Profile's Account ID.</param>
        public static void RegisterProfile(PlayerProfile profile)
        {
            if (!ulong.TryParse(profile.AccountID, out _))
                return; // Skip invalid Account IDs
            _profiles.Enqueue(profile);
        }

        #endregion

        #region Internal API

        private static async Task WorkerRoutineAsync()
        {
            while (true)
            {
                try
                {
                    while (!Memory.InRaid)
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    CancellationToken ct;
                    lock (_syncRoot)
                    {
                        ct = _cts.Token;
                    }
                    var cache = LocalCache.GetProfileCollection();
                    while (_profiles.TryDequeue(out var profile))
                    {
                        await ProcessProfileAsync(profile, cache, ct);
                        await Task.Delay(0, ct);
                    }
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
        /// <param name="profile">Profile to lookup.</param>
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
            if (provider is null)
            {
                TryReEnqueueProfile(); // Eligible for retry
                return;
            }
            var result = await provider.GetProfileAsync(profile.AccountID, ct);
            if (result is not null) // Success
            {
                profile.Data ??= result.Data; // Set result on profile
                var cachedProfile = cache.FindById(acctIdLong);
                if (cachedProfile is not null && result.LastUpdated <= cachedProfile.Updated)
                    return;
                if (result.Raw is null)
                    return; // Don't cache if we don't have raw data
                cachedProfile ??= new CachedPlayerProfile
                {
                    Id = acctIdLong,
                };
                cachedProfile.Data = result.Raw;
                cachedProfile.Updated = result.LastUpdated;
                cachedProfile.CachedAt = DateTimeOffset.UtcNow;
                _ = cache.Upsert(cachedProfile);
            }
            else // Fail
            {
                TryReEnqueueProfile(); // Eligible for retry
            }
            bool IsValidProvider(IProfileApiProvider provider) => provider.IsEnabled && provider.CanLookup(profile.AccountID);
            void TryReEnqueueProfile()
            {
                if (IProfileApiProvider.AllProviders.Any(IsValidProvider)) 
                {
                    _profiles.Enqueue(profile);
                }
            }
        }

        #endregion
    }
}
