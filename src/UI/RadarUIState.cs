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

using LoneEftDmaRadar.Tarkov.GameWorld.Player;
using LoneEftDmaRadar.UI.Loot;
using LoneEftDmaRadar.UI.Misc;
using System.Collections.ObjectModel;

namespace LoneEftDmaRadar.UI
{
    /// <summary>
    /// Central UI State for the ImGui-based Radar.
    /// Replaces WPF ViewModels with a simpler state container.
    /// </summary>
    internal sealed class RadarUIState
    {
        #region Singleton

        private static readonly Lazy<RadarUIState> _instance = new(() => new RadarUIState());
        public static RadarUIState Instance => _instance.Value;

        private RadarUIState()
        {
            InitializeWatchlist();
        }

        #endregion

        #region Main Window State

        /// <summary>
        /// Current FPS counter.
        /// </summary>
        public int Fps { get; set; }

        /// <summary>
        /// Current window title.
        /// </summary>
        public string WindowTitle => $"{Program.Name} ({Fps} fps)";

        /// <summary>
        /// Currently active tab index.
        /// </summary>
        public int ActiveTabIndex { get; set; }

        /// <summary>
        /// Whether the settings panel is open.
        /// </summary>
        public bool IsSettingsPanelOpen { get; set; }

        /// <summary>
        /// Whether the loot overlay is visible.
        /// </summary>
        public bool IsLootOverlayVisible { get; set; }

        /// <summary>
        /// Whether the loot filters window is open.
        /// </summary>
        public bool IsLootFiltersOpen { get; set; }

        /// <summary>
        /// Whether the watchlist window is open.
        /// </summary>
        public bool IsWatchlistOpen { get; set; }

        /// <summary>
        /// Whether the player history window is open.
        /// </summary>
        public bool IsHistoryOpen { get; set; }

        /// <summary>
        /// Whether the web radar window is open.
        /// </summary>
        public bool IsWebRadarOpen { get; set; }

        /// <summary>
        /// Whether map free mode is enabled.
        /// </summary>
        public bool IsMapFreeEnabled { get; set; }

        /// <summary>
        /// Map pan position when in free mode.
        /// </summary>
        public Vector2 MapPanPosition { get; set; }

        #endregion

        #region Radar Overlay State

        /// <summary>
        /// Loot search text filter.
        /// </summary>
        public string LootSearchText { get; set; } = string.Empty;

        /// <summary>
        /// Show meds toggle.
        /// </summary>
        public bool ShowMeds
        {
            get => LootFilter.ShowMeds;
            set => LootFilter.ShowMeds = value;
        }

        /// <summary>
        /// Show food toggle.
        /// </summary>
        public bool ShowFood
        {
            get => LootFilter.ShowFood;
            set => LootFilter.ShowFood = value;
        }

        /// <summary>
        /// Show backpacks toggle.
        /// </summary>
        public bool ShowBackpacks
        {
            get => LootFilter.ShowBackpacks;
            set => LootFilter.ShowBackpacks = value;
        }

        /// <summary>
        /// Show quest items toggle.
        /// </summary>
        public bool ShowQuestItems
        {
            get => LootFilter.ShowQuestItems;
            set => LootFilter.ShowQuestItems = value;
        }

        /// <summary>
        /// Apply search text to loot filter.
        /// </summary>
        public void ApplyLootSearch()
        {
            LootFilter.SearchString = LootSearchText?.Trim();
            Memory.Loot?.RefreshFilter();
        }

        #endregion

        #region Loot Filters State

        /// <summary>
        /// Available filter names.
        /// </summary>
        public List<string> FilterNames { get; private set; } = new(Program.Config.LootFilters.Filters.Keys);

        /// <summary>
        /// Currently selected filter name.
        /// </summary>
        public string SelectedFilterName
        {
            get => Program.Config.LootFilters.Selected;
            set
            {
                if (Program.Config.LootFilters.Selected != value)
                {
                    Program.Config.LootFilters.Selected = value;
                    RefreshCurrentFilterEntries();
                }
            }
        }

        /// <summary>
        /// Current filter entries for display.
        /// </summary>
        public ObservableCollection<LootFilterEntry> CurrentFilterEntries { get; private set; } = new();

        /// <summary>
        /// Item search text for adding new entries.
        /// </summary>
        public string ItemSearchText { get; set; } = string.Empty;

        /// <summary>
        /// Refreshes the current filter entries.
        /// </summary>
        public void RefreshCurrentFilterEntries()
        {
            CurrentFilterEntries.Clear();
            if (Program.Config.LootFilters.Filters.TryGetValue(SelectedFilterName, out var filter))
            {
                foreach (var entry in filter.Entries)
                {
                    entry.ParentFilter = filter;
                    CurrentFilterEntries.Add(entry);
                }
            }
        }

        /// <summary>
        /// Refreshes filter names from config.
        /// </summary>
        public void RefreshFilterNames()
        {
            FilterNames.Clear();
            FilterNames.AddRange(Program.Config.LootFilters.Filters.Keys);
        }

        /// <summary>
        /// Gets the current filter.
        /// </summary>
        public UserLootFilter GetCurrentFilter()
        {
            Program.Config.LootFilters.Filters.TryGetValue(SelectedFilterName, out var filter);
            return filter;
        }

        #endregion

        #region Player Watchlist State

        private readonly ConcurrentDictionary<string, PlayerWatchlistEntry> _watchlistLookup = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Thread-safe watchlist lookup.
        /// </summary>
        public IReadOnlyDictionary<string, PlayerWatchlistEntry> WatchlistLookup => _watchlistLookup;

        /// <summary>
        /// Observable watchlist entries (for UI binding).
        /// </summary>
        public ObservableCollection<PlayerWatchlistEntry> WatchlistEntries => Program.Config.PlayerWatchlist;

        /// <summary>
        /// Currently selected watchlist entry.
        /// </summary>
        public PlayerWatchlistEntry SelectedWatchlistEntry { get; set; }

        private void InitializeWatchlist()
        {
            foreach (var entry in Program.Config.PlayerWatchlist)
            {
                _watchlistLookup.TryAdd(entry.AcctID, entry);
            }
            WatchlistEntries.CollectionChanged += (s, e) =>
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems is not null)
                {
                    foreach (PlayerWatchlistEntry entry in e.NewItems)
                        _watchlistLookup.TryAdd(entry.AcctID, entry);
                }
                else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove && e.OldItems is not null)
                {
                    foreach (PlayerWatchlistEntry entry in e.OldItems)
                        _watchlistLookup.TryRemove(entry.AcctID, out _);
                }
            };
        }

        /// <summary>
        /// Add an entry to the watchlist.
        /// </summary>
        public void AddToWatchlist(PlayerWatchlistEntry entry)
        {
            var existing = WatchlistEntries.FirstOrDefault(x =>
                string.Equals(x.AcctID, entry.AcctID, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                existing.Reason = $"{entry.Reason} | {existing.Reason}";
            }
            else
            {
                WatchlistEntries.Add(entry);
            }
        }

        #endregion

        #region Player History State

        /// <summary>
        /// Player history entries.
        /// </summary>
        public List<ObservedPlayer> PlayerHistoryEntries { get; } = new();

        /// <summary>
        /// Currently selected history entry.
        /// </summary>
        public ObservedPlayer SelectedHistoryEntry { get; set; }

        /// <summary>
        /// Add player to history.
        /// </summary>
        public void AddToPlayerHistory(ObservedPlayer player)
        {
            PlayerHistoryEntries.Insert(0, player);
        }

        #endregion

        #region Web Radar State

        /// <summary>
        /// Web radar server is running.
        /// </summary>
        public bool IsWebRadarRunning { get; set; }

        /// <summary>
        /// Web radar start button text.
        /// </summary>
        public string WebRadarStartButtonText { get; set; } = "Start";

        /// <summary>
        /// Web radar server URL.
        /// </summary>
        public string WebRadarServerUrl { get; set; } = string.Empty;

        /// <summary>
        /// Web radar UI enabled state.
        /// </summary>
        public bool IsWebRadarUiEnabled { get; set; } = true;

        #endregion

        #region Settings State

        /// <summary>
        /// Map setup helper window is open.
        /// </summary>
        public bool IsMapSetupHelperOpen { get; set; }

        /// <summary>
        /// Map setup helper visibility (legacy overlay).
        /// </summary>
        public bool ShowMapSetupHelper { get; set; }

        /// <summary>
        /// Map setup helper coordinates display.
        /// </summary>
        public string MapSetupCoords { get; set; } = string.Empty;

        /// <summary>
        /// Container tracking lookup by ID.
        /// </summary>
        public bool ContainerIsTracked(string id)
        {
            return Program.Config.Containers.Selected.ContainsKey(id);
        }

        /// <summary>
        /// Toggle container tracking.
        /// </summary>
        public void SetContainerTracked(string id, bool tracked)
        {
            if (tracked)
                Program.Config.Containers.Selected.TryAdd(id, 0);
            else
                Program.Config.Containers.Selected.TryRemove(id, out _);
        }

        #endregion

        #region Color Picker State

        /// <summary>
        /// Color picker dialog is open.
        /// </summary>
        public bool IsColorPickerOpen { get; set; }

        /// <summary>
        /// Currently editing color option.
        /// </summary>
        public ColorPicker.ColorPickerOption? EditingColorOption { get; set; }

        /// <summary>
        /// Current color being edited (RGB values 0-1).
        /// </summary>
        public Vector3 EditingColor { get; set; }

        #endregion

        #region Hotkey Manager State

        /// <summary>
        /// Hotkey manager dialog is open.
        /// </summary>
        public bool IsHotkeyManagerOpen { get; set; }

        #endregion
    }
}
