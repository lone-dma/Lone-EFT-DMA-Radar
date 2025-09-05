namespace EftDmaRadarLite.Twitch
{
    public sealed class CachedTwitchEntry
    {
        [JsonPropertyName("timestamp")]
        [JsonInclude]
        public DateTime Timestamp { get; init; } = DateTime.Now;
        [JsonPropertyName("twitchLogin")]
        [JsonInclude]
        public string TwitchLogin { get; init; }

        [JsonIgnore]
        public TimeSpan Age => DateTime.Now - Timestamp;
        [JsonIgnore]
        public bool Expired => Age > TimeSpan.FromMinutes(15);
    }
}
