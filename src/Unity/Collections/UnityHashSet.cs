using eft_dma_radar.DMA;
using eft_dma_radar.Misc;
using Microsoft.Extensions.ObjectPool;
using System.Runtime.InteropServices;

namespace eft_dma_radar.Unity.Collections
{
    /// <summary>
    /// DMA Wrapper for a C# HashSet
    /// Must initialize before use. Must dispose after use.
    /// </summary>
    /// <typeparam name="T">Collection Type</typeparam>
    public sealed class UnityHashSet<T> : SharedArray<UnityHashSet<T>.MemHashEntry>
        where T : unmanaged
    {
        public const uint CountOffset = 0x3C;
        public const uint ArrOffset = 0x18;
        public const uint ArrStartOffset = 0x20;

        /// <summary>
        /// Constructor for Unity HashSet.
        /// </summary>
        /// <param name="addr">Base Address for this collection.</param>
        /// <param name="useCache">Perform cached reading.</param>
        public UnityHashSet(ulong addr, bool useCache = true) : base()
        {
            Initialize(addr, useCache);
        }

        /// <summary>
        /// Initializer for Unity HashSet
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
                var hashSetBase = Memory.ReadPtr(addr + ArrOffset, useCache) + ArrStartOffset;
                Memory.ReadSpan(hashSetBase, Span, useCache);
            }
            catch
            {
                Dispose();
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
