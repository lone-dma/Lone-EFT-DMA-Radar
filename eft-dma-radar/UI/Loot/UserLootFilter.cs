using System.Collections.ObjectModel;

namespace eft_dma_radar.UI.Loot
{
    public sealed class UserLootFilter
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;

        [JsonInclude]
        [JsonPropertyName("entries")]
        public ObservableCollection<LootFilterEntry> Entries { get; init; } = new();
    }
}