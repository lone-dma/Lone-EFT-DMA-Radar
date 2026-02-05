/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using Collections.Pooled;
using ImGuiNET;
using LoneEftDmaRadar.Misc;
using LoneEftDmaRadar.Tarkov.World.Loot;
using LoneEftDmaRadar.Tarkov.World.Player;
using LoneEftDmaRadar.UI.Loot;

namespace LoneEftDmaRadar.UI.Widgets
{
    /// <summary>
    /// Loot Widget that displays a sortable table of filtered loot using ImGui.
    /// </summary>
    public static class LootWidget
    {
        private const float MinHeight = 100f;
        private const float MaxHeight = 500f;
        private const int VisibleRows = 10;
        private const float RowHeight = 18f;
        private const float HeaderHeight = 26f;

        private static EftDmaConfig Config { get; } = Program.Config;

        /// <summary>
        /// Whether the Loot Widget is open.
        /// </summary>
        public static bool IsOpen
        {
            get => Config.LootWidget.Enabled;
            set => Config.LootWidget.Enabled = value;
        }

        // Data sources
        private static LocalPlayer LocalPlayer => Memory.LocalPlayer;
        private static IEnumerable<LootItem> FilteredLoot => Memory.Loot?.FilteredLoot;

        // Sorting state
        private static uint _sortColumnId = 1; // Default: Value
        private static bool _sortAscending = false; // Default: highest value first

        // Search state
        private static string _searchText = string.Empty;

        internal static void Initialize()
        {
            _sortColumnId = Config.LootWidget.SortColumn;
            _sortAscending = Config.LootWidget.SortAscending;
        }

        /// <summary>
        /// Apply loot search filter.
        /// </summary>
        private static void ApplyLootSearch()
        {
            LootFilter.SearchString = _searchText?.Trim();
            Memory.Loot?.RefreshFilter();
        }

        /// <summary>
        /// Draw the Loot Widget.
        /// </summary>
        public static void Draw()
        {
            if (!IsOpen || Program.State != AppState.InRaid)
                return;

            var localPlayer = LocalPlayer;
            var filteredLoot = FilteredLoot;
            if (localPlayer is null || filteredLoot is null)
                return;

            // Default (initial) height targets ~10 rows, but the window remains resizable.
            float defaultTableHeight = HeaderHeight + (RowHeight * VisibleRows);
            ImGui.SetNextWindowSize(new Vector2(450, defaultTableHeight + 100), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(200, MinHeight), new Vector2(600, MaxHeight));

            bool isOpen = IsOpen;
            var windowFlags = ImGuiWindowFlags.None;

            if (!ImGui.Begin("物品", ref isOpen, windowFlags))
            {
                IsOpen = isOpen;
                ImGui.End();
                return;
            }
            IsOpen = isOpen;

            // Tabbed interface
            if (ImGui.BeginTabBar("LootTabBar"))
            {
                if (ImGui.BeginTabItem("物品列表"))
                {
                    DrawLootListTab(localPlayer, filteredLoot);
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("选项"))
                {
                    DrawOptionsTab();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.End();
        }

        private static void DrawLootListTab(LocalPlayer localPlayer, IEnumerable<LootItem> filteredLoot)
        {
            // Search at the top
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputText("##LootSearch", ref _searchText, 64))
            {
                ApplyLootSearch();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("按名称搜索物品");
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                ImGui.SameLine();
                if (ImGui.Button("X##ClearSearch"))
                {
                    _searchText = string.Empty;
                    ApplyLootSearch();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("清除搜索");
            }

            ImGui.Separator();

            // Convert to pooled list for sorting
            var localPos = localPlayer.Position;
            using var lootList = filteredLoot.ToPooledList();

            if (lootList.Count == 0)
            {
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "未检测到物品");
                return;
            }

            // Compact table with tight padding
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(4, 2));

            const ImGuiTableFlags tableFlags = ImGuiTableFlags.Borders |
                                               ImGuiTableFlags.RowBg |
                                               ImGuiTableFlags.Sortable |
                                               ImGuiTableFlags.SizingFixedFit |
                                               ImGuiTableFlags.ScrollY |
                                               ImGuiTableFlags.Resizable;

            // Fill available space so resizing the window resizes the table.
            var tableSize = new Vector2(0, -1);
            if (ImGui.BeginTable("LootTable", 3, tableFlags, tableSize))
            {
                ImGui.TableSetupScrollFreeze(0, 1); // Freeze header row

                var nameFlags = ImGuiTableColumnFlags.WidthStretch;
                var valueFlags = ImGuiTableColumnFlags.WidthFixed;
                var distFlags = ImGuiTableColumnFlags.WidthFixed;

                // Apply configured default sort column on first startup.
                switch (_sortColumnId)
                {
                    case 0:
                        nameFlags |= ImGuiTableColumnFlags.DefaultSort;
                        break;
                    case 1:
                        valueFlags |= ImGuiTableColumnFlags.DefaultSort;
                        break;
                    case 2:
                        distFlags |= ImGuiTableColumnFlags.DefaultSort;
                        break;
                }

                // Apply preferred direction (ImGui will use descending for the DefaultSort column when this is present).
                // Not all ImGui.NET builds expose PreferSortDescending; if it doesn't exist, this line will be removed by build.
                if (!_sortAscending)
                {
                    nameFlags |= ImGuiTableColumnFlags.PreferSortDescending;
                    valueFlags |= ImGuiTableColumnFlags.PreferSortDescending;
                    distFlags |= ImGuiTableColumnFlags.PreferSortDescending;
                }

                ImGui.TableSetupColumn("名称", nameFlags, 0f, 0);
                ImGui.TableSetupColumn("价值", valueFlags, 60f, 1);
                ImGui.TableSetupColumn("距离", distFlags, 45f, 2);
                ImGui.TableHeadersRow();

                // Handle sorting
                var sortSpecs = ImGui.TableGetSortSpecs();
                if (sortSpecs.SpecsDirty)
                {
                    if (sortSpecs.SpecsCount > 0)
                    {
                        var spec = sortSpecs.Specs;
                        var newColumn = spec.ColumnUserID;
                        var newAscending = spec.SortDirection == ImGuiSortDirection.Ascending;

                        if (newColumn != _sortColumnId || newAscending != _sortAscending)
                        {
                            _sortColumnId = newColumn;
                            _sortAscending = newAscending;

                            Config.LootWidget.SortColumn = _sortColumnId;
                            Config.LootWidget.SortAscending = _sortAscending;
                        }
                    }
                    sortSpecs.SpecsDirty = false;
                }

                // Sort the list based on current sort spec
                SortLootList(lootList, localPos);

                foreach (var item in lootList.Span)
                {
                    ImGui.TableNextRow();

                    // Check for double-click on entire row
                    ImGui.TableNextColumn();

                    // Make the row selectable for double-click detection
                    bool isSelected = false;
                    ImGui.PushID(item.GetHashCode());
                    if (ImGui.Selectable($"##row", ref isSelected, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick))
                    {
                        if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        {
                            RadarWindow.PingMapEntity(item);
                        }
                    }
                    ImGui.SameLine();

                    // Column 0: Name
                    ImGui.Text(item.Name ?? "--");

                    // Column 1: Value
                    ImGui.TableNextColumn();
                    ImGui.Text(Utilities.FormatNumberKM(item.Price).ToString());

                    // Column 2: Distance
                    ImGui.TableNextColumn();
                    var distance = (int)Vector3.Distance(localPos, item.Position);
                    ImGui.Text(distance.ToString());

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }

            ImGui.PopStyleVar(); // CellPadding
        }

        private static void DrawOptionsTab()
        {
            // Value Thresholds - side by side
            ImGui.Text("最低价值:");
            ImGui.SameLine(150);
            ImGui.Text("贵重最低价值:");

            ImGui.SetNextItemWidth(140);
            int minValue = Config.Loot.MinValue;
            if (ImGui.InputInt("##MinValue", ref minValue, 1000, 10000))
            {
                Config.Loot.MinValue = Math.Max(0, minValue);
                Memory.Loot?.RefreshFilter();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("显示普通物品的最低价值");
            ImGui.SameLine(150);
            ImGui.SetNextItemWidth(140);
            int valuableMin = Config.Loot.MinValueValuable;
            if (ImGui.InputInt("##ValuableMin", ref valuableMin, 1000, 10000))
            {
                Config.Loot.MinValueValuable = Math.Max(0, valuableMin);
                Memory.Loot?.RefreshFilter();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("高亮显示为贵重物品的最低价值");

            ImGui.Separator();

            // Price options on one line
            bool pricePerSlot = Config.Loot.PricePerSlot;
            if (ImGui.Checkbox("每格价格", ref pricePerSlot))
            {
                Config.Loot.PricePerSlot = pricePerSlot;
                Memory.Loot?.RefreshFilter();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("根据每个背包格子的价格计算价值");
            ImGui.SameLine(150);
            ImGui.Text("模式:");
            ImGui.SameLine();
            int priceMode = (int)Config.Loot.PriceMode;
            if (ImGui.RadioButton("跳蚤市场", ref priceMode, 0))
            {
                Config.Loot.PriceMode = LootPriceMode.FleaMarket;
                Memory.Loot?.RefreshFilter();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("使用跳蚤市场价格");
            ImGui.SameLine();
            if (ImGui.RadioButton("商人", ref priceMode, 1))
            {
                Config.Loot.PriceMode = LootPriceMode.Trader;
                Memory.Loot?.RefreshFilter();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("使用商人出售价格");

            ImGui.Separator();

            // Category toggles
            bool hideCorpses = Config.Loot.HideCorpses;
            if (ImGui.Checkbox("隐藏尸体", ref hideCorpses))
            {
                Config.Loot.HideCorpses = hideCorpses;
                Memory.Loot?.RefreshFilter();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("在雷达上隐藏玩家尸体");
            ImGui.SameLine(150);
            bool showMeds = LootFilter.ShowMeds;
            if (ImGui.Checkbox("显示医疗用品", ref showMeds))
            {
                LootFilter.ShowMeds = showMeds;
                Memory.Loot?.RefreshFilter();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("无视价值显示医疗物品");

            bool showFood = LootFilter.ShowFood;
            if (ImGui.Checkbox("显示食物", ref showFood))
            {
                LootFilter.ShowFood = showFood;
                Memory.Loot?.RefreshFilter();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("无视价值显示食物和饮料");
            ImGui.SameLine(150);
            bool showBackpacks = LootFilter.ShowBackpacks;
            if (ImGui.Checkbox("显示背包", ref showBackpacks))
            {
                LootFilter.ShowBackpacks = showBackpacks;
                Memory.Loot?.RefreshFilter();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("无视价值显示背包");

            bool showQuestItems = LootFilter.ShowQuestItems;
            if (ImGui.Checkbox("显示任务物品", ref showQuestItems))
            {
                LootFilter.ShowQuestItems = showQuestItems;
                Memory.Loot?.RefreshFilter();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("在地图上显示所有静态任务物品。");
        }

        private static void SortLootList(PooledList<LootItem> list, Vector3 localPos)
        {
            list.Span.Sort((a, b) =>
            {
                int result = _sortColumnId switch
                {
                    0 => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase), // Name
                    1 => a.Price.CompareTo(b.Price), // Value
                    2 => Vector3.DistanceSquared(localPos, a.Position).CompareTo(Vector3.DistanceSquared(localPos, b.Position)), // Distance
                    _ => 0
                };

                return _sortAscending ? result : -result;
            });
        }
    }
}

