/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using ImGuiNET;
using LoneEftDmaRadar.Misc.JSON;
using LoneEftDmaRadar.Tarkov;
using LoneEftDmaRadar.UI.ColorPicker;
using LoneEftDmaRadar.UI.Hotkeys;
using LoneEftDmaRadar.UI.Misc;

namespace LoneEftDmaRadar.UI.Panels
{
    /// <summary>
    /// Settings Panel for the ImGui-based Radar.
    /// </summary>
    internal static class SettingsPanel
    {
        private static List<StaticContainerEntry> _containerEntries;

        private static float _pendingRadarScale;
        private static float _pendingMenuScale;
        private static bool _pendingScalesInitialized;

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
        }

        /// <summary>
        /// Draw the settings panel.
        /// </summary>
        public static void Draw()
        {
            bool isOpen = _isOpen;
            if (!ImGui.Begin("设置", ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
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
            if (ImGui.BeginTabItem("常规"))
            {
                ImGui.SeparatorText("工具");

                if (ImGui.Button("热键管理"))
                {
                    HotkeyManagerPanel.IsOpen = true;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("配置雷达功能的键盘热键");
                ImGui.SameLine();
                if (ImGui.Button("颜色选择"))
                {
                    ColorPickerPanel.IsOpen = true;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("自定义玩家、物品和UI元素的颜色");
                ImGui.SameLine();
                if (ImGui.Button("地图设置助手##btn"))
                {
                    MapSetupHelperPanel.IsOpen = true;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("调整地图校准设置 (X, Y, 缩放)");

                ImGui.Separator();

                if (ImGui.Button("重启雷达"))
                {
                    Memory.Game?.Restart();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("重启雷达内存读取器");
                ImGui.SameLine();
                if (ImGui.Button("备份配置"))
                {
                    BackupConfig();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("创建当前配置的备份");
                ImGui.SameLine();
                if (ImGui.Button("打开配置文件夹"))
                {
                    OpenConfigFolder();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("打开包含配置文件的文件夹");

                ImGui.SeparatorText("显示设置");

                // Initialize pending scales
                if (!_pendingScalesInitialized)
                {
                    _pendingRadarScale = Config.UI.RadarScale;
                    _pendingMenuScale = Config.UI.MenuScale;
                    _pendingScalesInitialized = true;
                }

                // Radar Scale
                ImGui.SliderFloat("雷达缩放", ref _pendingRadarScale, 0.5f, 2.0f, "%.1f");
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("缩放雷达地图和瞄准视图");

                bool radarScaleDirty = MathF.Abs(_pendingRadarScale - Config.UI.RadarScale) > 0.0001f;

                ImGui.SameLine();
                if (!radarScaleDirty)
                    ImGui.BeginDisabled();
                if (ImGui.Button("应用##RadarScale"))
                {
                    Config.UI.RadarScale = _pendingRadarScale;
                }
                if (!radarScaleDirty)
                    ImGui.EndDisabled();

                // Menu Scale
                ImGui.SliderFloat("菜单缩放", ref _pendingMenuScale, 0.5f, 2.0f, "%.1f");
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("缩放菜单和窗口");

                bool menuScaleDirty = MathF.Abs(_pendingMenuScale - Config.UI.MenuScale) > 0.0001f;

                ImGui.SameLine();
                if (!menuScaleDirty)
                    ImGui.BeginDisabled();
                if (ImGui.Button("应用##MenuScale"))
                {
                    Config.UI.MenuScale = _pendingMenuScale;
                    RadarWindow.ApplyCustomImGuiStyle(); // Refresh ImGui style with new scale
                }
                if (!menuScaleDirty)
                    ImGui.EndDisabled();

                // Zoom
                int zoom = Config.UI.Zoom;
                if (ImGui.SliderInt("缩放 (F1/F2)", ref zoom, 1, 200))
                {
                    Config.UI.Zoom = zoom;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("地图缩放级别 (越小越放大)");

                // Aimline Length
                int aimlineLength = Config.UI.AimLineLength;
                if (ImGui.SliderInt("瞄准线长度", ref aimlineLength, 0, 1500))
                {
                    Config.UI.AimLineLength = aimlineLength;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("玩家瞄准方向线的长度");

                // Max Distance (snaps to nearest 25)
                int maxDistanceRaw = (int)Config.UI.MaxDistance;
                int maxDistance = (int)(MathF.Round(maxDistanceRaw / 25f) * 25);
                if (ImGui.SliderInt("最大距离", ref maxDistance, 50, 1500, "%d"))
                {
                    maxDistance = (int)(MathF.Round(maxDistance / 25f) * 25);
                    maxDistance = Math.Clamp(maxDistance, 50, 1500);
                    Config.UI.MaxDistance = maxDistance;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("瞄准视图中渲染目标的最大距离");

                ImGui.SeparatorText("小部件");

                bool aimviewWidget = Config.AimviewWidget.Enabled;
                if (ImGui.Checkbox("瞄准视图", ref aimviewWidget))
                {
                    Config.AimviewWidget.Enabled = aimviewWidget;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("显示视野内玩家的3D视图");

                bool infoWidget = Config.InfoWidget.Enabled;
                if (ImGui.Checkbox("玩家信息", ref infoWidget))
                {
                    Config.InfoWidget.Enabled = infoWidget;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("显示附近玩家的详细列表");

                bool lootWidget = Config.LootWidget.Enabled;
                if (ImGui.Checkbox("物品列表", ref lootWidget))
                {
                    Config.LootWidget.Enabled = lootWidget;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("显示可排序的物品列表");

                ImGui.SeparatorText("可见性");

                bool showExfils = Config.UI.ShowExfils;
                if (ImGui.Checkbox("显示撤离点", ref showExfils))
                {
                    Config.UI.ShowExfils = showExfils;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("在地图上显示撤离点");

                bool showHazards = Config.UI.ShowHazards;
                if (ImGui.Checkbox("显示危险区域", ref showHazards))
                {
                    Config.UI.ShowHazards = showHazards;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("显示地雷、狙击区和其他危险区域");

                ImGui.EndTabItem();
            }
        }

        private static void DrawPlayersTab()
        {
            if (ImGui.BeginTabItem("玩家"))
            {
                ImGui.SeparatorText("玩家显示");

                bool teammateAimlines = Config.UI.TeammateAimlines;
                if (ImGui.Checkbox("队友瞄准线", ref teammateAimlines))
                {
                    Config.UI.TeammateAimlines = teammateAimlines;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("显示队友的瞄准方向线");

                bool aiAimlines = Config.UI.AIAimlines;
                if (ImGui.Checkbox("AI瞄准线", ref aiAimlines))
                {
                    Config.UI.AIAimlines = aiAimlines;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("显示AI玩家的动态瞄准线");

                bool connectGroups = Program.Config.UI.ConnectGroups;
                if (ImGui.Checkbox("连接小队", ref connectGroups))
                {
                    Program.Config.UI.ConnectGroups = connectGroups;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("在小队玩家之间画线");

                ImGui.SeparatorText("其他");

                bool autoGroups = Config.Misc.AutoGroups;
                if (ImGui.Checkbox("自动分组", ref autoGroups))
                {
                    Config.Misc.AutoGroups = autoGroups;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("尽力尝试：根据距离在战局开始前自动推断小队");

                ImGui.EndTabItem();
            }
        }

        private static void DrawLootTab()
        {
            if (ImGui.BeginTabItem("物品"))
            {
                ImGui.SeparatorText("物品设置");

                bool lootEnabled = Config.Loot.Enabled;
                if (ImGui.Checkbox("显示物品 (F3)", ref lootEnabled))
                {
                    Config.Loot.Enabled = lootEnabled;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("在雷达上显示/隐藏物品");

                if (!lootEnabled)
                {
                    ImGui.BeginDisabled();
                }

                bool showWishlist = Config.Loot.ShowWishlist;
                if (ImGui.Checkbox("显示愿望清单物品", ref showWishlist))
                {
                    Config.Loot.ShowWishlist = showWishlist;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("高亮显示塔科夫愿望清单中的物品");

                if (!lootEnabled)
                {
                    ImGui.EndDisabled();
                }

                ImGui.EndTabItem();
            }
        }

        private static void DrawContainersTab()
        {
            if (ImGui.BeginTabItem("容器"))
            {
                bool containersEnabled = Config.Containers.Enabled;
                if (ImGui.Checkbox("显示容器", ref containersEnabled))
                {
                    Config.Containers.Enabled = containersEnabled;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("在雷达上显示可搜刮的容器");

                if (!containersEnabled)
                {
                    ImGui.BeginDisabled();
                }

                float drawDistance = Config.Containers.DrawDistance;
                if (ImGui.SliderFloat("显示距离", ref drawDistance, 10, 500))
                {
                    Config.Containers.DrawDistance = drawDistance;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("显示容器的最大距离");

                bool selectAll = Config.Containers.SelectAll;
                if (ImGui.Checkbox("全选", ref selectAll))
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
                    ImGui.SetTooltip("切换所有容器类型");

                ImGui.SeparatorText("容器类型");

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
            if (ImGui.BeginTabItem("任务助手"))
            {
                bool questHelperEnabled = Config.QuestHelper.Enabled;
                if (ImGui.Checkbox("启用任务助手", ref questHelperEnabled))
                {
                    Config.QuestHelper.Enabled = questHelperEnabled;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("在雷达上显示任务目标和物品");

                ImGui.SeparatorText("当前任务");

                if (Memory.QuestManager?.Quests is IReadOnlyDictionary<string, Tarkov.World.Quests.QuestEntry> quests)
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
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "没有活跃任务 (不在战局中)");
                }

                ImGui.EndTabItem();
            }
        }

        private static void DrawAboutTab()
        {
            if (ImGui.BeginTabItem("关于"))
            {
                ImGui.Text(Program.Name);
                ImGui.Separator();
                ImGui.TextWrapped("基于DMA的逃离塔科夫雷达工具。");

                ImGui.Spacing();
                if (ImGui.Button("访问网站"))
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
                MessageBox.Show(RadarWindow.Handle, $"已备份到 {backupFile}", "备份配置", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(RadarWindow.Handle, $"错误: {ex.Message}", "备份配置", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(RadarWindow.Handle, $"错误: {ex.Message}", "打开配置", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}

