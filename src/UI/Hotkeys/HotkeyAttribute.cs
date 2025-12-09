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

namespace LoneEftDmaRadar.UI.Hotkeys
{
    /// <summary>
    /// Used to decorate methods as Hotkey action handlers.
    /// </summary>
    /// <remarks>
    /// Methods decorated with this attribute should match the method signature of <see cref="HotkeyDelegate"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class HotkeyAttribute : Attribute
    {
        /// <summary>
        /// Name of the Hotkey to be displayed to the User.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Type of Hotkey activation. Default: OnKeyStateChanged
        /// </summary>
        public HotkeyType Type { get; } = HotkeyType.OnKeyStateChanged;
        /// <summary>
        /// Interval (ms) between Hotkey activations. Default: 100ms
        /// </summary>
        public double Interval { get; } = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="HotkeyAttribute"/> class.
        /// </summary>
        /// <param name="name">Name of the Hotkey to be displayed to the User.</param>
        /// <param name="type">Type of Hotkey activation. Default: OnKeyStateChanged</param>
        /// <param name="interval">Interval (ms) between Hotkey activations. Default: 100ms</param>
        public HotkeyAttribute(string name, HotkeyType type = HotkeyType.OnKeyStateChanged, double interval = 100)
        {
            Name = name;
            Type = type;
            Interval = interval;
        }
    }
}
