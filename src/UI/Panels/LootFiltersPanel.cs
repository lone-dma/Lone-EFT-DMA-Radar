/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using ImGuiNET;
using LoneEftDmaRadar.Tarkov;
using LoneEftDmaRadar.UI.Loot;
using LoneEftDmaRadar.UI.Skia;
using LoneEftDmaRadar.Web.TarkovDev;

namespace LoneEftDmaRadar.UI.Panels
{
    /// <summary>
    /// Loot Filters Panel for the ImGui-based Radar.
    /// </summary>
    internal static class LootFiltersPanel
    {
        // Panel-local state
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
        // Filter state
        private static readonly List<string> _filterNames = new();
        private static readonly List<LootFilterEntry> _currentFilterEntries = new();

        private static EftDmaConfig Config { get; } = Program.Config;

        /// <summary>
        /// Currently selected filter name.
        /// </summary>
        private static string SelectedFilterName
        {
            get => Config.LootFilters.Selected;
            set
            {
                if (Config.LootFilters.Selected != value)
                {
                    Config.LootFilters.Selected = value;
                    RefreshCurrentFilterEntries();
                }
            }
        }

        static LootFiltersPanel()
        {
            TarkovDataManager.DataUpdated += TarkovDataManager_DataUpdated;
        }


        private static void TarkovDataManager_DataUpdated(object sender, EventArgs e)
        {
            _ = RadarWindow.Dispatcher.InvokeAsync(() =>
            {
                Initialize(); // Re-initialize on data update to ensure filters properly set,etc.
            });
        }

        /// <summary>
        /// Initialize the loot filters panel.
        /// </summary>
        public static void Initialize()
        {
            _allItems = TarkovDataManager.AllItems.Values.OrderBy(x => x.Name).ToList();
            _filteredItems = new List<TarkovMarketItem>(_allItems);
            RefreshFilterIndex();
            RefreshCurrentFilterEntries();
            UpdateFilterColorFromCurrent();
            RefreshLootFilter();
        }

        private static void RefreshFilterNames()
        {
            _filterNames.Clear();
            _filterNames.AddRange(Config.LootFilters.Filters.Keys);
        }

        private static void RefreshFilterIndex()
        {
            RefreshFilterNames();
            _selectedFilterIndex = _filterNames.IndexOf(SelectedFilterName);
            if (_selectedFilterIndex < 0 && _filterNames.Count > 0)
            {
                _selectedFilterIndex = 0;
                SelectedFilterName = _filterNames[0];
            }
        }

        private static void RefreshCurrentFilterEntries()
        {
            _currentFilterEntries.Clear();
            if (Config.LootFilters.Filters.TryGetValue(SelectedFilterName, out var filter))
            {
                foreach (var entry in filter.Entries)
                {
                    entry.ParentFilter = filter;
                    _currentFilterEntries.Add(entry);
                }
            }
        }

        private static UserLootFilter GetCurrentFilter()
        {
            Config.LootFilters.Filters.TryGetValue(SelectedFilterName, out var filter);
            return filter;
        }

        /// <summary>
        /// Draw the loot filters panel as a tab.
        /// </summary>
        public static void Draw()
        {
            ImGui.SeparatorText("过滤器选择");

            // Filter dropdown
            if (_filterNames.Count > 0)
            {
                string currentFilter = SelectedFilterName ?? string.Empty;
                if (ImGui.BeginCombo("当前过滤器", currentFilter))
                {
                    for (int i = 0; i < _filterNames.Count; i++)
                    {
                        bool isSelected = (i == _selectedFilterIndex);
                        if (ImGui.Selectable(_filterNames[i], isSelected))
                        {
                            _selectedFilterIndex = i;
                            SelectedFilterName = _filterNames[i];
                            RefreshCurrentFilterEntries();
                            UpdateFilterColorFromCurrent();
                        }
                        if (isSelected)
                            ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndCombo();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("选择要编辑的物品过滤器");
            }

            // Filter management buttons
            if (ImGui.Button("添加过滤器"))
            {
                _newFilterName = string.Empty;
                _showAddFilterPopup = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("创建新的物品过滤器");
            ImGui.SameLine();
            if (ImGui.Button("重命名"))
            {
                _renameFilterName = SelectedFilterName ?? string.Empty;
                _showRenamePopup = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("重命名当前过滤器");
            ImGui.SameLine();
            if (ImGui.Button("删除"))
            {
                DeleteCurrentFilter();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("删除当前过滤器");

            // Add Filter Popup
            if (_showAddFilterPopup)
            {
                ImGui.OpenPopup("添加过滤器");
            }
            if (ImGui.BeginPopupModal("添加过滤器", ref _showAddFilterPopup, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("输入新过滤器的名称:");
                ImGui.InputText("##NewFilterName", ref _newFilterName, 64);

                if (ImGui.Button("创建", new Vector2(120, 0)))
                {
                    AddFilter(_newFilterName);
                    _showAddFilterPopup = false;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("取消", new Vector2(120, 0)))
                {
                    _showAddFilterPopup = false;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }

            // Rename Popup
            if (_showRenamePopup)
            {
                ImGui.OpenPopup("重命名过滤器");
            }
            if (ImGui.BeginPopupModal("重命名过滤器", ref _showRenamePopup, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("输入新的过滤器名称:");
                ImGui.InputText("##RenameFilterName", ref _renameFilterName, 64);

                if (ImGui.Button("重命名", new Vector2(120, 0)))
                {
                    RenameCurrentFilter(_renameFilterName);
                    _showRenamePopup = false;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("取消", new Vector2(120, 0)))
                {
                    _showRenamePopup = false;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }

            ImGui.Separator();

            // Current filter settings
            var currentFilterObj = GetCurrentFilter();
            if (currentFilterObj is not null)
            {
                bool filterEnabled = currentFilterObj.Enabled;
                if (ImGui.Checkbox("启用过滤器", ref filterEnabled))
                {
                    currentFilterObj.Enabled = filterEnabled;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("启用或禁用此过滤器");

                // Filter color with color picker
                ImGui.Text("过滤器颜色:");
                ImGui.SameLine();

                // Color preview button
                var filterColorVec4 = new Vector4(_filterColor.X, _filterColor.Y, _filterColor.Z, 1f);
                if (ImGui.ColorButton("##FilterColorPreview", filterColorVec4, ImGuiColorEditFlags.None, new Vector2(20, 20)))
                {
                    ImGui.OpenPopup("FilterColorPicker");
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("点击更改过滤器颜色");

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
                if (ImGui.Button("应用到全部"))
                {
                    string filterColor = currentFilterObj.Color;
                    foreach (var entry in _currentFilterEntries)
                    {
                        entry.Color = filterColor;
                    }
                    _entryColors.Clear();
                    _entryColorHexes.Clear();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("将所有条目重置为继承过滤器颜色");
            }

            ImGui.SeparatorText("添加物品到过滤器");

            // Item search
            if (ImGui.InputText("搜索物品", ref _itemSearchText, 128))
            {
                FilterItems();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("搜索要添加到过滤器的物品");

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

            if (ImGui.Button("添加选中物品") && _selectedItemIndex >= 0 && _selectedItemIndex < _filteredItems.Count)
            {
                AddItemToFilter(_filteredItems[_selectedItemIndex]);
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("将选中的物品添加到此过滤器");

            ImGui.SeparatorText("过滤器条目");

            // Entries table
            var tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Sortable;
            if (ImGui.BeginTable("FilterEntriesTable", 5, tableFlags, new Vector2(0, 200)))
            {
                ImGui.TableSetupColumn("启用", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("物品", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("类型", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn("颜色", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort, 100);
                ImGui.TableSetupColumn("移除", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort, 60);
                ImGui.TableHeadersRow();

                // Apply sorting to the underlying list when requested by ImGui.
                unsafe
                {
                    var sortSpecs = ImGui.TableGetSortSpecs();
                    if (sortSpecs.NativePtr != null && sortSpecs.SpecsDirty)
                    {
                        ApplyEntriesSort(currentFilterObj, sortSpecs);
                        sortSpecs.SpecsDirty = false;
                    }
                }

                var entriesToRemove = new List<LootFilterEntry>();
                int entryIndex = 0;

                foreach (var entry in _currentFilterEntries)
                {
                    ImGui.TableNextRow();

                    // Enabled checkbox (leftmost column)
                    ImGui.TableNextColumn();
                    bool enabled = entry.Enabled;
                    if (ImGui.Checkbox($"##enabled{entryIndex}", ref enabled))
                    {
                        entry.Enabled = enabled;
                    }

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
                        if (ImGui.Button("继承过滤器颜色"))
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
                        ImGui.TextDisabled("(继承)");
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
                    _currentFilterEntries.Remove(entry);
                    currentFilterObj?.Entries.Remove(entry);
                }

                // Clean up removed entry colors and refresh filter
                if (entriesToRemove.Count > 0)
                {
                    _entryColors.Clear();
                    _entryColorHexes.Clear();
                }
            }
        }

        private static void ApplyEntriesSort(UserLootFilter currentFilterObj, ImGuiTableSortSpecsPtr sortSpecs)
        {
            if (sortSpecs.SpecsCount <= 0)
                return;

            // Currently only respect the primary sort column.
            unsafe
            {
                var spec = sortSpecs.Specs;
                bool asc = spec.SortDirection == ImGuiSortDirection.Ascending;

                Comparison<LootFilterEntry> comparison = spec.ColumnIndex switch
                {
                    0 => (a, b) => (a.Enabled ? 1 : 0).CompareTo(b.Enabled ? 1 : 0),
                    1 => (a, b) => string.Compare(GetItemName(a.ItemID), GetItemName(b.ItemID), StringComparison.OrdinalIgnoreCase),
                    2 => (a, b) => ((int)a.Type).CompareTo((int)b.Type),
                    _ => null
                };

                if (comparison is null)
                    return;

                if (!asc)
                {
                    var inner = comparison;
                    comparison = (a, b) => -inner(a, b);
                }

                _currentFilterEntries.Sort(comparison);

                // Keep the backing config list in the same order so the sort persists.
                if (currentFilterObj?.Entries is not null)
                {
                    currentFilterObj.Entries.Sort(comparison);
                }

                _entryColors.Clear();
                _entryColorHexes.Clear();
            }
        }

        #region Helper Methods

        private static void UpdateFilterColorFromCurrent()
        {
            var currentFilter = GetCurrentFilter();
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
                return "(未知)";
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

                if (!Config.LootFilters.Filters.TryAdd(name, new UserLootFilter
                {
                    Enabled = true,
                    Color = defaultColor,
                    Entries = new()
                }))
                {
                    MessageBox.Show(RadarWindow.Handle, "该过滤器已存在。", "物品过滤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                RefreshFilterIndex();
                SelectedFilterName = name;
                _selectedFilterIndex = _filterNames.IndexOf(name);
                RefreshCurrentFilterEntries();
                UpdateFilterColorFromCurrent();
            }
            catch (Exception ex)
            {
                MessageBox.Show(RadarWindow.Handle, $"添加过滤器出错: {ex.Message}", "物品过滤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void RenameCurrentFilter(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                return;

            string oldName = SelectedFilterName;
            if (string.IsNullOrEmpty(oldName))
                return;

            try
            {
                if (Config.LootFilters.Filters.TryGetValue(oldName, out var filter)
                    && Config.LootFilters.Filters.TryAdd(newName, filter)
                    && Config.LootFilters.Filters.TryRemove(oldName, out _))
                {
                    RefreshFilterIndex();
                    SelectedFilterName = newName;
                    _selectedFilterIndex = _filterNames.IndexOf(newName);
                }
                else
                {
                    MessageBox.Show(RadarWindow.Handle, "重命名失败。", "物品过滤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(RadarWindow.Handle, $"重命名过滤器出错: {ex.Message}", "物品过滤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void DeleteCurrentFilter()
        {
            string name = SelectedFilterName;
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show(RadarWindow.Handle, "未选择物品过滤器！", "物品过滤", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(RadarWindow.Handle, $"确定要删除 '{name}' 吗？", "物品过滤",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                if (!Config.LootFilters.Filters.TryRemove(name, out _))
                {
                    MessageBox.Show(RadarWindow.Handle, "删除失败。", "物品过滤", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Ensure at least one filter remains
                if (Config.LootFilters.Filters.IsEmpty)
                {
                    Config.LootFilters.Filters.TryAdd("default", new UserLootFilter
                    {
                        Enabled = true,
                        Entries = new()
                    });
                }

                RefreshFilterIndex();
                RefreshCurrentFilterEntries();
            }
            catch (Exception ex)
            {
                MessageBox.Show(RadarWindow.Handle, $"删除过滤器出错: {ex.Message}", "物品过滤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void AddItemToFilter(TarkovMarketItem item)
        {
            var currentFilter = GetCurrentFilter();
            if (currentFilter is null)
                return;

            var entry = new LootFilterEntry
            {
                ItemID = item.BsgId,
                Color = currentFilter.Color, // inherit filter color
                ParentFilter = currentFilter
            };

            currentFilter.Entries.Add(entry);
            _currentFilterEntries.Add(entry);
        }

        /// <summary>
        /// Refreshes the loot filter system.
        /// </summary>
        public static void RefreshLootFilter()
        {
            foreach (var item in TarkovDataManager.AllItems.Values)
                item.SetFilter(null);

            // Ensure every entry has its ParentFilter populated.
            // This is required for inheritance (e.g. color) and for any logic that relies on ParentFilter.
            foreach (var filter in Config.LootFilters.Filters.Values)
            {
                if (filter?.Entries is null)
                    continue;

                foreach (var entry in filter.Entries)
                    entry.ParentFilter = filter;
            }

            var currentFilters = Config.LootFilters.Filters
                .Values
                .Where(x => x.Enabled)
                .SelectMany(x => x.Entries)
                .Where(e => e.Enabled); // Only enabled entries

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

