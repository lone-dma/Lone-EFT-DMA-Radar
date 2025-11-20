using LoneEftDmaRadar.DMA;
using VmmSharpEx;

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

        /// <summary>
        /// Looks up the Address of the Game Object Manager.
        /// </summary>
        /// <param name="unityBase">UnityPlayer.dll module base address.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static ulong GetAddr(ulong unityBase)
        {
            try
            {
                try
                {
                    const string signature = "48 89 05 ?? ?? ?? ?? 48 83 C4 ?? C3 33 C9";
                    ulong gomSig = Memory.FindSignature(signature);
                    gomSig.ThrowIfInvalidVirtualAddress(nameof(gomSig));
                    uint rel = Memory.ReadValueEnsure<uint>(gomSig + 3);
                    var gomPtr = Memory.ReadValueEnsure<VmmPointer>(gomSig + 7 + rel);
                    gomPtr.ThrowIfInvalid();
                    Debug.WriteLine("GOM Located via Signature.");
                    return gomPtr;
                }
                catch
                {
                    var gomPtr = Memory.ReadValueEnsure<VmmPointer>(unityBase + UnitySDK.UnityOffsets.GameObjectManager);
                    gomPtr.ThrowIfInvalid();
                    Debug.WriteLine("GOM Located via Hardcoded Offset.");
                    return gomPtr;
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
                return Memory.ReadValueEnsure<GameObjectManager>(Memory.GOM);
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

        public ulong GetGameWorld(out string map)
        {
            map = default;
            var currentObject = Memory.ReadValue<LinkedListObject>(ActiveNodes);
            var lastObject = Memory.ReadValue<LinkedListObject>(LastActiveNode);

            if (currentObject.ThisObject != 0x0)
            {
                while (currentObject.ThisObject != 0x0 && currentObject.ThisObject != lastObject.ThisObject)
                {
                    var objectNamePtr = Memory.ReadPtr(currentObject.ThisObject + UnitySDK.UnityOffsets.GameObject_NameOffset);
                    var objectNameStr = Memory.ReadUtf8String(objectNamePtr, 64);
                    //Debug.WriteLine("GOM Object: " + objectNameStr);
                    if (objectNameStr.Equals("GameWorld", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var localGameWorld = Memory.ReadPtrChain(currentObject.ThisObject, false, UnitySDK.UnityOffsets.GameWorldChain);
                            /// Get Selected Map
                            var mapPtr = Memory.ReadValue<ulong>(localGameWorld + Offsets.GameWorld.Location, false);
                            if (mapPtr == 0x0) // Offline Mode
                            {
                                var localPlayer = Memory.ReadPtr(localGameWorld + Offsets.ClientLocalGameWorld.MainPlayer, false);
                                mapPtr = Memory.ReadPtr(localPlayer + Offsets.Player.Location, false);
                            }

                            map = Memory.ReadUnicodeString(mapPtr, 128, false);
                            Debug.WriteLine("Detected Map " + map);
                            //Debug.WriteLine(currentObject.ThisObject.ToString("X"));
                            if (!StaticGameData.MapNames.ContainsKey(map)) // Also makes sure we're not in the hideout
                                throw new ArgumentException("Invalid Map ID!");
                            return localGameWorld;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Invalid GameWorld Instance: {ex}");
                        }
                    }

                    currentObject = Memory.ReadValue<LinkedListObject>(currentObject.NextObjectLink); // Read next object
                }
            }
            throw new InvalidOperationException("GameWorld not found.");
        }
    }
}
