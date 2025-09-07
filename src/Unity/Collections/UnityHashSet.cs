using EftDmaRadarLite.Misc;

namespace EftDmaRadarLite.Unity.Collections
{
    /// <summary>
    /// DMA Wrapper for a C# HashSet
    /// Must initialize before use. Must dispose after use.
    /// </summary>
    /// <typeparam name="T">Collection Type</typeparam>
    public sealed class UnityHashSet<T> : PooledArray<UnityHashSet<T>.MemHashEntry>
        where T : unmanaged
    {
        public const uint CountOffset = 0x3C;
        public const uint ArrOffset = 0x18;
        public const uint ArrStartOffset = 0x20;

        private UnityHashSet() { }
        private UnityHashSet(MemHashEntry[] array, int count) : base(array, count) { }

        /// <summary>
        /// Factory method to create a new <see cref="UnityHashSet{T}"/> instance from a memory address.
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="useCache"></param>
        /// <returns></returns>
        public static UnityHashSet<T> Create(ulong addr, bool useCache = true)
        {
            var count = Memory.ReadValue<int>(addr + CountOffset, useCache);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 16384, nameof(count));
            var array = ArrayPool<MemHashEntry>.Shared.Rent(count);
            try
            {
                if (count == 0)
                {
                    return new UnityHashSet<T>(array, 0);
                }
                var hashSetBase = Memory.ReadPtr(addr + ArrOffset, useCache) + ArrStartOffset;
                Memory.ReadSpan(hashSetBase, array.AsSpan(0, count), useCache);
                return new UnityHashSet<T>(array, count);
            }
            catch
            {
                ArrayPool<MemHashEntry>.Shared.Return(array);
                throw;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public readonly struct MemHashEntry
        {
            public static implicit operator T(MemHashEntry x) => x.Value;

            private readonly int _hashCode;
            private readonly int _next;
            public readonly T Value;
        }
    }
}
