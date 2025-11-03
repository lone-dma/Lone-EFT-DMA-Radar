using LoneEftDmaRadar.Web.ProfileApi.Schema;

namespace LoneEftDmaRadar.Web.ProfileApi
{
    public sealed class EFTProfileResponse
    {
        /// <summary>
        /// <see cref="ProfileData"/> instance ready for consumption.
        /// </summary>
        public ProfileData Data { get; init; }
        /// <summary>
        /// Raw web response from the provider (for caching purposes).
        /// </summary>
        public string Raw { get; init; }
        /// <summary>
        /// Date and time when the profile was originally looked up by the provider.
        /// </summary>
        public DateTimeOffset Updated { get; init; }
    }
}
