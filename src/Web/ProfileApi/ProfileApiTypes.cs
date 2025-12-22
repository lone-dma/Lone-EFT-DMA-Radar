namespace LoneEftDmaRadar.Web.ProfileApi
{
    public static class ProfileApiTypes
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

        public sealed class StatsContainer
        {
            [JsonPropertyName("eft")]
            public CountersContainer Counters { get; set; }
        }

        public sealed class CountersContainer
        {
            [JsonPropertyName("totalInGameTime")]
            public int TotalInGameTime { get; set; }

            [JsonPropertyName("overAllCounters")]
            public OverallCounters OverallCounters { get; set; }
        }

        public sealed class OverallCounters
        {
            [JsonPropertyName("Items")]
            public List<OverallCountersItem> Items { get; set; }
        }
        public sealed class OverallCountersItem
        {
            [JsonPropertyName("Key")]
            public List<string> Key { get; set; } = new();

            [JsonPropertyName("Value")]
            public int Value { get; set; }
        }
    }
}
