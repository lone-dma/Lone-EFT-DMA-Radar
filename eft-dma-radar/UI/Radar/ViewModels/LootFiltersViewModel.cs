using eft_dma_radar.Tarkov.Data;
using eft_dma_radar.Tarkov.Data.TarkovMarket;
using eft_dma_radar.UI.Loot;
using eft_dma_radar.UI.Misc;
using eft_dma_radar.UI.Radar.Views;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows.Input;

namespace eft_dma_radar.UI.Radar.ViewModels
{
    public sealed class LootFiltersViewModel : INotifyPropertyChanged
    {
        #region Startup

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string n = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        }

        public LootFiltersViewModel(LootFiltersTab parent)
        {
            FilterNames = new ObservableCollection<string>(App.Config.LootFilters.Filters.Keys);
            AvailableItems = new ObservableCollection<TarkovMarketItem>(
                EftDataManager.AllItems.Values.OrderBy(x => x.Name));

            AddFilterCommand = new SimpleCommand(OnAddFilter);
            RenameFilterCommand = new SimpleCommand(OnRenameFilter);
            DeleteFilterCommand = new SimpleCommand(OnDeleteFilter);

            AddEntryCommand = new SimpleCommand(OnAddEntry);

            if (FilterNames.Any())
                SelectedFilterName = App.Config.LootFilters.Selected;
            EnsureFirstItemSelected();
            RefreshLootFilter();
            parent.IsVisibleChanged += Parent_IsVisibleChanged;
        }

        private void Parent_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool visible && !visible)
            {
                RefreshLootFilter();
            }
        }

        #endregion

        #region Top Section - Filters

        private bool _currentFilterEnabled;
        public bool CurrentFilterEnabled
        {
            get => _currentFilterEnabled;
            set
            {
                if (_currentFilterEnabled == value) return;
                _currentFilterEnabled = value;
                // persist to config
                App.Config.LootFilters.Filters[SelectedFilterName].Enabled = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> FilterNames { get; } // ComboBox of filter names
        private string _selectedFilterName;
        public string SelectedFilterName
        {
            get => _selectedFilterName;
            set
            {
                if (_selectedFilterName == value) return;
                _selectedFilterName = value;
                App.Config.LootFilters.Selected = value;
                var userFilter = App.Config.LootFilters.Filters[value];
                CurrentFilterEnabled = userFilter.Enabled;
                Entries = userFilter.Entries;
                OnPropertyChanged();
            }
        }

        public ICommand AddFilterCommand { get; }
        private void OnAddFilter()
        {
            var dlg = new InputBoxWindow("Loot Filter", "Enter the name of the new loot filter:");
            if (dlg.ShowDialog() != true)
                return; // user cancelled
            var name = dlg.InputText;
            if (string.IsNullOrEmpty(name)) return;

            try
            {
                if (!App.Config.LootFilters.Filters.TryAdd(name, new UserLootFilter
                {
                    Enabled = true,
                    Entries = new()
                }))
                    throw new InvalidOperationException("That filter already exists.");

                FilterNames.Add(name);
                SelectedFilterName = name;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"ERROR Adding Filter: {ex.Message}",
                    "Loot Filter",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public ICommand RenameFilterCommand { get; }
        private void OnRenameFilter()
        {
            var oldName = SelectedFilterName;
            if (string.IsNullOrEmpty(oldName)) return;

            var dlg = new InputBoxWindow($"Rename {oldName}", "Enter the new filter name:");
            if (dlg.ShowDialog() != true)
                return; // user cancelled
            var newName = dlg.InputText;
            if (string.IsNullOrEmpty(newName)) return;

            try
            {
                if (App.Config.LootFilters.Filters.TryGetValue(oldName, out var filter)
                    && App.Config.LootFilters.Filters.TryAdd(newName, filter)
                    && App.Config.LootFilters.Filters.TryRemove(oldName, out _))
                {
                    var idx = FilterNames.IndexOf(oldName);
                    FilterNames[idx] = newName;
                    SelectedFilterName = newName;
                }
                else
                {
                    throw new InvalidOperationException("Rename failed.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"ERROR Renaming Filter: {ex.Message}",
                    "Loot Filter",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public ICommand DeleteFilterCommand { get; }
        private void OnDeleteFilter()
        {
            var name = SelectedFilterName;
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show(
                    "No loot filter selected!",
                    "Loot Filter",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete '{name}'?",
                "Loot Filter",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                if (!App.Config.LootFilters.Filters.TryRemove(name, out _))
                    throw new InvalidOperationException("Remove failed.");

                // ensure at least one filter remains
                if (App.Config.LootFilters.Filters.IsEmpty)
                    App.Config.LootFilters.Filters.TryAdd("default", new UserLootFilter
                    {
                        Enabled = true,
                        Entries = new()
                    });

                FilterNames.Clear();
                foreach (var key in App.Config.LootFilters.Filters.Keys)
                    FilterNames.Add(key);

                SelectedFilterName = FilterNames[0];
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"ERROR Deleting Filter: {ex.Message}",
                    "Loot Filter",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region Bottom Section - Entries

        public ObservableCollection<TarkovMarketItem> AvailableItems { get; } // List of items
        private ICollectionView _filteredItems;
        public ICollectionView FilteredItems // Filtered list of items
        {
            get
            {
                if (_filteredItems == null)
                {
                    // create the view once
                    _filteredItems = CollectionViewSource.GetDefaultView(AvailableItems);
                    _filteredItems.Filter = FilterPredicate;
                }
                return _filteredItems;
            }
        }

        private TarkovMarketItem _selectedItemToAdd;
        public TarkovMarketItem SelectedItemToAdd
        {
            get => _selectedItemToAdd;
            set { if (_selectedItemToAdd != value) { _selectedItemToAdd = value; OnPropertyChanged(); } }
        }

        private void EnsureFirstItemSelected()
        {
            var first = FilteredItems.Cast<TarkovMarketItem>().FirstOrDefault();
            SelectedItemToAdd = first;
        }

        private string _itemSearchText;
        public string ItemSearchText
        {
            get => _itemSearchText;
            set
            {
                if (_itemSearchText == value) return;
                _itemSearchText = value;
                OnPropertyChanged();
                _filteredItems.Refresh(); // refresh the filter
                EnsureFirstItemSelected();
            }
        }

        public ICommand AddEntryCommand { get; }
        private void OnAddEntry()
        {
            if (SelectedItemToAdd == null) return;

            var entry = new LootFilterEntry
            {
                ItemID = SelectedItemToAdd.BsgId,
                Color = SKColors.Turquoise.ToString()
            };

            Entries.Add(entry);
            SelectedItemToAdd = null;
        }

        public IEnumerable<LootFilterEntryType> FilterEntryTypes { get; } = Enum // ComboBox of Entry Types within DataGrid
            .GetValues<LootFilterEntryType>()
            .Cast<LootFilterEntryType>();

        private ObservableCollection<LootFilterEntry> _entries = new();
        public ObservableCollection<LootFilterEntry> Entries // Entries grid
        {
            get => _entries;
            set
            {
                if (_entries != value)
                {
                    _entries = value;
                    OnPropertyChanged(nameof(Entries));
                }
            }
        }

        #endregion

        #region Misc

        private bool FilterPredicate(object obj)
        {
            if (string.IsNullOrWhiteSpace(_itemSearchText))
                return true;

            var itm = obj as TarkovMarketItem;
            return itm?.Name
                       .IndexOf(_itemSearchText,
                                StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Refreshes the Loot Filter.
        /// Should be called at startup and during validation.
        /// </summary>
        private static void RefreshLootFilter()
        {
            /// Remove old filters (if any)
            foreach (var item in EftDataManager.AllItems.Values)
                item.SetFilter(null);
            /// Set new filters
            var currentFilters = App.Config.LootFilters.Filters
                .Values
                .Where(x => x.Enabled)
                .SelectMany(x => x.Entries);
            if (!currentFilters.Any())
                return;
            foreach (var filter in currentFilters)
            {
                if (string.IsNullOrEmpty(filter.ItemID))
                    continue;
                if (EftDataManager.AllItems.TryGetValue(filter.ItemID, out var item))
                    item.SetFilter(filter);
            }
        }

        #endregion
    }
}
