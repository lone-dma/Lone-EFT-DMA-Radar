namespace LoneEftDmaRadar.Tarkov.Unity.Structures
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct ComponentArray
    {
        public readonly ulong ArrayBase; // To ComponentArrayEntry[]
        public readonly ulong MemLabelId;
        public readonly ulong Size;
        public readonly ulong Capacity;

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        public readonly struct Entry
        {
            [FieldOffset(0x8)]
            public readonly ulong Component;
        }
    }
}
