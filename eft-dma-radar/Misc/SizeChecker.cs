namespace eft_dma_radar.Misc
{
    /// <summary>
    /// Caches Type Sizes of value types.
    /// Regulates type safety for situations where you cannot enforce types at compile time.
    /// </summary>
    /// <typeparam name="T">Type to check.</typeparam>
    internal static class SizeChecker<T>
    {
        /// <summary>
        /// Size of this Type.
        /// </summary>
        public static readonly int Size = GetSize();

        private static int GetSize()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                throw new NotSupportedException(typeof(T).ToString());
            return Unsafe.SizeOf<T>();
        }
    }
}
