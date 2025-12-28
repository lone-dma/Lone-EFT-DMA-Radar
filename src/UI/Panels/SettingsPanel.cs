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
using LoneEftDmaRadar.Misc.JSON;
using LoneEftDmaRadar.Tarkov;
using LoneEftDmaRadar.UI.ColorPicker;
using LoneEftDmaRadar.UI.Hotkeys;
using LoneEftDmaRadar.UI.Misc;
using LoneEftDmaRadar.UI.Skia;

namespace LoneEftDmaRadar.UI.Panels
{
    /// <summary>
    /// Settings Panel for the ImGui-based Radar.
    /// </summary>
    internal static class SettingsPanel
    {
        private static List<StaticContainerEntry> _containerEntries;

        // Panel-local state for tracking window open/close
        private static bool _isOpen;

        private static EftDmaConfig Config { get; } = Program.Config;

        /// <summary>
        /// Whether the settings panel is open.
        /// </summary>
        public static bool IsOpen
        {
            get => _isOpen;
            set => _isOpen = value;
        }

        /// <summary>
        /// Initialize the settings panel.
        /// </summary>
        public static void Initialize()
        {
            // Initialize container entries from TarkovDataManager
            _containerEntries = TarkovDataManager.AllContainers.Values
                .OrderBy(x => x.Name)
                .Select(x => new StaticContainerEntry(x))
                .ToList();

            // Apply UI scale from config at startup
            UpdateScaleValues(Config.UI.UIScale);
        }

        /// <summary>
        /// Draw the settings panel.
        /// </summary>
        public static void Draw()
        {
            bool isOpen = _isOpen;
            if (!ImGui.Begin("Settings", ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                _isOpen = isOpen;
                ImGui.End();
                return;
            }
            _isOpen = isOpen;

            if (ImGui.BeginTabBar("SettingsTabs"))
            {
                DrawGeneralTab();
                DrawPlayersTab();
                DrawLootTab();
                DrawContainersTab();
                DrawQuestHelperTab();
                DrawAboutTab();

                ImGui.EndTabBar();
            }

            ImGui.End();
        }

        private static void DrawGeneralTab()
        {
            if (ImGui.BeginTabItem("General"))
            {
                ImGui.SeparatorText("Tools");

                if (ImGui.Button("Hotkey Manager"))
                {
                    HotkeyManagerPanel.IsOpen = true;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Configure keyboard hotkeys for radar functions");
                ImGui.SameLine();
                if (ImGui.Button("Color Picker"))
                {
                    ColorPickerPanel.IsOpen = true;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Customize colors for players, loot, and UI elements");
                ImGui.SameLine();
                if (ImGui.Button("Map Setup Helper##btn"))
                {
                    MapSetupHelperPanel.IsOpen = true;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Adjust map calibration settings (X, Y, Scale)");

                ImGui.Separator();

                if (ImGui.Button("Restart Radar"))
                {
                    Memory.RestartRadar();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Restart the radar memory reader");
                ImGui.SameLine();
                if (ImGui.Button("Backup Config"))
                {
                    BackupConfig();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Create a backup of your current configuration");
                ImGui.SameLine();
                if (ImGui.Button("Open Config Folder"))
                {
                    OpenConfigFolder();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Open the folder containing configuration files");

                ImGui.SeparatorText("Display Settings");

                // UI Scale
                float uiScale = Config.UI.UIScale;
                if (ImGui.SliderFloat("UI Scale", ref uiScale, 0.5f, 2.0f, "%.1f"))
                {
                    Config.UI.UIScale = uiScale;
                    UpdateScaleValues(uiScale);
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Scale UI elements (text, icons, widgets)");

                // Zoom
                int zoom = Config.UI.Zoom;
                if (ImGui.SliderInt("Zoom (F1/F2)", ref zoom, 1, 200))
                {
                    Config.UI.Zoom = zoom;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Map zoom level (lower = more zoomed in)");

                // Aimline Length
                int aimlineLength = Config.UI.AimLineLength;
                if (ImGui.SliderInt("Aimline Length", ref aimlineLength, 0, 1500))
                {
                    Config.UI.AimLineLength = aimlineLength;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Length of player aim direction lines");

                // Max Distance (snaps to nearest 25)
                int maxDistanceRaw = (int)Config.UI.MaxDistance;
                int maxDistance = (int)(MathF.Round(maxDistanceRaw / 25f) * 25);
                if (ImGui.SliderInt("Max Distance", ref maxDistance, 50, 1500, "%d"))
                {
                    maxDistance = (int)(MathF.Round(maxDistance / 25f) * 25);
                    maxDistance = Math.Clamp(maxDistance, 50, 1500);
                    Config.UI.MaxDistance = maxDistance;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Maximum distance to render targets in aimview");

                ImGui.SeparatorText("Widgets");

                bool aimviewWidget = Config.AimviewWidget.Enabled;
                if (ImGui.Checkbox("Aimview Widget", ref aimviewWidget))
                {
                    Config.AimviewWidget.Enabled = aimviewWidget;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("3D view showing players in your field of view");

                bool infoWidget = Config.InfoWidget.Enabled;
                if (ImGui.Checkbox("Player Info Widget", ref infoWidget))
                {
                    Config.InfoWidget.Enabled = infoWidget;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Displays a list of nearby players with details");

                ImGui.SeparatorText("Visibility");

                bool showExfils = Config.UI.ShowExfils;
                if (ImGui.Checkbox("Show Exfils", ref showExfils))
                {
                    Config.UI.ShowExfils = showExfils;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Show extraction points on the map");

                bool showHazards = Config.UI.ShowHazards;
                if (ImGui.Checkbox("Show Hazards", ref showHazards))
                {
                    Config.UI.ShowHazards = showHazards;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Show mines, sniper zones, and other hazards");

                ImGui.EndTabItem();
            }
        }

        private static void DrawPlayersTab()
        {
            if (ImGui.BeginTabItem("Players"))
            {
                ImGui.SeparatorText("Player Display");

                bool teammateAimlines = Config.UI.TeammateAimlines;
                if (ImGui.Checkbox("Teammate Aimlines", ref teammateAimlines))
                {
                    Config.UI.TeammateAimlines = teammateAimlines;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Show aim direction lines for teammates");

                bool aiAimlines = Config.UI.AIAimlines;
                if (ImGui.Checkbox("AI Aimlines", ref aiAimlines))
                {
                    Config.UI.AIAimlines = aiAimlines;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Show dynamic aim lines for AI players");

                ImGui.EndTabItem();
            }
        }

        private static void DrawLootTab()
        {
            if (ImGui.BeginTabItem("Loot"))
            {
                ImGui.SeparatorText("Loot Settings");

                bool lootEnabled = Config.Loot.Enabled;
                if (ImGui.Checkbox("Show Loot (F3)", ref lootEnabled))
                {
                    Config.Loot.Enabled = lootEnabled;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Toggle loot display on the radar");

                if (!lootEnabled)
                {
                    ImGui.BeginDisabled();
                }

                bool showWishlist = Config.Loot.ShowWishlist;
                if (ImGui.Checkbox("Show Wishlist Items", ref showWishlist))
                {
                    Config.Loot.ShowWishlist = showWishlist;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Highlight items from your Tarkov wishlist");

                if (!lootEnabled)
                {
                    ImGui.EndDisabled();
                }

                ImGui.EndTabItem();
            }
        }

        private static void DrawContainersTab()
        {
            if (ImGui.BeginTabItem("Containers"))
            {
                bool containersEnabled = Config.Containers.Enabled;
                if (ImGui.Checkbox("Show Containers", ref containersEnabled))
                {
                    Config.Containers.Enabled = containersEnabled;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Show lootable containers on the radar");

                if (!containersEnabled)
                {
                    ImGui.BeginDisabled();
                }

                float drawDistance = Config.Containers.DrawDistance;
                if (ImGui.SliderFloat("Draw Distance", ref drawDistance, 10, 500))
                {
                    Config.Containers.DrawDistance = drawDistance;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Maximum distance to show containers");

                bool selectAll = Config.Containers.SelectAll;
                if (ImGui.Checkbox("Select All", ref selectAll))
                {
                    Config.Containers.SelectAll = selectAll;
                    if (_containerEntries is not null)
                    {
                        foreach (var entry in _containerEntries)
                        {
                            entry.IsTracked = selectAll;
                        }
                    }
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Toggle all container types");

                ImGui.SeparatorText("Container Types");

                if (_containerEntries is not null)
                {
                    ImGui.BeginChild("ContainerList", new Vector2(0, 200), ImGuiChildFlags.Borders);
                    foreach (var entry in _containerEntries)
                    {
                        ImGui.PushID(entry.Id);
                        bool isTracked = entry.IsTracked;
                        if (ImGui.Checkbox(entry.Name, ref isTracked))
                        {
                            entry.IsTracked = isTracked;
                        }
                        ImGui.PopID();
                    }
                    ImGui.EndChild();
                }

                if (!containersEnabled)
                {
                    ImGui.EndDisabled();
                }

                ImGui.EndTabItem();
            }
        }

        private static void DrawQuestHelperTab()
        {
            if (ImGui.BeginTabItem("Quest Helper"))
            {
                bool questHelperEnabled = Config.QuestHelper.Enabled;
                if (ImGui.Checkbox("Enable Quest Helper", ref questHelperEnabled))
                {
                    Config.QuestHelper.Enabled = questHelperEnabled;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Show quest objectives and items on the radar");

                ImGui.SeparatorText("Active Quests");

                if (Memory.QuestManager?.Quests is IReadOnlyDictionary<string, Tarkov.GameWorld.Quests.QuestEntry> quests)
                {
                    ImGui.BeginChild("QuestList", new Vector2(0, 200), ImGuiChildFlags.Borders);
                    foreach (var quest in quests.Values.OrderBy(x => x.Name))
                    {
                        ImGui.PushID(quest.Id);
                        bool isBlacklisted = Config.QuestHelper.BlacklistedQuests.ContainsKey(quest.Id);
                        bool showQuest = !isBlacklisted;
                        if (ImGui.Checkbox(quest.Name ?? quest.Id, ref showQuest))
                        {
                            if (showQuest)
                                Config.QuestHelper.BlacklistedQuests.TryRemove(quest.Id, out _);
                            else
                                Config.QuestHelper.BlacklistedQuests.TryAdd(quest.Id, 0);
                        }
                        ImGui.PopID();
                    }
                    ImGui.EndChild();
                }
                else
                {
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "No active quests (not in raid)");
                }

                ImGui.EndTabItem();
            }
        }

        private static void DrawAboutTab()
        {
            if (ImGui.BeginTabItem("About"))
            {
                ImGui.Text(Program.Name);
                ImGui.Separator();
                ImGui.TextWrapped("A DMA-based radar for Escape From Tarkov.");

                ImGui.Spacing();
                if (ImGui.Button("Visit Website"))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo("https://lone-dma.org/") { UseShellExecute = true });
                    }
                    catch { }
                }

                ImGui.EndTabItem();
            }
        }

        #region Helper Methods

        private static void BackupConfig()
        {
            try
            {
                var backupFile = Path.Combine(Program.ConfigPath.FullName, $"{EftDmaConfig.Filename}.userbak");
                File.WriteAllText(backupFile, JsonSerializer.Serialize(Program.Config, AppJsonContext.Default.EftDmaConfig));
                MessageBox.Show(RadarWindow.Handle, $"Backed up to {backupFile}", "Backup Config", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(RadarWindow.Handle, $"Error: {ex.Message}", "Backup Config", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void OpenConfigFolder()
        {
            try
            {
                Process.Start(new ProcessStartInfo(Program.ConfigPath.FullName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(RadarWindow.Handle, $"Error: {ex.Message}", "Open Config", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void UpdateScaleValues(float newScale)
        {
            // Update Paints
            SKPaints.TextOutline.StrokeWidth = 2f * newScale;
            SKPaints.PaintLocalPlayer.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintTeammate.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintPMC.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintScav.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintRaider.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintBoss.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintFocused.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintPScav.StrokeWidth = 1.66f * newScale;
            SKPaints.PaintCorpse.StrokeWidth = 0.25f * newScale;
            SKPaints.PaintMeds.StrokeWidth = 0.25f * newScale;
            SKPaints.PaintFood.StrokeWidth = 0.25f * newScale;
            SKPaints.PaintBackpacks.StrokeWidth = 0.25f * newScale;
            SKPaints.PaintDeathMarker.StrokeWidth = 3f * newScale;
            SKPaints.PaintLoot.StrokeWidth = 0.25f * newScale;
            SKPaints.PaintImportantLoot.StrokeWidth = 0.25f * newScale;
            SKPaints.PaintContainerLoot.StrokeWidth = 0.25f * newScale;
            SKPaints.PaintTransparentBacker.StrokeWidth = 0.25f * newScale;
            SKPaints.PaintExplosives.StrokeWidth = 3f * newScale;
            SKPaints.PaintExfil.StrokeWidth = 0.25f * newScale;
            SKPaints.PaintExfilTransit.StrokeWidth = 0.25f * newScale;
            SKPaints.PaintQuestZone.StrokeWidth = 0.25f * newScale;
            SKPaints.PaintQuestItem.StrokeWidth = 0.25f * newScale;
            SKPaints.PaintWishlistItem.StrokeWidth = 0.25f * newScale;

            // Fonts
            SKFonts.UIRegular.Size = 12f * newScale;
            SKFonts.UILarge.Size = 48f * newScale;
        }

        #endregion
    }
}
