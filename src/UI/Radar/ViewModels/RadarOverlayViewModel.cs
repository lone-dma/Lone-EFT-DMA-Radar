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

using LoneEftDmaRadar.UI.Loot;

namespace LoneEftDmaRadar.UI.Radar.ViewModels
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

        // ─── FilteredLoot settings ─────────────────────────────────────────────────────
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

        public bool ShowQuestItems
        {
            get => LootFilter.ShowQuestItems;
            set
            {
                if (LootFilter.ShowQuestItems != value)
                {
                    LootFilter.ShowQuestItems = value;
                    OnPropertyChanged(nameof(ShowQuestItems));
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
