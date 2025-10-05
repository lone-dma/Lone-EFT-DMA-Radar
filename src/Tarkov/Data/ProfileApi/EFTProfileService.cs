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

using EftDmaRadarLite.DMA;
using EftDmaRadarLite.Misc;
using EftDmaRadarLite.Misc.Cache;
using EftDmaRadarLite.Tarkov.Data.ProfileApi.Providers;
using EftDmaRadarLite.Tarkov.Player;
using LiteDB;
using System.Threading.Tasks.Dataflow;

namespace EftDmaRadarLite.Tarkov.Data.ProfileApi
{
    internal static class EFTProfileService
    {
        private static readonly ActionBlock<ProfileJob> _block;
        private static readonly IProfileApiProvider[] _providers;
        private static CancellationTokenSource _cts = new();

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
                return; // No providers, exit early
            // Start Dataflow block
            _block = new ActionBlock<ProfileJob>(
            async job =>
            {
                try
                {
                    if (job.Token.IsCancellationRequested)
                        return;
                    await ProcessProfileAsync(job);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[EFTProfileService] Unhandled Exception: {ex}");
                }
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Math.Min(5, Environment.ProcessorCount),
                BoundedCapacity = DataflowBlockOptions.Unbounded,
                EnsureOrdered = false
            });
            MemDMA.RaidStopped += MemDMA_RaidStopped;
        }

        private static void MemDMA_RaidStopped(object sender, EventArgs e)
        {
            var old = Interlocked.Exchange(ref _cts, new CancellationTokenSource());
            old.Cancel();
            old.Dispose();
        }

        /// <summary>
        /// Attempt to register a Profile for lookup.
        /// </summary>
        public static void RegisterProfile(PlayerProfile profile)
        {
            if (_providers.Length == 0)
                return; // No providers, skip
            _block.Post(new ProfileJob(profile, _cts.Token));
        }

        /// <summary>
        /// Get profile data for a particular Account ID.
        /// </summary>
        private static async Task ProcessProfileAsync(ProfileJob job)
        {
            var profile = job.Profile;
            var ct = job.Token;
            ct.ThrowIfCancellationRequested();
            if (!long.TryParse(profile.AccountID, out var acctIdLong))
                return; // Skip invalid Account IDs

            var cache = LocalCache.GetProfileCollection();
            // Check Cache for recent data
            var cachedDto = cache.FindById(acctIdLong);
            if (cachedDto is not null && cachedDto.IsCachedRecent) // Avoid API lookups if we have recent cached data
            {
                try
                {
                    profile.Data ??= cachedDto.ToProfileData();
                    return; // Done
                }
                catch
                {
                    // Corrupted cache, proceed to do lookups
                }
            }
            foreach (var provider in _providers)
            {
                if (provider.CanRun && provider.CanLookup(profile.AccountID))
                {
                    var result = await provider.GetProfileAsync(profile.AccountID, ct);
                    ct.ThrowIfCancellationRequested();
                    if (result is not null) // Success
                    {
                        // Validate result members
                        ArgumentNullException.ThrowIfNull(result.Data, nameof(result.Data));
                        ArgumentException.ThrowIfNullOrWhiteSpace(result.Raw, nameof(result.Raw));
                        ArgumentOutOfRangeException.ThrowIfEqual(result.Updated, default, nameof(result.Updated));
                        // Check result against cache
                        if (cachedDto is not null && cachedDto.Updated > result.Updated)
                        {
                            try
                            {
                                profile.Data ??= cachedDto.ToProfileData(); // Use newer cached data
                                return; // Don't overwrite with older data
                            }
                            catch
                            {
                                // Corrupted cache, proceed to overwrite
                            }
                        }
                        // Set result and update cache
                        profile.Data ??= result.Data;
                        cachedDto ??= new EftProfileDto
                        {
                            Id = acctIdLong,
                        };
                        cachedDto.Data = result.Raw.MinifyJson();
                        cachedDto.Updated = result.Updated;
                        cachedDto.Cached = DateTimeOffset.UtcNow;
                        _ = cache.Upsert(cachedDto);
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
                try
                {
                    profile.Data ??= cachedDto?.ToProfileData();
                }
                catch { } // This may throw, ignore
                // Can't find it but we have no options left ¯\_(ツ)_/¯
            }
            else // Still have providers to try
            {
                // Put back for retry -> avoid busy looping
                // Returns immediately so other processing can continue
                _ = Task.Run(async () => 
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), ct);
                    _block.Post(job);
                });
            }
        }

        private sealed record ProfileJob(PlayerProfile Profile, CancellationToken Token);
    }
}