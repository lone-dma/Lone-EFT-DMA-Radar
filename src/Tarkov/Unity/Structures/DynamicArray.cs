/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
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

