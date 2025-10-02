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

        [field: FieldOffset(0x10)]
        public ulong StringID { get; }
    }
}
