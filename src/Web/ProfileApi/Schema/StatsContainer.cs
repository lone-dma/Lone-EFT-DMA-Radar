namespace LoneEftDmaRadar.Web.ProfileApi.Schema
{
    public sealed class StatsContainer
    {
        [JsonPropertyName("eft")]
        public CountersContainer Counters { get; set; }
    }
}
