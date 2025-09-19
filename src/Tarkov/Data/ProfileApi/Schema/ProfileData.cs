namespace EftDmaRadarLite.Tarkov.Data.ProfileApi.Schema
{
    public sealed class ProfileData
    {

        [JsonPropertyName("info")]
        public ProfileInfo Info { get; set; }

        [JsonPropertyName("pmcStats")]
        public StatsContainer PmcStats { get; set; }
        /// <summary>
        /// Only for Tarkov.Dev, otherwise it's set upon return from the Provider.
        /// </summary>
        [JsonPropertyName("updated")]
        public long Epoch { get; set; }
    }
}
