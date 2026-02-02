/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
namespace LoneEftDmaRadar.UI.Loot
{
    public sealed class UserLootFilter
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;

        [JsonPropertyName("color")] public string Color { get; set; } = SKColors.Turquoise.ToString();

        [JsonPropertyName("entries")]
        public List<LootFilterEntry> Entries { get; set; } = [];
    }
}
