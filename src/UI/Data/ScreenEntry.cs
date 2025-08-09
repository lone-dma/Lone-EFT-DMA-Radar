namespace eft_dma_radar.UI.Data
{
    public sealed class ScreenEntry
    {
        /// <summary>
        /// Screen Index Number.
        /// </summary>
        public int ScreenNumber { get; }

        public ScreenEntry(int screenNumber)
        {
            ScreenNumber = screenNumber;
        }

        public override string ToString() => $"Screen {ScreenNumber}";
    }
}
