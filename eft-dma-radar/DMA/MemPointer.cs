using eft_dma_radar.Misc;
using System.Runtime.CompilerServices;

namespace eft_dma_radar.DMA
{
    /// <summary>
    /// Represents a 64-Bit Unsigned Pointer Address.
    /// </summary>
    public readonly struct MemPointer
    {
        public static implicit operator MemPointer(ulong x) => x;
        public static implicit operator ulong(MemPointer x) => x._pointer;
        /// <summary>
        /// Virtual Address of this Pointer.
        /// </summary>
#pragma warning disable CS0649
        private readonly ulong _pointer;
#pragma warning restore CS0649

        /// <summary>
        /// Validates the Pointer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Validate() =>
            _pointer.ThrowIfInvalidVirtualAddress();

        public override string ToString() => _pointer.ToString("X");
    }
}
