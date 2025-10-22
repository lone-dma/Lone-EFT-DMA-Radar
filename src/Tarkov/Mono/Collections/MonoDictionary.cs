/*
 * Lone EFT DMA Radar
 * Brought to you by Lone (Lone DMA)
 * 
MIT License

Copyright (c) 2025 Lone DMA

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 *
*/

using Collections.Pooled;
using LoneEftDmaRadar.DMA;

namespace LoneEftDmaRadar.Tarkov.Mono.Collections
{
    /// <summary>
    /// DMA Wrapper for a C# Dictionary
    /// Must initialize before use. Must dispose after use.
    /// </summary>
    /// <typeparam name="TKey">Key Type between 1-8 bytes.</typeparam>
    /// <typeparam name="TValue">Value Type between 1-8 bytes.</typeparam>
    public sealed class MonoDictionary<TKey, TValue> : PooledMemory<MonoDictionary<TKey, TValue>.MemDictEntry>
        where TKey : unmanaged
        where TValue : unmanaged
    {
        public const uint CountOffset = 0x40;
        public const uint EntriesOffset = 0x18;
        public const uint EntriesStartOffset = 0x20;

        private MonoDictionary() : base(0) { }
        private MonoDictionary(int count) : base(count) { }

        /// <summary>
        /// Factory method to create a new <see cref="MonoDictionary{TKey, TValue}"/> instance from a memory address.
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="useCache"></param>
        /// <returns></returns>
        public static MonoDictionary<TKey, TValue> Create(ulong addr, bool useCache = true)
        {
            var count = MemoryInterface.Memory.ReadValue<int>(addr + CountOffset, useCache);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 16384, nameof(count));
            var dict = new MonoDictionary<TKey, TValue>(count);
            try
            {
                if (count == 0)
                {
                    return dict;
                }
                var dictBase = MemoryInterface.Memory.ReadPtr(addr + EntriesOffset, useCache) + EntriesStartOffset;
                MemoryInterface.Memory.ReadSpan(dictBase, dict.Span, useCache); // Single read into mem buffer
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
