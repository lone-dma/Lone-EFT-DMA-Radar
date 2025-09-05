namespace EftDmaRadarLite.Tarkov.Data.ProfileApi.Schema
{
    /// <summary>
    /// Profile response from eft-api.tech
    /// </summary>
    public sealed class EftApiTechResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        [JsonPropertyName("data")]
        public ProfileData Data { get; set; }

    }
}
