using eft_dma_radar.DMA;
using eft_dma_radar.Misc.Pools;
using Microsoft.Extensions.ObjectPool;

namespace eft_dma_radar.Unity.Collections
{
    /// <summary>
    /// DMA Wrapper for a C# List
    /// Must initialize before use. Must dispose after use.
    /// </summary>
    /// <typeparam name="T">Collection Type</typeparam>
    public sealed class MemList<T> : SharedArray<T>, IPooledObject
        where T : unmanaged
    {
        public const uint CountOffset = 0x18;
        public const uint ArrOffset = 0x10;
        public const uint ArrStartOffset = 0x20;

        /// <summary>
        /// Get a MemList <typeparamref name="T"/> from the object pool.
        /// </summary>
        /// <param name="addr">Base Address for this collection.</param>
        /// <param name="useCache">Perform cached reading.</param>
        /// <returns>Rented MemList <typeparamref name="T"/> instance.</returns>
        public static ObjectPoolLease<MemList<T>> Lease(ulong addr, bool useCache, out MemList<T> value)
        {
            var lease = ObjectPoolLease<MemList<T>>.Create(out value);
            value.Initialize(addr, useCache);
            return lease;
        }

        /// <summary>
        /// Initializer for Unity List
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
                var listBase = Memory.ReadPtr(addr + ArrOffset, useCache) + ArrStartOffset;
                Memory.ReadBuffer(listBase, Span, useCache);
            }
            catch
            {
                Return();
                throw;
            }
        }

        [Obsolete("You must lease this object via Lease()")]
        public MemList()
        { }

        public override void Return()
        {
            MyObjectPool<MemList<T>>.Instance.Return(this);
        }
    }
}
