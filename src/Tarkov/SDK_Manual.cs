namespace SDK
{
    public readonly partial struct Enums
    {
        public enum EPlayerSide
        {
            Usec = 0x1,
            Bear = 0x2,
            Savage = 0x4
        }

        [Flags]
        public enum ETagStatus
        {
            Unaware = 1,
            Aware = 2,
            Combat = 4,
            Solo = 8,
            Coop = 16,
            Bear = 32,
            Usec = 64,
            Scav = 128,
            TargetSolo = 256,
            TargetMultiple = 512,
            Healthy = 1024,
            Injured = 2048,
            BadlyInjured = 4096,
            Dying = 8192,
            Birdeye = 16384,
            Knight = 32768,
            BigPipe = 65536
        }
    }

    public readonly struct Types
    {
        /// <summary>
        /// EFT.MongoID Struct
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Pack = 8)]
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
}