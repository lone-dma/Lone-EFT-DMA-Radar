/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using Collections.Pooled;

namespace LoneEftDmaRadar.Tarkov.Unity.Collections
{
    /// <summary>
    /// DMA Wrapper for a C# Dictionary
    /// Must initialize before use. Must dispose after use.
    /// </summary>
    /// <typeparam name="TKey">Key Type between 1-8 bytes.</typeparam>
    /// <typeparam name="TValue">Value Type between 1-8 bytes.</typeparam>
    public sealed class UnityDictionary<TKey, TValue> : PooledMemory<UnityDictionary<TKey, TValue>.MemDictEntry>
        where TKey : unmanaged
        where TValue : unmanaged
    {
        public const uint CountOffset = 0x20;
        public const uint EntriesOffset = 0x18;
        public const uint EntriesStartOffset = 0x20;

        private UnityDictionary() : base(0) { }
        private UnityDictionary(int count) : base(count) { }

        /// <summary>
        /// Factory method to create a new <see cref="UnityDictionary{TKey, TValue}"/> instance from a memory address.
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="useCache"></param>
        /// <returns></returns>
        public static UnityDictionary<TKey, TValue> Create(ulong addr, bool useCache = true)
        {
            var count = LoneEftDmaRadar.DMA.Memory.ReadValue<int>(addr + CountOffset, useCache);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 16384, nameof(count));
            var dict = new UnityDictionary<TKey, TValue>(count);
            try
            {
                if (count == 0)
                {
                    return dict;
                }
                var dictBase = LoneEftDmaRadar.DMA.Memory.ReadPtr(addr + EntriesOffset, useCache) + EntriesStartOffset;
                LoneEftDmaRadar.DMA.Memory.ReadSpan(dictBase, dict.Span, useCache); // Single read into mem buffer
                return dict;
            }
            catch
            {
                dict.Dispose();
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

