namespace EftDmaRadarLite.Misc
{
    /// <summary>
    /// Caches the size of unmanaged types.
    /// </summary>
    /// <typeparam name="T">Unmanaged type.</typeparam>
    internal static unsafe class SizeCache<T>
        where T : unmanaged, allows ref struct
    {
        /// <summary>
        /// Size of the unmanaged type T in bytes as a 32 bit integer.
        /// </summary>
        public static readonly int Size = sizeof(T);
        /// <summary>
        /// Size of the unmanaged type T in bytes as a 32 bit unsigned integer.
        /// </summary>
        public static readonly uint SizeU = (uint)sizeof(T);
    }
}
