namespace LoneEftDmaRadar.Tarkov.Unity.Structures
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct DynamicArray
    {
        [FieldOffset(0x0)]
        public readonly int FirstIndex;
        [FieldOffset(0x8)]
        public readonly ulong FirstValue;
        [FieldOffset(0x10)]
        public readonly ulong Size;

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        public readonly struct Entry
        {
            [FieldOffset(0x8)]
            public readonly ulong Component;
        }
    }
}
