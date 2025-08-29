using eft_dma_radar.Misc;

namespace eft_dma_radar.Unity.Collections
{
    /// <summary>
    /// DMA Wrapper for a C# Array
    /// Must initialize before use. Must dispose after use.
    /// </summary>
    /// <typeparam name="T">Array Type</typeparam>
    public sealed class UnityArray<T> : SharedArray<T>
        where T : unmanaged
    {
        public const uint CountOffset = 0x18;
        public const uint ArrBaseOffset = 0x20;

        /// <summary>
        /// Constructor for Unity Array.
        /// </summary>
        /// <param name="addr">Base Address for this collection.</param>
        /// <param name="useCache">Perform cached reading.</param>
        public UnityArray(ulong addr, bool useCache = true) : base()
        {
            try
            {
                var count = Memory.ReadValue<int>(addr + CountOffset, useCache);
                base.Initialize(count);
                if (count == 0)
                    return;
                Memory.ReadSpan(addr + ArrBaseOffset, Span, useCache);
            }
            catch
            {
                Dispose();
                throw;
            }
        }
    }
}
