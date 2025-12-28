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
using LoneEftDmaRadar.UI.Maps;

namespace LoneEftDmaRadar.UI.Panels
{
    /// <summary>
    /// Map Setup Helper Panel - Allows editing map X, Y, and Scale values.
    /// </summary>
    internal static class MapSetupHelperPanel
    {
        // Panel-local state
        private static float _x;
        private static float _y;
        private static float _scale;
        private static bool _valuesLoaded;

        private static EftDmaConfig Config { get; } = Program.Config;

        /// <summary>
        /// Whether the map setup helper panel is open.
        /// </summary>
        public static bool IsOpen { get; set; }

        /// <summary>
        /// Whether the map setup helper overlay is shown.
        /// </summary>
        public static bool ShowOverlay { get; set; }

        /// <summary>
        /// Map setup helper coordinates display.
        /// </summary>
        public static string Coords { get; set; } = string.Empty;

        /// <summary>
        /// Draw the Map Setup Helper window.
        /// </summary>
        public static void Draw()
        {
            bool isOpen = IsOpen;
            ImGui.SetNextWindowSize(new Vector2(350, 220), ImGuiCond.FirstUseEver);

            if (ImGui.Begin("Map Setup Helper", ref isOpen))
            {
                var currentMap = EftMapManager.Map?.Config;

                // Load current map values when window opens or map changes
                if (currentMap is not null && (!_valuesLoaded || ShouldReloadValues(currentMap)))
                {
                    LoadMapValues(currentMap);
                }

                // Current coordinates display
                ImGui.Text("Current Coordinates:");
                ImGui.TextColored(new Vector4(0.2f, 0.8f, 0.2f, 1f), Coords);

                ImGui.Separator();

                if (currentMap is null)
                {
                    ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "No map loaded!");
                }
                else
                {
                    // Map name display
                    ImGui.Text($"Map: {currentMap.Name}");
                    ImGui.Spacing();

                    // X, Y inputs on same line
                    ImGui.Text("X, Y:");
                    ImGui.SetNextItemWidth(120);
                    ImGui.InputFloat("##MapX", ref _x, 1f, 10f, "%.1f");
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Map X offset");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(120);
                    ImGui.InputFloat("##MapY", ref _y, 1f, 10f, "%.1f");
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Map Y offset");

                    // Scale input
                    ImGui.Text("Scale:");
                    ImGui.SetNextItemWidth(120);
                    ImGui.InputFloat("##MapScale", ref _scale, 0.01f, 0.1f, "%.4f");
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Map scale factor");

                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();

                    // Apply button
                    if (ImGui.Button("Apply", new Vector2(80, 0)))
                    {
                        ApplyMapValues(currentMap);
                    }
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Apply changes to the map");

                    ImGui.SameLine();

                    // Reset button
                    if (ImGui.Button("Reset", new Vector2(80, 0)))
                    {
                        LoadMapValues(currentMap);
                    }
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Reset to current map values");
                }
            }
            ImGui.End();
            IsOpen = isOpen;

            // Reset loaded state when window closes
            if (!isOpen)
            {
                _valuesLoaded = false;
            }
        }

        private static bool ShouldReloadValues(EftMapConfig currentMap)
        {
            // Reload if map X/Y/Scale don't match our cached values
            // This handles map changes
            return _x != currentMap.X || _y != currentMap.Y || _scale != currentMap.Scale;
        }

        private static void LoadMapValues(EftMapConfig currentMap)
        {
            _x = currentMap.X;
            _y = currentMap.Y;
            _scale = currentMap.Scale;
            _valuesLoaded = true;
        }

        private static void ApplyMapValues(EftMapConfig currentMap)
        {
            currentMap.X = _x;
            currentMap.Y = _y;
            currentMap.Scale = _scale;
        }
    }
}
