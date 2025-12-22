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
using LoneEftDmaRadar.UI.Loot;

namespace LoneEftDmaRadar.UI.Panels
{
    /// <summary>
    /// Radar Overlay Panel - Quick access controls shown over the radar.
    /// </summary>
    internal static class RadarOverlayPanel
    {
        private static readonly RadarUIState _state = RadarUIState.Instance;
        private static string _searchText = string.Empty;
        private static bool _lootOverlayVisible;
        private static bool _mapSetupVisible;

        /// <summary>
        /// Draw the overlay controls at the top of the radar.
        /// </summary>
        public static void DrawTopBar()
        {
            // Static position in top-left, below menu bar
            ImGui.SetNextWindowPos(new Vector2(10, 25), ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0.7f);

            var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize |
                        ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings;

            if (ImGui.Begin("RadarTopBar", flags))
            {
                // Map Free Toggle Button
                bool isMapFree = _state.IsMapFreeEnabled;
                if (isMapFree)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.6f, 0.2f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.7f, 0.3f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.1f, 0.5f, 0.1f, 1.0f));
                }

                if (ImGui.Button(isMapFree ? "Map Free: ON" : "Map Free: OFF"))
                {
                    _state.IsMapFreeEnabled = !isMapFree;
                    if (isMapFree) // Was on, now turning off
                    {
                        _state.MapPanPosition = Vector2.Zero;
                    }
                }

                if (isMapFree)
                {
                    ImGui.PopStyleColor(3);
                }

                // Loot button - only show when loot is enabled
                if (Program.Config.Loot.Enabled)
                {
                    ImGui.SameLine();
                    if (ImGui.Button("Loot"))
                    {
                        _state.IsLootOverlayVisible = !_state.IsLootOverlayVisible;
                        _lootOverlayVisible = _state.IsLootOverlayVisible;
                    }
                }

                ImGui.End();
            }
        }

        /// <summary>
        /// Draw the loot overlay panel (shown when loot button is clicked).
        /// </summary>
        public static void DrawLootOverlay()
        {
            if (!Program.Config.Loot.Enabled)
                return;

            // Loot Options Panel - only show if toggled on
            _lootOverlayVisible = _state.IsLootOverlayVisible;
            if (!_lootOverlayVisible)
                return;

            ImGui.SetNextWindowPos(new Vector2(10, 70), ImGuiCond.Appearing);
            ImGui.SetNextWindowBgAlpha(0.9f);

            var flags = ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize;
            if (ImGui.Begin("Loot Options", ref _lootOverlayVisible, flags))
            {
                DrawLootOptions();
            }
            ImGui.End();
            _state.IsLootOverlayVisible = _lootOverlayVisible;
        }

        private static void DrawLootOptions()
        {
            // Use a wider layout with two columns for checkboxes
            ImGui.SetNextItemWidth(250);

            // Search
            ImGui.Text("Item Search:");
            ImGui.SetNextItemWidth(250);
            if (ImGui.InputText("##LootSearch", ref _searchText, 64))
            {
                _state.LootSearchText = _searchText;
                _state.ApplyLootSearch();
            }
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                ImGui.SameLine();
                if (ImGui.Button("X##ClearSearch"))
                {
                    _searchText = string.Empty;
                    _state.LootSearchText = string.Empty;
                    _state.ApplyLootSearch();
                }
            }

            ImGui.Separator();

            // Value Thresholds - side by side
            ImGui.Text("Min Value:");
            ImGui.SameLine(150);
            ImGui.Text("Valuable Min:");

            ImGui.SetNextItemWidth(140);
            int minValue = Program.Config.Loot.MinValue;
            if (ImGui.InputInt("##MinValue", ref minValue, 1000, 10000))
            {
                Program.Config.Loot.MinValue = Math.Max(0, minValue);
                Memory.Loot?.RefreshFilter();
            }
            ImGui.SameLine(150);
            ImGui.SetNextItemWidth(140);
            int valuableMin = Program.Config.Loot.MinValueValuable;
            if (ImGui.InputInt("##ValuableMin", ref valuableMin, 1000, 10000))
            {
                Program.Config.Loot.MinValueValuable = Math.Max(0, valuableMin);
                Memory.Loot?.RefreshFilter();
            }

            ImGui.Separator();

            // Price options on one line
            bool pricePerSlot = Program.Config.Loot.PricePerSlot;
            if (ImGui.Checkbox("Price per Slot", ref pricePerSlot))
            {
                Program.Config.Loot.PricePerSlot = pricePerSlot;
                Memory.Loot?.RefreshFilter();
            }
            ImGui.SameLine(150);
            ImGui.Text("Mode:");
            ImGui.SameLine();
            int priceMode = (int)Program.Config.Loot.PriceMode;
            if (ImGui.RadioButton("Flea", ref priceMode, 0))
            {
                Program.Config.Loot.PriceMode = LootPriceMode.FleaMarket;
                Memory.Loot?.RefreshFilter();
            }
            ImGui.SameLine();
            if (ImGui.RadioButton("Trader", ref priceMode, 1))
            {
                Program.Config.Loot.PriceMode = LootPriceMode.Trader;
                Memory.Loot?.RefreshFilter();
            }

            ImGui.Separator();

            // Category toggles - two columns
            bool hideCorpses = Program.Config.Loot.HideCorpses;
            if (ImGui.Checkbox("Hide Corpses", ref hideCorpses))
            {
                Program.Config.Loot.HideCorpses = hideCorpses;
                Memory.Loot?.RefreshFilter();
            }
            ImGui.SameLine(150);
            bool showMeds = _state.ShowMeds;
            if (ImGui.Checkbox("Show Meds", ref showMeds))
            {
                _state.ShowMeds = showMeds;
                Memory.Loot?.RefreshFilter();
            }

            bool showFood = _state.ShowFood;
            if (ImGui.Checkbox("Show Food", ref showFood))
            {
                _state.ShowFood = showFood;
                Memory.Loot?.RefreshFilter();
            }
            ImGui.SameLine(150);
            bool showBackpacks = _state.ShowBackpacks;
            if (ImGui.Checkbox("Show Backpacks", ref showBackpacks))
            {
                _state.ShowBackpacks = showBackpacks;
                Memory.Loot?.RefreshFilter();
            }

            bool showQuestItems = _state.ShowQuestItems;
            if (ImGui.Checkbox("Show Quest Items", ref showQuestItems))
            {
                _state.ShowQuestItems = showQuestItems;
                Memory.Loot?.RefreshFilter();
            }
        }

        /// <summary>
        /// Draw the map setup helper overlay.
        /// </summary>
        public static void DrawMapSetupHelper()
        {
            _mapSetupVisible = _state.ShowMapSetupHelper;
            if (!_mapSetupVisible)
                return;

            var io = ImGui.GetIO();
            ImGui.SetNextWindowPos(new Vector2(io.DisplaySize.X - 310, 10), ImGuiCond.Appearing);
            ImGui.SetNextWindowSize(new Vector2(300, 80), ImGuiCond.Appearing);
            ImGui.SetNextWindowBgAlpha(0.8f);

            if (ImGui.Begin("Map Setup Helper", ref _mapSetupVisible, ImGuiWindowFlags.NoSavedSettings))
            {
                ImGui.Text("Current Coordinates:");
                ImGui.TextColored(new Vector4(0.2f, 0.8f, 0.2f, 1f), _state.MapSetupCoords);

                if (ImGui.Button("Copy Coords"))
                {
                    try
                    {
                        Clipboard.SetText(_state.MapSetupCoords);
                    }
                    catch { }
                }
            }
            ImGui.End();
            _state.ShowMapSetupHelper = _mapSetupVisible;
        }
    }
}
