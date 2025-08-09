using eft_dma_radar.Misc.Pools;
using Microsoft.Extensions.ObjectPool;

namespace eft_dma_radar.DMA.ScatterAPI
{
    /// <summary>
    /// Provides mapping for a Scatter Read Operation. May contain multiple Scatter Read Rounds.
    /// This API is *NOT* Thread Safe! Keep operations synchronous.
    /// </summary>
    public sealed class ScatterReadMap : IPooledObject
    {
        private readonly List<ScatterReadRound> _rounds = new();
        /// <summary>
        /// [Optional] Callback(s) to be executed on completion of *all* scatter read executions.
        /// NOTE: Be sure to handle exceptions!
        /// </summary>
        public Action CompletionCallbacks { get; set; }

        [Obsolete("You must lease this object via Lease()")]
        public ScatterReadMap() { }

        /// <summary>
        /// Get a ScatterReadMap.
        /// </summary>
        /// <returns>ScatterReadMap object from the object pool.</returns>
        public static ObjectPoolLease<ScatterReadMap> Lease(out ScatterReadMap value)
        {
            var lease = ObjectPoolLease<ScatterReadMap>.Create(out value);
            return lease;
        }

        /// <summary>
        /// Executes Scatter Read operation as defined per the map.
        /// </summary>
        public void Execute()
        {
            if (_rounds.Count == 0)
                return;
            foreach (var round in _rounds)
                round.Run();
            CompletionCallbacks?.Invoke();
        }
        /// <summary>
        /// (Base)
        /// Add scatter read rounds to the operation. Each round is a successive scatter read, you may need multiple
        /// rounds if you have reads dependent on earlier scatter reads result(s).
        /// </summary>
        /// <returns>ScatterReadRound object.</returns>
        public ScatterReadRound AddRound(bool useCache = true)
        {
            var round = ScatterReadRound.Lease(useCache, out _);
            _rounds.Add(round);
            return round;
        }

        public void Return()
        {
            MyObjectPool<ScatterReadMap>.Instance.Return(this);
        }

        public bool TryReset()
        {
            foreach (var rd in _rounds)
                rd.Return();
            _rounds.Clear();
            CompletionCallbacks = default;
            return true;
        }
    }
}