using eft_dma_radar.Misc.Pools;
using Microsoft.Extensions.ObjectPool;

namespace eft_dma_radar.DMA.ScatterAPI
{
    /// <summary>
    /// Defines a Scatter Read Round. Each round will execute a single scatter read. If you have reads that
    /// are dependent on previous reads (chained pointers for example), you may need multiple rounds.
    /// </summary>
    public sealed class ScatterReadRound : IPooledObject
    {
        private readonly Dictionary<int, ScatterReadIndex> _indexes = new();
        public bool UseCache { get; private set; }

        [Obsolete("You must lease this object via Lease()")]
        public ScatterReadRound() { }

        /// <summary>
        /// Get a Scatter Read Round from the Object Pool.
        /// </summary>
        /// <returns>Rented ScatterReadRound instance.</returns>
        public static ObjectPoolLease<ScatterReadRound> Lease(bool useCache, out ScatterReadRound value)
        {
            var lease = ObjectPoolLease<ScatterReadRound>.Create(out value);
            value.UseCache = useCache;
            return lease;
        }

        /// <summary>
        /// Returns the requested ScatterReadIndex.
        /// </summary>
        /// <param name="index">Index to retrieve.</param>
        /// <returns>ScatterReadIndex object.</returns>
        public ScatterReadIndex this[int index]
        {
            get
            {
                if (_indexes.TryGetValue(index, out var existing))
                    return existing;
                return _indexes[index] = MyObjectPool<ScatterReadIndex>.Instance.Get();
            }
        }

        /// <summary>
        /// ** Internal use only do not use **
        /// </summary>
        internal void Run()
        {
            Memory.ReadScatter(_indexes.Values.SelectMany(x => x.Entries.Values).ToArray(), UseCache);
            foreach (var index in _indexes)
                index.Value.ExecuteCallback();
        }

        public void Return()
        {
            MyObjectPool<ScatterReadRound>.Instance.Return(this);
        }

        public bool TryReset()
        {
            foreach (var index in _indexes.Values)
                index.Return();
            _indexes.Clear();
            UseCache = default;
            return true;
        }
    }
}
