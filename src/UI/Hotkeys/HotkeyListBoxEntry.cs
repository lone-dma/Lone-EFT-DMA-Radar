using EftDmaRadarLite.Unity;

namespace EftDmaRadarLite.UI.Hotkeys
{
    /// <summary>
    /// ListBox wrapper for Hotkey/Action Entries in Hotkey Manager.
    /// </summary>
    public sealed class HotkeyListBoxEntry
    {
        private readonly string _name;
        /// <summary>
        /// Hotkey Key Value.
        /// </summary>
        public UnityKeyCode Hotkey { get; }
        /// <summary>
        /// Hotkey Action Object that contains state/delegate.
        /// </summary>
        public HotkeyAction Action { get; }

        public HotkeyListBoxEntry(UnityKeyCode hotkey, HotkeyAction action)
        {
            Hotkey = hotkey;
            Action = action;
            _name = hotkey.ToString();
        }

        public override string ToString() => $"{Action.Name} == {_name}";
    }
}
