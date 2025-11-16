namespace LoneEftDmaRadar.Tarkov.Unity.Structures
{

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public readonly struct GameObject // EditorExtension : Object
    {
        // 1.0 shifted +0x18
        public const uint ObjectClassOffset = 0x40;
        public const uint ComponentsOffset = 0x48;
        public const uint NameOffset = 0x78;

        [FieldOffset((int)ObjectClassOffset)]
        public readonly ulong ObjectClass; // m_Object
        [FieldOffset((int)NameOffset)]
        public readonly ulong Name; // m_Name, String

        /// <summary>
        /// Return the name of this game object.
        /// </summary>
        /// <returns>Name string.</returns>
        public readonly string GetName() =>
            Memory.ReadUtf8String(Name, 128);

        ///// <summary>
        ///// Gets a component class from a Game Object.
        ///// </summary>
        ///// <param name="gameObject">Game object to scan.</param>
        ///// <param name="className">Name of class of child.</param>
        ///// <returns>Child class component.</returns>
        //public static ulong GetComponent(ulong gameObject, string className)
        //{
        //    // component list
        //    var componentArr = Memory.ReadValue<ComponentArray>(gameObject + ComponentsOffset);
        //    int size = componentArr.Size <= 0x1000 ?
        //        (int)componentArr.Size : 0x1000;
        //    using var compsBuf = Memory.ReadArray<ComponentArray.Entry>(componentArr.ArrayBase, size);
        //    foreach (var comp in compsBuf)
        //    {
        //        var compClass = Memory.ReadPtr(comp.Component + MonoBehaviour.ObjectClassOffset);
        //        var name = Structures.ObjectClass.ReadName(compClass);
        //        if (name.Equals(className, StringComparison.OrdinalIgnoreCase))
        //            return compClass;
        //    }
        //    throw new InvalidOperationException("Component Not Found!");
        //}
    }
}
