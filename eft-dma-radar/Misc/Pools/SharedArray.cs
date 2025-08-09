using Microsoft.Extensions.ObjectPool;

namespace eft_dma_radar.Misc.Pools
{
    /// <summary>
    /// Represents a flexible array buffer that uses the Shared Array Pool.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SharedArray<T> : IEnumerable<T>, IPooledObject
        where T : unmanaged
    {
        private T[] _arr;

        /// <summary>
        /// Returns a Span <typeparamref name="T"/> over this instance.
        /// </summary>
        public Span<T> Span => _arr.AsSpan(0, Count);

        /// <summary>
        /// Returns a ReadOnlySpan <typeparamref name="T"/> over this instance.
        /// </summary>
        public ReadOnlySpan<T> ReadOnlySpan => _arr.AsSpan(0, Count);

        [Obsolete("You must lease this object via Lease() or Get()")]
        public SharedArray() { }

        /// <summary>
        /// Get a SharedArray <typeparamref name="T"/> lease from the object pool.
        /// </summary>
        /// <param name="count">Number of elements in the array.</param>
        /// <param name="value">Leased value (out)</param>
        /// <returns>Leased SharedArray <typeparamref name="T"/> instance.</returns>
        public static ObjectPoolLease<SharedArray<T>> Lease(int count, out SharedArray<T> value)
        {
            var lease = ObjectPoolLease<SharedArray<T>>.Create(out value);
            try
            {
                value.Initialize(count);
                return lease;
            }
            catch
            {
                lease.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Get a SharedArray <typeparamref name="T"/> from the object pool.
        /// </summary>
        /// <param name="count">Number of elements in the array.</param>
        /// <returns>Rented SharedArray <typeparamref name="T"/> instance.</returns>
        public static SharedArray<T> Get(int count)
        {
            var arr = MyObjectPool<SharedArray<T>>.Instance.Get();
            try
            {
                arr.Initialize(count);
                return arr;
            }
            catch
            {
                arr.Return();
                throw;
            }
        }

        /// <summary>
        /// Initialize the array to a defined length.
        /// </summary>
        /// <param name="count">Number of elements in the array.</param>
        protected void Initialize(int count)
        {
            if (_arr is not null)
                throw new InvalidOperationException("Shared Array Pool is already rented!");
            Count = count;
            _arr = ArrayPool<T>.Shared.Rent(count); // Will throw exception on negative counts
        }

        #region IReadOnlyList
        public int Count { get; private set; }

        public ref T this[int index] => ref Span[index]; // Modified from default implementation.

        public Enumerator GetEnumerator() =>
            new Enumerator(Span);

        [Obsolete("This implementation uses a slower interface enumerator. Use GetEnumerator() for better performance.")]
        IEnumerator<T> IEnumerable<T>.GetEnumerator() // For LINQ and other interface compatibility.
        {
            int count = Count;
            for (int i = 0; i < count; i++)
            {
                yield return _arr[i];
            }
        }

        [Obsolete("This implementation uses a slower interface enumerator. Use GetEnumerator() for better performance.")]
        IEnumerator IEnumerable.GetEnumerator() // For LINQ and other interface compatibility.
        {
            int count = Count;
            for (int i = 0; i < count; i++)
            {
                yield return _arr[i];
            }
        }

        /// <summary>
        /// Custom high perf stackonly SharedArray <typeparamref name="T"/> Enumerator.
        /// </summary>
        public ref struct Enumerator
        {
            private readonly Span<T> _span;
            private int _index = -1;

            public Enumerator(Span<T> span)
            {
                _span = span;
            }

            public readonly ref T Current => ref _span[_index];

            public bool MoveNext()
            {
                return ++_index < _span.Length;
            }

            public void Reset()
            {
                _index = -1;
            }

            public readonly void Dispose()
            {
            }
        }
        #endregion

        #region ObjectPool

        public virtual void Return()
        {
            MyObjectPool<SharedArray<T>>.Instance.Return(this);
        }

        public bool TryReset()
        {
            if (Interlocked.Exchange(ref _arr, null) is T[] arr)
            {
                ArrayPool<T>.Shared.Return(arr);
            }
            Count = default;
            return true;
        }

        #endregion
    }
}
