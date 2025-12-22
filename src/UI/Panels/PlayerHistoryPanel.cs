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
using LoneEftDmaRadar.Tarkov.GameWorld.Player;
using LoneEftDmaRadar.Tarkov.GameWorld.Player.Helpers;
using LoneEftDmaRadar.UI.Misc;
using LoneEftDmaRadar.UI.Skia;

namespace LoneEftDmaRadar.UI.Panels
{
    /// <summary>
    /// Player History Panel for the ImGui-based Radar.
    /// </summary>
    internal static class PlayerHistoryPanel
    {
        // Panel-local state
        private static int _selectedIndex = -1;
        private static bool _showAddToWatchlistPopup = false;
        private static string _watchlistReason = string.Empty;
        private static ObservedPlayer _playerToAdd = null;

        // Player history entries (session-only, not persisted)
        private static readonly List<ObservedPlayer> _playerHistoryEntries = new();

        /// <summary>
        /// Player history entries (read-only access).
        /// </summary>
        public static IReadOnlyList<ObservedPlayer> PlayerHistoryEntries => _playerHistoryEntries;

        /// <summary>
        /// Add player to history.
        /// </summary>
        public static void AddToPlayerHistory(ObservedPlayer player)
        {
            _playerHistoryEntries.Insert(0, player);
        }

        /// <summary>
        /// Clear player history (e.g., on new raid).
        /// </summary>
        public static void ClearHistory()
        {
            _playerHistoryEntries.Clear();
            _selectedIndex = -1;
        }

        /// <summary>
        /// Draw the player history panel.
        /// /// </summary>
        public static void Draw()
        {
            // Controls
            if (ImGui.Button("Add to Watchlist") && _selectedIndex >= 0 && _selectedIndex < _playerHistoryEntries.Count)
            {
                _playerToAdd = _playerHistoryEntries[_selectedIndex];
                _watchlistReason = string.Empty;
                _showAddToWatchlistPopup = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Add the selected player to your watchlist");

            // Add to Watchlist Popup
            if (_showAddToWatchlistPopup && _playerToAdd is not null)
            {
                ImGui.OpenPopup("Add to Watchlist");
            }
            if (ImGui.BeginPopupModal("Add to Watchlist", ref _showAddToWatchlistPopup, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text($"Add '{_playerToAdd?.Name}' to watchlist?");
                ImGui.Text("Enter reason/note:");
                ImGui.InputText("##WatchlistReason", ref _watchlistReason, 128);

                if (ImGui.Button("Add", new Vector2(120, 0)))
                {
                    if (_playerToAdd is not null)
                    {
                        var entry = new PlayerWatchlistEntry
                        {
                            AcctID = _playerToAdd.AccountID?.Trim() ?? string.Empty,
                            Reason = _watchlistReason.Trim(),
                            Timestamp = DateTime.Now
                        };
                        PlayerWatchlistPanel.AddToWatchlist(entry);
                        _playerToAdd.UpdateAlerts(_watchlistReason);
                    }
                    _showAddToWatchlistPopup = false;
                    _playerToAdd = null;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel", new Vector2(120, 0)))
                {
                    _showAddToWatchlistPopup = false;
                    _playerToAdd = null;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }

            // History table
            if (ImGui.BeginTable("HistoryTable", 10,
                ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg,
                new Vector2(0, -ImGui.GetFrameHeightWithSpacing())))
            {
                ImGui.TableSetupScrollFreeze(0, 1); // Freeze header row
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 120);
                ImGui.TableSetupColumn("Acct ID", ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableSetupColumn("Acct", ImGuiTableColumnFlags.WidthFixed, 35);
                ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 65);
                ImGui.TableSetupColumn("K/D", ImGuiTableColumnFlags.WidthFixed, 40);
                ImGui.TableSetupColumn("Hrs", ImGuiTableColumnFlags.WidthFixed, 45);
                ImGui.TableSetupColumn("Raids", ImGuiTableColumnFlags.WidthFixed, 45);
                ImGui.TableSetupColumn("S/R", ImGuiTableColumnFlags.WidthFixed, 40);
                ImGui.TableSetupColumn("Grp", ImGuiTableColumnFlags.WidthFixed, 35);
                ImGui.TableSetupColumn("Alerts", ImGuiTableColumnFlags.WidthFixed, 250);
                ImGui.TableHeadersRow();

                int displayIndex = 0;

                foreach (var player in _playerHistoryEntries)
                {
                    ImGui.TableNextRow();

                    // Name (selectable, double-click to add to watchlist)
                    ImGui.TableNextColumn();
                    bool isSelected = (_selectedIndex == displayIndex);
                    if (ImGui.Selectable($"{player.Name ?? "(Unknown)"}##sel{displayIndex}", isSelected,
                        ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick))
                    {
                        _selectedIndex = displayIndex;

                        // Double-click to add to watchlist
                        if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        {
                            _playerToAdd = player;
                            _watchlistReason = string.Empty;
                            _showAddToWatchlistPopup = true;
                        }
                    }

                    // Account ID
                    ImGui.TableNextColumn();
                    ImGui.Text(player.AccountID ?? "-");

                    // Acct (UH, EOD, --)
                    ImGui.TableNextColumn();
                    ImGui.Text(player.Profile?.Acct ?? "--");

                    // Player Type
                    ImGui.TableNextColumn();
                    string typeStr = player.Type.ToString();
                    var typeColor = GetPlayerTypeColor(player.Type);
                    ImGui.TextColored(typeColor, typeStr);

                    // K/D Ratio
                    ImGui.TableNextColumn();
                    float? kd = player.Profile?.Overall_KD;
                    ImGui.Text(kd.HasValue ? kd.Value.ToString("F2") : "-");

                    // Total Hours
                    ImGui.TableNextColumn();
                    int? hours = player.Profile?.Hours;
                    ImGui.Text(hours.HasValue ? hours.Value.ToString() : "-");

                    // Raids
                    ImGui.TableNextColumn();
                    int? raids = player.Profile?.RaidCount;
                    ImGui.Text(raids.HasValue ? raids.Value.ToString() : "-");

                    // S/R %
                    ImGui.TableNextColumn();
                    float? sr = player.Profile?.SurvivedRate;
                    ImGui.Text(sr.HasValue ? sr.Value.ToString("F1") : "-");

                    // Group
                    ImGui.TableNextColumn();
                    int groupId = player.GroupID;
                    ImGui.Text(groupId >= 0 ? groupId.ToString() : "-");

                    // Alerts
                    ImGui.TableNextColumn();
                    string alerts = player.Profile?.Alerts;
                    if (!string.IsNullOrEmpty(alerts))
                    {
                        // Truncate display but show full text in tooltip
                        ImGui.TextColored(new Vector4(1f, 0.8f, 0.2f, 1f), alerts);
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip(alerts);
                        }
                    }
                    else
                    {
                        ImGui.Text("-");
                    }

                    displayIndex++;
                }

                ImGui.EndTable();
            }

            // Stats footer
            ImGui.Text($"Total: {_playerHistoryEntries.Count}");
        }

        private static Vector4 GetPlayerTypeColor(PlayerType type)
        {
            // Use configured colors from SKPaints (same as InfoWidget)
            var color = type switch
            {
                PlayerType.Teammate => SKPaints.TextTeammate.Color,
                PlayerType.PMC => SKPaints.TextPMC.Color,
                PlayerType.AIScav => SKPaints.TextScav.Color,
                PlayerType.PScav => SKPaints.TextPScav.Color,
                PlayerType.AIBoss => SKPaints.TextBoss.Color,
                PlayerType.AIRaider => SKPaints.TextRaider.Color,
                PlayerType.SpecialPlayer => SKPaints.TextWatchlist.Color,
                PlayerType.Streamer => SKPaints.TextStreamer.Color,
                _ => SKColors.White
            };
            return new Vector4(color.Red / 255f, color.Green / 255f, color.Blue / 255f, 1f);
        }
    }
}
