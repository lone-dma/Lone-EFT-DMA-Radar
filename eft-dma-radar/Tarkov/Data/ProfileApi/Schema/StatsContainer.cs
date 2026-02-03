namespace eft_dma_radar.Tarkov.Data.ProfileApi.Schema
{
    public sealed class StatsContainer
    {
        [JsonPropertyName("eft")]
        public CountersContainer Counters { get; set; }
    }
}
