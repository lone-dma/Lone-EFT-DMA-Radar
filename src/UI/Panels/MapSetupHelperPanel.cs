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
        private static string _loadedMapKey;

        // Scan state
        private static bool _isScanning;
        private static float _scanBaseX;
        private static float _scanBaseY;
        private static int _scanRing;
        private static int _scanIndexInRing;
        private static long _lastScanTick;

        private const float ScanStep = 25f;
        private const int ScanIntervalMs = 150;

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
                if (currentMap is not null)
                {
                    var key = GetMapKey(currentMap);
                    if (!_valuesLoaded || !string.Equals(_loadedMapKey, key, StringComparison.Ordinal))
                    {
                        LoadMapValues(currentMap);
                        _loadedMapKey = key;
                    }
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
                    // Tick scan (updates _x/_y and live map config)
                    TickScanIfNeeded(currentMap);

                    // Map name display
                    ImGui.Text($"Map: {currentMap.Name}");
                    ImGui.Spacing();

                    // X, Y inputs on same line
                    ImGui.Text("X, Y:");
                    ImGui.SetNextItemWidth(120);
                    if (ImGui.InputFloat("##MapX", ref _x, 1f, 10f, "%.1f"))
                    {
                        if (_isScanning)
                            StopScan();
                    }
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Map X offset");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(120);
                    if (ImGui.InputFloat("##MapY", ref _y, 1f, 10f, "%.1f"))
                    {
                        if (_isScanning)
                            StopScan();
                    }
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Map Y offset");

                    // Scale input
                    ImGui.Text("Scale:");
                    ImGui.SetNextItemWidth(120);
                    if (ImGui.InputFloat("##MapScale", ref _scale, 0.01f, 0.1f, "%.4f"))
                    {
                        if (_isScanning)
                            StopScan();
                    }
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
                        StopScan();
                        LoadMapValues(currentMap);
                        _loadedMapKey = GetMapKey(currentMap);
                    }
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Reset to current map values");

                    ImGui.SameLine();

                    // Scan button
                    var scanLabel = _isScanning ? "Stop Scan" : "Scan X/Y";
                    if (ImGui.Button(scanLabel, new Vector2(100, 0)))
                    {
                        if (_isScanning)
                        {
                            StopScan();
                        }
                        else
                        {
                            StartScan();
                        }
                    }
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Step X/Y around 0,0 in ~25 increments to help find the map image");
                }
            }
            ImGui.End();
            IsOpen = isOpen;

            // Reset loaded state when window closes
            if (!isOpen)
            {
                _valuesLoaded = false;
                _loadedMapKey = null;
                StopScan();
            }
        }

        private static void StartScan()
        {
            _isScanning = true;
            _scanBaseX = 0f;
            _scanBaseY = 0f;
            _scanRing = 0;
            _scanIndexInRing = 0;
            _lastScanTick = Environment.TickCount64;
        }

        private static void StopScan()
        {
            _isScanning = false;
        }

        private static void TickScanIfNeeded(EftMapConfig currentMap)
        {
            if (!_isScanning)
                return;

            long now = Environment.TickCount64;
            if (now - _lastScanTick < ScanIntervalMs)
                return;
            _lastScanTick = now;

            // Expand outward ring-by-ring around 0,0:
            // ring 0 => (0,0)
            // ring 1 => all perimeter points where max(|x|,|y|)=1
            // ring 2 => all perimeter points where max(|x|,|y|)=2
            // etc.
            var (xi, yi) = GetRingPoint(_scanRing, _scanIndexInRing);

            _x = _scanBaseX + xi * ScanStep;
            _y = _scanBaseY + yi * ScanStep;

            // Apply immediately so the map updates live
            currentMap.X = _x;
            currentMap.Y = _y;

            // Advance within current ring
            _scanIndexInRing++;
            int ringCount = GetRingPointCount(_scanRing);
            if (_scanIndexInRing >= ringCount)
            {
                _scanRing++;
                _scanIndexInRing = 0;
            }
        }

        private static int GetRingPointCount(int ring)
        {
            if (ring <= 0)
                return 1;

            // perimeter of square with side length (2r+1): 8r
            return 8 * ring;
        }

        private static (int x, int y) GetRingPoint(int ring, int index)
        {
            if (ring <= 0)
                return (0, 0);

            // Enumerate the perimeter clockwise starting at (r, -r)
            // Bottom edge: 2r points (x: r..-r+1, y: -r)
            // Left edge:   2r points (x: -r, y: -r..r-1)
            // Top edge:    2r points (x: -r..r-1, y: r)
            // Right edge:  2r points (x: r, y: r..-r+1)
            int r = ring;
            int edgeLen = 2 * r;
            int i = index % (8 * r);

            if (i < edgeLen)
            {
                // bottom: (r - i, -r)
                return (r - i, -r);
            }
            i -= edgeLen;

            if (i < edgeLen)
            {
                // left: (-r, -r + i)
                return (-r, -r + i);
            }
            i -= edgeLen;

            if (i < edgeLen)
            {
                // top: (-r + i, r)
                return (-r + i, r);
            }
            i -= edgeLen;

            // right: (r, r - i)
            return (r, r - i);
        }

        private static string GetMapKey(EftMapConfig currentMap)
        {
            var id = currentMap.MapID is { Count: > 0 } ? currentMap.MapID[0] : string.Empty;
            return $"{currentMap.Name}|{id}";
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

            // Keep UI in sync and prevent immediate reloads
            _valuesLoaded = true;
        }
    }
}
