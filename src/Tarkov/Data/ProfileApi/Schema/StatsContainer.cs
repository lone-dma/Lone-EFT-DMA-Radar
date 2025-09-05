namespace EftDmaRadarLite.Tarkov.Data.ProfileApi.Schema
{
    public sealed class StatsContainer
    {
        [JsonPropertyName("eft")]
        public CountersContainer Counters { get; set; }
    }
}
