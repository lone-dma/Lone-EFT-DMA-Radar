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
using LoneEftDmaRadar.Tarkov.GameWorld.Exits;
using LoneEftDmaRadar.Tarkov.GameWorld.Explosives;
using LoneEftDmaRadar.Tarkov.GameWorld.Hazards;
using LoneEftDmaRadar.Tarkov.GameWorld.Loot;
using LoneEftDmaRadar.Tarkov.GameWorld.Player;
using LoneEftDmaRadar.Tarkov.GameWorld.Player.Helpers;
using LoneEftDmaRadar.Tarkov.GameWorld.Quests;
using LoneEftDmaRadar.Tarkov.Unity.Structures;
using VmmSharpEx.Extensions;
using VmmSharpEx.Options;

namespace LoneEftDmaRadar.Tarkov.GameWorld
{
    /// <summary>
    /// Class containing Game (Raid) instance.
    /// IDisposable.
    /// </summary>
    public sealed class LocalGameWorld : IDisposable
    {
        #region Fields / Properties / Constructors

        public static implicit operator ulong(LocalGameWorld x) => x.Base;

        private static EftDmaConfig Config { get; } = Program.Config;

        /// <summary>
        /// LocalGameWorld Address.
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

        private LocalGameWorld() { }

        /// <summary>
        /// Game Constructor.
        /// Only called internally.
        /// </summary>
        private LocalGameWorld(ulong localGameWorld, string mapID)
        {
            try
            {
                Base = localGameWorld;
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
                var rgtPlayersAddr = Memory.ReadPtr(localGameWorld + Offsets.GameWorld.RegisteredPlayers, false);
                _rgtPlayers = new RegisteredPlayers(rgtPlayersAddr, this);
                ArgumentOutOfRangeException.ThrowIfLessThan(_rgtPlayers.GetPlayerCount(), 1, nameof(_rgtPlayers));
                QuestManager = new(_rgtPlayers.LocalPlayer.Profile);
                Loot = new(localGameWorld);
                _explosivesManager = new(localGameWorld);
                Hazards = GetHazards(MapID);
                Exits = GetExits(MapID, _rgtPlayers.LocalPlayer.IsPmc);
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
        /// Blocks until a LocalGameWorld Singleton Instance can be instantiated.
        /// </summary>
        public static LocalGameWorld CreateGameInstance(CancellationToken ct)
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                Memory.ThrowIfProcessNotRunning();
                try
                {
                    var instance = GetLocalGameWorld(ct);
                    Logging.WriteLine("Raid has started!");
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
        /// Loads Local Game World resources.
        /// </summary>
        /// <returns>True if Raid has started, otherwise False.</returns>
        private static LocalGameWorld GetLocalGameWorld(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                /// Get LocalGameWorld
                var localGameWorld = GameObjectManager.Get().GetGameWorld(ct, out string map);
                return new LocalGameWorld(localGameWorld, map);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("ERROR Getting LocalGameWorld", ex);
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
        /// Managed Worker Thread that does ~Slow Local Game World Updates.
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
                if (localPlayer.Hands is ulong hands && hands.IsValidUserVA())
                {
                    string handsType = ObjectClass.ReadName(hands);
                    RaidStarted = !string.IsNullOrWhiteSpace(handsType) && handsType != "ClientEmptyHandsController";
                    if (RaidStarted)
                    {
                        Logging.WriteLine("[PreRaidStartChecks] Raid has started!");
                    }
                    if (!RaidStarted && !localPlayer.IsScav && Config.Misc.AutoGroups)
                    {
                        RefreshGroups(localPlayer, ct);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"[PreRaidStartChecks] ERROR: {ex}");
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
            const float groupDistanceThreshold = 15f; // 10 was missing some groups
            int raidId = localPlayer.RaidId;
            if (!Config.Cache.Groups.TryGetValue(raidId, out var groups))
                Config.Cache.Groups[raidId] = groups = new();

            var humanPlayers = _rgtPlayers
                .Where(p => p.IsPmc && p.Position.IsNormal() && Vector3.Distance(p.Position, Vector3.Zero) > 1f)
                .OfType<ObservedPlayer>()
                .ToList();

            if (humanPlayers.Count == 0)
                return;

            // Players near LocalPlayer (probable teammates)
            var nearLocalPlayer = humanPlayers
                .Where(p => p.Type != PlayerType.Teammate && Vector3.Distance(localPlayer.Position, p.Position) <= groupDistanceThreshold);

            foreach (var player in nearLocalPlayer)
            {
                groups[player.Id] = AbstractPlayer.TeammateGroupId;
                player.AssignTeammate();
            }
            var hostilePlayers = humanPlayers
                .Where(p => p.IsHostile)
                .ToList();

            // Group hostile players within threshold distance using transitive clustering
            var ungrouped = new HashSet<ObservedPlayer>(hostilePlayers);
            int groupId = groups.Values.Where(v => v >= 0).DefaultIfEmpty(0).Max() + 1; // Start after existing max group ID

            while (ungrouped.Count > 0)
            {
                ct.ThrowIfCancellationRequested();
                var seed = ungrouped.First();
                ungrouped.Remove(seed);

                // Check if player already has a group assignment
                if (groups.ContainsKey(seed.Id))
                    continue;

                var group = new List<ObservedPlayer> { seed };

                // Expand group transitively - find all players connected within threshold
                var toAdd = Enumerable.Empty<ObservedPlayer>();
                do
                {
                    toAdd = ungrouped
                        .Where(p => group.Any(g => Vector3.Distance(g.Position, p.Position) <= groupDistanceThreshold))
                        .ToList();

                    toAdd.ToList().ForEach(p =>
                    {
                        ungrouped.Remove(p);
                        group.Add(p);
                    });
                } while (toAdd.Any());

                // Assign group ID to all members if group has 2+ players, otherwise leave as default (solo)
                if (group.Count > 1)
                {
                    group.ForEach(p =>
                    {
                        groups[p.Id] = groupId;
                        p.AssignGroup(groupId);
                    });
                    groupId++;
                }
            }
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

        #endregion
    }
}