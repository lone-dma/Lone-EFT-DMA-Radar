namespace LoneEftDmaRadar.UI.Hotkeys
{
    /// <summary>
    /// Enumeration of Hotkey Types.
    /// </summary>
    public enum HotkeyType
    {
        /// <summary>
        /// Hotkey fires once when the key state changes (pressed or released).
        /// </summary>
        OnKeyStateChanged,
        /// <summary>
        /// Hotkey fires repeatedly at the specified interval while the key is held down.
        /// </summary>
        OnIntervalElapsed
    }
}
