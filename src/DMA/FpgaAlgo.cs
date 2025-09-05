namespace EftDmaRadarLite.DMA
{
    /// <summary>
    /// FPGA Read Algorithm
    /// </summary>
    public enum FpgaAlgo : int
    {
        /// <summary>
        /// Auto 'fpga' parameter.
        /// </summary>
        Auto = -1,
        /// <summary>
        /// Async Normal Read (default)
        /// </summary>
        AsyncNormal = 0,
        /// <summary>
        /// Async Tiny Read
        /// </summary>
        AsyncTiny = 1,
        /// <summary>
        /// Old Normal Read
        /// </summary>
        OldNormal = 2,
        /// <summary>
        /// Old Tiny Read
        /// </summary>
        OldTiny = 3
    }
}
