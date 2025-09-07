namespace EftDmaRadarLite.Misc
{
    /// <summary>
    /// Represents the base implementation of a pooled array of unmanaged types.
    /// </summary>
    /// <typeparam name="T">Unmanaged value type.</typeparam>
    public abstract class PooledArray<T> : IReadOnlyList<T>, IEnumerable<T>, IDisposable
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
        /// Construct a new PooledArray from an existing *rented* array via <see cref="ArrayPool{T}.Shared"/>.
        /// This class will handle returning the array to the pool when disposed.
        /// </summary>
        /// <param name="array">Existing rented <see cref="T[]"/> instance. This class will handle returning the array to the pool when disposed.</param>
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
