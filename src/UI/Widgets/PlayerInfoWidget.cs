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

            // Calculate dynamic height based on number of entries
            float contentHeight = HeaderHeight + (filteredPlayers.Count * RowHeight);
            float windowHeight = Math.Clamp(contentHeight + WindowPadding, MinHeight, MaxHeight);

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

            if (ImGui.BeginTable("PlayersTable", 9, tableFlags))
            {
                // Setup columns with tight fixed widths
                ImGui.TableSetupColumn("Fac/Lvl/Name", ImGuiTableColumnFlags.WidthFixed, 140f);
                ImGui.TableSetupColumn("Acct", ImGuiTableColumnFlags.WidthFixed, 35f);
                ImGui.TableSetupColumn("Achv", ImGuiTableColumnFlags.WidthFixed, 30f);
                ImGui.TableSetupColumn("K/D", ImGuiTableColumnFlags.WidthFixed, 35f);
                ImGui.TableSetupColumn("Hours", ImGuiTableColumnFlags.WidthFixed, 40f);
                ImGui.TableSetupColumn("Raids", ImGuiTableColumnFlags.WidthFixed, 40f);
                ImGui.TableSetupColumn("S/R%", ImGuiTableColumnFlags.WidthFixed, 35f);
                ImGui.TableSetupColumn("Grp", ImGuiTableColumnFlags.WidthFixed, 25f);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthFixed, 45f);
                ImGui.TableHeadersRow();

                foreach (var player in filteredPlayers.Span)
                {
                    ImGui.TableNextRow();

                    var rowColor = GetTextColor(player);

                    // Column 0: Faction/Level/Name
                    ImGui.TableNextColumn();
                    string name = (Program.Config.UI.HideNames && player.IsHuman) ? "<Hidden>" : player.Name;
                    char faction = player.PlayerSide.ToString()[0];
                    string level = "0";

                    if (player is ObservedPlayer obs && obs.Profile.Level is int lvl)
                        level = lvl.ToString();

                    ImGui.TextColored(rowColor, $"{faction}{level}:{name}");

                    // Column 1: Account Edition
                    ImGui.TableNextColumn();
                    string edition = "--";
                    if (player is ObservedPlayer obs1 && !string.IsNullOrEmpty(obs1.Profile.Acct))
                        edition = obs1.Profile.Acct;
                    ImGui.TextColored(rowColor, edition);

                    // Column 2: Achievement Level
                    ImGui.TableNextColumn();
                    string achievs = "--";
                    if (player is ObservedPlayer obs2 && obs2.Profile.AchievLevel is int al)
                    {
                        achievs = al switch
                        {
                            1 => "+",
                            2 => "++",
                            _ => "--",
                        };
                    }
                    ImGui.TextColored(rowColor, achievs);

                    // Column 3: K/D
                    ImGui.TableNextColumn();
                    string kd = "--";
                    if (player is ObservedPlayer obs3 && obs3.Profile.Overall_KD is float kdVal)
                        kd = kdVal.ToString("n1");
                    ImGui.TextColored(rowColor, kd);

                    // Column 4: Hours
                    ImGui.TableNextColumn();
                    string hours = "--";
                    if (player is ObservedPlayer obs4 && obs4.Profile.Hours is int hrs)
                        hours = Utilities.FormatNumberKM(hrs);
                    ImGui.TextColored(rowColor, hours);

                    // Column 5: Raid Count
                    ImGui.TableNextColumn();
                    string raidCount = "--";
                    if (player is ObservedPlayer obs5 && obs5.Profile.RaidCount is int rc)
                        raidCount = Utilities.FormatNumberKM(rc);
                    ImGui.TextColored(rowColor, raidCount);

                    // Column 6: Survive Rate
                    ImGui.TableNextColumn();
                    string survivePercent = "--";
                    if (player is ObservedPlayer obs6 && obs6.Profile.SurvivedRate is float sr)
                        survivePercent = sr.ToString("n1");
                    ImGui.TextColored(rowColor, survivePercent);

                    // Column 7: Group ID
                    ImGui.TableNextColumn();
                    string grp = player.GroupID != -1 ? player.GroupID.ToString() : "--";
                    ImGui.TextColored(rowColor, grp);

                    // Column 8: Equipment Value
                    ImGui.TableNextColumn();
                    string value = "--";
                    if (player is ObservedPlayer obs7)
                        value = Utilities.FormatNumberKM(obs7.Equipment.Value);
                    ImGui.TextColored(rowColor, value);
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
                    PlayerType.Streamer => SKPaints.PaintStreamer.Color,
                    PlayerType.SpecialPlayer => SKPaints.PaintWatchlist.Color,
                    _ => SKColors.White
                };
            }
            color = color.AdjustBrightness(0.5f);
            return new Vector4(color.Red / 255f, color.Green / 255f, color.Blue / 255f, 1f);
        }
    }
}
