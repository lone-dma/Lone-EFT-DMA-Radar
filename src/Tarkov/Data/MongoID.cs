namespace EftDmaRadarLite.Tarkov.Data
{
    /// <summary>
    /// EFT.MongoID Struct
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct MongoID
    {
        [FieldOffset(0x0)]
        private readonly uint _timeStamp;
        [FieldOffset(0x8)]
        private readonly ulong _counter;
        [FieldOffset(0x10)]
        private readonly ulong _stringId;

        /// <summary>
        /// Read the string value of the MongoID.
        /// </summary>
        /// <param name="cb"></param>
        /// <param name="useCache"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadString(int cb = 128, bool useCache = true)
        {
            return Memory.ReadUnicodeString(_stringId, cb, useCache);
        }
    }
}
