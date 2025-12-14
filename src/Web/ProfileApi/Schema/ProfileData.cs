namespace LoneEftDmaRadar.Web.ProfileApi.Schema
{
    public sealed class ProfileData
    {

        [JsonPropertyName("info")]
        public ProfileInfo Info { get; set; }

        [JsonPropertyName("pmcStats")]
        public StatsContainer PmcStats { get; set; }

        [JsonPropertyName("achievements")]
        public Dictionary<string, long> Achievements { get; set; }
    }
}
