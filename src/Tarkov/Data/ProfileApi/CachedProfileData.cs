using EftDmaRadarLite.Tarkov.Data.ProfileApi.Schema;

namespace EftDmaRadarLite.Tarkov.Data.ProfileApi
{
    public sealed class CachedProfileData
    {
        [JsonPropertyName("timestamp")]
        [JsonInclude]
        public DateTime Timestamp { get; init; } = DateTime.Now;
        [JsonPropertyName("data")]
        [JsonInclude]
        public ProfileData Data { get; init; }

        [JsonIgnore]
        public bool IsExpired => DateTime.Now - Timestamp > TimeSpan.FromDays(1);
    }
}
