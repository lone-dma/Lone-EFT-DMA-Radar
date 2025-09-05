namespace EftDmaRadarLite.UI.Hotkeys
{
    public sealed class HotkeyEventArgs : EventArgs
    {
        /// <summary>
        /// State of the Hotkey.
        /// True: Key is down.
        /// False: Key is up.
        /// </summary>
        public bool State { get; }

        public HotkeyEventArgs(bool state)
        {
            State = state;
        }
    }
}
