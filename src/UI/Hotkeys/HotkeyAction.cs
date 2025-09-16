/*
 * EFT DMA Radar Lite
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

namespace EftDmaRadarLite.UI.Hotkeys
{
    /// <summary>
    /// Links a Unity Hotkey to it's Action Controller.
    /// Wrapper for GUI/Backend Interop.
    /// </summary>
    public sealed class HotkeyAction
    {
        /// <summary>
        /// Registered Hotkey Action Controllers (API Internal).
        /// </summary>
        internal static ConcurrentBag<HotkeyActionController> Controllers { get; } = new();
        /// <summary>
        /// Action Name used for lookup.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Action Controller to execute.
        /// </summary>
        private HotkeyActionController Action { get; set; }

        public HotkeyAction(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Register an action controller.
        /// </summary>
        /// <param name="controller">Controller to register.</param>
        internal static void RegisterController(HotkeyActionController controller)
        {
            Controllers.Add(controller);
        }

        /// <summary>
        /// Execute the Hotkey action controller.
        /// </summary>
        /// <param name="isKeyDown">True if the key is pressed.</param>
        public void Execute(bool isKeyDown)
        {
            Action ??= Controllers.FirstOrDefault(x => x.Name == Name);
            Action?.Execute(isKeyDown);
        }

        public override string ToString() => Name;
    }
}
