/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using LoneEftDmaRadar.UI.Hotkeys.Internal;
using VmmSharpEx.Extensions.Input;

namespace LoneEftDmaRadar.UI.Hotkeys
{
    /// <summary>
    /// Static hotkey manager for the ImGui-based radar.
    /// </summary>
    public static class HotkeyManager
    {
        private static readonly ConcurrentDictionary<Win32VirtualKey, HotkeyAction> _hotkeys = new();

        /// <summary>
        /// The live set of hotkeys (key ? action)
        /// </summary>
        public static IReadOnlyDictionary<Win32VirtualKey, HotkeyAction> Hotkeys => _hotkeys;

        /// <summary>
        /// All available action controllers.
        /// </summary>
        public static IEnumerable<HotkeyActionController> Controllers => HotkeyAction.RegisteredControllers;

        /// <summary>
        /// All possible virtual keys.
        /// </summary>
        public static IReadOnlyList<Win32VirtualKey> AllKeys { get; } =
            Enum.GetValues<Win32VirtualKey>().ToList();

        static HotkeyManager()
        {
            // Load hotkeys from config
            foreach (var kvp in Program.Config.Hotkeys)
            {
                var action = new HotkeyAction(kvp.Value);
                _hotkeys.TryAdd(kvp.Key, action);
            }
        }

        /// <summary>
        /// Add a new hotkey binding.
        /// </summary>
        /// <param name="key">Virtual key code.</param>
        /// <param name="actionName">Name of the action controller.</param>
        /// <returns>True if added successfully, false if key already exists.</returns>
        public static bool AddHotkey(Win32VirtualKey key, string actionName)
        {
            var action = new HotkeyAction(actionName);
            if (_hotkeys.TryAdd(key, action))
            {
                Program.Config.Hotkeys[key] = actionName;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove a hotkey binding.
        /// </summary>
        /// <param name="key">Virtual key code to remove.</param>
        /// <returns>True if removed successfully.</returns>
        public static bool RemoveHotkey(Win32VirtualKey key)
        {
            if (_hotkeys.TryRemove(key, out _))
            {
                Program.Config.Hotkeys.TryRemove(key, out _);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get the action name for a given key.
        /// </summary>
        /// <param name="key">Virtual key code.</param>
        /// <returns>Action name or null if not found.</returns>
        public static string GetActionName(Win32VirtualKey key)
        {
            return _hotkeys.TryGetValue(key, out var action) ? action.Name : null;
        }
    }
}

