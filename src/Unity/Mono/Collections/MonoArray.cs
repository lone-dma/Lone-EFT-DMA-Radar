/*
 * EFT DMA Radar Lite
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
using EftDmaRadarLite.DMA;

namespace EftDmaRadarLite.Unity.Mono.Collections
{
    /// <summary>
    /// DMA Wrapper for a C# Array
    /// Must initialize before use. Must dispose after use.
    /// </summary>
    /// <typeparam name="T">Array Type</typeparam>
    public sealed class MonoArray<T> : PooledMemory<T>
        where T : unmanaged
    {
        public const uint CountOffset = 0x18;
        public const uint ArrBaseOffset = 0x20;

        private MonoArray() : base(0) { }
        private MonoArray(int count) : base(count) { }

        /// <summary>
        /// Factory method to create a new <see cref="MonoArray{T}"/> instance from a memory address.
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="useCache"></param>
        /// <returns></returns>
        public static MonoArray<T> Create(ulong addr, bool useCache = true)
        {
            var count = MemoryInterface.Memory.ReadValue<int>(addr + CountOffset, useCache);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 16384, nameof(count));
            var array = new MonoArray<T>(count);
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
