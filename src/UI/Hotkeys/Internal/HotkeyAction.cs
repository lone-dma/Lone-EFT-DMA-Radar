/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
namespace LoneEftDmaRadar.UI.Hotkeys.Internal
{
    /// <summary>
    /// Links a Unity Hotkey to it's Action Controller.
    /// Wrapper for GUI/Backend Interop.
    /// </summary>
    public sealed class HotkeyAction
    {
        private static readonly ConcurrentDictionary<string, HotkeyActionController> _controllers =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Registered Hotkey Action Controllers.
        /// </summary>
        public static IEnumerable<HotkeyActionController> RegisteredControllers => _controllers.Values;

        /// <summary>
        /// Action Name used for lookup.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Action Controller to execute.
        /// </summary>
        private HotkeyActionController _action;

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
            _controllers.TryAdd(controller.Name, controller);
        }

        /// <summary>
        /// Execute the Hotkey action controller.
        /// </summary>
        /// <param name="isKeyDown">True if the key is pressed.</param>
        public void Execute(bool isKeyDown)
        {
            _action ??= _controllers.GetValueOrDefault(Name);
            _action?.Execute(isKeyDown);
        }

        public override string ToString() => Name;
    }
}

