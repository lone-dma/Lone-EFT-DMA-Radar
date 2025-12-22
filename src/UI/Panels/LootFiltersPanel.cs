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
using LoneEftDmaRadar.Tarkov;
using LoneEftDmaRadar.UI.Loot;
using LoneEftDmaRadar.UI.Skia;
using LoneEftDmaRadar.Web.TarkovDev.Data;

namespace LoneEftDmaRadar.UI.Panels
{
    /// <summary>
    /// Loot Filters Panel for the ImGui-based Radar.
    /// </summary>
    internal static class LootFiltersPanel
    {
        private static readonly RadarUIState _state = RadarUIState.Instance;
        private static List<TarkovMarketItem> _allItems;
        private static List<TarkovMarketItem> _filteredItems;
        private static string _itemSearchText = string.Empty;
        private static int _selectedItemIndex = -1;
        private static int _selectedFilterIndex = 0;
        private static string _newFilterName = string.Empty;
        private static string _renameFilterName = string.Empty;
        private static bool _showRenamePopup = false;
        private static bool _showAddFilterPopup = false;
        private static Vector3 _filterColor = Vector3.One;
        private static string _filterColorHex = "#FFFFFF";
        private static readonly Dictionary<int, Vector3> _entryColors = new();
        private static readonly Dictionary<int, string> _entryColorHexes = new();

        /// <summary>
        /// Initialize the loot filters panel.
        /// </summary>
        public static void Initialize()
        {
            _allItems = TarkovDataManager.AllItems.Values.OrderBy(x => x.Name).ToList();
            _filteredItems = new List<TarkovMarketItem>(_allItems);
            RefreshFilterIndex();
            _state.RefreshCurrentFilterEntries();
            UpdateFilterColorFromCurrent();
        }

        private static void RefreshFilterIndex()
        {
            _state.RefreshFilterNames();
            _selectedFilterIndex = _state.FilterNames.IndexOf(_state.SelectedFilterName);
            if (_selectedFilterIndex < 0 && _state.FilterNames.Count > 0)
            {
                _selectedFilterIndex = 0;
                _state.SelectedFilterName = _state.FilterNames[0];
            }
        }

        /// <summary>
        /// Draw the loot filters panel as a tab.
        /// </summary>
        public static void Draw()
        {
            ImGui.SeparatorText("Filter Selection");

            // Filter dropdown
            if (_state.FilterNames.Count > 0)
            {
                string currentFilter = _state.SelectedFilterName ?? string.Empty;
                if (ImGui.BeginCombo("Active Filter", currentFilter))
                {
                    for (int i = 0; i < _state.FilterNames.Count; i++)
                    {
                        bool isSelected = (i == _selectedFilterIndex);
                        if (ImGui.Selectable(_state.FilterNames[i], isSelected))
                        {
                            _selectedFilterIndex = i;
                            _state.SelectedFilterName = _state.FilterNames[i];
                            _state.RefreshCurrentFilterEntries();
                            UpdateFilterColorFromCurrent();
                        }
                        if (isSelected)
                            ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndCombo();
                }
            }

            // Filter management buttons
            if (ImGui.Button("Add Filter"))
            {
                _newFilterName = string.Empty;
                _showAddFilterPopup = true;
            }
            ImGui.SameLine();
            if (ImGui.Button("Rename"))
            {
                _renameFilterName = _state.SelectedFilterName ?? string.Empty;
                _showRenamePopup = true;
            }
            ImGui.SameLine();
            if (ImGui.Button("Delete"))
            {
                DeleteCurrentFilter();
            }

            // Add Filter Popup
            if (_showAddFilterPopup)
            {
                ImGui.OpenPopup("Add Filter");
            }
            if (ImGui.BeginPopupModal("Add Filter", ref _showAddFilterPopup, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("Enter the name of the new loot filter:");
                ImGui.InputText("##NewFilterName", ref _newFilterName, 64);

                if (ImGui.Button("Create", new Vector2(120, 0)))
                {
                    AddFilter(_newFilterName);
                    _showAddFilterPopup = false;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel", new Vector2(120, 0)))
                {
                    _showAddFilterPopup = false;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }

            // Rename Popup
            if (_showRenamePopup)
            {
                ImGui.OpenPopup("Rename Filter");
            }
            if (ImGui.BeginPopupModal("Rename Filter", ref _showRenamePopup, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("Enter the new filter name:");
                ImGui.InputText("##RenameFilterName", ref _renameFilterName, 64);

                if (ImGui.Button("Rename", new Vector2(120, 0)))
                {
                    RenameCurrentFilter(_renameFilterName);
                    _showRenamePopup = false;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel", new Vector2(120, 0)))
                {
                    _showRenamePopup = false;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }

            ImGui.Separator();

            // Current filter settings
            var currentFilterObj = _state.GetCurrentFilter();
            if (currentFilterObj is not null)
            {
                bool filterEnabled = currentFilterObj.Enabled;
                if (ImGui.Checkbox("Filter Enabled", ref filterEnabled))
                {
                    currentFilterObj.Enabled = filterEnabled;
                    RefreshLootFilter();
                }

                // Filter color with color picker
                ImGui.Text("Filter Color:");
                ImGui.SameLine();

                // Color preview button
                var filterColorVec4 = new Vector4(_filterColor.X, _filterColor.Y, _filterColor.Z, 1f);
                if (ImGui.ColorButton("##FilterColorPreview", filterColorVec4, ImGuiColorEditFlags.None, new Vector2(20, 20)))
                {
                    ImGui.OpenPopup("FilterColorPicker");
                }

                // Color picker popup
                if (ImGui.BeginPopup("FilterColorPicker"))
                {
                    if (ImGui.ColorPicker3("##FilterColorPickerControl", ref _filterColor))
                    {
                        _filterColorHex = ColorToHex(_filterColor);
                        currentFilterObj.Color = _filterColorHex;
                    }
                    ImGui.EndPopup();
                }

                ImGui.SameLine();
                if (ImGui.Button("Apply to all"))
                {
                    string filterColor = currentFilterObj.Color;
                    foreach (var entry in _state.CurrentFilterEntries)
                    {
                        entry.Color = filterColor; // Set to filter color to indicate inheritance
                    }
                    _entryColors.Clear();
                    _entryColorHexes.Clear();
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Reset all entries to inherit filter color");
                }
            }

            ImGui.SeparatorText("Add Item to Filter");

            // Item search
            if (ImGui.InputText("Search Items", ref _itemSearchText, 128))
            {
                FilterItems();
            }

            // Item list
            if (ImGui.BeginListBox("##ItemList", new Vector2(-1, 150)))
            {
                for (int i = 0; i < _filteredItems.Count; i++)
                {
                    var item = _filteredItems[i];
                    bool isSelected = (i == _selectedItemIndex);
                    if (ImGui.Selectable($"{item.Name}##item{i}", isSelected))
                    {
                        _selectedItemIndex = i;
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndListBox();
            }

            if (ImGui.Button("Add Selected Item") && _selectedItemIndex >= 0 && _selectedItemIndex < _filteredItems.Count)
            {
                AddItemToFilter(_filteredItems[_selectedItemIndex]);
                RefreshLootFilter();
            }

            ImGui.SeparatorText("Filter Entries");

            // Entries table
            if (ImGui.BeginTable("FilterEntriesTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY, new Vector2(0, 200)))
            {
                ImGui.TableSetupColumn("Item", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn("Color", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn("Remove", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableHeadersRow();

                var entriesToRemove = new List<LootFilterEntry>();
                int entryIndex = 0;

                foreach (var entry in _state.CurrentFilterEntries)
                {
                    ImGui.TableNextRow();

                    // Item name
                    ImGui.TableNextColumn();
                    string itemName = GetItemName(entry.ItemID);
                    ImGui.Text(itemName);

                    // Type dropdown
                    ImGui.TableNextColumn();
                    int typeIndex = (int)entry.Type;
                    if (ImGui.Combo($"##type{entryIndex}", ref typeIndex, Enum.GetNames<LootFilterEntryType>(), Enum.GetValues<LootFilterEntryType>().Length))
                    {
                        entry.Type = (LootFilterEntryType)typeIndex;
                    }

                    // Color input with color picker
                    ImGui.TableNextColumn();

                    // Get or initialize entry color - use filter color as fallback
                    string filterColorHex = currentFilterObj?.Color ?? SKColors.Turquoise.ToString();
                    if (!_entryColors.TryGetValue(entryIndex, out var entryColor))
                    {
                        // Get the actual stored color value (accessing Color property may have side effects)
                        string colorHex = entry.Color;
                        TryParseHex(colorHex, out entryColor);
                        _entryColors[entryIndex] = entryColor;
                        _entryColorHexes[entryIndex] = colorHex;
                    }

                    // Check if entry color matches filter color (indicating inheritance)
                    string currentEntryColor = _entryColorHexes.TryGetValue(entryIndex, out var hex) ? hex : entry.Color;
                    bool inheritsColor = currentEntryColor == filterColorHex ||
                                         ColorsAreEqual(currentEntryColor, filterColorHex);

                    var entryColorVec4 = new Vector4(entryColor.X, entryColor.Y, entryColor.Z, 1f);
                    if (ImGui.ColorButton($"##EntryColorPreview{entryIndex}", entryColorVec4, ImGuiColorEditFlags.None, new Vector2(16, 16)))
                    {
                        ImGui.OpenPopup($"EntryColorPicker{entryIndex}");
                    }

                    if (ImGui.BeginPopup($"EntryColorPicker{entryIndex}"))
                    {
                        if (ImGui.ColorPicker3($"##EntryColorPickerControl{entryIndex}", ref entryColor))
                        {
                            _entryColors[entryIndex] = entryColor;
                            string newHex = ColorToHex(entryColor);
                            _entryColorHexes[entryIndex] = newHex;
                            entry.Color = newHex;
                        }
                        ImGui.Separator();
                        if (ImGui.Button("Inherit from filter"))
                        {
                            entry.Color = filterColorHex; // Set to filter color to indicate inheritance
                            TryParseHex(filterColorHex, out entryColor);
                            _entryColors[entryIndex] = entryColor;
                            _entryColorHexes[entryIndex] = filterColorHex;
                            ImGui.CloseCurrentPopup();
                        }
                        ImGui.EndPopup();
                    }

                    ImGui.SameLine();
                    if (inheritsColor)
                    {
                        ImGui.TextDisabled("(inherited)");
                    }

                    // Remove button
                    ImGui.TableNextColumn();
                    if (ImGui.Button($"X##remove{entryIndex}"))
                    {
                        entriesToRemove.Add(entry);
                    }

                    entryIndex++;
                }

                ImGui.EndTable();

                // Remove entries outside of enumeration
                foreach (var entry in entriesToRemove)
                {
                    _state.CurrentFilterEntries.Remove(entry);
                    currentFilterObj?.Entries.Remove(entry);
                }

                // Clean up removed entry colors and refresh filter
                if (entriesToRemove.Count > 0)
                {
                    _entryColors.Clear();
                    _entryColorHexes.Clear();
                    RefreshLootFilter();
                }
            }
        }

        #region Helper Methods

        private static void UpdateFilterColorFromCurrent()
        {
            var currentFilter = _state.GetCurrentFilter();
            if (currentFilter is not null)
            {
                // Use the filter's saved color (already has a default of Turquoise in UserLootFilter)
                string colorHex = currentFilter.Color;
                if (TryParseHex(colorHex, out var color))
                {
                    _filterColor = color;
                    _filterColorHex = colorHex;
                }
            }
            _entryColors.Clear();
            _entryColorHexes.Clear();
        }

        private static void FilterItems()
        {
            if (string.IsNullOrWhiteSpace(_itemSearchText))
            {
                _filteredItems = new List<TarkovMarketItem>(_allItems);
            }
            else
            {
                _filteredItems = _allItems
                    .Where(x => x.Name.Contains(_itemSearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            _selectedItemIndex = _filteredItems.Count > 0 ? 0 : -1;
        }

        private static string GetItemName(string bsgId)
        {
            if (string.IsNullOrEmpty(bsgId))
                return "(Unknown)";
            if (TarkovDataManager.AllItems.TryGetValue(bsgId, out var item))
                return item.Name;
            return bsgId;
        }

        private static void AddFilter(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            try
            {
                // Use ImportantLoot color as default for new filters
                string defaultColor = SKPaints.PaintImportantLoot.Color.ToString();

                if (!Program.Config.LootFilters.Filters.TryAdd(name, new UserLootFilter
                {
                    Enabled = true,
                    Color = defaultColor,
                    Entries = new()
                }))
                {
                    MessageBox.Show("That filter already exists.", "Loot Filter", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                RefreshFilterIndex();
                _state.SelectedFilterName = name;
                _selectedFilterIndex = _state.FilterNames.IndexOf(name);
                _state.RefreshCurrentFilterEntries();
                UpdateFilterColorFromCurrent();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding filter: {ex.Message}", "Loot Filter", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void RenameCurrentFilter(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                return;

            string oldName = _state.SelectedFilterName;
            if (string.IsNullOrEmpty(oldName))
                return;

            try
            {
                if (Program.Config.LootFilters.Filters.TryGetValue(oldName, out var filter)
                    && Program.Config.LootFilters.Filters.TryAdd(newName, filter)
                    && Program.Config.LootFilters.Filters.TryRemove(oldName, out _))
                {
                    RefreshFilterIndex();
                    _state.SelectedFilterName = newName;
                    _selectedFilterIndex = _state.FilterNames.IndexOf(newName);
                }
                else
                {
                    MessageBox.Show("Rename failed.", "Loot Filter", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error renaming filter: {ex.Message}", "Loot Filter", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void DeleteCurrentFilter()
        {
            string name = _state.SelectedFilterName;
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("No loot filter selected!", "Loot Filter", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to delete '{name}'?", "Loot Filter",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                if (!Program.Config.LootFilters.Filters.TryRemove(name, out _))
                {
                    MessageBox.Show("Remove failed.", "Loot Filter", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Ensure at least one filter remains
                if (Program.Config.LootFilters.Filters.IsEmpty)
                {
                    Program.Config.LootFilters.Filters.TryAdd("default", new UserLootFilter
                    {
                        Enabled = true,
                        Entries = new()
                    });
                }

                RefreshFilterIndex();
                _state.RefreshCurrentFilterEntries();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting filter: {ex.Message}", "Loot Filter", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void AddItemToFilter(TarkovMarketItem item)
        {
            var currentFilter = _state.GetCurrentFilter();
            if (currentFilter is null)
                return;

            var entry = new LootFilterEntry
            {
                ItemID = item.BsgId,
                Color = currentFilter.Color, // inherit filter color
                ParentFilter = currentFilter
            };

            currentFilter.Entries.Add(entry);
            _state.CurrentFilterEntries.Add(entry);
        }

        /// <summary>
        /// Refreshes the loot filter system.
        /// </summary>
        public static void RefreshLootFilter()
        {
            // Remove old filters
            foreach (var item in TarkovDataManager.AllItems.Values)
                item.SetFilter(null);

            // Set new filters
            var currentFilters = Program.Config.LootFilters.Filters
                .Values
                .Where(x => x.Enabled)
                .SelectMany(x => x.Entries);

            foreach (var filter in currentFilters)
            {
                if (string.IsNullOrEmpty(filter.ItemID))
                    continue;
                if (TarkovDataManager.AllItems.TryGetValue(filter.ItemID, out var item))
                    item.SetFilter(filter);
            }
        }

        private static string ColorToHex(Vector3 color)
        {
            int r = (int)(color.X * 255);
            int g = (int)(color.Y * 255);
            int b = (int)(color.Z * 255);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        private static bool ColorsAreEqual(string hex1, string hex2)
        {
            if (string.IsNullOrEmpty(hex1) || string.IsNullOrEmpty(hex2))
                return false;

            // Parse both colors and compare RGB values
            if (TryParseHex(hex1, out var color1) && TryParseHex(hex2, out var color2))
            {
                // Allow small tolerance for floating point comparison
                const float tolerance = 0.01f;
                return Math.Abs(color1.X - color2.X) < tolerance &&
                       Math.Abs(color1.Y - color2.Y) < tolerance &&
                       Math.Abs(color1.Z - color2.Z) < tolerance;
            }
            return false;
        }

        private static bool TryParseHex(string hex, out Vector3 color)
        {
            color = Vector3.One;
            if (string.IsNullOrEmpty(hex))
                return false;

            hex = hex.TrimStart('#');
            if (hex.Length < 6)
                return false;

            // Handle 8-character hex (AARRGGBB format from SKColor.ToString())
            if (hex.Length == 8)
                hex = hex.Substring(2); // Skip alpha

            try
            {
                int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                color = new Vector3(r / 255f, g / 255f, b / 255f);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
