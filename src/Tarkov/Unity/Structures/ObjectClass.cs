namespace LoneEftDmaRadar.Tarkov.Unity.Structures
{

    /// <summary>
    /// Most higher level EFT Assembly Classes and Game Objects.
    /// </summary>
    public readonly struct ObjectClass
    {
        public const uint MonoBehaviourOffset = 0x10;

        public static uint[] To_GameObject { get; } = new[] { MonoBehaviourOffset, MonoBehaviour.GameObjectOffset };
        public static uint[] To_NamePtr { get; } = new uint[] { 0x0, 0x10 };

        /// <summary>
        /// Read the Class Name from any ObjectClass that implements MonoBehaviour.
        /// </summary>
        /// <param name="objectClass">ObjectClass address.</param>
        /// <returns>Name (string) of the object class given.</returns>
        public static string ReadName(ulong objectClass, int length = 128, bool useCache = true)
        {
            var namePtr = Memory.ReadPtrChain(objectClass, useCache, To_NamePtr);
            return Memory.ReadUtf8String(namePtr, length, useCache);
        }
    }
}
