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
