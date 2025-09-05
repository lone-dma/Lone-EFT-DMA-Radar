namespace EftDmaRadarLite.Tarkov.Data.ProfileApi.Schema
{
    public sealed class OverallCounters
    {
        [JsonPropertyName("Items")]
        public List<OverallCountersItem> Items { get; set; }
    }
}
