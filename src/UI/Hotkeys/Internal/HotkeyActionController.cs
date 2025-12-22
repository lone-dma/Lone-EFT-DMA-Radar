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

namespace LoneEftDmaRadar.UI.Hotkeys.Internal
{
    /// <summary>
    /// Wraps a Unity Hotkey/Event Delegate, and maintains it's State.
    /// *NOT* Thread Safe!
    /// Does not need to implement IDisposable (Timer) since this object will live for the lifetime
    /// of the application.
    /// </summary>
    public sealed class HotkeyActionController
    {
        private readonly HotkeyType _type;
        private readonly HotkeyDelegate _delegate;
        private readonly System.Timers.Timer _timer;
        private bool _state;

        /// <summary>
        /// Action Name used for lookup.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// GUI Thread/Window to execute delegate(s) on.
        /// </summary>
        private MainWindow Window { get; set; }

        private HotkeyActionController() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name of action.</param>
        /// <param name="type">Type of Hotkey activation.</param>
        /// <param name="delegate">Hotkey action delegate.</param>
        /// <param name="interval">Interval (ms) between Hotkey activations.</param>
        public HotkeyActionController(string name, HotkeyType type, HotkeyDelegate @delegate, double interval = 100)
        {
            Name = name;
            _type = type;
            _delegate = @delegate;
            if (type == HotkeyType.OnIntervalElapsed)
            {
                _timer = new()
                {
                    Interval = interval,
                    AutoReset = true
                };
                _timer.Elapsed += OnHotkeyIntervalElapsed;
            }
        }

        /// <summary>
        /// Execute the Action.
        /// </summary>
        /// <param name="isKeyDown">True if Hotkey is currently down.</param>
        public void Execute(bool isKeyDown)
        {
            Window ??= MainWindow.Instance; // Get Main Form Window if not set.
            if (Window is null)
                return; // No Window, cannot execute.
            bool keyDown = !_state && isKeyDown;
            bool keyUp = _state && !isKeyDown;
            if (keyDown || keyUp) // State has changed
            {
                _state = isKeyDown;
                switch (_type)
                {
                    case HotkeyType.OnKeyStateChanged:
                        Window?.Dispatcher.InvokeAsync(() =>
                        {
                            _delegate.Invoke(isKeyDown);
                        });
                        break;
                    case HotkeyType.OnIntervalElapsed:
                        if (isKeyDown) // Key Down
                        {
                            Window?.Dispatcher.InvokeAsync(() =>
                            {
                                _delegate.Invoke(true); // Initial Invoke
                            });
                            _timer.Start(); // Start Callback Timer
                        }
                        else // Key Up
                        {
                            _timer.Stop(); // Stop Timer (Resets to 0)
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Invokes 'HotkeyDelayElapsed' Event Delegate.
        /// </summary>
        private void OnHotkeyIntervalElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Window?.Dispatcher.InvokeAsync(() =>
            {
                _delegate.Invoke(true);
            });
        }

        public override string ToString() => Name;
    }
}
