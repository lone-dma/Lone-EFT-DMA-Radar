/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using Collections.Pooled;
using ImGuiNET;
using LoneEftDmaRadar.Misc;
using LoneEftDmaRadar.Tarkov.World.Player;
using LoneEftDmaRadar.Tarkov.World.Player.Helpers;
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

        /// <summary>
        /// Draw the Player Info Widget.
        /// </summary>
        public static void Draw()
        {
            if (!IsOpen || Program.State != AppState.InRaid)
                return;

            var localPlayer = LocalPlayer;
            var allPlayers = AllPlayers;
            if (localPlayer is null || allPlayers is null)
                return;

            // Filter and sort players: only hostile humans, sorted by distance
            var localPos = localPlayer.Position;
            using var filteredPlayers = allPlayers
                .OfType<ObservedPlayer>()
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

            if (ImGui.BeginTable("PlayersTable", 6, tableFlags))
            {
                // New compact column layout
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 65f);
                ImGui.TableSetupColumn("Grp", ImGuiTableColumnFlags.WidthFixed, 25f);
                ImGui.TableSetupColumn("In Hands", ImGuiTableColumnFlags.WidthFixed, 115f);
                ImGui.TableSetupColumn("Secure", ImGuiTableColumnFlags.WidthFixed, 45f);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthFixed, 45f);
                ImGui.TableSetupColumn("Dist", ImGuiTableColumnFlags.WidthFixed, 35f);
                ImGui.TableHeadersRow();

                bool mouseOp = false;
                foreach (var player in filteredPlayers.Span)
                {
                    ImGui.TableNextRow();

                    var rowColor = GetTextColor(player);

                    bool rowSelected = false;
                    ImGui.TableNextColumn();

                    ImGui.PushID(player.Id);
                    _ = ImGui.Selectable("##row", ref rowSelected, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick);

                    if (!mouseOp && ImGui.IsItemHovered())
                    {
                        if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        {
                            RadarWindow.PingMapEntity(player);
                            mouseOp = true;
                        }
                        else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                        {
                            player.SetFocus(!player.IsFocused);
                            mouseOp = true;
                        }
                    }

                    // Render row contents on top of the selectable.
                    ImGui.SameLine();
                    ImGui.TextColored(rowColor, player.Name ?? "--");

                    // Column 1: Group
                    ImGui.TableNextColumn();
                    ImGui.TextColored(rowColor, player.GroupId == AbstractPlayer.SoloGroupId ? "--" : player.GroupId.ToString());

                    // Column 2: In Hands
                    ImGui.TableNextColumn();
                    ImGui.TextColored(rowColor, player.Equipment?.InHands?.ShortName ?? "--");

                    // Column 3: Secure
                    ImGui.TableNextColumn();
                    ImGui.TextColored(rowColor, player.Equipment?.SecuredContainer?.ShortName ?? "--");

                    // Column 4: Value
                    ImGui.TableNextColumn();
                    ImGui.TextColored(rowColor, Utilities.FormatNumberKM(player.Equipment?.Value ?? 0).ToString() ?? "--");

                    // Column 5: Dist
                    ImGui.TableNextColumn();
                    ImGui.TextColored(rowColor, ((int)Vector3.Distance(player.Position, localPlayer.Position)).ToString());

                    ImGui.PopID();
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
