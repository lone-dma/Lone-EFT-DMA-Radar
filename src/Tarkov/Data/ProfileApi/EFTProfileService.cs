﻿/*
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

using EftDmaRadarLite.Misc.Cache;
using EftDmaRadarLite.Tarkov.Data.ProfileApi.Providers;
using EftDmaRadarLite.Tarkov.Player;
using LiteDB;
using System.Threading.Channels;

namespace EftDmaRadarLite.Tarkov.Data.ProfileApi
{
    internal static class EFTProfileService
    {
        private static readonly ParallelOptions _parallelOptions = new()
        {
            MaxDegreeOfParallelism = Math.Min(5, Environment.ProcessorCount)
        };
        private static readonly Channel<PlayerProfile> _channel = Channel.CreateUnbounded<PlayerProfile>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });
        private static readonly IProfileApiProvider[] _providers;

        static EFTProfileService()
        {
            // Ensure static ctors run
            RuntimeHelpers.RunClassConstructor(typeof(EftApiTechProvider).TypeHandle);
            RuntimeHelpers.RunClassConstructor(typeof(TarkovDevProvider).TypeHandle);
            // Get providers
            _providers = IProfileApiProvider.AllProviders
                .Where(x => x.IsEnabled)
                .OrderBy(x => x.Priority)
                .ToArray();
            if (_providers.Length == 0)
                return; // No providers, don't start worker
            // Start worker
            _ = Task.Run(WorkerRoutineAsync);
        }

        /// <summary>
        /// Attempt to register a Profile for lookup.
        /// </summary>
        public static void RegisterProfile(PlayerProfile profile)
        {
            if (_providers.Length == 0)
                return; // No providers, skip
            _ = _channel.Writer.TryWrite(profile);
        }

        private static async Task WorkerRoutineAsync()
        {
            while (true)
            {
                try
                {
                    while (!Memory.InRaid)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }

                    var cache = LocalCache.GetProfileCollection();

                    await Parallel.ForEachAsync(
                        _channel.Reader.ReadAllAsync(),
                        _parallelOptions,
                        async (profile, _) => await ProcessProfileAsync(profile, cache));
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
        private static async Task ProcessProfileAsync(PlayerProfile profile, ILiteCollection<CachedPlayerProfile> cache)
        {
            if (!long.TryParse(profile.AccountID, out var acctIdLong))
                return; // Skip invalid Account IDs

            foreach (var provider in _providers)
            {
                if (provider.CanRun && provider.CanLookup(profile.AccountID))
                {
                    var result = await provider.GetProfileAsync(profile.AccountID);
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
                        return; // Processed, don't continue
                    }
                    // Failed to get profile, try next provider
                } // end if
            } // end foreach
            // No providers were successful. Providers may be on cooldown, or none can lookup this Account ID, but we are not sure which at this point.
            // Before we use the cache we should make sure that no providers are actually capable of looking this up, otherwise it's just best to wait and retry later.
            bool anyValidProviders = false;
            foreach (var provider in _providers)
            {
                if (provider.CanLookup(profile.AccountID))
                {
                    anyValidProviders = true;
                    break;
                }
            }
            if (!anyValidProviders) // No providers left to try -> check cache as a last ditch effort
            {
                var cachedProfile = cache.FindById(acctIdLong);
                try
                {
                    profile.Data ??= cachedProfile?.ToProfileData();
                }
                catch { } // This may throw, ignore
                // Can't find it but we have no options left ¯\_(ツ)_/¯
            }
            else // Still have providers to try, retry later
            {
                await _channel.Writer.WriteAsync(profile); // Put back for retry
            }
        }
    }
}