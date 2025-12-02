using LoneEftDmaRadar.Tarkov.Unity;
using LoneEftDmaRadar.Tarkov.Unity.Structures;
using VmmSharpEx.Extensions;

namespace LoneEftDmaRadar.Tarkov.GameWorld
{
    public static class GameWorldExtensions
    {
        /// <summary>
        /// Get the GameWorld instance from the GameObjectManager.
        /// </summary>
        /// <param name="gom"></param>
        /// <param name="ct">Restart radar cancellation token.</param>
        /// <param name="map">Map for the located gameworld, otherwise null.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static ulong GetGameWorld(this GameObjectManager gom, CancellationToken ct, out string map)
        {
            ct.ThrowIfCancellationRequested();
            Debug.WriteLine("Searching for GameWorld...");
            var firstObject = Memory.ReadValue<LinkedListObject>(gom.ActiveNodes);
            var lastObject = Memory.ReadValue<LinkedListObject>(gom.LastActiveNode);
            firstObject.ThisObject.ThrowIfInvalidUserVA(nameof(firstObject));
            firstObject.NextObjectLink.ThrowIfInvalidUserVA(nameof(firstObject));
            lastObject.ThisObject.ThrowIfInvalidUserVA(nameof(lastObject));
            lastObject.PreviousObjectLink.ThrowIfInvalidUserVA(nameof(lastObject));

            using var cts = new CancellationTokenSource();
            try
            {
                Task<GameWorldResult> winner = null;
                var tasks = new List<Task<GameWorldResult>>()
                {
                    Task.Run(() => ReadShallow(cts.Token, ct)),
                    Task.Run(() => ReadForward(firstObject, lastObject, cts.Token, ct)),
                    Task.Run(() => ReadBackward(lastObject, firstObject, cts.Token, ct))
                };
                while (tasks.Count > 1) // Shallow will never exit normally
                {
                    var finished = Task.WhenAny(tasks).GetAwaiter().GetResult();
                    ct.ThrowIfCancellationRequested();
                    tasks.Remove(finished);

                    if (finished.Status == TaskStatus.RanToCompletion)
                    {
                        winner = finished;
                        break;
                    }
                }
                if (winner is null)
                    throw new InvalidOperationException("GameWorld not found.");
                map = winner.Result.Map;
                return winner.Result.GameWorld;
            }
            finally
            {
                cts.Cancel();
            }
        }

        private static GameWorldResult ReadShallow(CancellationToken ct1, CancellationToken ct2)
        {
            const int maxDepth = 10000;
            while (true)
            {
                ct1.ThrowIfCancellationRequested();
                ct2.ThrowIfCancellationRequested();
                try
                {
                    // This implementation is completely self-contained to keep memory state fresh on re-loops
                    var gom = GameObjectManager.Get();
                    var currentObject = Memory.ReadValue<LinkedListObject>(gom.ActiveNodes);
                    int iterations = 0;
                    while (currentObject.ThisObject.IsValidUserVA())
                    {
                        ct1.ThrowIfCancellationRequested();
                        ct2.ThrowIfCancellationRequested();
                        if (iterations++ >= maxDepth)
                            break;
                        if (ParseGameWorld(ref currentObject) is GameWorldResult result)
                        {
                            Debug.WriteLine("GameWorld Found! (Shallow)");
                            return result;
                        }

                        currentObject = Memory.ReadValue<LinkedListObject>(currentObject.NextObjectLink); // Read next object
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch { }
            }
        }

        private static GameWorldResult ReadForward(LinkedListObject currentObject, LinkedListObject lastObject, CancellationToken ct1, CancellationToken ct2)
        {
            while (currentObject.ThisObject != lastObject.ThisObject)
            {
                ct1.ThrowIfCancellationRequested();
                ct2.ThrowIfCancellationRequested();
                if (ParseGameWorld(ref currentObject) is GameWorldResult result)
                {
                    Debug.WriteLine("GameWorld Found! (Forward)");
                    return result;
                }

                currentObject = Memory.ReadValue<LinkedListObject>(currentObject.NextObjectLink); // Read next object
            }
            throw new InvalidOperationException("GameWorld not found.");
        }

        private static GameWorldResult ReadBackward(LinkedListObject currentObject, LinkedListObject lastObject, CancellationToken ct1, CancellationToken ct2)
        {
            while (currentObject.ThisObject != lastObject.ThisObject)
            {
                ct1.ThrowIfCancellationRequested();
                ct2.ThrowIfCancellationRequested();
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
                currentObject.ThisObject.ThrowIfInvalidUserVA(nameof(currentObject));
                var objectNamePtr = Memory.ReadPtr(currentObject.ThisObject + UnitySDK.UnityOffsets.GameObject_NameOffset);
                var objectNameStr = Memory.ReadUtf8String(objectNamePtr, 64);
                if (objectNameStr.Equals("GameWorld", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var localGameWorld = Memory.ReadPtrChain(currentObject.ThisObject, true, UnitySDK.UnityOffsets.GameWorldChain);
                        /// Get Selected Map
                        var mapPtr = Memory.ReadValue<ulong>(localGameWorld + Offsets.GameWorld.LocationId);
                        if (mapPtr == 0x0) // Offline Mode
                        {
                            var localPlayer = Memory.ReadPtr(localGameWorld + Offsets.GameWorld.MainPlayer);
                            mapPtr = Memory.ReadPtr(localPlayer + Offsets.Player.Location);
                        }

                        string map = Memory.ReadUnityString(mapPtr, 128);
                        Debug.WriteLine("Detected Map " + map);
                        if (!TarkovDataManager.MapData.ContainsKey(map)) // Also makes sure we're not in the hideout
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
