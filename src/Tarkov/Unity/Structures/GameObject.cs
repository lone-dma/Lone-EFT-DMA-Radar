/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
namespace LoneEftDmaRadar.Tarkov.Unity.Structures
{

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct GameObject // EditorExtension : Object
    {
        [FieldOffset((int)UnityOffsets.GameObject_ObjectClassOffset)]
        public readonly ulong ObjectClass; // m_Object
        [FieldOffset((int)UnityOffsets.GameObject_NameOffset)]
        public readonly ulong Name; // m_Name, String
        [FieldOffset((int)UnityOffsets.GameObject_ComponentsOffset)]
        public readonly ulong Components; // m_Components, DynamicArray

        /// <summary>
        /// Return the name of this game object.
        /// </summary>
        /// <returns>Name string.</returns>
        public readonly string GetName() =>
            Memory.ReadUtf8String(Name, 128);
    }
}

