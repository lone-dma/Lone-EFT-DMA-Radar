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

        /// <summary>
        /// Whether the hotkey manager panel is open.
        /// </summary>
        public static bool IsOpen { get; set; }

        private static void Initialize()
        {
            if (_initialized) return;

            // Get all enum values in their original enum order (not sorted)
            _keyValues = Enum.GetValues<Win32VirtualKey>()
                .Where(k => (int)k != 0) // Exclude None/zero value
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
            if (!ImGui.Begin("Hotkey Manager", ref isOpen))
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
            ImGui.Text("Current Hotkey Bindings:");
            ImGui.Separator();

            DrawCurrentBindingsTable();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Add New Binding Section
            ImGui.Text("Add New Binding:");

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
                ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Key", ImGuiTableColumnFlags.WidthFixed, 120);
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
                        ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1f), "(Not Set)");
                    }

                    // Remove button
                    ImGui.TableNextColumn();
                    if (currentKey.HasValue)
                    {
                        ImGui.PushID($"remove_{controller.Name}");
                        if (ImGui.SmallButton("Remove"))
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
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "No actions registered yet.");
                return;
            }

            // Action dropdown
            ImGui.SetNextItemWidth(250);
            if (ImGui.Combo("Action", ref _selectedActionIndex, _actionNames, _actionNames.Length))
            {
                // Selection changed
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Select the action to bind");

            ImGui.SameLine();

            // Key dropdown
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo("Key", ref _selectedKeyIndex, _keyNames, _keyNames.Length))
            {
                // Selection changed
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Select the key to bind");

            ImGui.SameLine();

            // Add button
            bool canAdd = _selectedActionIndex >= 0 && _selectedActionIndex < _actionNames.Length &&
                          _selectedKeyIndex >= 0 && _selectedKeyIndex < _keyValues.Length;

            if (!canAdd)
                ImGui.BeginDisabled();

            if (ImGui.Button("Add"))
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
                ImGui.SetTooltip("Add the hotkey binding");

            if (!canAdd)
                ImGui.EndDisabled();

            // Help text
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "Tip: Select an action and key, then click Add to bind.");
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "Adding a binding will replace any existing binding for that action or key.");
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
