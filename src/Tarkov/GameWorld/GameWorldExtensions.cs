using LoneEftDmaRadar.DMA;
using LoneEftDmaRadar.Tarkov.Unity;
using LoneEftDmaRadar.Tarkov.Unity.Structures;

namespace LoneEftDmaRadar.Tarkov.GameWorld
{
    public static class GameWorldExtensions
    {
        public static ulong GetGameWorld(this GameObjectManager gom, out string map)
        {
            Debug.WriteLine("Searching for GameWorld...");
            var firstObject = Memory.ReadValue<LinkedListObject>(gom.ActiveNodes);
            var lastObject = Memory.ReadValue<LinkedListObject>(gom.LastActiveNode);
            firstObject.ThisObject.ThrowIfInvalidVirtualAddress(nameof(firstObject));
            firstObject.NextObjectLink.ThrowIfInvalidVirtualAddress(nameof(firstObject));
            lastObject.ThisObject.ThrowIfInvalidVirtualAddress(nameof(lastObject));
            lastObject.PreviousObjectLink.ThrowIfInvalidVirtualAddress(nameof(lastObject));

            using var cts = new CancellationTokenSource();
            Task<GameWorldResult> winner = null;
            var tasks = new List<Task<GameWorldResult>>()
            {
                Task.Run(() => ReadForward(firstObject, lastObject, cts.Token)),
                Task.Run(() => ReadBackward(lastObject, firstObject, cts.Token))
            };
            while (tasks.Count > 0)
            {
                var finished = Task.WhenAny(tasks).GetAwaiter().GetResult();
                tasks.Remove(finished);

                if (finished.Status == TaskStatus.RanToCompletion)
                {
                    winner = finished;
                    break;
                }
            }
            cts.Cancel();
            if (winner is null)
                throw new InvalidOperationException("GameWorld not found.");
            map = winner.Result.Map;
            return winner.Result.GameWorld;
        }

        private static GameWorldResult ReadForward(LinkedListObject currentObject, LinkedListObject lastObject, CancellationToken ct)
        {
            while (currentObject.ThisObject != lastObject.ThisObject)
            {
                ct.ThrowIfCancellationRequested();
                if (ParseGameWorld(ref currentObject) is GameWorldResult result)
                {
                    Debug.WriteLine("GameWorld Found! (Forward)");
                    return result;
                }

                currentObject = Memory.ReadValue<LinkedListObject>(currentObject.NextObjectLink); // Read next object
            }
            throw new InvalidOperationException("GameWorld not found.");
        }

        private static GameWorldResult ReadBackward(LinkedListObject currentObject, LinkedListObject lastObject, CancellationToken ct)
        {
            while (currentObject.ThisObject != lastObject.ThisObject)
            {
                ct.ThrowIfCancellationRequested();
                if (ParseGameWorld(ref currentObject) is GameWorldResult result)
                {
                    Debug.WriteLine("GameWorld Found! (Backward)");
                    return result;
                }

                currentObject = Memory.ReadValue<LinkedListObject>(currentObject.PreviousObjectLink); // Read previous object
            }
            throw new InvalidOperationException("GameWorld not found.");
        }

        private static GameWorldResult ParseGameWorld(ref LinkedListObject currentObject)
        {
            try
            {
                currentObject.ThisObject.ThrowIfInvalidVirtualAddress(nameof(currentObject));
                var objectNamePtr = Memory.ReadPtr(currentObject.ThisObject + UnitySDK.UnityOffsets.GameObject_NameOffset);
                var objectNameStr = Memory.ReadUtf8String(objectNamePtr, 64);
                if (objectNameStr.Equals("GameWorld", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var localGameWorld = Memory.ReadPtrChain(currentObject.ThisObject, true, UnitySDK.UnityOffsets.GameWorldChain);
                        /// Get Selected Map
                        var mapPtr = Memory.ReadValue<ulong>(localGameWorld + Offsets.GameWorld.Location);
                        if (mapPtr == 0x0) // Offline Mode
                        {
                            var localPlayer = Memory.ReadPtr(localGameWorld + Offsets.ClientLocalGameWorld.MainPlayer);
                            mapPtr = Memory.ReadPtr(localPlayer + Offsets.Player.Location);
                        }

                        string map = Memory.ReadUnicodeString(mapPtr, 128);
                        Debug.WriteLine("Detected Map " + map);
                        if (!StaticGameData.MapNames.ContainsKey(map)) // Also makes sure we're not in the hideout
                            throw new ArgumentException("Invalid Map ID!");
                        return new GameWorldResult()
                        {
                            GameWorld = localGameWorld,
                            Map = map
                        };
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Invalid GameWorld Instance: {ex}");
                    }
                }
            }
            catch { }
            return null;
        }

        private class GameWorldResult
        {
            public ulong GameWorld { get; init; }
            public string Map { get; init; }
        }
    }
}
