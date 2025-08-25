using eft_dma_radar.DMA;
using eft_dma_radar.Misc;
using Microsoft.Extensions.ObjectPool;

namespace eft_dma_radar.Unity.Collections
{
    /// <summary>
    /// DMA Wrapper for a C# List
    /// Must initialize before use. Must dispose after use.
    /// </summary>
    /// <typeparam name="T">Collection Type</typeparam>
    public sealed class UnityList<T> : SharedArray<T>
        where T : unmanaged
    {
        public const uint CountOffset = 0x18;
        public const uint ArrOffset = 0x10;
        public const uint ArrStartOffset = 0x20;

        /// <summary>
        /// Constructor for Unity List.
        /// </summary>
        /// <param name="addr">Base Address for this collection.</param>
        /// <param name="useCache">Perform cached reading.</param>
        public UnityList(ulong addr, bool useCache = true) : base()
        {
            Initialize(addr, useCache);
        }

        /// <summary>
        /// Initializer for Unity List
        /// </summary>
        /// <param name="addr">Base Address for this collection.</param>
        /// <param name="useCache">Perform cached reading.</param>
        private void Initialize(ulong addr, bool useCache = true)
        {
            try
            {
                var count = Memory.ReadValue<int>(addr + CountOffset, useCache);
                base.Initialize(count);
                if (count == 0)
                    return;
                var listBase = Memory.ReadPtr(addr + ArrOffset, useCache) + ArrStartOffset;
                Memory.ReadSpan(listBase, Span, useCache);
            }
            catch
            {
                Dispose();
                throw;
            }
        }
    }
}
