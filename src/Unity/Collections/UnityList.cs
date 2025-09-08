using Collections.Pooled;
using EftDmaRadarLite.DMA;
using EftDmaRadarLite.Misc;

namespace EftDmaRadarLite.Unity.Collections
{
    /// <summary>
    /// DMA Wrapper for a C# List
    /// Must initialize before use. Must dispose after use.
    /// </summary>
    /// <typeparam name="T">Collection Type</typeparam>
    public sealed class UnityList<T> : PooledMemory<T>
        where T : unmanaged
    {
        public const uint CountOffset = 0x18;
        public const uint ArrOffset = 0x10;
        public const uint ArrStartOffset = 0x20;

        private UnityList() : base(0) { }
        private UnityList(int count) : base(count) { }

        /// <summary>
        /// Factory method to create a new <see cref="UnityList{T}"/> instance from a memory address.
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="useCache"></param>
        /// <returns></returns>
        public static UnityList<T> Create(ulong addr, bool useCache = true)
        {
            var count = MemoryInterface.Memory.ReadValue<int>(addr + CountOffset, useCache);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 16384, nameof(count));
            var list = new UnityList<T>(count);
            try
            {
                if (count == 0)
                {
                    return list;
                }
                var listBase = MemoryInterface.Memory.ReadPtr(addr + ArrOffset, useCache) + ArrStartOffset;
                MemoryInterface.Memory.ReadSpan(listBase, list.Span, useCache);
                return list;
            }
            catch
            {
                list.Dispose();
                throw;
            }
        }
    }
}
