namespace EftDmaRadarLite.UI.Hotkeys
{
    public sealed class HotkeyModeListItem
    {
        public string Name { get; }
        public EMode Mode { get; }
        public HotkeyModeListItem(EMode mode)
        {
            Name = mode.ToString();
            Mode = mode;
        }
        public override string ToString() => Name;


        public enum EMode
        {
            /// <summary>
            /// Continuous Hold the hotkey.
            /// </summary>
            Hold = 1,
            /// <summary>
            /// Toggle hotkey on/off.
            /// </summary>
            Toggle = 2
        }
    }
}
