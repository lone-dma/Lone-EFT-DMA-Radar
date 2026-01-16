/*
 * Lone EFT DMA Radar
 * Brought to you by Lone (Lone DMA)
 * 
MIT License

Copyright (c) 2025 Lone DMA

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 *
*/

using LoneEftDmaRadar.Misc;
using LoneEftDmaRadar.Misc.Workers;
using LoneEftDmaRadar.Tarkov.IL2CPP;
using LoneEftDmaRadar.Tarkov.Unity;
using LoneEftDmaRadar.Tarkov.Unity.Structures;
using LoneEftDmaRadar.Tarkov.World.Exits;
using LoneEftDmaRadar.Tarkov.World.Explosives;
using LoneEftDmaRadar.Tarkov.World.Hazards;
using LoneEftDmaRadar.Tarkov.World.Loot;
using LoneEftDmaRadar.Tarkov.World.Player;
using LoneEftDmaRadar.Tarkov.World.Player.Helpers;
using LoneEftDmaRadar.Tarkov.World.Quests;
using VmmSharpEx.Extensions;
using VmmSharpEx.Options;

namespace LoneEftDmaRadar.Tarkov.World
{
    /// <summary>
    /// Class containing Game (Raid) instance.
    /// IDisposable.
    /// </summary>
    public sealed class GameWorld : IDisposable
    {
        #region Fields / Properties / Constructors

        public static implicit operator ulong(GameWorld x) => x.Base;

        private static EftDmaConfig Config { get; } = Program.Config;

        /// <summary>
        /// World Address.
        /// </summary>
        private ulong Base { get; }

        private readonly RegisteredPlayers _rgtPlayers;
        private readonly ExplosivesManager _explosivesManager;
        private readonly WorkerThread _t1;
        private readonly WorkerThread _t2;
        private readonly WorkerThread _t3;

        /// <summary>
        /// Map ID of Current Map.
        /// </summary>
        public string MapID { get; }

        public bool InRaid => !_disposed;
        public IReadOnlyCollection<AbstractPlayer> Players => _rgtPlayers;
        public IReadOnlyCollection<IExplosiveItem> Explosives => _explosivesManager;
        public LocalPlayer LocalPlayer => _rgtPlayers?.LocalPlayer;
        public LootManager Loot { get; }
        public QuestManager QuestManager { get; }
        public IReadOnlyList<IExitPoint> Exits { get; }
        public IReadOnlyList<IWorldHazard> Hazards { get; }
        public bool RaidStarted { get; private set; }

        private GameWorld() { }

        /// <summary>
        /// Game Constructor.
        /// Only called internally.
        /// </summary>
        private GameWorld(ulong gameWorld, string mapID)
        {
            try
            {
                Base = gameWorld;
                MapID = mapID;
                _t1 = new WorkerThread()
                {
                    Name = "Realtime Worker",
                    ThreadPriority = ThreadPriority.AboveNormal,
                    SleepDuration = TimeSpan.FromMilliseconds(8),
                    SleepMode = WorkerThreadSleepMode.DynamicSleep
                };
                _t1.PerformWork += RealtimeWorker_PerformWork;
                _t2 = new WorkerThread()
                {
                    Name = "Slow Worker",
                    ThreadPriority = ThreadPriority.BelowNormal,
                    SleepDuration = TimeSpan.FromMilliseconds(50)
                };
                _t2.PerformWork += SlowWorker_PerformWork;
                _t3 = new WorkerThread()
                {
                    Name = "Explosives Worker",
                    SleepDuration = TimeSpan.FromMilliseconds(30),
                    SleepMode = WorkerThreadSleepMode.DynamicSleep
                };
                _t3.PerformWork += ExplosivesWorker_PerformWork;
                var rgtPlayersAddr = Memory.ReadPtr(gameWorld + Offsets.GameWorld.RegisteredPlayers, false);
                _rgtPlayers = new RegisteredPlayers(rgtPlayersAddr, this);
                ArgumentOutOfRangeException.ThrowIfLessThan(_rgtPlayers.GetPlayerCount(), 1, nameof(_rgtPlayers));
                QuestManager = new(_rgtPlayers.LocalPlayer.Profile);
                Loot = new(gameWorld);
                _explosivesManager = new(gameWorld);
                Hazards = GetHazards(MapID);
                Exits = GetExits(MapID, _rgtPlayers.LocalPlayer.IsPmc);
                // Ensure Cache
                Config.Cache.RaidCache ??= new();
                if (Config.Cache.RaidCache.GameWorld != gameWorld)
                {
                    Config.Cache.RaidCache = new()
                    {
                        GameWorld = gameWorld
                    };
                }
                // Check if raid already started
                RaidStarted = _rgtPlayers.LocalPlayer.CheckIsRaidStarted() ?? false;
                if (RaidStarted)
                {
                    Logging.WriteLine("[GameWorld] Raid has already started!");
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private static List<IWorldHazard> GetHazards(string mapId)
        {
            var list = new List<IWorldHazard>();
            if (TarkovDataManager.MapData.TryGetValue(mapId, out var mapData))
            {
                foreach (var hazard in mapData.Hazards)
                {
                    list.Add(hazard);
                }
            }
            return list;
        }

        private static List<IExitPoint> GetExits(string mapId, bool isPMC)
        {
            var list = new List<IExitPoint>();
            if (TarkovDataManager.MapData.TryGetValue(mapId, out var mapData))
            {
                var filteredExfils = isPMC ?
                    mapData.Extracts.Where(x => x.IsShared || x.IsPmc) :
                    mapData.Extracts.Where(x => !x.IsPmc);
                foreach (var exfil in filteredExfils)
                {
                    list.Add(new Exfil(exfil));
                }
                foreach (var transit in mapData.Transits)
                {
                    list.Add(new TransitPoint(transit));
                }
            }
            return list;
        }

        /// <summary>
        /// Start all Game Threads.
        /// </summary>
        public void Start()
        {
            _t1.Start();
            _t2.Start();
            _t3.Start();
        }

        /// <summary>
        /// Blocks until a World Singleton Instance can be instantiated.
        /// </summary>
        public static GameWorld CreateGameInstance(CancellationToken ct)
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                Memory.ThrowIfProcessNotRunning();
                try
                {
                    var instance = GetGameWorld(ct);
                    Logging.WriteLine($"Valid GameWorld Found! {instance}");
                    return instance;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logging.WriteLine($"ERROR Instantiating Game Instance: {ex}");
                }
                finally
                {
                    Thread.Sleep(1000);
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Checks if a Raid has started.
        /// Loads Game World resources.
        /// </summary>
        /// <returns>True if Raid has started, otherwise False.</returns>
        private static GameWorld GetGameWorld(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                Lookup.Find(ct, out ulong gameWorld, out string map);
                return new GameWorld(gameWorld, map);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("ERROR Getting GameWorld", ex);
            }
        }

        /// <summary>
        /// Main Game Loop executed by Memory Worker Thread. Refreshes/Updates Player List and performs Player Allocations.
        /// </summary>
        public void Refresh()
        {
            try
            {
                ThrowIfRaidEnded();
                if (MapID.Equals("tarkovstreets", StringComparison.OrdinalIgnoreCase) ||
                    MapID.Equals("woods", StringComparison.OrdinalIgnoreCase))
                    TryAllocateBTR();
                _rgtPlayers.Refresh(); // Check for new players, add to list, etc.
            }
            catch (RaidEndedException ex) // Raid Ended
            {
                Logging.WriteLine(ex.Message);
                Dispose();
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"CRITICAL ERROR - Raid ended due to unhandled exception: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Throws an exception if the current raid instance has ended.
        /// </summary>
        /// <exception cref="RaidEndedException"></exception>
        private void ThrowIfRaidEnded()
        {
            for (int i = 0; i < 5; i++) // Re-attempt if read fails -- 5 times
            {
                try
                {
                    if (IsRaidActive())
                        return;
                }
                catch { }
                Thread.Sleep(50); // Small delay before retry
            }
            throw new RaidEndedException(); // Still not valid? Raid must have ended.
        }

        /// <summary>
        /// Checks if the Current Raid is Active, and LocalPlayer is alive/active.
        /// </summary>
        /// <returns>True if raid is active, otherwise False.</returns>
        private bool IsRaidActive()
        {
            try
            {
                var mainPlayer = Memory.ReadPtr(this + Offsets.GameWorld.MainPlayer, false);
                ArgumentOutOfRangeException.ThrowIfNotEqual(mainPlayer, _rgtPlayers.LocalPlayer, nameof(mainPlayer));
                return _rgtPlayers.GetPlayerCount() > 0;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Realtime Thread T1

        /// <summary>
        /// Managed Worker Thread that does realtime (player position/info) updates.
        /// </summary>
        private void RealtimeWorker_PerformWork(object sender, WorkerThreadArgs e)
        {
            bool hasPlayers = false;

            using var scatter = Memory.CreateScatter(VmmFlags.NOCACHE);
            foreach (var player in _rgtPlayers)
            {
                if (player.IsActive && player.IsAlive)
                {
                    hasPlayers = true;
                    player.OnRealtimeLoop(scatter);
                }
            }

            if (!hasPlayers)
            {
                Thread.Sleep(1);
                return;
            }

            scatter.Execute();
        }

        #endregion

        #region Slow Thread T2

        /// <summary>
        /// Managed Worker Thread that does ~Slow Game World Updates.
        /// *** THIS THREAD HAS A LONG RUN TIME! LOOPS ~MAY~ TAKE ~10 SECONDS OR MORE ***
        /// </summary>
        private void SlowWorker_PerformWork(object sender, WorkerThreadArgs e)
        {
            var ct = e.CancellationToken;
            ValidatePlayerTransforms(); // Check for transform anomalies
            Loot.Refresh(ct);
            if (Config.Loot.ShowWishlist)
                Memory.LocalPlayer?.RefreshWishlist(ct);
            RefreshEquipment(ct);
            RefreshQuestHelper(ct);
            PreRaidStartChecks(ct);
        }

        /// <summary>
        /// Executes pre-raid start checks to determine if the raid has started, and various child operations.
        /// </summary>
        /// <param name="ct"></param>
        private void PreRaidStartChecks(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (RaidStarted || this.LocalPlayer is not LocalPlayer localPlayer)
                return;
            try
            {
                RaidStarted = localPlayer.CheckIsRaidStarted() ??
                    throw new InvalidOperationException("Unable to get Hands Data!");
                if (RaidStarted)
                {
                    Logging.WriteLine("[PreRaidStartChecks] Raid has started!");
                }
                if (!RaidStarted && !localPlayer.IsScav)
                {
                    RefreshSpecialAi(ct);
                    if (Config.Misc.AutoGroups)
                    {
                        RefreshGroups(localPlayer, ct);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"[PreRaidStartChecks] ERROR: {ex}");
            }
        }

        /// <summary>
        /// Refreshes AI Types for all Special AI players, including but not limited to:
        /// Santa, Guards, etc.
        /// </summary>
        private void RefreshSpecialAi(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            const float guardDistanceThreshold = 15f;

            var aiPlayers = _rgtPlayers.Where(p => p.IsAI && p.Position.IsNormal())
                .OfType<ObservedPlayer>()
                .ToList();

            var bossPositions = aiPlayers
                .Where(p => p.Type == PlayerType.AIBoss)
                .Select(p => p.Position)
                .ToList();

            // Iterate all AI
            foreach (var ai in aiPlayers)
            {
                ct.ThrowIfCancellationRequested();
                if (ai.GetSpecialAiRole() is AIRole specialRole) // Santa, etc.
                {
                    ai.AssignSpecialAiRole(specialRole);
                }
                else if (ai.Type == PlayerType.AIScav || ai.Name == "Guard") // Guards
                {
                    bool isGuard = false;
                    foreach (var bossPos in bossPositions)
                    {
                        if (Vector3.Distance(ai.Position, bossPos) <= guardDistanceThreshold)
                        {
                            isGuard = true;
                            break;
                        }
                    }
                    if (isGuard)
                    {
                        ai.AssignSpecialAiRole(new("Guard", PlayerType.AIRaider));
                    }
                    else
                    {
                        ai.AssignSpecialAiRole(null);
                    }
                }
            }
        }

        /// <summary>
        /// Refreshes Player Groups based on proximity to each other before raid start.
        /// </summary>
        /// <param name="localPlayer"></param>
        /// <param name="ct"></param>
        private void RefreshGroups(LocalPlayer localPlayer, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            const float groupDistanceThreshold = 15f;

            // Build new assignments in a local dict
            var newGroups = new ConcurrentDictionary<int, int>();

            // Collect all valid human pmc players
            var players = _rgtPlayers
                .Where(p => p.IsHuman && p.IsPmc && p.Position.IsNormal())
                .OfType<ObservedPlayer>()
                .ToList();

            if (players.Count == 0)
            {
                // No players - replace with empty dict
                Config.Cache.RaidCache.Groups = newGroups;
                return;
            }

            // Include LocalPlayer as a node (synthetic ID)
            const int localId = int.MinValue;
            var allNodes = new Dictionary<int, Vector3>
            {
                [localId] = localPlayer.Position
            };

            foreach (var p in players)
                allNodes[p.Id] = p.Position;

            // Union-Find
            var parent = allNodes.Keys.ToDictionary(id => id, id => id);

            int Find(int id)
            {
                if (parent[id] != id)
                    parent[id] = Find(parent[id]);
                return parent[id];
            }

            void Union(int a, int b)
            {
                var ra = Find(a);
                var rb = Find(b);
                if (ra != rb)
                    parent[ra] = rb;
            }

            // Build proximity graph
            var ids = allNodes.Keys.ToList();
            for (int i = 0; i < ids.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                for (int j = i + 1; j < ids.Count; j++)
                {
                    if (Vector3.Distance(allNodes[ids[i]], allNodes[ids[j]]) <= groupDistanceThreshold)
                        Union(ids[i], ids[j]);
                }
            }

            // Build components (excluding LocalPlayer for now)
            var components = new Dictionary<int, List<ObservedPlayer>>();
            foreach (var p in players)
            {
                var root = Find(p.Id);
                if (!components.TryGetValue(root, out var list))
                    components[root] = list = [];
                list.Add(p);
            }

            // Assign group IDs
            int nextGroupId = 1;
            var localRoot = Find(localId);

            foreach (var component in components.Values)
            {
                ct.ThrowIfCancellationRequested();

                // Check if LocalPlayer is in this cluster (compare roots directly)
                var componentRoot = Find(component[0].Id);
                bool containsLocal = componentRoot == localRoot;

                if (containsLocal)
                {
                    // Teammate cluster - assign even if solo
                    foreach (var p in component)
                    {
                        newGroups[p.Id] = AbstractPlayer.TeammateGroupId;
                        p.AssignTeammate(true);
                    }
                    continue;
                }

                // Hostile clusters - assign group ID (solo players get SoloGroupId)
                if (component.Count < 2)
                {
                    // Solo hostile player
                    foreach (var p in component)
                    {
                        newGroups[p.Id] = AbstractPlayer.SoloGroupId;
                        p.AssignTeammate(false);
                        p.AssignGroup(AbstractPlayer.SoloGroupId);
                    }
                    continue;
                }

                // Multi-player hostile group
                int groupId = nextGroupId++;

                foreach (var p in component)
                {
                    newGroups[p.Id] = groupId;
                    p.AssignTeammate(false);
                    p.AssignGroup(groupId);
                }
            }

            // Atomic replacement - swap the entire dict reference
            Config.Cache.RaidCache.Groups = newGroups;
        }

        private void RefreshEquipment(CancellationToken ct)
        {
            var players = _rgtPlayers
                .OfType<ObservedPlayer>()
                .Where(x => !x.IsAI // Only human players
                    && x.IsActive && x.IsAlive);
            foreach (var player in players)
            {
                ct.ThrowIfCancellationRequested();
                player.Equipment.Refresh();
            }
        }

        private void RefreshQuestHelper(CancellationToken ct)
        {
            if (Config.QuestHelper.Enabled)
            {
                QuestManager.Refresh(ct);
            }
        }

        public void ValidatePlayerTransforms()
        {
            try
            {
                using var map = Memory.CreateScatterMap();
                var round1 = map.AddRound();
                var round2 = map.AddRound();
                bool hasPlayers = false;

                foreach (var player in _rgtPlayers)
                {
                    if (player.IsActive && player.IsAlive && player is not BtrPlayer)
                    {
                        hasPlayers = true;
                        player.OnValidateTransforms(round1, round2);
                    }
                }

                if (hasPlayers)
                    map.Execute();
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"CRITICAL ERROR - ValidatePlayerTransforms Loop FAILED: {ex}");
            }
        }

        #endregion

        #region Explosives Thread T3

        /// <summary>
        /// Managed Worker Thread that does Explosives (grenades,etc.) updates.
        /// </summary>
        private void ExplosivesWorker_PerformWork(object sender, WorkerThreadArgs e)
        {
            _explosivesManager.Refresh(e.CancellationToken);
        }

        #endregion

        #region BTR Vehicle

        /// <summary>
        /// Checks if there is a Bot attached to the BTR Turret and re-allocates the player instance.
        /// </summary>
        public void TryAllocateBTR()
        {
            try
            {
                if (_rgtPlayers.Any(p => p is BtrPlayer))
                    return;
                var btrController = Memory.ReadPtr(this + Offsets.GameWorld.BtrController);
                var btrView = Memory.ReadPtr(btrController + Offsets.BtrController.BtrView);
                var btrTurretView = Memory.ReadPtr(btrView + Offsets.BTRView.turret);
                var btrOperator = Memory.ReadPtr(btrTurretView + Offsets.BTRTurretView._bot);
                _rgtPlayers.TryAllocateBTR(btrView, btrOperator);
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"ERROR Allocating BTR: {ex}");
            }
        }

        #endregion

        #region IDisposable

        private bool _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, true) == false)
            {
                _t1?.Dispose();
                _t2?.Dispose();
                _t3?.Dispose();
            }
        }

        #endregion

        #region Misc

        private sealed class RaidEndedException : Exception
        {
            public RaidEndedException()
                : base("Raid has ended!")
            {
            }
        }

        public override string ToString()
        {
            return $"GameWorld:{Base:X}";
        }

        private static class Lookup
        {
            public static void Find(CancellationToken ct, out ulong gameWorld, out string map)
            {
                ct.ThrowIfCancellationRequested();
                Logging.WriteLine("Searching for GameWorld...");

                using var searchCts = new CancellationTokenSource();
                try
                {
                    Task<GameWorldResult> winner = null;
                    var tasks = new List<Task<GameWorldResult>>()
                    {
                        Task.Run(() => FindViaIL2CPP(searchCts.Token, ct)),
                        Task.Run(() => FindViaGOM(searchCts.Token, ct))
                    };

                    while (tasks.Count > 1) // IL2CPP will never exit normally
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

                    gameWorld = winner.Result.GameWorld;
                    map = winner.Result.Map;
                }
                finally
                {
                    searchCts.Cancel();
                }
            }

            /// <summary>
            /// Finds GameWorld using IL2CPP interop.
            /// Attempts up to 3 times before giving up.
            /// </summary>
            private static GameWorldResult FindViaIL2CPP(CancellationToken ct1, CancellationToken ct2)
            {
                while (true)
                {
                    ct1.ThrowIfCancellationRequested();
                    ct2.ThrowIfCancellationRequested();
                    try
                    {
                        if (IL2CPPLib.TryGetGameWorld(out ulong gameWorld, out string map))
                        {
                            Logging.WriteLine("GameWorld Found! (IL2CPP)");
                            return new GameWorldResult
                            {
                                GameWorld = gameWorld,
                                Map = map
                            };
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch { }
                }
            }

            /// <summary>
            /// Finds GameWorld using Unity GameObjectManager with 3 parallel subtasks.
            /// </summary>
            private static GameWorldResult FindViaGOM(CancellationToken ct1, CancellationToken ct2)
            {
                var gom = GameObjectManager.Get();
                var firstObject = Memory.ReadValue<LinkedListObject>(gom.ActiveNodes);
                var lastObject = Memory.ReadValue<LinkedListObject>(gom.LastActiveNode);
                firstObject.ThisObject.ThrowIfInvalidUserVA(nameof(firstObject));
                firstObject.NextObjectLink.ThrowIfInvalidUserVA(nameof(firstObject));
                lastObject.ThisObject.ThrowIfInvalidUserVA(nameof(lastObject));

                using var gomCts = new CancellationTokenSource();
                try
                {
                    Task<GameWorldResult> winner = null;
                    var tasks = new List<Task<GameWorldResult>>()
                    {
                        Task.Run(() => GOM_ReadShallow(gomCts.Token, ct1, ct2)),
                        Task.Run(() => GOM_ReadForward(firstObject, lastObject, gomCts.Token, ct1, ct2))
                    };

                    while (tasks.Count > 1) // Shallow will never exit normally
                    {
                        var finished = Task.WhenAny(tasks).GetAwaiter().GetResult();
                        ct1.ThrowIfCancellationRequested();
                        ct2.ThrowIfCancellationRequested();
                        tasks.Remove(finished);

                        if (finished.Status == TaskStatus.RanToCompletion)
                        {
                            winner = finished;
                            break;
                        }
                    }

                    if (winner is null)
                        throw new InvalidOperationException("GameWorld not found via GOM.");

                    return winner.Result;
                }
                finally
                {
                    gomCts.Cancel();
                }
            }

            private static GameWorldResult GOM_ReadShallow(CancellationToken gomCt, CancellationToken ct1, CancellationToken ct2)
            {
                const int maxDepth = 10000;
                while (true)
                {
                    gomCt.ThrowIfCancellationRequested();
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
                            gomCt.ThrowIfCancellationRequested();
                            ct1.ThrowIfCancellationRequested();
                            ct2.ThrowIfCancellationRequested();
                            if (iterations++ >= maxDepth)
                                break;
                            if (ParseGameWorldGameObject(ref currentObject) is GameWorldResult result)
                            {
                                Logging.WriteLine("GameWorld Found! (GOM Shallow)");
                                return result;
                            }

                            currentObject = Memory.ReadValue<LinkedListObject>(currentObject.NextObjectLink); // Read next object
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch { }
                }
            }

            private static GameWorldResult GOM_ReadForward(LinkedListObject currentObject, LinkedListObject lastObject, CancellationToken gomCt, CancellationToken ct1, CancellationToken ct2)
            {
                while (currentObject.ThisObject != lastObject.ThisObject)
                {
                    gomCt.ThrowIfCancellationRequested();
                    ct1.ThrowIfCancellationRequested();
                    ct2.ThrowIfCancellationRequested();
                    if (ParseGameWorldGameObject(ref currentObject) is GameWorldResult result)
                    {
                        Logging.WriteLine("GameWorld Found! (GOM Forward)");
                        return result;
                    }

                    currentObject = Memory.ReadValue<LinkedListObject>(currentObject.NextObjectLink); // Read next object
                }
                throw new InvalidOperationException("GameWorld not found.");
            }

            private static GameWorldResult ParseGameWorldGameObject(ref LinkedListObject gameObject)
            {
                try
                {
                    gameObject.ThisObject.ThrowIfInvalidUserVA(nameof(gameObject));
                    var objectNamePtr = Memory.ReadPtr(gameObject.ThisObject + UnitySDK.UnityOffsets.GameObject_NameOffset);
                    var objectNameStr = Memory.ReadUtf8String(objectNamePtr, 64);
                    if (objectNameStr.Equals("GameWorld", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var gameWorld = Memory.ReadPtrChain(gameObject.ThisObject, true, UnitySDK.UnityOffsets.GameWorldChain);
                            /// Get Selected Map
                            var mapPtr = Memory.ReadValue<ulong>(gameWorld + Offsets.GameWorld.LocationId);
                            if (mapPtr == 0x0) // Offline Mode
                            {
                                var localPlayer = Memory.ReadPtr(gameWorld + Offsets.GameWorld.MainPlayer);
                                mapPtr = Memory.ReadPtr(localPlayer + Offsets.Player.Location);
                            }

                            string map = Memory.ReadUnityString(mapPtr, 128);
                            Logging.WriteLine("Detected Map " + map);
                            if (!TarkovDataManager.MapData.ContainsKey(map)) // Also makes sure we're not in the hideout
                                throw new ArgumentException("Invalid Map ID!");
                            return new GameWorldResult()
                            {
                                GameWorld = gameWorld,
                                Map = map
                            };
                        }
                        catch (Exception ex)
                        {
                            Logging.WriteLine($"Invalid GameWorld Instance: {ex}");
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

        #endregion
    }
}