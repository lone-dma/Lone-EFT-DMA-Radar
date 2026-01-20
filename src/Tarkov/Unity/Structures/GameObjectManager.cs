using VmmSharpEx;
using VmmSharpEx.Extensions;

namespace LoneEftDmaRadar.Tarkov.Unity.Structures
{
    /// <summary>
    /// Unity Game Object Manager. Contains all Game Objects.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct GameObjectManager
    {
        [FieldOffset(0x20)]
        public readonly ulong LastActiveNode; // 0x20
        [FieldOffset(0x28)]
        public readonly ulong ActiveNodes; // 0x28

        private static PersistentCache Cache => Program.Config.Cache;

        /// <summary>
        /// Initializes the Game Object Manager address.
        /// </summary>
        /// <param name="unityBase">UnityPlayer.dll module base address.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void Init(ulong unityBase)
        {
            try
            {
                if (Cache.GameObjectManager.IsValidUserVA())
                {
                    Logging.WriteLine("GOM Initialized via Cache.");
                    return; // Already initialized
                }
                try
                {
                    const string signature = "48 89 05 ?? ?? ?? ?? 48 83 C4 ?? C3 33 C9";
                    ulong gomSig = Memory.FindSignature(signature);
                    gomSig.ThrowIfInvalidUserVA(nameof(gomSig));
                    int rva = Memory.ReadValueEnsure<int>(gomSig + 3);
                    var gomPtr = Memory.ReadValueEnsure<VmmPointer>(gomSig.AddRVA(7, rva));
                    gomPtr.ThrowIfInvalidUserVA();
                    Logging.WriteLine("GOM Initialized via Signature.");
                    Cache.GameObjectManager = gomPtr;
                }
                catch
                {
                    var gomPtr = Memory.ReadValueEnsure<VmmPointer>(unityBase + UnitySDK.UnityOffsets.GameObjectManager);
                    gomPtr.ThrowIfInvalidUserVA();
                    Logging.WriteLine("GOM Initialized via Hardcoded Offset.");
                    Cache.GameObjectManager = gomPtr;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("ERROR Locating Game Object Manager Address", ex);
            }
        }

        /// <summary>
        /// Returns the Game Object Manager for the current UnityPlayer.
        /// </summary>
        /// <returns>Game Object Manager</returns>
        public static GameObjectManager Get()
        {
            try
            {
                Cache.GameObjectManager.ThrowIfInvalidUserVA(nameof(Cache.GameObjectManager));
                return Memory.ReadValueEnsure<GameObjectManager>(Cache.GameObjectManager);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("ERROR Reading Game Object Manager", ex);
            }
        }

        /// <summary>
        /// Helper method to locate GOM Objects.
        /// </summary>
        public ulong GetObjectFromList(string objectName)
        {
            var currentObject = Memory.ReadValue<LinkedListObject>(ActiveNodes);
            var lastObject = Memory.ReadValue<LinkedListObject>(LastActiveNode);

            if (currentObject.ThisObject != 0x0)
            {
                while (currentObject.ThisObject != 0x0 && currentObject.ThisObject != lastObject.ThisObject)
                {
                    var objectNamePtr = Memory.ReadPtr(currentObject.ThisObject + UnitySDK.UnityOffsets.GameObject_NameOffset);
                    var objectNameStr = Memory.ReadUtf8String(objectNamePtr, 64);
                    if (objectNameStr.Equals(objectName, StringComparison.OrdinalIgnoreCase))
                        return currentObject.ThisObject;

                    currentObject = Memory.ReadValue<LinkedListObject>(currentObject.NextObjectLink); // Read next object
                }
            }
            return 0x0;
        }
    }
}
