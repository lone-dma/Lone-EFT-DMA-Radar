namespace eft_dma_radar.Tarkov.Data.ProfileApi.Schema
{
    public sealed class ProfileData
    {

        [JsonPropertyName("info")]
        public ProfileInfo Info { get; set; }

        [JsonPropertyName("pmcStats")]
        public StatsContainer PmcStats { get; set; }

    }
}
