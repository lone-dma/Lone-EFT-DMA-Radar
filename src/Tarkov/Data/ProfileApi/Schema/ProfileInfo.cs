namespace eft_dma_radar.Tarkov.Data.ProfileApi.Schema
{
    public sealed class ProfileInfo
    {
        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }

        [JsonPropertyName("experience")]
        public int Experience { get; set; }

        [JsonPropertyName("memberCategory")]
        public int MemberCategory { get; set; }

        [JsonPropertyName("registrationDate")]
        public int RegistrationDate { get; set; }
    }
}
