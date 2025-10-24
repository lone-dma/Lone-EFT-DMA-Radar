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
    /// JSON Wrapper for Important Loot, now with INotifyPropertyChanged.
    /// </summary>
    public sealed class LootFilterEntry : INotifyPropertyChanged
    {
        private string _itemID = string.Empty;
        /// <summary>
        /// Item's BSG ID.
        /// </summary>
        [JsonPropertyName("itemID")]
        public string ItemID
        {
            get => _itemID;
            set
            {
                if (_itemID == value) return;
                _itemID = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Name));   // update Name too
            }
        }

        private bool _enabled = true;
        /// <summary>
        /// True if this entry is Enabled/Active.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled
        {
            get => _enabled;
            set { if (_enabled != value) { _enabled = value; OnPropertyChanged(); } }
        }

        private LootFilterEntryType _type = LootFilterEntryType.ImportantLoot;
        /// <summary>
        /// Entry Type (0 = Important Loot, 1 = Blacklisted Loot)
        /// </summary>
        [JsonPropertyName("type")]
        public LootFilterEntryType Type
        {
            get => _type;
            set { if (_type != value) { _type = value; OnPropertyChanged(); } }
        }

        [JsonIgnore]
        public bool Important => Type == LootFilterEntryType.ImportantLoot;
        [JsonIgnore]
        public bool Blacklisted => Type == LootFilterEntryType.BlacklistedLoot;

        /// <summary>
        /// Item Long Name per Tarkov Market.
        /// </summary>
        [JsonIgnore]
        public string Name
        {
            get
            {
                // lazy‑load via your EftDataManager
                return TarkovDataManager.AllItems?
                           .FirstOrDefault(x => x.Key.Equals(ItemID, StringComparison.OrdinalIgnoreCase))
                           .Value?.Name
                       ?? "NULL";
            }
        }

        private string _comment = string.Empty;
        /// <summary>
        /// Entry Comment (name of item,etc.)
        /// </summary>
        [JsonPropertyName("comment")]
        public string Comment
        {
            get => _comment;
            set { if (_comment != value) { _comment = value; OnPropertyChanged(); } }
        }

        private string _color = SKColors.Turquoise.ToString();
        /// <summary>
        /// Hex value of the rgba color.
        /// </summary>
        [JsonPropertyName("color")]
        public string Color
        {
            get => _color;
            set { if (_color != value) { _color = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        public sealed class EntryType
        {
            public int Id { get; init; }
            public string Name { get; init; }
            public override string ToString() => Name;
        }
    }
}
