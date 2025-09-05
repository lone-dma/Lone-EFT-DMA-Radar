using System.Collections.ObjectModel;

namespace EftDmaRadarLite.UI.Loot
{
    public sealed class UserLootFilter
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;

        [JsonInclude]
        [JsonPropertyName("entries")]
        public ObservableCollection<LootFilterEntry> Entries { get; init; } = new();
    }
}