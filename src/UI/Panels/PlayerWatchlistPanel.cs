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

using ImGuiNET;
using LoneEftDmaRadar.UI.Misc;
using System.Collections.ObjectModel;

namespace LoneEftDmaRadar.UI.Panels
{
    /// <summary>
    /// Player Watchlist Panel for the ImGui-based Radar.
    /// </summary>
    internal static class PlayerWatchlistPanel
    {
        // Panel-local state
        private static string _newAcctId = string.Empty;
        private static string _newReason = string.Empty;
        private static string _searchText = string.Empty;
        private static bool _showAddPopup = false;
        private static int _selectedIndex = -1;

        // Watchlist lookup for fast access (moved from RadarUIState)
        private static readonly ConcurrentDictionary<string, PlayerWatchlistEntry> _watchlistLookup = new(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized;

        /// <summary>
        /// Thread-safe watchlist lookup.
        /// </summary>
        public static IReadOnlyDictionary<string, PlayerWatchlistEntry> WatchlistLookup => _watchlistLookup;

        /// <summary>
        /// Observable watchlist entries (from config).
        /// </summary>
        private static ObservableCollection<PlayerWatchlistEntry> WatchlistEntries => Program.Config.PlayerWatchlist;

        /// <summary>
        /// Initialize the watchlist panel (call once at startup after config is loaded).
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            // Build initial lookup from config
            foreach (var entry in WatchlistEntries)
            {
                _watchlistLookup.TryAdd(entry.AcctID, entry);
            }

            // Keep lookup in sync with collection changes
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
        public static void AddToWatchlist(PlayerWatchlistEntry entry)
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

        /// <summary>
        /// Draw the player watchlist panel.
        /// </summary>
        public static void Draw()
        {
            ImGui.SeparatorText("Player Watchlist");

            // Add new entry button
            if (ImGui.Button("Add Player"))
            {
                _newAcctId = string.Empty;
                _newReason = string.Empty;
                _showAddPopup = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Manually add a player to the watchlist");
            ImGui.SameLine();
            if (ImGui.Button("Remove Selected") && _selectedIndex >= 0 && _selectedIndex < WatchlistEntries.Count)
            {
                WatchlistEntries.RemoveAt(_selectedIndex);
                _selectedIndex = -1;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Remove the selected player from the watchlist");

            // Add Player Popup
            if (_showAddPopup)
            {
                ImGui.OpenPopup("Add Player to Watchlist");
            }
            if (ImGui.BeginPopupModal("Add Player to Watchlist", ref _showAddPopup, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("Account ID:");
                ImGui.InputText("##AcctId", ref _newAcctId, 32);

                ImGui.Text("Reason/Note:");
                ImGui.InputText("##Reason", ref _newReason, 128);

                if (ImGui.Button("Add", new Vector2(120, 0)))
                {
                    if (!string.IsNullOrWhiteSpace(_newAcctId))
                    {
                        var entry = new PlayerWatchlistEntry
                        {
                            AcctID = _newAcctId.Trim(),
                            Reason = _newReason.Trim(),
                            Timestamp = DateTime.Now
                        };
                        AddToWatchlist(entry);
                    }
                    _showAddPopup = false;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel", new Vector2(120, 0)))
                {
                    _showAddPopup = false;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }

            ImGui.Separator();

            // Search
            ImGui.SetNextItemWidth(200);
            ImGui.InputText("Search", ref _searchText, 64);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Filter watchlist by account ID or reason");

            // Watchlist table
            if (ImGui.BeginTable("WatchlistTable", 4,
                ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg,
                new Vector2(0, 400)))
            {
                ImGui.TableSetupColumn("Account ID", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn("Reason", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Added", ImGuiTableColumnFlags.WidthFixed, 120);
                ImGui.TableSetupColumn("##Remove", ImGuiTableColumnFlags.WidthFixed, 30);
                ImGui.TableHeadersRow();

                var entriesToRemove = new List<PlayerWatchlistEntry>();
                int displayIndex = 0;

                foreach (var entry in WatchlistEntries)
                {
                    // Filter by search text
                    if (!string.IsNullOrWhiteSpace(_searchText))
                    {
                        bool matches = entry.AcctID.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                                      (entry.Reason?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false);
                        if (!matches)
                            continue;
                    }

                    ImGui.TableNextRow();

                    // Account ID (selectable, but not spanning all columns)
                    ImGui.TableNextColumn();
                    bool isSelected = (_selectedIndex == displayIndex);
                    if (ImGui.Selectable($"{entry.AcctID}##sel{displayIndex}", isSelected))
                    {
                        _selectedIndex = displayIndex;
                    }

                    // Reason (editable)
                    ImGui.TableNextColumn();
                    string reason = entry.Reason ?? string.Empty;
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputText($"##reason{displayIndex}", ref reason, 256))
                    {
                        entry.Reason = reason;
                    }

                    // Timestamp
                    ImGui.TableNextColumn();
                    ImGui.Text(entry.Timestamp.ToString("yyyy-MM-dd HH:mm"));

                    // Remove button
                    ImGui.TableNextColumn();
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.6f, 0.2f, 0.2f, 1f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.8f, 0.3f, 0.3f, 1f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.9f, 0.1f, 0.1f, 1f));
                    if (ImGui.Button($"X##remove{displayIndex}", new Vector2(-1, 0)))
                    {
                        entriesToRemove.Add(entry);
                    }
                    ImGui.PopStyleColor(3);

                    displayIndex++;
                }

                ImGui.EndTable();

                // Remove entries outside enumeration
                foreach (var entry in entriesToRemove)
                {
                    WatchlistEntries.Remove(entry);
                }
            }

            // Stats
            ImGui.Text($"Total: {WatchlistEntries.Count} players");
        }
    }
}
