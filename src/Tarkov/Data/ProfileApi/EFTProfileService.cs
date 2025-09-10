using EftDmaRadarLite.DMA;
using EftDmaRadarLite.Tarkov.Data.ProfileApi.Providers;
using EftDmaRadarLite.Tarkov.Player;

namespace EftDmaRadarLite.Tarkov.Data.ProfileApi
{
    internal static class EFTProfileService
    {
        #region Fields / Constructor
        private static readonly Lock _syncRoot = new();
        private static readonly ConcurrentQueue<PlayerProfile> _profiles = new();
        private static CancellationTokenSource _cts = new();

        /// <summary>
        /// Persistent Cache Access.
        /// </summary>
        private static ConcurrentDictionary<string, CachedProfileData> Cache { get; } = App.Config.Cache.ProfileService;

        static EFTProfileService()
        {
            RuntimeHelpers.RunClassConstructor(typeof(EftApiTechProvider).TypeHandle);
            RuntimeHelpers.RunClassConstructor(typeof(TarkovDevProvider).TypeHandle);
            MemDMA.ProcessStopped += MemDMA_ProcessStopped;
            // Cleanup Cache
            var expiredProfiles = Cache.Where(x => x.Value.IsExpired);
            foreach (var expired in expiredProfiles)
                Cache.TryRemove(expired.Key, out _);
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
                    while (_profiles.TryDequeue(out var profile))
                    {
                        await ProcessProfileAsync(profile, ct);
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
        private static async Task ProcessProfileAsync(PlayerProfile profile, CancellationToken ct)
        {
            if (Cache.TryGetValue(profile.AccountID, out var cachedProfile) && !cachedProfile.IsExpired)
            {
                profile.Data ??= cachedProfile.Data;
                return; // Success exit early
            }
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
                Cache[profile.AccountID] = new CachedProfileData()
                {
                    Data = result
                };
                profile.Data ??= result;
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
