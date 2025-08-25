namespace eft_dma_radar.Misc
{
    /// <summary>
    /// Caches Type Sizes of value types.
    /// </summary>
    /// <typeparam name="T">Type to check.</typeparam>
    internal static class SizeChecker<T>
        where T : unmanaged
    {
        /// <summary>
        /// Size of this Type.
        /// </summary>
        public static int Size { get; } = Unsafe.SizeOf<T>();
    }
}
