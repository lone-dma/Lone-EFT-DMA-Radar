namespace eft_dma_radar.Misc
{
    /// <summary>
    /// Represents a flexible array buffer that uses the Shared Array Pool.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SharedArray<T> : IReadOnlyList<T>, IEnumerable<T>, IDisposable
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

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
        public void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        {
            _mem?.Dispose();
            _mem = null;
        }


        #region IReadOnlyList

        public int Count { get; private set; }

        public T this[int index] => Span[index];

        public Span<T>.Enumerator GetEnumerator() => Span.GetEnumerator(); // Use the Span enumerator for better performance.

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

        #endregion
    }
}
