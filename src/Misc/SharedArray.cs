namespace eft_dma_radar.Misc
{
    /// <summary>
    /// Represents a flexible array buffer that uses the Shared Array Pool.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SharedArray<T> : IEnumerable<T>, IDisposable
        where T : unmanaged
    {
        private IMemoryOwner<T> _mem;

        /// <summary>
        /// Returns a Span <typeparamref name="T"/> over this instance.
        /// </summary>
        public Span<T> Span => _mem.Memory.Span.Slice(0, Count);

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
            _mem = MemoryPool<T>.Shared.Rent(count);
            Count = count;
        }

        public void Dispose()
        {
            _mem?.Dispose();
            _mem = null;
        }


        #region IReadOnlyList

        public int Count { get; private set; }

        public ref T this[int index] => ref Span[index]; // Modified from default implementation.

        public Enumerator GetEnumerator() =>
            new Enumerator(Span);

        [Obsolete("This implementation uses a slower interface enumerator. Use GetEnumerator() for better performance.")]
        IEnumerator<T> IEnumerable<T>.GetEnumerator() // For LINQ and other interface compatibility.
        {
            var mem = _mem.Memory.Slice(0, Count);
            for (int i = 0; i < mem.Length; i++)
            {
                yield return mem.Span[i];
            }
        }

        [Obsolete("This implementation uses a slower interface enumerator. Use GetEnumerator() for better performance.")]
        IEnumerator IEnumerable.GetEnumerator() // For LINQ and other interface compatibility.
        {
            var mem = _mem.Memory.Slice(0, Count);
            for (int i = 0; i < mem.Length; i++)
            {
                yield return mem.Span[i];
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
