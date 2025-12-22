/*
 * Lone EFT DMA Radar
 * Brought to you by Lone (Lone DMA)
 * 
MIT License

Copyright (c) 2025 Lone DMA

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 *
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
