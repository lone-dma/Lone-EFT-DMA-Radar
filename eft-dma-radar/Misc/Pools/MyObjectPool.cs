using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace eft_dma_radar.Misc.Pools
{
    /// <summary>
    /// Object Pool for objects that implement IPooledObject.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal static class MyObjectPool<T>
        where T : class, IPooledObject, new()
    {
        /// <summary>
        /// Default Object Pool for <typeparamref name="T"/>.
        /// </summary>
        public static ObjectPool<T> Instance { get; } = App.ServiceProvider
            .GetRequiredService<ObjectPoolProvider>()
            .Create<T>();
    }
}
