namespace LoneEftDmaRadar.Tarkov.Unity.Structures
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct MonoBehaviour // Behaviour : Component : EditorExtension : Object
    {
        public const uint ObjectClassOffset = 0x40;
        public const uint GameObjectOffset = 0x48;

        [FieldOffset((int)ObjectClassOffset)]
        public readonly ulong ObjectClass; // m_Object
        [FieldOffset((int)GameObjectOffset)]
        public readonly ulong GameObject; // m_GameObject

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
            //return Structures.GameObject.GetComponent(go, className);

            return 0;
        }
    }
}
