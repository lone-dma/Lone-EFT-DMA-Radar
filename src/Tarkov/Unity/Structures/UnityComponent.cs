namespace LoneEftDmaRadar.Tarkov.Unity.Structures
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct UnityComponent // MonoBehaviour : Behaviour : << Component >> : EditorExtension : Object
    {
        [FieldOffset((int)UnitySDK.UnityOffsets.Component_ObjectClassOffset)]
        public readonly ulong ObjectClass; // m_Object
        [FieldOffset((int)UnitySDK.UnityOffsets.Component_GameObjectOffset)]
        public readonly ulong GameObject; // m_GameObject

        /// <summary>
        /// Return the game object of this UnityComponent.
        /// </summary>
        /// <returns>GameObject struct.</returns>
        public readonly GameObject GetGameObject() =>
            Memory.ReadValue<GameObject>(ObjectClass);

        /// <summary>
        /// Gets a component class from a Behaviour object.
        /// </summary>
        /// <param name="behaviour">Behaviour object to scan.</param>
        /// <param name="className">Name of class of child.</param>
        /// <returns>Child class component.</returns>
        public ulong GetComponent(ulong behaviour, string className)
        {
            return GetGameObject().GetComponent(className);
        }
    }
}
