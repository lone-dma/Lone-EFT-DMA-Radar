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

using Collections.Pooled;
using ImGuiNET;
using LoneEftDmaRadar.Misc;
using LoneEftDmaRadar.Tarkov.World.Loot;
using LoneEftDmaRadar.Tarkov.World.Player;

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
        private static bool InRaid => Memory.InRaid;

        // Sorting state
        private static uint _sortColumnId = 1; // Default: Value
        private static bool _sortAscending = false; // Default: highest value first

        /// <summary>
        /// Draw the Loot Widget.
        /// </summary>
        public static void Draw()
        {
            if (!IsOpen || !InRaid)
                return;

            var localPlayer = LocalPlayer;
            var filteredLoot = FilteredLoot;
            if (localPlayer is null || filteredLoot is null)
                return;

            // Default (initial) height targets ~10 rows, but the window remains resizable.
            float defaultTableHeight = HeaderHeight + (RowHeight * VisibleRows);
            ImGui.SetNextWindowSize(new Vector2(450, defaultTableHeight + 60), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(200, MinHeight), new Vector2(600, MaxHeight));

            bool isOpen = IsOpen;
            var windowFlags = ImGuiWindowFlags.None;

            if (!ImGui.Begin("Loot", ref isOpen, windowFlags))
            {
                IsOpen = isOpen;
                ImGui.End();
                return;
            }
            IsOpen = isOpen;

            // Convert to pooled list for sorting
            var localPos = localPlayer.Position;
            using var lootList = filteredLoot.ToPooledList();

            if (lootList.Count == 0)
            {
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "No loot detected");
                ImGui.End();
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
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 0f, 0);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthFixed, 60f, 1);
                ImGui.TableSetupColumn("Dist", ImGuiTableColumnFlags.WidthFixed, 45f, 2);
                ImGui.TableHeadersRow();

                // Handle sorting
                var sortSpecs = ImGui.TableGetSortSpecs();
                if (sortSpecs.SpecsDirty)
                {
                    if (sortSpecs.SpecsCount > 0)
                    {
                        var spec = sortSpecs.Specs;
                        _sortColumnId = spec.ColumnUserID;
                        _sortAscending = spec.SortDirection == ImGuiSortDirection.Ascending;
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
                            RadarWindow.PingItem(item);
                        }
                    }
                    ImGui.SameLine();
                    
                    // Column 0: Name
                    ImGui.Text(item.ShortName ?? "--");

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

            ImGui.End();
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
