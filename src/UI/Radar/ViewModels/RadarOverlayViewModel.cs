using eft_dma_radar.UI.Loot;

namespace eft_dma_radar.UI.Radar.ViewModels
{
    public sealed class RadarOverlayViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            LootFilter.SearchString = SearchText?.Trim();
            Memory.Loot?.RefreshFilter();
        }

        public RadarOverlayViewModel() { }

        // ─── Overlay visibility ────────────────────────────────────────────────
        private string _mapFreeButtonText = "Map Free";
        public string MapFreeButtonText
        {
            get => _mapFreeButtonText;
            set
            {
                if (_mapFreeButtonText == value) return;
                _mapFreeButtonText = value;
                OnPropertyChanged(nameof(MapFreeButtonText));
            }
        }
        private bool _isMapFreeEnabled;
        public bool IsMapFreeEnabled
        {
            get => _isMapFreeEnabled;
            set
            {
                if (_isMapFreeEnabled == value) return;
                _isMapFreeEnabled = value;
                if (_isMapFreeEnabled)
                {
                    MapFreeButtonText = "Map Follow";
                }
                else
                {
                    MapFreeButtonText = "Map Free";
                }
                OnPropertyChanged(nameof(IsMapFreeEnabled));
            }
        }

        private bool _isLootButtonVisible = App.Config.Loot.Enabled;
        public bool IsLootButtonVisible
        {
            get => _isLootButtonVisible;
            set
            {
                if (_isLootButtonVisible == value) return;
                _isLootButtonVisible = value;
                OnPropertyChanged(nameof(IsLootButtonVisible));
            }
        }

        private bool _isLootOverlayVisible;
        public bool IsLootOverlayVisible
        {
            get => _isLootOverlayVisible;
            set
            {
                if (_isLootOverlayVisible == value) return;
                _isLootOverlayVisible = value;
                OnPropertyChanged(nameof(IsLootOverlayVisible));
            }
        }

        // ─── Loot settings ─────────────────────────────────────────────────────
        public int RegularValue
        {
            get => App.Config.Loot.MinValue;
            set
            {
                if (App.Config.Loot.MinValue != value)
                {
                    App.Config.Loot.MinValue = value;
                    OnPropertyChanged(nameof(RegularValue));
                }
            }
        }

        public int ValuableValue
        {
            get => App.Config.Loot.MinValueValuable;
            set
            {
                if (App.Config.Loot.MinValueValuable != value)
                {
                    App.Config.Loot.MinValueValuable = value;
                    OnPropertyChanged(nameof(ValuableValue));
                }
            }
        }

        public bool PricePerSlot
        {
            get => App.Config.Loot.PricePerSlot;
            set
            {
                if (App.Config.Loot.PricePerSlot != value)
                {
                    App.Config.Loot.PricePerSlot = value;
                    OnPropertyChanged(nameof(PricePerSlot));
                }
            }
        }

        public bool IsFleaPrices
        {
            get => App.Config.Loot.PriceMode == LootPriceMode.FleaMarket;
            set
            {
                if (value && App.Config.Loot.PriceMode != LootPriceMode.FleaMarket)
                {
                    App.Config.Loot.PriceMode = LootPriceMode.FleaMarket;
                    OnPropertyChanged(nameof(IsTraderPrices));    // also refresh the other radio
                }
            }
        }

        public bool IsTraderPrices
        {
            get => App.Config.Loot.PriceMode == LootPriceMode.Trader;
            set
            {
                if (value && App.Config.Loot.PriceMode != LootPriceMode.Trader)
                {
                    App.Config.Loot.PriceMode = LootPriceMode.Trader;
                    OnPropertyChanged(nameof(IsFleaPrices));     // also refresh the other radio
                }
            }
        }

        public bool HideCorpses
        {
            get => App.Config.Loot.HideCorpses;
            set
            {
                if (App.Config.Loot.HideCorpses != value)
                {
                    App.Config.Loot.HideCorpses = value;
                    OnPropertyChanged(nameof(HideCorpses));
                }
            }
        }

        public bool ShowMeds
        {
            get => LootFilter.ShowMeds;
            set
            {
                if (LootFilter.ShowMeds != value)
                {
                    LootFilter.ShowMeds = value;
                    OnPropertyChanged(nameof(ShowMeds));
                }
            }
        }

        public bool ShowFood
        {
            get => LootFilter.ShowFood;
            set
            {
                if (LootFilter.ShowFood != value)
                {
                    LootFilter.ShowFood = value;
                    OnPropertyChanged(nameof(ShowFood));
                }
            }
        }

        public bool ShowBackpacks
        {
            get => LootFilter.ShowBackpacks;
            set
            {
                if (LootFilter.ShowBackpacks != value)
                {
                    LootFilter.ShowBackpacks = value;
                    OnPropertyChanged(nameof(ShowBackpacks));
                }
            }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                }
            }
        }
    }
}
