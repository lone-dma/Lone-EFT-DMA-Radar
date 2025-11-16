namespace LoneEftDmaRadar.Tarkov.Unity.Structures
{
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public readonly struct LinkedListObject
    {
        public readonly ulong PreviousObjectLink; // 0x0
        public readonly ulong NextObjectLink; // 0x8
        public readonly ulong ThisObject; // 0x10   (to Offsets.GameObject)
    };
}
