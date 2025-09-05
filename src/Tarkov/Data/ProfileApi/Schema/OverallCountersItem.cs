namespace EftDmaRadarLite.Tarkov.Data.ProfileApi.Schema
{
    public sealed class OverallCountersItem
    {
        [JsonPropertyName("Key")]
        public List<string> Key { get; set; } = new();

        [JsonPropertyName("Value")]
        public int Value { get; set; }
    }
}
