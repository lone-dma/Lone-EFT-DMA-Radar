/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using LoneEftDmaRadar.Tarkov.Unity.Structures;

namespace SDK
{
    public readonly struct UnityOffsets
    {
        public const uint GameObjectManager = 0x1A233A0;

        public const uint GameObject_ObjectClassOffset = 0x80;
        public const uint GameObject_ComponentsOffset = 0x58;
        public const uint GameObject_NameOffset = 0x88;

        public const uint Component_ObjectClassOffset = 0x20;
        public const uint Component_GameObjectOffset = 0x58;

        public const uint TransformAccess_IndexOffset = 0x78;
        public const uint TransformAccess_HierarchyOffset = 0x70;

        public const uint Hierarchy_VerticesOffset = 0x68;
        public const uint Hierarchy_IndicesOffset = 0x40;

        public static readonly uint[] GameWorldChain =
        [
            GameObject_ComponentsOffset,
            0x18,
            Component_ObjectClassOffset
        ];

        public static readonly uint[] TransformChain =
        [
            ObjectClass.MonoBehaviourOffset,
            Component_GameObjectOffset,
            GameObject_ComponentsOffset,
            0x8,
            Component_ObjectClassOffset,
            0x10 // Transform Internal
        ];
    }
}
