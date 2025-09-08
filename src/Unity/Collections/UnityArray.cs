using Collections.Pooled;
using EftDmaRadarLite.DMA;

namespace EftDmaRadarLite.Unity.Collections
{
    /// <summary>
    /// DMA Wrapper for a C# Array
    /// Must initialize before use. Must dispose after use.
    /// </summary>
    /// <typeparam name="T">Array Type</typeparam>
    public sealed class UnityArray<T> : PooledMemory<T>
        where T : unmanaged
    {
        public const uint CountOffset = 0x18;
        public const uint ArrBaseOffset = 0x20;

        private UnityArray() : base(0) { }
        private UnityArray(int count) : base(count) { }

        /// <summary>
        /// Factory method to create a new <see cref="UnityArray{T}"/> instance from a memory address.
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="useCache"></param>
        /// <returns></returns>
        public static UnityArray<T> Create(ulong addr, bool useCache = true)
        {
            var count = MemoryInterface.Memory.ReadValue<int>(addr + CountOffset, useCache);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 16384, nameof(count));
            var array = new UnityArray<T>(count);
            try
            {
                if (count == 0)
                {
                    return array;
                }
                MemoryInterface.Memory.ReadSpan(addr + ArrBaseOffset, array.Span, useCache);
                return array;
            }
            catch
            {
                array.Dispose();
                throw;
            }
        }
    }
}
