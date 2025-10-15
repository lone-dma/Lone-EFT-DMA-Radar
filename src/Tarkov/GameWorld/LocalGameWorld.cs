/*
 * EFT DMA Radar Lite
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

using EftDmaRadarLite.Misc.Workers;
using EftDmaRadarLite.Mono;
using EftDmaRadarLite.Tarkov.Data;
using EftDmaRadarLite.Tarkov.GameWorld.Exits;
using EftDmaRadarLite.Tarkov.GameWorld.Explosives;
using EftDmaRadarLite.Tarkov.Loot;
using EftDmaRadarLite.Tarkov.Player;
using EftDmaRadarLite.Tarkov.Quests;
using VmmSharpEx.Options;

namespace EftDmaRadarLite.Tarkov.GameWorld
{
    /// <summary>
    /// Class containing Game (Raid) instance.
    /// IDisposable.
    /// </summary>
    public sealed class LocalGameWorld : IDisposable
    {
        #region Fields / Properties / Constructors

        public static implicit operator ulong(LocalGameWorld x) => x.Base;

        /// <summary>
        /// LocalGameWorld Address.
        /// </summary>
        private ulong Base { get; }

        private readonly RegisteredPlayers _rgtPlayers;
        private readonly ExitManager _exfilManager;
        private readonly ExplosivesManager _explosivesManager;
        private readonly WorkerThread _t1;
        private readonly WorkerThread _t2;
        private readonly WorkerThread _t3;
        private readonly WorkerThread _t4;

        /// <summary>
        /// Map ID of Current Map.
        /// </summary>
        public string MapID { get; }

        public bool InRaid => !_disposed;
        public IReadOnlyCollection<AbstractPlayer> Players => _rgtPlayers;
        public IReadOnlyCollection<IExplosiveItem> Explosives => _explosivesManager;
        public IReadOnlyCollection<IExitPoint> Exits => _exfilManager;
        public LocalPlayer LocalPlayer => _rgtPlayers?.LocalPlayer;
        public LootManager Loot { get; }
        public QuestManager QuestManager { get; }
        public CameraManager CameraManager { get; private set; }

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
                _t4 = new WorkerThread()
                {
                    Name = "Fast Worker",
                    SleepDuration = TimeSpan.FromMilliseconds(100)
                };
                _t4.PerformWork += FastWorker_PerformWork;
                var rgtPlayersAddr = Memory.ReadPtr(localGameWorld + Offsets.ClientLocalGameWorld.RegisteredPlayers, false);
                _rgtPlayers = new RegisteredPlayers(rgtPlayersAddr, this);
                ArgumentOutOfRangeException.ThrowIfLessThan(_rgtPlayers.GetPlayerCount(), 1, nameof(_rgtPlayers));
                QuestManager = new(_rgtPlayers.LocalPlayer.Profile);
                Loot = new(localGameWorld);
                _exfilManager = new(localGameWorld, _rgtPlayers.LocalPlayer.IsPmc);
                _explosivesManager = new(localGameWorld);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// Start all Game Threads.
        /// </summary>
        public void Start()
        {
            _t1.Start();
            _t2.Start();
            _t3.Start();
            _t4.Start();
        }

        /// <summary>
        /// Blocks until a LocalGameWorld Singleton Instance can be instantiated.
        /// </summary>
        public static LocalGameWorld CreateGameInstance()
        {
            while (true)
            {
                ResourceJanitor.Run();
                Memory.ThrowIfProcessNotRunning();
                try
                {
                    var instance = GetLocalGameWorld();
                    Debug.WriteLine("Raid has started!");
                    return instance;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ERROR Instantiating Game Instance: {ex}");
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
        private static LocalGameWorld GetLocalGameWorld()
        {
            try
            {
                /// Get LocalGameWorld
                var localGameWorld = Memory.ReadPtr(MonoLib.GameWorldField, false); // Game world >> Local Game World
                /// Get Selected Map
                var mapPtr = Memory.ReadValue<ulong>(localGameWorld + Offsets.GameWorld.Location, false);
                if (mapPtr == 0x0) // Offline Mode
                {
                    var localPlayer = Memory.ReadPtr(localGameWorld + Offsets.ClientLocalGameWorld.MainPlayer, false);
                    mapPtr = Memory.ReadPtr(localPlayer + Offsets.Player.Location, false);
                }

                var map = Memory.ReadUnicodeString(mapPtr, 128, false);
                Debug.WriteLine("Detected Map " + map);
                if (!StaticGameData.MapNames.ContainsKey(map)) // Also makes sure we're not in the hideout
                    throw new ArgumentException("Invalid Map ID!");
                return new LocalGameWorld(localGameWorld, map);
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
            catch (OperationCanceledException ex) // Raid Ended
            {
                Debug.WriteLine(ex.Message);
                Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CRITICAL ERROR - Raid ended due to unhandled exception: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Throws an exception if the current raid instance has ended.
        /// </summary>
        /// <exception cref="OperationCanceledException"></exception>
        private void ThrowIfRaidEnded()
        {
            for (int i = 0; i < 5; i++) // Re-attempt if read fails -- 5 times
            {
                try
                {
                    if (!IsRaidActive())
                        continue;
                    return;
                }
                catch { Thread.Sleep(10); } // short delay between read attempts
            }
            throw new OperationCanceledException("Raid has ended!"); // Still not valid? Raid must have ended.
        }

        /// <summary>
        /// Checks if the Current Raid is Active, and LocalPlayer is alive/active.
        /// </summary>
        /// <returns>True if raid is active, otherwise False.</returns>
        private bool IsRaidActive()
        {
            try
            {
                var localGameWorld = Memory.ReadPtr(MonoLib.GameWorldField, false);
                ArgumentOutOfRangeException.ThrowIfNotEqual(localGameWorld, this, nameof(localGameWorld));
                var mainPlayer = Memory.ReadPtr(localGameWorld + Offsets.ClientLocalGameWorld.MainPlayer, false);
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
            bool espRunning = App.Config.EspWidget.Enabled; // Save resources if ESP is not running
            var players = _rgtPlayers.Where(x => x.IsActive && x.IsAlive);
            var localPlayer = LocalPlayer;
            if (!players.Any()) // No players - Throttle
            {
                Thread.Sleep(1);
                return;
            }

            using var scatter = Memory.CreateScatter(VmmFlags.NOCACHE);
            if (espRunning && CameraManager is CameraManager cm)
            {
                cm.OnRealtimeLoop(scatter, localPlayer);
            }
            foreach (var player in players)
            {
                player.OnRealtimeLoop(scatter, espRunning);
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
            // Refresh exfils
            _exfilManager.Refresh();
            // Refresh Loot
            Loot.Refresh(ct);
            if (App.Config.Loot.ShowWishlist)
                Memory.LocalPlayer?.RefreshWishlist(ct);
            RefreshGear(ct); // Update gear periodically
            if (App.Config.QuestHelper.Enabled)
                QuestManager.Refresh(ct);
        }

        /// <summary>
        /// Refresh Gear Manager
        /// </summary>
        private void RefreshGear(CancellationToken ct)
        {
            if (_rgtPlayers?
                .Where(x => x.IsHostileActive) is IEnumerable<AbstractPlayer> players && players.Any())
            {
                foreach (var player in players)
                {
                    ct.ThrowIfCancellationRequested();
                    player.RefreshGear();
                }
            }
        }

        public void ValidatePlayerTransforms()
        {
            try
            {
                var players = _rgtPlayers
                    .Where(x => x.IsActive && x.IsAlive && x is not BtrOperator);
                if (players.Any()) // at least 1 player
                {
                    using var map = Memory.CreateScatterMap();
                    var round1 = map.AddRound();
                    var round2 = map.AddRound();
                    foreach (var player in players)
                    {
                        player.OnValidateTransforms(round1, round2);
                    }
                    map.Execute(); // execute scatter read
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CRITICAL ERROR - ValidatePlayerTransforms Loop FAILED: {ex}");
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

        #region Fast Thread T4

        /// <summary>
        /// Managed Worker Thread that does Hands Manager / DMA Toolkit updates.
        /// No long operations on this thread.
        /// </summary>
        private void FastWorker_PerformWork(object sender, WorkerThreadArgs e)
        {
            var ct = e.CancellationToken;
            try { CameraManager ??= new(); } catch { }
            if (_rgtPlayers?
                .Where(x => x.IsActive && x.IsAlive) is IEnumerable<AbstractPlayer> players && 
                players.Any())
            {
                foreach (var player in players)
                {
                    ct.ThrowIfCancellationRequested();
                    player.RefreshHands();
                }
            }
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
                var btrController = Memory.ReadPtr(this + Offsets.ClientLocalGameWorld.BtrController);
                var btrView = Memory.ReadPtr(btrController + Offsets.BtrController.BtrView);
                var btrTurretView = Memory.ReadPtr(btrView + Offsets.BTRView.turret);
                var btrOperator = Memory.ReadPtr(btrTurretView + Offsets.BTRTurretView.AttachedBot);
                _rgtPlayers.TryAllocateBTR(btrView, btrOperator);
            }
            catch
            {
                //Debug.WriteLine($"ERROR Allocating BTR: {ex}");
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
                _t4?.Dispose();
            }
        }

        #endregion
    }
}