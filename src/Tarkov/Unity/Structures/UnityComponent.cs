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
