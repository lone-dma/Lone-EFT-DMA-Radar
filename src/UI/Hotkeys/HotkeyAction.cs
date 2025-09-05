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
