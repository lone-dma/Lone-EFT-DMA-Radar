/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
namespace LoneEftDmaRadar.Tarkov.Unity.Structures
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct UnityComponent // MonoBehaviour : Behaviour : << Component >> : EditorExtension : Object
    {
        [FieldOffset((int)UnityOffsets.Component_ObjectClassOffset)]
        public readonly ulong ObjectClass; // m_Object
        [FieldOffset((int)UnityOffsets.Component_GameObjectOffset)]
        public readonly ulong GameObject; // m_GameObject

        /// <summary>
        /// Return the game object of this UnityComponent.
        /// </summary>
        /// <returns>GameObject struct.</returns>
        public readonly GameObject GetGameObject() =>
            Memory.ReadValue<GameObject>(ObjectClass);
    }
}

