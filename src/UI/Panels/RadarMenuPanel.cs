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

namespace LoneEftDmaRadar.UI.Panels
{
    /// <summary>
    /// Radar Menu Panel ( top bar )
    /// </summary>
    internal static class RadarMenuPanel
    {
        private static EftDmaConfig Config { get; } = Program.Config;

        /// <summary>
        /// Draw the overlay controls at the top of the radar.
        /// </summary>
        public static void Draw()
        {
            // Static position in top-left, below menu bar.
            // Use ImGui's current frame height so this stays correct when UI scale changes.
            float topOffset = ImGui.GetFrameHeightWithSpacing();
            ImGui.SetNextWindowPos(new Vector2(10, topOffset), ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0.7f);

            var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize |
                        ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings;

            if (ImGui.Begin("RadarTopBar", flags))
            {
                // Map mode toggle button
                bool isMapFree = RadarWindow.IsMapFreeEnabled;
                if (isMapFree)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.6f, 0.2f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.7f, 0.3f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.1f, 0.5f, 0.1f, 1.0f));
                }

                if (ImGui.Button(isMapFree ? "Map Free" : "Map Follow"))
                {
                    RadarWindow.IsMapFreeEnabled = !isMapFree;
                    if (isMapFree) // Was free, now switching back to follow
                    {
                        RadarWindow.MapPanPosition = Vector2.Zero;
                    }
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(isMapFree ? "Free map panning (drag to move map)" : "Follow player (map centered on you)");

                if (isMapFree)
                {
                    ImGui.PopStyleColor(3);
                }

                ImGui.End();
            }
        }
    }
}
