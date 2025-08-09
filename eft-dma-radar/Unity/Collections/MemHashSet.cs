using eft_dma_radar.DMA;
using eft_dma_radar.Misc.Pools;
using Microsoft.Extensions.ObjectPool;
using System.Runtime.InteropServices;

namespace eft_dma_radar.Unity.Collections
{
    /// <summary>
    /// DMA Wrapper for a C# HashSet
    /// Must initialize before use. Must dispose after use.
    /// </summary>
    /// <typeparam name="T">Collection Type</typeparam>
    public sealed class MemHashSet<T> : SharedArray<MemHashSet<T>.MemHashEntry>, IPooledObject
        where T : unmanaged
    {
        public const uint CountOffset = 0x3C;
        public const uint ArrOffset = 0x18;
        public const uint ArrStartOffset = 0x20;

        /// <summary>
        /// Get a MemHashSet <typeparamref name="T"/> from the object pool.
        /// </summary>
        /// <param name="addr">Base Address for this collection.</param>
        /// <param name="useCache">Perform cached reading.</param>
        /// <returns>Rented MemHashSet <typeparamref name="T"/> instance.</returns>
        public static ObjectPoolLease<MemHashSet<T>> Lease(ulong addr, bool useCache, out MemHashSet<T> value)
        {
            var lease = ObjectPoolLease<MemHashSet<T>>.Create(out value);
            value.Initialize(addr, useCache);
            return lease;
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
                ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 16384, nameof(count));
                Initialize(count);
                if (count == 0)
                    return;
                var hashSetBase = Memory.ReadPtr(addr + ArrOffset, useCache) + ArrStartOffset;
                Memory.ReadBuffer(hashSetBase, Span, useCache);
            }
            catch
            {
                Return();
                throw;
            }
        }

        [Obsolete("You must lease this object via Lease()")]
        public MemHashSet()
        { }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public readonly struct MemHashEntry
        {
            public static implicit operator T(MemHashEntry x) => x.Value;

            private readonly int _hashCode;
            private readonly int _next;
            public readonly T Value;
        }

        public override void Return()
        {
            MyObjectPool<MemHashSet<T>>.Instance.Return(this);
        }
    }
}
