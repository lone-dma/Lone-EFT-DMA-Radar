using EftDmaRadarLite.Misc;

namespace EftDmaRadarLite.Unity.Collections
{
    /// <summary>
    /// DMA Wrapper for a C# Dictionary
    /// Must initialize before use. Must dispose after use.
    /// </summary>
    /// <typeparam name="TKey">Key Type between 1-8 bytes.</typeparam>
    /// <typeparam name="TValue">Value Type between 1-8 bytes.</typeparam>
    public sealed class UnityDictionary<TKey, TValue> : SharedArray<UnityDictionary<TKey, TValue>.MemDictEntry>
    where TKey : unmanaged
        where TValue : unmanaged
    {
        public const uint CountOffset = 0x40;
        public const uint EntriesOffset = 0x18;
        public const uint EntriesStartOffset = 0x20;

        /// <summary>
        /// Constructor for Unity Dictionary.
        /// </summary>
        /// <param name="addr">Base Address for this collection.</param>
        /// <param name="useCache">Perform cached reading.</param>
        public UnityDictionary(ulong addr, bool useCache = true) : base()
        {
            try
            {
                var count = Memory.ReadValue<int>(addr + CountOffset, useCache);
                base.Initialize(count);
                if (count == 0)
                    return;
                var dictBase = Memory.ReadPtr(addr + EntriesOffset, useCache) + EntriesStartOffset;
                Memory.ReadSpan(dictBase, Span, useCache); // Single read into mem buffer
            }
            catch
            {
                Dispose();
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
