/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using ImGuiNET;
using LoneEftDmaRadar.UI.Hotkeys.Internal;
using VmmSharpEx.Extensions.Input;

namespace LoneEftDmaRadar.UI.Hotkeys
{
    /// <summary>
    /// Hotkey Manager Panel for the ImGui-based Radar.
    /// Allows viewing, adding, and removing hotkey bindings.
    /// </summary>
    internal static class HotkeyManagerPanel
    {
        // Panel-local state
        private static int _selectedActionIndex = -1;
        private static int _selectedKeyIndex = -1;
        private static string[] _actionNames;
        private static string[] _keyNames;
        private static Win32VirtualKey[] _keyValues;
        private static Win32VirtualKey? _keyToRemove;
        private static bool _initialized;

        private static EftDmaConfig Config { get; } = Program.Config;

        /// <summary>
        /// Whether the hotkey manager panel is open.
        /// </summary>
        public static bool IsOpen { get; set; }

        private static void Initialize()
        {
            if (_initialized) return;

            // Get all enum values in their original enum order (not sorted)
            _keyValues = Enum.GetValues<Win32VirtualKey>()
                .Where(k => (int)k != 0) // Exclude Error/zero value
                .ToArray();

            // Use the enum name directly as the display name
            _keyNames = _keyValues.Select(k => k.ToString()).ToArray();
            _initialized = true;
        }

        private static void RefreshActionNames()
        {
            _actionNames = HotkeyAction.RegisteredControllers
                .OrderBy(x => x.Name)
                .Select(x => x.Name)
                .ToArray();
        }

        private static string FormatKeyName(Win32VirtualKey key)
        {
            // Use the enum name directly
            return key.ToString();
        }

        /// <summary>
        /// Draw the hotkey manager panel.
        /// </summary>
        public static void Draw()
        {
            Initialize();

            bool isOpen = IsOpen;

            ImGui.SetNextWindowSize(new Vector2(550, 450), ImGuiCond.FirstUseEver);
            if (!ImGui.Begin("热键管理", ref isOpen))
            {
                IsOpen = isOpen;
                ImGui.End();
                return;
            }

            IsOpen = isOpen;

            // Refresh action names if needed
            if (_actionNames is null || _actionNames.Length == 0)
            {
                RefreshActionNames();
            }

            // Current Bindings Section
            ImGui.Text("当前热键绑定:");
            ImGui.Separator();

            DrawCurrentBindingsTable();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Add New Binding Section
            ImGui.Text("添加新绑定:");

            DrawAddBindingSection();

            ImGui.End();

            // Handle deferred removal
            if (_keyToRemove.HasValue)
            {
                HotkeyManager.RemoveHotkey(_keyToRemove.Value);
                _keyToRemove = null;
            }
        }

        private static void DrawCurrentBindingsTable()
        {
            if (ImGui.BeginTable("HotkeysTable", 3,
                ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg,
                new Vector2(0, 250)))
            {
                ImGui.TableSetupColumn("功能", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("按键", ImGuiTableColumnFlags.WidthFixed, 120);
                ImGui.TableSetupColumn("##Remove", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableHeadersRow();

                // Show all registered controllers with their bindings
                foreach (var controller in HotkeyAction.RegisteredControllers.OrderBy(x => x.Name))
                {
                    ImGui.TableNextRow();

                    // Action name
                    ImGui.TableNextColumn();
                    ImGui.Text(controller.Name);

                    // Current hotkey binding
                    ImGui.TableNextColumn();
                    var currentKey = GetCurrentHotkeyKey(controller.Name);
                    if (currentKey.HasValue)
                    {
                        ImGui.TextColored(new Vector4(0.4f, 0.8f, 0.4f, 1f), FormatKeyName(currentKey.Value));
                    }
                    else
                    {
                        ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "(未设置)");
                    }

                    // Remove button
                    ImGui.TableNextColumn();
                    if (currentKey.HasValue)
                    {
                        ImGui.PushID($"remove_{controller.Name}");
                        if (ImGui.SmallButton("移除"))
                        {
                            _keyToRemove = currentKey.Value;
                        }
                        ImGui.PopID();
                    }
                }

                ImGui.EndTable();
            }
        }

        private static void DrawAddBindingSection()
        {
            if (_actionNames is null || _actionNames.Length == 0)
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "尚未注册任何功能。");
                return;
            }

            // Action dropdown
            ImGui.SetNextItemWidth(250);
            if (ImGui.Combo("功能", ref _selectedActionIndex, _actionNames, _actionNames.Length))
            {
                // Selection changed
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("选择要绑定的功能");

            ImGui.SameLine();

            // Key dropdown
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo("按键", ref _selectedKeyIndex, _keyNames, _keyNames.Length))
            {
                // Selection changed
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("选择要绑定的按键");

            ImGui.SameLine();

            // Add button
            bool canAdd = _selectedActionIndex >= 0 && _selectedActionIndex < _actionNames.Length &&
                          _selectedKeyIndex >= 0 && _selectedKeyIndex < _keyValues.Length;

            if (!canAdd)
                ImGui.BeginDisabled();

            if (ImGui.Button("添加"))
            {
                if (canAdd)
                {
                    var actionName = _actionNames[_selectedActionIndex];
                    var key = _keyValues[_selectedKeyIndex];

                    // Check if key is already bound
                    if (HotkeyManager.Hotkeys.ContainsKey(key))
                    {
                        // Remove existing binding for this key
                        HotkeyManager.RemoveHotkey(key);
                    }

                    // Also remove any existing binding for this action
                    var existingKey = GetCurrentHotkeyKey(actionName);
                    if (existingKey.HasValue)
                    {
                        HotkeyManager.RemoveHotkey(existingKey.Value);
                    }

                    // Add new binding
                    HotkeyManager.AddHotkey(key, actionName);

                    // Reset selection
                    _selectedActionIndex = -1;
                    _selectedKeyIndex = -1;
                }
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("添加热键绑定");

            if (!canAdd)
                ImGui.EndDisabled();

            // Help text
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "提示: 选择一个功能和按键，然后点击添加进行绑定。");
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "添加绑定将替换该功能或按键的现有绑定。");
        }

        private static Win32VirtualKey? GetCurrentHotkeyKey(string actionName)
        {
            foreach (var kvp in HotkeyManager.Hotkeys)
            {
                if (string.Equals(kvp.Value.Name, actionName, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Key;
                }
            }
            return null;
        }
    }
}

