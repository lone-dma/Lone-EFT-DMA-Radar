using EftDmaRadarLite.Misc;

namespace EftDmaRadarLite.Unity.Collections
{
    /// <summary>
    /// DMA Wrapper for a C# Dictionary
    /// Must initialize before use. Must dispose after use.
    /// </summary>
    /// <typeparam name="TKey">Key Type between 1-8 bytes.</typeparam>
    /// <typeparam name="TValue">Value Type between 1-8 bytes.</typeparam>
    public sealed class UnityDictionary<TKey, TValue> : PooledArray<UnityDictionary<TKey, TValue>.MemDictEntry>
        where TKey : unmanaged
        where TValue : unmanaged
    {
        public const uint CountOffset = 0x40;
        public const uint EntriesOffset = 0x18;
        public const uint EntriesStartOffset = 0x20;

        private UnityDictionary() { }
        private UnityDictionary(MemDictEntry[] array, int count) : base(array, count) { }

        /// <summary>
        /// Factory method to create a new <see cref="UnityDictionary{TKey, TValue}"/> instance from a memory address.
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="useCache"></param>
        /// <returns></returns>
        public static UnityDictionary<TKey, TValue> Create(ulong addr, bool useCache = true)
        {
            var count = Memory.ReadValue<int>(addr + CountOffset, useCache);
            var array = ArrayPool<MemDictEntry>.Shared.Rent(count);
            try
            {
                if (count == 0)
                {
                    return new UnityDictionary<TKey, TValue>(array, 0);
                }
                var dictBase = Memory.ReadPtr(addr + EntriesOffset, useCache) + EntriesStartOffset;
                Memory.ReadSpan(dictBase, array.AsSpan(0, count), useCache); // Single read into mem buffer
                return new UnityDictionary<TKey, TValue>(array, count);
            }
            catch
            {
                ArrayPool<MemDictEntry>.Shared.Return(array);
                throw;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public readonly struct MemDictEntry
        {
            private readonly ulong _pad00;
            public readonly TKey Key;
            public readonly TValue Value;
        }
    }
}
