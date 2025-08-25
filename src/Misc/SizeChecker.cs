namespace eft_dma_radar.Misc
{
    /// <summary>
    /// Caches Type Sizes of value types.
    /// Regulates type safety for situations where you cannot enforce types at compile time.
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
