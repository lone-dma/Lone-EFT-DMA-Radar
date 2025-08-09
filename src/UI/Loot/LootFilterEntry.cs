using eft_dma_radar.Tarkov.Data;

namespace eft_dma_radar.UI.Loot
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
                return EftDataManager.AllItems?
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
