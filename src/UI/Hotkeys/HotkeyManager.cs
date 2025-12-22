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
