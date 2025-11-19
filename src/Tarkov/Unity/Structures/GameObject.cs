namespace LoneEftDmaRadar.Tarkov.Unity.Structures
{

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct GameObject // EditorExtension : Object
    {
        [FieldOffset((int)UnitySDK.UnityOffsets.GameObject_ObjectClassOffset)]
        public readonly ulong ObjectClass; // m_Object
        [FieldOffset((int)UnitySDK.UnityOffsets.GameObject_NameOffset)]
        public readonly ulong Name; // m_Name, String
        [FieldOffset((int)UnitySDK.UnityOffsets.GameObject_ComponentsOffset)]
        public readonly ulong Components; // m_Components, DynamicArray

        /// <summary>
        /// Return the name of this game object.
        /// </summary>
        /// <returns>Name string.</returns>
        public readonly string GetName() =>
            Memory.ReadUtf8String(Name, 128);

        /// <summary>
        /// Gets a component class from a Game Object.
        /// </summary>
        /// <param name="className">Name of class of component.</param>
        /// <returns>Requested component class.</returns>
        public ulong GetComponent(string className)
        {
            throw new NotImplementedException("TODO");
            // component list
            var componentArr = Memory.ReadValue<DynamicArray>(Components);
            int size = componentArr.Size <= 0x1000 ?
                (int)componentArr.Size : 0x1000;
            using var compsBuf = Memory.ReadArray<DynamicArray.Entry>(0x0, size); // TODO: componentArr.ArrayBase
            foreach (var comp in compsBuf)
            {
                var compClass = Memory.ReadPtr(comp.Component + UnitySDK.UnityOffsets.Component_ObjectClassOffset);
                var name = Structures.ObjectClass.ReadName(compClass);
                if (name.Equals(className, StringComparison.OrdinalIgnoreCase))
                    return compClass;
            }
            throw new InvalidOperationException("Component Not Found!");
        }
    }
}
