namespace EftDmaRadarLite.Misc
{
    /// <summary>
    /// Represents a flexible array buffer that uses the Shared Array Pool.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PooledArray<T> : IReadOnlyList<T>, IEnumerable<T>, IDisposable
        where T : unmanaged
    {
        private T[] _array;

        /// <summary>
        /// Returns a Span <typeparamref name="T"/> over this instance.
        /// </summary>
        public Span<T> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array.AsSpan(0, Count);
        }

        protected PooledArray() { }

        /// <summary>
        /// Construct a new SharedArray with a defined length.
        /// </summary>
        /// <param name="count">Number of array elements.</param>
        public PooledArray(int count) 
        {
            _array = ArrayPool<T>.Shared.Rent(count);
            Count = count;
        }

        /// <summary>
        /// Construct a new SharedArray from an existing rented array.
        /// This class will become the new array owner.
        /// </summary>
        /// <param name="array">Existing <see cref="T[]"/> instance. This class will become the new owner.</param>
        protected PooledArray(T[] array, int count)
        {
            _array = array;
            Count = count;
        }

        #region Interfaces

        public int Count { get; }

        public T this[int index] => Span[index]; // Span enforces bounds

        public Span<T>.Enumerator GetEnumerator() => Span.GetEnumerator(); // Use the Span enumerator for better performance.

        IEnumerator<T> IEnumerable<T>.GetEnumerator() // For LINQ and other interface compatibility.
        {
            for (int i = 0; i < Count; i++)
            {
                yield return _array[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() // For LINQ and other interface compatibility.
        {
            for (int i = 0; i < Count; i++)
            {
                yield return _array[i];
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _array, null) is T[] array)
            {
                ArrayPool<T>.Shared.Return(array);
                GC.SuppressFinalize(this);
            }
        }

        #endregion
    }
}
