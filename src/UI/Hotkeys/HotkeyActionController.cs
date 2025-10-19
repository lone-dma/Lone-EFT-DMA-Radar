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
    /// Wraps a Unity Hotkey/Event Delegate, and maintains it's State.
    /// *NOT* Thread Safe!
    /// Does not need to implement IDisposable (Timer) since this object will live for the lifetime
    /// of the application.
    /// </summary>
    public sealed class HotkeyActionController
    {
        /// <summary>
        /// Action Name used for lookup.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Delay (ms) between 'HotkeyDelayElapsed' Event Firing.
        /// Default: 100ms
        /// </summary>
        public double Delay
        {
            get => _timer.Interval;
            set => _timer.Interval = value;
        }
        /// <summary>
        /// GUI Thread/Window to execute delegate(s) on.
        /// </summary>
        private MainWindow Window { get; set; }
        /// <summary>
        /// Event Occurs when associated Hotkey changes state.
        /// </summary>
        public event EventHandler<HotkeyEventArgs> HotkeyStateChanged;
        /// <summary>
        /// Event Occurs during Initial 'Key Down', and repeats while key is down.
        /// Be sure to set the 'Delay' Property (Default: 100ms).
        /// </summary>
        public event EventHandler HotkeyDelayElapsed;

        private readonly System.Timers.Timer _timer;
        private bool _state;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name of action.</param>
        /// Required for OnHotkeyDelay.</param>
        public HotkeyActionController(string name)
        {
            Name = name;
            _timer = new()
            {
                Interval = 100,
                AutoReset = true
            };
            _timer.Elapsed += OnHotkeyDelayElapsed;
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
                UpdateState(keyDown);
                if (HotkeyStateChanged is not null) // Invoke Event if Set.
                    OnHotkeyStateChanged(new HotkeyEventArgs(keyDown));
            }
        }

        /// <summary>
        /// Executed whenever a Hotkey Changes State.
        /// Updates the Internal 'State' of this controller and it's Events.
        /// </summary>
        /// <param name="newState">New State of the Hotkey.
        /// True: Key is down.
        /// False: Key is up.</param>
        private void UpdateState(bool newState)
        {
            _state = newState;
            if (HotkeyDelayElapsed is not null) // Set 'HotkeyDelayElapsed' State
            {
                if (newState) // Key Down
                {
                    Window?.Dispatcher.InvokeAsync(() =>
                    {
                        HotkeyDelayElapsed?.Invoke(this, EventArgs.Empty); // Invoke Delay Event on Initial Keydown
                    });
                    _timer.Start(); // Start Callback Timer
                }
                else // Key Up
                    _timer.Stop(); // Stop Timer (Resets to 0)
            }
        }

        /// <summary>
        /// Invokes 'HotkeyStateChanged' Event Delegate.
        /// </summary>
        private void OnHotkeyStateChanged(HotkeyEventArgs e)
        {
            Window?.Dispatcher.InvokeAsync(() =>
            {
                HotkeyStateChanged?.Invoke(this, e);
            });
        }

        /// <summary>
        /// Invokes 'HotkeyDelayElapsed' Event Delegate.
        /// </summary>
        private void OnHotkeyDelayElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Window?.Dispatcher.InvokeAsync(() =>
            {
                HotkeyDelayElapsed?.Invoke(this, EventArgs.Empty);
            });
        }

        public override string ToString() => Name;
    }
}
