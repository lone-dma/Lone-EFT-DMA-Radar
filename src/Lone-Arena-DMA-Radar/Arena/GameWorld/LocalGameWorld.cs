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

using LoneArenaDmaRadar.Arena.GameWorld.Explosives;
using LoneArenaDmaRadar.Arena.GameWorld.Player;
using LoneArenaDmaRadar.Arena.Mono;
using LoneArenaDmaRadar.Misc;
using LoneArenaDmaRadar.Misc.Workers;
using VmmSharpEx.Options;

namespace LoneArenaDmaRadar.Arena.GameWorld
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
        private readonly ExplosivesManager _explosivesManager;
        private readonly WorkerThread _t1;
        private readonly WorkerThread _t2;
        private readonly WorkerThread _t3;

        /// <summary>
        /// Current Game Instance Mode.
        /// </summary>
        public static Enums.ERaidMode MatchMode { get; private set; }
        /// <summary>
        /// True if the current game mode has teams (armbands).
        /// </summary>
        public static bool MatchHasTeams
        {
            get
            {
                switch (MatchMode)
                {
                    case Enums.ERaidMode.LastHero:
                        return false;
                    default:
                        return true;
                }
            }
        }

        /// <summary>
        /// Map ID of Current Map.
        /// </summary>
        public string MapID { get; }

        public bool InRaid => !_disposed;
        public IReadOnlyCollection<AbstractPlayer> Players => _rgtPlayers;
        public IReadOnlyCollection<IExplosiveItem> Explosives => _explosivesManager;
        public LocalPlayer LocalPlayer => _rgtPlayers?.LocalPlayer;

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
                    SleepDuration = TimeSpan.FromMilliseconds(4),
                    SleepMode = WorkerThreadSleepMode.DynamicSleep
                };
                _t1.PerformWork += RealtimeWorker_PerformWork;
                _t2 = new WorkerThread()
                {
                    Name = "Slow Worker",
                    ThreadPriority = ThreadPriority.BelowNormal,
                    SleepDuration = TimeSpan.FromMilliseconds(500)
                };
                _t2.PerformWork += SlowWorker_PerformWork;
                _t3 = new WorkerThread()
                {
                    Name = "Explosives Worker",
                    SleepDuration = TimeSpan.FromMilliseconds(30),
                    SleepMode = WorkerThreadSleepMode.DynamicSleep
                };
                _t3.PerformWork += ExplosivesWorker_PerformWork;
                var rgtPlayersAddr = Memory.ReadPtr(localGameWorld + Offsets.ClientLocalGameWorld.RegisteredPlayers, false);
                _rgtPlayers = new RegisteredPlayers(rgtPlayersAddr, this);
                ArgumentOutOfRangeException.ThrowIfLessThan(_rgtPlayers.GetPlayerCount(), 1, nameof(_rgtPlayers));
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
                var map = Memory.ReadUnicodeString(mapPtr, 64, false);
                Debug.WriteLine("Detected Map " + map);
                if (!StaticGameData.MapNames.ContainsKey(map))
                    throw new Exception("Invalid Map ID!");
                /// Get Raid Instance / Players List
                var inMatch = Memory.ReadValue<bool>(localGameWorld + Offsets.ClientLocalGameWorld.IsInRaid, false);
                if (!inMatch)
                    throw new Exception("Invalid Match Instance (Hideout?)");
                var networkGame = Memory.ReadPtr(MonoLib.AbstractGameField, false);
                var networkGameData = Memory.ReadPtr(networkGame + Offsets.NetworkGame.NetworkGameData, false);
                var raidMode = Memory.ReadValue<int>(networkGameData + Offsets.NetworkGameData.raidMode, false);
                if (raidMode is < 0 or > 20)
                    throw new ArgumentOutOfRangeException(nameof(raidMode));
                MatchMode = (Enums.ERaidMode)raidMode;
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
                    if (_rgtPlayers.GetPlayerCount() < 1)
                        throw new InvalidOperationException("Not in match!");
                    return;
                }
                catch { Thread.Sleep(10); } // short delay between read attempts
            }
            throw new OperationCanceledException("Match has ended!"); // Still not valid? Match must have ended.
        }

        #endregion

        #region Realtime Thread T1

        /// <summary>
        /// Managed Worker Thread that does realtime (player position/info) updates.
        /// </summary>
        private void RealtimeWorker_PerformWork(object sender, WorkerThreadArgs e)
        {
            var players = _rgtPlayers.Where(x => x.IsActive && x.IsAlive);
            var localPlayer = LocalPlayer;
            if (!players.Any()) // No players - Throttle
            {
                Thread.Sleep(1);
                return;
            }

            using var scatter = Memory.CreateScatter(VmmFlags.NOCACHE);
            foreach (var player in players)
            {
                player.OnRealtimeLoop(scatter);
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
        }

        public void ValidatePlayerTransforms()
        {
            try
            {
                var players = _rgtPlayers
                    .Where(x => x.IsActive && x.IsAlive);
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
    }
}