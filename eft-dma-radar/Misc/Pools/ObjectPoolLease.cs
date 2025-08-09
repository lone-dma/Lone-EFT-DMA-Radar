namespace eft_dma_radar.Misc.Pools
{
    /// <summary>
    /// Object Pool Lease for a type that implements IPooledObject.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly struct ObjectPoolLease<T> : IDisposable
        where T : class, IPooledObject, new()
    {
        public static implicit operator T(ObjectPoolLease<T> x) => x.Value;

        /// <summary>
        /// Value of the leased object.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Rent from pool.
        /// </summary>
        [Obsolete("Use Create(out T value) instead.")]
        public ObjectPoolLease()
        {
            Value = MyObjectPool<T>.Instance.Get();
        }

        public void Dispose() => Value?.Return(); // ensure default struct doesn't throw nullref

        /// <summary>
        /// Static Factory Method to create an ObjectPoolLease, and return the value.
        /// </summary>
        /// <param name="value">Value of the allocated object.</param>
        /// <returns>IDisposable Lease</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectPoolLease<T> Create(out T value)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var lease = new ObjectPoolLease<T>();
#pragma warning restore CS0618 // Type or member is obsolete
            value = lease.Value;
            return lease;
        }
    }
}
