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
        /// Returns the Game Object Manager for the current UnityPlayer.
        /// </summary>
        /// <param name="unityBase">UnityPlayer Base Addr</param>
        /// <returns>Game Object Manager</returns>
        public static GameObjectManager Get(ulong unityBase)
        {
            try
            {
                var gomPtr = Memory.ReadPtr(unityBase + UnitySDK.ShuffledOffsets.GameObjectManager, false);
                //var dump = new byte[128];
                //Memory.ReadSpan(gomPtr - 64, dump, false);
                //DumpBytes(dump);
                //Environment.Exit(0);
                return Memory.ReadValue<GameObjectManager>(gomPtr, false);
            }
            catch (Exception ex)
            {
                throw new Exception("ERROR Loading Game Object Manager", ex);
            }
        }

        private static void DumpBytes(byte[] bytes)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append($"{bytes[i]:X2} ");
                if ((i + 1) % 16 == 0)
                    sb.AppendLine();
            }
            Debug.WriteLine($"Bytes:\n" + sb.ToString());
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
                    var objectNamePtr = Memory.ReadPtr(currentObject.ThisObject + UnitySDK.ShuffledOffsets.GameObject_NameOffset);
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
                    var objectNamePtr = Memory.ReadPtr(currentObject.ThisObject + UnitySDK.ShuffledOffsets.GameObject_NameOffset);
                    var objectNameStr = Memory.ReadUtf8String(objectNamePtr, 64);
                    if (objectNameStr.Equals("GameWorld", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var localGameWorld = Memory.ReadPtrChain(currentObject.ThisObject, false, UnitySDK.ShuffledOffsets.GameWorldChain);
                            /// Get Selected Map
                            var mapPtr = Memory.ReadValue<ulong>(localGameWorld + Offsets.GameWorld.Location, false);
                            if (mapPtr == 0x0) // Offline Mode
                            {
                                var localPlayer = Memory.ReadPtr(localGameWorld + Offsets.ClientLocalGameWorld.MainPlayer, false);
                                mapPtr = Memory.ReadPtr(localPlayer + Offsets.Player.Location, false);
                            }

                            map = Memory.ReadUnicodeString(mapPtr, 128, false);
                            Debug.WriteLine("Detected Map " + map);
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
            return 0x0;
        }
    }
}
