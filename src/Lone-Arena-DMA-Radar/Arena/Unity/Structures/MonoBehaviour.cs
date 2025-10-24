namespace LoneArenaDmaRadar.Arena.Unity.Structures
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public readonly struct MonoBehaviour // Behaviour : Component : EditorExtension : Object
    {
        public const uint InstanceIDOffset = 0x8;
        public const uint ObjectClassOffset = 0x28;
        public const uint GameObjectOffset = 0x30;
        public const uint EnabledOffset = 0x38;
        public const uint IsAddedOffset = 0x39;

        [FieldOffset((int)InstanceIDOffset)]
        public readonly int InstanceID; // m_InstanceID
        [FieldOffset((int)ObjectClassOffset)]
        public readonly ulong ObjectClass; // m_Object
        [FieldOffset((int)GameObjectOffset)]
        public readonly ulong GameObject; // m_GameObject
        [FieldOffset((int)EnabledOffset)]
        public readonly bool Enabled; // m_Enabled
        [FieldOffset((int)IsAddedOffset)]
        public readonly bool IsAdded; // m_IsAdded

        /// <summary>
        /// Return the game object of this MonoBehaviour.
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
        public static ulong GetComponent(ulong behaviour, string className)
        {
            var go = Memory.ReadPtr(behaviour + GameObjectOffset);
            return Structures.GameObject.GetComponent(go, className);
        }
    }
}
