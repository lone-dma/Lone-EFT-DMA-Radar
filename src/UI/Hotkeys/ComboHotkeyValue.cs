using eft_dma_radar.Unity;

namespace eft_dma_radar.UI.Hotkeys
{
    /// <summary>
    /// Combo Box Wrapper for UnityKeyCode Enums for Hotkey Manager.
    /// </summary>
    public sealed class ComboHotkeyValue
    {
        /// <summary>
        /// Full name of the Key.
        /// </summary>
        public string Key { get; }
        /// <summary>
        /// Key enum value.
        /// </summary>
        public UnityKeyCode Code { get; }

        public ComboHotkeyValue(UnityKeyCode keyCode)
        {
            Key = keyCode.ToString();
            Code = keyCode;
        }

        public override string ToString() => Key;
    }
}
