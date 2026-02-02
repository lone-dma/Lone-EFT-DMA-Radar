/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using LoneEftDmaRadar.Tarkov;

namespace LoneEftDmaRadar.UI.Loot
{
    /// <summary>
    /// JSON Wrapper for Important FilteredLoot.
    /// </summary>
    public sealed class LootFilterEntry
    {
        /// <summary>
        /// Item's BSG ID.
        /// </summary>
        [JsonPropertyName("itemID")]
        public string ItemID { get; set; } = string.Empty;

        /// <summary>
        /// True if this entry is Enabled/Active.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Entry Type (0 = Important FilteredLoot, 1 = Blacklisted FilteredLoot)
        /// </summary>
        [JsonPropertyName("type")]
        public LootFilterEntryType Type { get; set; } = LootFilterEntryType.ImportantLoot;

        [JsonIgnore]
        public bool Important => Type == LootFilterEntryType.ImportantLoot;

        [JsonIgnore]
        public bool Blacklisted => Type == LootFilterEntryType.BlacklistedLoot;

        /// <summary>
        /// Item Long Name per Tarkov Market.
        /// </summary>
        [JsonIgnore]
        public string Name =>
            TarkovDataManager.AllItems?
                .FirstOrDefault(x => x.Key.Equals(ItemID, StringComparison.OrdinalIgnoreCase))
                .Value?.Name
            ?? "NULL";

        /// <summary>
        /// Entry Comment (name of item, etc.)
        /// </summary>
        [JsonPropertyName("comment")]
        public string Comment { get; set; } = string.Empty;

        private string _color;
        /// <summary>
        /// Hex value of the rgba color. If not set, inherits from parent filter.
        /// </summary>
        [JsonPropertyName("color")]
        public string Color
        {
            get => _color ??= ParentFilter?.Color ?? SKColors.Turquoise.ToString();
            set => _color = value;
        }

        /// <summary>
        /// Reference to the parent filter (not serialized).
        /// </summary>
        [JsonIgnore]
        public UserLootFilter ParentFilter { get; set; }

        public sealed class EntryType
        {
            public int Id { get; init; }
            public string Name { get; init; }
            public override string ToString() => Name;
        }
    }
}

