/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
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
            bool keyDown = !_state && isKeyDown;
            bool keyUp = _state && !isKeyDown;
            if (keyDown || keyUp) // State has changed
            {
                _state = isKeyDown;
                switch (_type)
                {
                    case HotkeyType.OnKeyStateChanged:
                        _delegate.Invoke(isKeyDown);
                        break;
                    case HotkeyType.OnIntervalElapsed:
                        if (isKeyDown) // Key Down
                        {
                            _delegate.Invoke(true); // Initial Invoke
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
            _delegate.Invoke(true);
        }

        public override string ToString() => Name;
    }
}

