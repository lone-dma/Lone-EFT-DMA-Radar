using eft_dma_radar.Tarkov.Data.ProfileApi.Schema;

namespace eft_dma_radar.Tarkov.Data.ProfileApi
{
    public interface IProfileApiProvider
    {
        private static readonly ConcurrentBag<IProfileApiProvider> _providers = new();
        /// <summary>
        /// All Profile API providers.
        /// </summary>
        public static IEnumerable<IProfileApiProvider> AllProviders => _providers;

        /// <summary>
        /// True if the provider is enabled.
        /// </summary>
        bool IsEnabled { get; }
        /// <summary>
        /// True if the provider can run at this time.
        /// </summary>
        bool CanRun { get; }
        /// <summary>
        /// Priority of this data. Lower values indicate a better quality provider.
        /// </summary>
        uint Priority { get; }

        /// <summary>
        /// See if the provider can lookup a profile by account ID, and hasn't previously returned NOT FOUND, etc. that indicates you should not try again.
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        bool CanLookup(string accountId);

        /// <summary>
        /// Lookup a profile by account ID.
        /// Must not throw exceptions.
        /// </summary>
        /// <param name="accountId">Account id of player profile to lookup.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Player profile result. NULL if not found or an error occurred.</returns>
        Task<ProfileData> GetProfileAsync(string accountId, CancellationToken ct);

        /// <summary>
        /// Add a provider to the collection.
        /// </summary>
        /// <param name="provider">Provider to add.</param>
        protected static void Register(IProfileApiProvider provider) => _providers.Add(provider);
    }
}
