namespace eft_dma_radar.Misc
{
    /// <summary>
    /// Represents a flexible array buffer that uses the Shared Array Pool.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SharedArray<T> : IEnumerable<T>, IDisposable
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

        /// <summary>
        /// Construct a new SharedArray with a defined length.
        /// </summary>
        /// <param name="count">Number of array elements.</param>
        public SharedArray(int count) 
        {
            Initialize(count);
        }

        /// <summary>
        /// Constructor for derived classes.
        /// Be sure to call <see cref="Initialize(int)"/> in the derived class."/>
        /// </summary>
        protected SharedArray() { }

        /// <summary>
        /// Initialize the array to a defined length.
        /// </summary>
        /// <param name="count">Number of elements in the array.</param>
        protected void Initialize(int count)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 16384, nameof(count));
            Count = count;
            _arr = ArrayPool<T>.Shared.Rent(count); // Will throw exception on negative counts
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _arr, null) is T[] arr)
            {
                ArrayPool<T>.Shared.Return(arr);
            }
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
    }
}
