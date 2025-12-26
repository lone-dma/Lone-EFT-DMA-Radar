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
using LoneEftDmaRadar.Tarkov.GameWorld.Player;
using LoneEftDmaRadar.Tarkov.GameWorld.Player.Helpers;
using LoneEftDmaRadar.UI.Skia;

namespace LoneEftDmaRadar.UI.Widgets
{
    /// <summary>
    /// Player Info Widget that displays a table of hostile human players using ImGui.
    /// </summary>
    public static class PlayerInfoWidget
    {
        // Row height estimation
        private const float RowHeight = 18f;
        private const float HeaderHeight = 20f;
        private const float WindowPadding = 30f; // Title bar + padding
        private const float MinHeight = 50f;
        private const float MaxHeight = 350f;

        /// <summary>
        /// Whether the Player Info Widget is open.
        /// </summary>
        public static bool IsOpen
        {
            get => Program.Config.InfoWidget.Enabled;
            set => Program.Config.InfoWidget.Enabled = value;
        }

        // Data sources
        private static LocalPlayer LocalPlayer => Memory.LocalPlayer;
        private static IReadOnlyCollection<AbstractPlayer> AllPlayers => Memory.Players;
        private static bool InRaid => Memory.InRaid;

        /// <summary>
        /// Draw the Player Info Widget.
        /// </summary>
        public static void Draw()
        {
            if (!IsOpen || !InRaid)
                return;

            var localPlayer = LocalPlayer;
            var allPlayers = AllPlayers;
            if (localPlayer is null || allPlayers is null)
                return;

            // Filter and sort players: only hostile humans, sorted by distance
            var localPos = localPlayer.Position;
            using var filteredPlayers = allPlayers
                .Where(p => p.IsHumanHostileActive)
                .OrderBy(p => Vector3.DistanceSquared(localPos, p.Position))
                .ToPooledList();

            // Set dynamic size - auto width based on content
            ImGui.SetNextWindowSizeConstraints(new Vector2(100, MinHeight), new Vector2(800, MaxHeight));

            bool isOpen = IsOpen;
            var windowFlags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar;

            if (!ImGui.Begin("Player Info", ref isOpen, windowFlags))
            {
                IsOpen = isOpen;
                ImGui.End();
                return;
            }
            IsOpen = isOpen;

            if (filteredPlayers.Count == 0)
            {
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "No hostile players detected");
                ImGui.End();
                return;
            }

            // Compact table with tight padding
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(4, 1));

            const ImGuiTableFlags tableFlags = ImGuiTableFlags.Borders |
                                               ImGuiTableFlags.RowBg |
                                               ImGuiTableFlags.SizingFixedFit |
                                               ImGuiTableFlags.NoPadOuterX;

            if (ImGui.BeginTable("PlayersTable", 5, tableFlags))
            {
                // New compact column layout
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 140f);
                ImGui.TableSetupColumn("Secure", ImGuiTableColumnFlags.WidthFixed, 35f);
                ImGui.TableSetupColumn("In Hands", ImGuiTableColumnFlags.WidthFixed, 100f);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthFixed, 45f);
                ImGui.TableSetupColumn("Dist", ImGuiTableColumnFlags.WidthFixed, 40f);
                ImGui.TableHeadersRow();

                foreach (var player in filteredPlayers.Span)
                {
                    ImGui.TableNextRow();

                    var rowColor = GetTextColor(player);

                    // Column 0: Name (placeholder-safe)
                    ImGui.TableNextColumn();
                    ImGui.TextColored(rowColor, player?.Name ?? "--");

                    // Column 1: Secure (placeholder)
                    ImGui.TableNextColumn();
                    ImGui.TextColored(rowColor, "--");

                    // Column 2: In Hands (placeholder)
                    ImGui.TableNextColumn();
                    ImGui.TextColored(rowColor, "--");

                    // Column 3: Value (placeholder)
                    ImGui.TableNextColumn();
                    ImGui.TextColored(rowColor, "--");

                    // Column 4: Dist (placeholder)
                    ImGui.TableNextColumn();
                    ImGui.TextColored(rowColor, "--");
                }

                ImGui.EndTable();
            }

            ImGui.PopStyleVar(); // CellPadding

            ImGui.End();
        }

        private static Vector4 GetTextColor(AbstractPlayer player)
        {
            SKColor color;
            if (player.IsFocused)
            {
                color = SKPaints.PaintFocused.Color;
            }
            else
            {
                color = player.Type switch
                {
                    PlayerType.PMC => SKPaints.PaintPMC.Color,
                    PlayerType.PScav => SKPaints.PaintPScav.Color,
                    _ => SKColors.White
                };
            }

            color = color.AdjustBrightness(0.5f);
            return new Vector4(color.Red / 255f, color.Green / 255f, color.Blue / 255f, 1f);
        }
    }
}