using eft_dma_radar.DMA;
using eft_dma_radar.Misc.Pools;
using Microsoft.Extensions.ObjectPool;

namespace eft_dma_radar.Unity.Collections
{
    /// <summary>
    /// DMA Wrapper for a C# Array
    /// Must initialize before use. Must dispose after use.
    /// </summary>
    /// <typeparam name="T">Array Type</typeparam>
    public sealed class MemArray<T> : SharedArray<T>, IPooledObject
        where T : unmanaged
    {
        public const uint CountOffset = 0x18;
        public const uint ArrBaseOffset = 0x20;

        /// <summary>
        /// Get a MemArray <typeparamref name="T"/> from the object pool.
        /// </summary>
        /// <param name="addr">Base Address for this collection.</param>
        /// <param name="useCache">Perform cached reading.</param>
        /// <returns>Rented MemArray <typeparamref name="T"/> instance.</returns>
        public static ObjectPoolLease<MemArray<T>> Lease(ulong addr, bool useCache, out MemArray<T> value)
        {
            var lease = ObjectPoolLease<MemArray<T>>.Create(out value);
            value.Initialize(addr, useCache);
            return lease;
        }

        /// <summary>
        /// Get a MemArray <typeparamref name="T"/> from the object pool.
        /// </summary>
        /// <param name="addr">Base Address for this collection.</param>
        /// <param name="count">Number of elements in array.</param>
        /// <param name="useCache">Perform cached reading.</param>
        /// <returns>Rented MemArray <typeparamref name="T"/> instance.</returns>
        public static ObjectPoolLease<MemArray<T>> Lease(ulong addr, int count, bool useCache, out MemArray<T> value)
        {
            var lease = ObjectPoolLease<MemArray<T>>.Create(out value);
            value.Initialize(addr, count, useCache);
            return lease;
        }

        /// <summary>
        /// Initializer for Unity Array
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
                Memory.ReadBuffer(addr + ArrBaseOffset, Span, useCache);
            }
            catch
            {
                Return();
                throw;
            }
        }

        /// <summary>
        /// Initializer for Raw Memory Array.
        /// Static defined count.
        /// Reading begins at addr + 0x0
        /// </summary>
        /// <param name="addr">Base Address for this collection.</param>
        /// <param name="count">Number of elements in array.</param>
        /// <param name="useCache">Perform cached reading.</param>
        private void Initialize(ulong addr, int count, bool useCache = true)
        {
            try
            {
                Initialize(count);
                if (count == 0)
                    return;
                Memory.ReadBuffer(addr, Span, useCache);
            }
            catch
            {
                Return();
                throw;
            }
        }

        [Obsolete("You must lease this object via Lease()")]
        public MemArray()
        { }

        public override void Return()
        {
            MyObjectPool<MemArray<T>>.Instance.Return(this);
        }
    }
}
