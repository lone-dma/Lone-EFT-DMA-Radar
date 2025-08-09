namespace eft_dma_radar.Tarkov.Data.ProfileApi.Schema
{
    public sealed class CountersContainer
    {
        [JsonPropertyName("totalInGameTime")]
        public int TotalInGameTime { get; set; }

        [JsonPropertyName("overAllCounters")]
        public OverallCounters OverallCounters { get; set; }
    }
}
