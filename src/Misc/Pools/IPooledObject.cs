using Microsoft.Extensions.ObjectPool;

namespace eft_dma_radar.Misc.Pools
{
    /// <summary>
    /// Defines an interface for objects that can be pooled via MyObjectPool.
    /// </summary>
    public interface IPooledObject : IResettable
    {
        /// <summary>
        /// Defines the method to return this object to the pool.
        /// </summary>
        void Return();
    }
}
