using EftDmaRadarLite.Misc;

namespace EftDmaRadarLite.Unity.Collections
{
    /// <summary>
    /// DMA Wrapper for a C# Array
    /// Must initialize before use. Must dispose after use.
    /// </summary>
    /// <typeparam name="T">Array Type</typeparam>
    public sealed class UnityArray<T> : PooledArray<T>
        where T : unmanaged
    {
        public const uint CountOffset = 0x18;
        public const uint ArrBaseOffset = 0x20;

        private UnityArray() { }
        private UnityArray(T[] array, int count) : base(array, count) { }

        /// <summary>
        /// Factory method to create a new <see cref="UnityArray{T}"/> instance from a memory address.
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="useCache"></param>
        /// <returns></returns>
        public static UnityArray<T> Create(ulong addr, bool useCache = true)
        {
            var count = Memory.ReadValue<int>(addr + CountOffset, useCache);
            var array = ArrayPool<T>.Shared.Rent(count);
            try
            {
                if (count == 0)
                {
                    return new UnityArray<T>(array, 0);
                }
                Memory.ReadSpan(addr + ArrBaseOffset, array.AsSpan(0, count), useCache);
                return new UnityArray<T>(array, count);
            }
            catch
            {
                ArrayPool<T>.Shared.Return(array);
                throw;
            }
        }
    }
}
