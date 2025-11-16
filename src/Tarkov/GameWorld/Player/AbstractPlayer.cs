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

using Collections.Pooled;
using LoneEftDmaRadar.DMA;
using LoneEftDmaRadar.Misc;
using LoneEftDmaRadar.Tarkov.GameWorld.Loot;
using LoneEftDmaRadar.Tarkov.GameWorld.Loot.Helpers;
using LoneEftDmaRadar.Tarkov.GameWorld.Player.Helpers;
using LoneEftDmaRadar.Tarkov.Unity;
using LoneEftDmaRadar.Tarkov.Unity.Structures;
using LoneEftDmaRadar.UI.Radar.Maps;
using LoneEftDmaRadar.UI.Radar.ViewModels;
using LoneEftDmaRadar.UI.Skia;
using SkiaSharp;
using VmmSharpEx.Scatter;
using static LoneEftDmaRadar.Tarkov.Unity.Structures.UnityTransform;

namespace LoneEftDmaRadar.Tarkov.GameWorld.Player
{
    /// <summary>
    /// Base class for Tarkov Players.
    /// Tarkov implements several distinct classes that implement a similar player interface.
    /// </summary>
    public abstract class AbstractPlayer : IWorldEntity, IMapEntity, IMouseoverEntity
    {
        #region Static Interfaces

        public static implicit operator ulong(AbstractPlayer x) => x.Base;
        protected static readonly ConcurrentDictionary<string, int> _groups = new(StringComparer.OrdinalIgnoreCase);
        protected static int _lastGroupNumber;
        protected static int _lastPscavNumber;

        static AbstractPlayer()
        {
            MemDMA.RaidStopped += MemDMA_RaidStopped;
        }

        private static void MemDMA_RaidStopped(object sender, EventArgs e)
        {
            _groups.Clear();
            _lastGroupNumber = default;
            _lastPscavNumber = default;
        }

        #endregion

        #region Cached Skia Paths

        private static readonly SKPath _playerPill = CreatePlayerPillBase();
        private static readonly SKPath _deathMarker = CreateDeathMarkerPath();
        private const float PP_LENGTH = 9f;
        private const float PP_RADIUS = 3f;
        private const float PP_HALF_HEIGHT = PP_RADIUS * 0.85f;
        private const float PP_NOSE_X = PP_LENGTH / 2f + PP_RADIUS * 0.18f;

        private static SKPath CreatePlayerPillBase()
        {
            var path = new SKPath();

            // Rounded back (left side)
            var backRect = new SKRect(-PP_LENGTH / 2f, -PP_HALF_HEIGHT, -PP_LENGTH / 2f + PP_RADIUS * 2f, PP_HALF_HEIGHT);
            path.AddArc(backRect, 90, 180);

            // Pointed nose (right side)
            float backFrontX = -PP_LENGTH / 2f + PP_RADIUS;

            float c1X = backFrontX + PP_RADIUS * 1.1f;
            float c2X = PP_NOSE_X - PP_RADIUS * 0.28f;
            float c1Y = -PP_HALF_HEIGHT * 0.55f;
            float c2Y = -PP_HALF_HEIGHT * 0.3f;

            path.CubicTo(c1X, c1Y, c2X, c2Y, PP_NOSE_X, 0f);
            path.CubicTo(c2X, -c2Y, c1X, -c1Y, backFrontX, PP_HALF_HEIGHT);

            path.Close();
            return path;
        }

        private static SKPath CreateDeathMarkerPath()
        {
            const float length = 6f;
            var path = new SKPath();

            path.MoveTo(-length, length);
            path.LineTo(length, -length);
            path.MoveTo(-length, -length);
            path.LineTo(length, length);

            return path;
        }

        #endregion

        #region Allocation

        /// <summary>
        /// Allocates a player.
        /// </summary>
        /// <param name="regPlayers">Player Dictionary collection to add the newly allocated player to.</param>
        /// <param name="playerBase">Player base memory address.</param>
        public static void Allocate(ConcurrentDictionary<ulong, AbstractPlayer> regPlayers, ulong playerBase)
        {
            try
            {
                _ = regPlayers.GetOrAdd(
                    playerBase,
                    addr => AllocateInternal(addr));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR during Player Allocation for player @ 0x{playerBase.ToString("X")}: {ex}");
            }
        }

        private static AbstractPlayer AllocateInternal(ulong playerBase)
        {
            AbstractPlayer player;
            var className = ObjectClass.ReadName(playerBase, 64);
            var isClientPlayer = className == "ClientPlayer" || className == "LocalPlayer";

            if (isClientPlayer)
                player = new ClientPlayer(playerBase);
            else
                player = new ObservedPlayer(playerBase);
            Debug.WriteLine($"Player '{player.Name}' allocated.");
            return player;
        }

        /// <summary>
        /// Player Constructor.
        /// </summary>
        protected AbstractPlayer(ulong playerBase)
        {
            ArgumentOutOfRangeException.ThrowIfZero(playerBase, nameof(playerBase));
            Base = playerBase;
        }

        #endregion

        #region Fields / Properties
        /// <summary>
        /// Player Class Base Address
        /// </summary>
        public ulong Base { get; }

        /// <summary>
        /// True if the Player is Active (in the player list).
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Type of player unit.
        /// </summary>
        public virtual PlayerType Type { get; protected set; }

        private Vector2 _rotation;
        /// <summary>
        /// Player's Rotation in Local Game World.
        /// </summary>
        public Vector2 Rotation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _rotation;
            private set
            {
                _rotation = value;
                float mapRotation = value.X; // Cache value
                mapRotation -= 90f;
                while (mapRotation < 0f)
                    mapRotation += 360f;
                MapRotation = mapRotation;
            }
        }

        /// <summary>
        /// Player's Map Rotation (with 90 degree correction applied).
        /// </summary>
        public float MapRotation { get; private set; }

        /// <summary>
        /// Corpse field value.
        /// </summary>
        public ulong? Corpse { get; private set; }

        /// <summary>
        /// Player's Skeleton Bones.
        /// Derived types MUST define this.
        /// </summary>
        public virtual Skeleton Skeleton => throw new NotImplementedException(nameof(Skeleton));

        /// <summary>
        /// TRUE if critical memory reads (position/rotation) have failed.
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        /// Player's Gear/Loadout Information and contained items.
        /// </summary>
        public GearManager Gear { get; private set; }

        /// <summary>
        /// Contains information about the item/weapons in Player's hands.
        /// </summary>
        public HandsManager Hands { get; private set; }

        /// <summary>
        /// True if player is being focused via Right-Click (UI).
        /// </summary>
        public bool IsFocused { get; set; }

        /// <summary>
        /// Dead Player's associated loot container object.
        /// </summary>
        public LootContainer LootObject { get; set; }
        /// <summary>
        /// Alerts for this Player Object.
        /// Used by Player History UI Interop.
        /// </summary>
        public virtual string Alerts { get; protected set; }

        #endregion

        #region Virtual Properties

        /// <summary>
        /// Player name.
        /// </summary>
        public virtual string Name { get; set; }

        /// <summary>
        /// Account UUID for Human Controlled Players.
        /// </summary>
        public virtual string AccountID { get; }

        /// <summary>
        /// Group that the player belongs to.
        /// </summary>
        public virtual int GroupID { get; protected set; } = -1;

        /// <summary>
        /// Player's Faction.
        /// </summary>
        public virtual Enums.EPlayerSide PlayerSide { get; protected set; }

        /// <summary>
        /// Player is Human-Controlled.
        /// </summary>
        public virtual bool IsHuman { get; }

        /// <summary>
        /// MovementContext / StateContext
        /// </summary>
        public virtual ulong MovementContext { get; }

        /// <summary>
        /// EFT.PlayerBody
        /// </summary>
        public virtual ulong Body { get; }

        /// <summary>
        /// Inventory Controller field address.
        /// </summary>
        public virtual ulong InventoryControllerAddr { get; }

        /// <summary>
        /// Hands Controller field address.
        /// </summary>
        public virtual ulong HandsControllerAddr { get; }

        /// <summary>
        /// Corpse field address..
        /// </summary>
        public virtual ulong CorpseAddr { get; }

        /// <summary>
        /// Player Rotation Field Address (view angles).
        /// </summary>
        public virtual ulong RotationAddress { get; }

        #endregion

        #region Boolean Getters

        /// <summary>
        /// Player is AI-Controlled.
        /// </summary>
        public bool IsAI => !IsHuman;

        /// <summary>
        /// Player is a PMC Operator.
        /// </summary>
        public bool IsPmc => PlayerSide is Enums.EPlayerSide.Usec || PlayerSide is Enums.EPlayerSide.Bear;

        /// <summary>
        /// Player is a SCAV.
        /// </summary>
        public bool IsScav => PlayerSide is Enums.EPlayerSide.Savage;

        /// <summary>
        /// Player is alive (not dead).
        /// </summary>
        public bool IsAlive => Corpse is null;

        /// <summary>
        /// True if Player is Friendly to LocalPlayer.
        /// </summary>
        public bool IsFriendly =>
            this is LocalPlayer || Type is PlayerType.Teammate;

        /// <summary>
        /// True if player is Hostile to LocalPlayer.
        /// </summary>
        public bool IsHostile => !IsFriendly;

        /// <summary>
        /// Player is Alive/Active and NOT LocalPlayer.
        /// </summary>
        public bool IsNotLocalPlayerAlive =>
            this is not LocalPlayer && IsActive && IsAlive;

        /// <summary>
        /// Player is a Hostile PMC Operator.
        /// </summary>
        public bool IsHostilePmc => IsPmc && IsHostile;

        /// <summary>
        /// Player is human-controlled (Not LocalPlayer).
        /// </summary>
        public bool IsHumanOther => IsHuman && this is not LocalPlayer;

        /// <summary>
        /// Player is AI Controlled and Alive/Active.
        /// </summary>
        public bool IsAIActive => IsAI && IsActive && IsAlive;

        /// <summary>
        /// Player is AI Controlled and Alive/Active & their AI Role is default.
        /// </summary>
        public bool IsDefaultAIActive => IsAI && Name == "defaultAI" && IsActive && IsAlive;

        /// <summary>
        /// Player is human-controlled and Active/Alive.
        /// </summary>
        public bool IsHumanActive =>
            IsHuman && IsActive && IsAlive;

        /// <summary>
        /// Player is hostile and alive/active.
        /// </summary>
        public bool IsHostileActive => IsHostile && IsActive && IsAlive;

        /// <summary>
        /// Player is human-controlled & Hostile.
        /// </summary>
        public bool IsHumanHostile => IsHuman && IsHostile;

        /// <summary>
        /// Player is human-controlled, hostile, and Active/Alive.
        /// </summary>
        public bool IsHumanHostileActive => IsHumanHostile && IsActive && IsAlive;

        /// <summary>
        /// Player is friendly to LocalPlayer (including LocalPlayer) and Active/Alive.
        /// </summary>
        public bool IsFriendlyActive => IsFriendly && IsActive && IsAlive;

        /// <summary>
        /// Player has exfil'd/left the raid.
        /// </summary>
        public bool HasExfild => !IsActive && IsAlive;

        #endregion

        #region Methods

        private readonly Lock _alertsLock = new();
        /// <summary>
        /// Update the Alerts for this Player Object.
        /// </summary>
        /// <param name="alert">Alert to set.</param>
        public void UpdateAlerts(string alert)
        {
            if (alert is null)
                return;
            lock (_alertsLock)
            {
                if (Alerts is null)
                    Alerts = alert;
                else
                    Alerts = $"{alert} | {Alerts}";
            }
        }

        /// <summary>
        /// Validates the Rotation Address.
        /// </summary>
        /// <param name="rotationAddr">Rotation va</param>
        /// <returns>Validated rotation virtual address.</returns>
        protected static ulong ValidateRotationAddr(ulong rotationAddr)
        {
            var rotation = Memory.ReadValue<Vector2>(rotationAddr, false);
            if (!rotation.IsNormalOrZero() ||
                Math.Abs(rotation.X) > 360f ||
                Math.Abs(rotation.Y) > 90f)
                throw new ArgumentOutOfRangeException(nameof(rotationAddr));

            return rotationAddr;
        }

        /// <summary>
        /// Refreshes non-realtime player information. Call in the Registered Players Loop (T0).
        /// </summary>
        /// <param name="scatter"></param>
        /// <param name="registered"></param>
        /// <param name="isActiveParam"></param>
        public virtual void OnRegRefresh(VmmScatter scatter, ISet<ulong> registered, bool? isActiveParam = null)
        {
            if (isActiveParam is not bool isActive)
                isActive = registered.Contains(this);
            if (isActive)
            {
                SetAlive();
            }
            else if (IsAlive) // Not in list, but alive
            {
                scatter.PrepareReadPtr(CorpseAddr);
                scatter.Completed += (sender, x1) =>
                {
                    if (x1.ReadPtr(CorpseAddr, out var corpsePtr))
                        SetDead(corpsePtr);
                    else
                        SetExfild();
                };
            }
        }

        /// <summary>
        /// Mark player as dead.
        /// </summary>
        /// <param name="corpse">Corpse address.</param>
        public void SetDead(ulong corpse)
        {
            Corpse = corpse;
            IsActive = false;
        }

        /// <summary>
        /// Mark player as exfil'd.
        /// </summary>
        private void SetExfild()
        {
            Corpse = null;
            IsActive = false;
        }

        /// <summary>
        /// Mark player as alive.
        /// </summary>
        private void SetAlive()
        {
            Corpse = null;
            LootObject = null;
            IsActive = true;
        }

        /// <summary>
        /// Executed on each Realtime Loop.
        /// </summary>
        /// <param name="index">Scatter read index dedicated to this player.</param>
        public virtual void OnRealtimeLoop(VmmScatter scatter, bool espRunning)
        {
            scatter.PrepareReadValue<Vector2>(RotationAddress); // Rotation
            scatter.PrepareReadArray<TrsX>(Skeleton.Root.VerticesAddr, Skeleton.Root.Count); // ESP Vertices
            //foreach (var tr in Skeleton.Bones)
            //{
            //    if (!espRunning && tr.Key is not Bones.HumanBase)
            //        continue;
            //    scatter.PrepareReadArray<TrsX>(tr.Value.VerticesAddr, tr.Value.Count); // ESP Vertices
            //}

            scatter.Completed += (sender, s) =>
            {
                bool successRot = false;
                bool successPos = true;
                if (s.ReadValue<Vector2>(RotationAddress, out var rotation))
                    successRot = SetRotation(rotation);
                //foreach (var tr in Skeleton.Bones)
                //{
                //    if (!espRunning && tr.Key is not Bones.HumanBase)
                //        continue;
                //    if (s.ReadArray<TrsX>(tr.Value.VerticesAddr, tr.Value.Count) is PooledMemory<TrsX> vertices)
                //    {
                //        using (vertices)
                //        {
                //            try
                //            {
                //                try
                //                {
                //                    _ = tr.Value.UpdatePosition(vertices.Span);
                //                }
                //                catch (Exception ex) // Attempt to re-allocate Transform on error
                //                {
                //                    Debug.WriteLine($"ERROR getting Player '{Name}' {tr.Key} Position: {ex}");
                //                    Skeleton.ResetTransform(tr.Key);
                //                }
                //            }
                //            catch
                //            {
                //                successPos = false;
                //            }
                //        }
                //    }
                //    else
                //    {
                //        successPos = false;
                //    }
                //}
                if (s.ReadArray<TrsX>(Skeleton.Root.VerticesAddr, Skeleton.Root.Count) is PooledMemory<TrsX> vertices)
                {
                    using (vertices)
                    {
                        try
                        {
                            try
                            {
                                _ = Skeleton.Root.UpdatePosition(vertices.Span);
                            }
                            catch (Exception ex) // Attempt to re-allocate Transform on error
                            {
                                Debug.WriteLine($"ERROR getting Player '{Name}' {Bones.HumanBase} Position: {ex}");
                                Skeleton.ResetTransform(Bones.HumanBase);
                            }
                        }
                        catch
                        {
                            successPos = false;
                        }
                    }
                }

                IsError = !successRot || !successPos;
            };
        }

        /// <summary>
        /// Executed on each Transform Validation Loop.
        /// </summary>
        /// <param name="round1">Index (round 1)</param>
        /// <param name="round2">Index (round 2)</param>
        public void OnValidateTransforms(VmmScatter round1, VmmScatter round2)
        {
            foreach (var tr in Skeleton.Bones)
            {
                round1.PrepareReadPtr(tr.Value.TransformInternal + UnitySDK.TransformInternal.TransformAccess); // Bone Hierarchy
                round1.Completed += (sender, x1) =>
                {
                    if (x1.ReadPtr(tr.Value.TransformInternal + UnitySDK.TransformInternal.TransformAccess, out var tra))
                    {
                        round2.PrepareReadPtr(tra + UnitySDK.TransformAccess.Vertices); // Vertices Ptr
                        round2.Completed += (sender, x2) =>
                        {
                            if (x2.ReadPtr(tra + UnitySDK.TransformAccess.Vertices, out var verticesPtr))
                            {
                                if (tr.Value.VerticesAddr != verticesPtr) // check if any addr changed
                                {
                                    Debug.WriteLine(
                                        $"WARNING - '{tr.Key}' Transform has changed for Player '{Name}'");
                                    Skeleton.ResetTransform(tr.Key); // alloc new transform
                                }
                            }
                        };
                    }
                };
            }
        }

        /// <summary>
        /// Set player rotation (Direction/Pitch)
        /// </summary>
        protected virtual bool SetRotation(Vector2 rotation)
        {
            try
            {
                rotation.ThrowIfAbnormalAndNotZero(nameof(rotation));
                rotation.X = rotation.X.NormalizeAngle();
                ArgumentOutOfRangeException.ThrowIfLessThan(rotation.X, 0f);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(rotation.X, 360f);
                ArgumentOutOfRangeException.ThrowIfLessThan(rotation.Y, -90f);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(rotation.Y, 90f);
                Rotation = rotation;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Refresh Gear if Active Human Player.
        /// </summary>
        public void RefreshGear()
        {
            try
            {
                Gear ??= new GearManager(this, IsPmc);
                Gear?.Refresh();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GearManager] ERROR for Player {Name}: {ex}");
            }
        }

        /// <summary>
        /// Refresh item in player's hands.
        /// </summary>
        public void RefreshHands()
        {
            try
            {
                if (IsActive && IsAlive)
                {
                    Hands ??= new HandsManager(this);
                    Hands?.Refresh();
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// All implementations are 6 elements long, so this is fine for now. If the chain ever updates we'll need to tweak this.
        /// </summary>
        internal const int TransformInternalChainCount = 6;
        /// <summary>
        /// Get the Transform Internal Chain for this Player.
        /// </summary>
        /// <param name="bone">Bone to lookup.</param>
        /// <param name="offsets">Buffer to receive offsets.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void GetTransformInternalChain(Bones bone, scoped Span<uint> offsets) =>
            throw new NotImplementedException();

        #endregion

        #region AI Player Types

        public readonly struct AIRole
        {
            public readonly string Name { get; init; }
            public readonly PlayerType Type { get; init; }
        }

        /// <summary>
        /// Lookup AI Info based on Voice Line.
        /// </summary>
        /// <param name="voiceLine"></param>
        /// <returns></returns>
        public static AIRole GetAIRoleInfo(string voiceLine)
        {
            switch (voiceLine)
            {
                case "BossSanitar":
                    return new AIRole
                    {
                        Name = "Sanitar",
                        Type = PlayerType.AIBoss
                    };
                case "BossBully":
                    return new AIRole
                    {
                        Name = "Reshala",
                        Type = PlayerType.AIBoss
                    };
                case "BossGluhar":
                    return new AIRole
                    {
                        Name = "Gluhar",
                        Type = PlayerType.AIBoss
                    };
                case "SectantPriest":
                    return new AIRole
                    {
                        Name = "Priest",
                        Type = PlayerType.AIBoss
                    };
                case "SectantWarrior":
                    return new AIRole
                    {
                        Name = "Cultist",
                        Type = PlayerType.AIRaider
                    };
                case "BossKilla":
                    return new AIRole
                    {
                        Name = "Killa",
                        Type = PlayerType.AIBoss
                    };
                case "BossTagilla":
                    return new AIRole
                    {
                        Name = "Tagilla",
                        Type = PlayerType.AIBoss
                    };
                case "Boss_Partizan":
                    return new AIRole
                    {
                        Name = "Partisan",
                        Type = PlayerType.AIBoss
                    };
                case "BossBigPipe":
                    return new AIRole
                    {
                        Name = "Big Pipe",
                        Type = PlayerType.AIBoss
                    };
                case "BossBirdEye":
                    return new AIRole
                    {
                        Name = "Birdeye",
                        Type = PlayerType.AIBoss
                    };
                case "BossKnight":
                    return new AIRole
                    {
                        Name = "Knight",
                        Type = PlayerType.AIBoss
                    };
                case "Arena_Guard_1":
                    return new AIRole
                    {
                        Name = "Arena Guard",
                        Type = PlayerType.AIScav
                    };
                case "Arena_Guard_2":
                    return new AIRole
                    {
                        Name = "Arena Guard",
                        Type = PlayerType.AIScav
                    };
                case "Boss_Kaban":
                    return new AIRole
                    {
                        Name = "Kaban",
                        Type = PlayerType.AIBoss
                    };
                case "Boss_Kollontay":
                    return new AIRole
                    {
                        Name = "Kollontay",
                        Type = PlayerType.AIBoss
                    };
                case "Boss_Sturman":
                    return new AIRole
                    {
                        Name = "Shturman",
                        Type = PlayerType.AIBoss
                    };
                case "Zombie_Generic":
                    return new AIRole
                    {
                        Name = "Zombie",
                        Type = PlayerType.AIScav
                    };
                case "BossZombieTagilla":
                    return new AIRole
                    {
                        Name = "Zombie Tagilla",
                        Type = PlayerType.AIBoss
                    };
                case "Zombie_Fast":
                    return new AIRole
                    {
                        Name = "Zombie",
                        Type = PlayerType.AIScav
                    };
                case "Zombie_Medium":
                    return new AIRole
                    {
                        Name = "Zombie",
                        Type = PlayerType.AIScav
                    };
            }
            if (voiceLine.Contains("scav", StringComparison.OrdinalIgnoreCase))
                return new AIRole
                {
                    Name = "Scav",
                    Type = PlayerType.AIScav
                };
            if (voiceLine.Contains("boss", StringComparison.OrdinalIgnoreCase))
                return new AIRole
                {
                    Name = "Boss",
                    Type = PlayerType.AIBoss
                };
            if (voiceLine.Contains("usec", StringComparison.OrdinalIgnoreCase))
                return new AIRole
                {
                    Name = "Usec",
                    Type = PlayerType.AIRaider
                };
            if (voiceLine.Contains("bear", StringComparison.OrdinalIgnoreCase))
                return new AIRole
                {
                    Name = "Bear",
                    Type = PlayerType.AIRaider
                };
            Debug.WriteLine($"Unknown Voice Line: {voiceLine}");
            return new AIRole
            {
                Name = "AI",
                Type = PlayerType.AIScav
            };
        }

        #endregion

        #region Interfaces

        public virtual ref readonly Vector3 Position => ref Skeleton.Root.Position;
        public Vector2 MouseoverPosition { get; set; }

        public void Draw(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            try
            {
                var point = Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams);
                MouseoverPosition = new Vector2(point.X, point.Y);
                if (!IsAlive) // Player Dead -- Draw 'X' death marker and move on
                {
                    DrawDeathMarker(canvas, point);
                }
                else
                {
                    DrawPlayerPill(canvas, localPlayer, point);
                    if (this == localPlayer)
                        return;
                    var height = Position.Y - localPlayer.Position.Y;
                    var dist = Vector3.Distance(localPlayer.Position, Position);
                    using var lines = new PooledList<string>();
                    if (!App.Config.UI.HideNames) // show full names & info
                    {
                        string name = null;
                        if (IsError)
                            name = "ERROR"; // In case POS stops updating, let us know!
                        else
                            name = Name;
                        string health = null; string level = null;
                        if (this is ObservedPlayer observed)
                        {
                            health = observed.HealthStatus is Enums.ETagStatus.Healthy
                                ? null
                                : $" ({observed.HealthStatus})"; // Only display abnormal health status
                            if (observed.Profile?.Level is int levelResult)
                                level = $"L{levelResult}:";
                        }
                        lines.Add($"{level}{name}{health}");
                        lines.Add($"H: {height:n0} D: {dist:n0}");
                    }
                    else // just height, distance
                    {
                        lines.Add($"{height:n0},{dist:n0}");
                        if (IsError)
                            lines[0] = "ERROR"; // In case POS stops updating, let us know!
                    }

                    if (Type is not PlayerType.Teammate
                        && ((Gear?.Loot?.Any(x => x.IsImportant) ?? false) ||
                            (App.Config.QuestHelper.Enabled && (Gear?.HasQuestItems ?? false))
                        ))
                        lines[0] = $"!!{lines[0]}"; // Notify important loot
                    DrawPlayerText(canvas, point, lines);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WARNING! Player Draw Error: {ex}");
            }
        }

        /// <summary>
        /// Draws a Player Pill on this location.
        /// </summary>
        private void DrawPlayerPill(SKCanvas canvas, LocalPlayer localPlayer, SKPoint point)
        {
            var paints = GetPaints();
            if (this != localPlayer && RadarViewModel.MouseoverGroup is int grp && grp == GroupID)
                paints.Item1 = SKPaints.PaintMouseoverGroup;

            float scale = 1.65f * App.Config.UI.UIScale;

            canvas.Save();
            canvas.Translate(point.X, point.Y);
            canvas.Scale(scale, scale);
            canvas.RotateDegrees(MapRotation);

            SKPaints.ShapeOutline.StrokeWidth = paints.Item1.StrokeWidth * 1.3f;
            // Draw the pill
            canvas.DrawPath(_playerPill, SKPaints.ShapeOutline); // outline
            canvas.DrawPath(_playerPill, paints.Item1);

            var aimlineLength = this == localPlayer || (IsFriendly && App.Config.UI.TeammateAimlines) ?
                App.Config.UI.AimLineLength : 0;
            if (!IsFriendly &&
                !(IsAI && !App.Config.UI.AIAimlines) &&
                this.IsFacingTarget(localPlayer, App.Config.UI.MaxDistance)) // Hostile Player, check if aiming at a friendly (High Alert)
                aimlineLength = 9999;

            if (aimlineLength > 0)
            {
                // Draw line from nose tip forward
                canvas.DrawLine(PP_NOSE_X, 0, PP_NOSE_X + aimlineLength, 0, SKPaints.ShapeOutline); // outline
                canvas.DrawLine(PP_NOSE_X, 0, PP_NOSE_X + aimlineLength, 0, paints.Item1);
            }

            canvas.Restore();
        }

        /// <summary>
        /// Draws a Death Marker on this location.
        /// </summary>
        private static void DrawDeathMarker(SKCanvas canvas, SKPoint point)
        {
            float scale = App.Config.UI.UIScale;

            canvas.Save();
            canvas.Translate(point.X, point.Y);
            canvas.Scale(scale, scale);
            canvas.DrawPath(_deathMarker, SKPaints.PaintDeathMarker);
            canvas.Restore();
        }

        /// <summary>
        /// Draws Player Text on this location.
        /// </summary>
        private void DrawPlayerText(SKCanvas canvas, SKPoint point, IList<string> lines)
        {
            var paints = GetPaints();
            if (RadarViewModel.MouseoverGroup is int grp && grp == GroupID)
                paints.Item2 = SKPaints.TextMouseoverGroup;
            point.Offset(9.5f * App.Config.UI.UIScale, 0);
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line?.Trim()))
                    continue;


                canvas.DrawText(line, point, SKTextAlign.Left, SKFonts.UIRegular, SKPaints.TextOutline); // Draw outline
                canvas.DrawText(line, point, SKTextAlign.Left, SKFonts.UIRegular, paints.Item2); // draw line text

                point.Offset(0, SKFonts.UIRegular.Spacing);
            }
        }

        private ValueTuple<SKPaint, SKPaint> GetPaints()
        {
            if (IsFocused)
                return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintFocused, SKPaints.TextFocused);
            if (this is LocalPlayer)
                return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintLocalPlayer, SKPaints.TextLocalPlayer);
            switch (Type)
            {
                case PlayerType.Teammate:
                    return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintTeammate, SKPaints.TextTeammate);
                case PlayerType.PMC:
                    return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintPMC, SKPaints.TextPMC);
                case PlayerType.AIScav:
                    return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintScav, SKPaints.TextScav);
                case PlayerType.AIRaider:
                    return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintRaider, SKPaints.TextRaider);
                case PlayerType.AIBoss:
                    return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintBoss, SKPaints.TextBoss);
                case PlayerType.PScav:
                    return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintPScav, SKPaints.TextPScav);
                case PlayerType.SpecialPlayer:
                    return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintWatchlist, SKPaints.TextWatchlist);
                case PlayerType.Streamer:
                    return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintStreamer, SKPaints.TextStreamer);
                default:
                    return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintPMC, SKPaints.TextPMC);
            }
        }

        public void DrawMouseover(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            if (this == localPlayer)
                return;
            using var lines = new PooledList<string>();
            var name = App.Config.UI.HideNames && IsHuman ? "<Hidden>" : Name;
            string health = null;
            if (this is ObservedPlayer observed)
                health = observed.HealthStatus is Enums.ETagStatus.Healthy
                    ? null
                    : $" ({observed.HealthStatus.ToString()})"; // Only display abnormal health status
            if (this is ObservedPlayer obs && obs.IsStreaming) // Streamer Notice
                lines.Add("[LIVE TTV - Double Click]");
            string alert = Alerts?.Trim();
            if (!string.IsNullOrEmpty(alert)) // Special Players,etc.
                lines.Add(alert);
            if (IsHostileActive) // Enemy Players, display information
            {
                lines.Add($"{name}{health} {AccountID}".Trim());
                var gear = Gear;
                var hands = Hands?.DisplayString;
                lines.Add($"Use:{(hands is null ? "--" : hands)}");
                var faction = PlayerSide.ToString();
                string g = null;
                if (GroupID != -1)
                    g = $" G:{GroupID} ";
                lines.Add($"{faction}{g}");
                var loot = gear?.Loot;
                if (loot is not null)
                {
                    var playerValue = Utilities.FormatNumberKM(gear?.Value ?? -1);
                    lines.Add($"Value: {playerValue}");
                    var iterations = 0;
                    foreach (var item in loot)
                    {
                        if (iterations++ >= 5)
                            break; // Only show first 5 Items (HV is on top)
                        lines.Add(item.GetUILabel(App.Config.QuestHelper.Enabled));
                    }
                }
            }
            else if (!IsAlive)
            {
                lines.Add($"{Type.ToString()}:{name}");
                string g = null;
                if (GroupID != -1)
                    g = $"G:{GroupID} ";
                if (g is not null) lines.Add(g);
                var corpseLoot = LootObject?.Loot?.Values?.OrderLoot();
                if (corpseLoot is not null)
                {
                    var sumPrice = corpseLoot.Sum(x => x.Price);
                    var corpseValue = Utilities.FormatNumberKM(sumPrice);
                    lines.Add($"Value: {corpseValue}"); // Player name, value
                    if (corpseLoot.Any())
                        foreach (var item in corpseLoot)
                            lines.Add(item.GetUILabel(App.Config.QuestHelper.Enabled));
                    else lines.Add("Empty");
                }
            }
            else if (IsAIActive)
            {
                lines.Add(name);
            }

            Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams).DrawMouseoverText(canvas, lines.Span);
        }

        #endregion

        #region High Alert

        /// <summary>
        /// True if Current Player is facing <paramref name="target"/>.
        /// </summary>
        public bool IsFacingTarget(AbstractPlayer target, float? maxDist = null)
        {
            Vector3 delta = target.Position - this.Position;

            if (maxDist is float m)
            {
                float maxDistSq = m * m;
                float distSq = Vector3.Dot(delta, delta);
                if (distSq > maxDistSq) return false;
            }

            float distance = delta.Length();
            if (distance <= 1e-6f)
                return true;

            Vector3 fwd = RotationToDirection(this.Rotation);

            float cosAngle = Vector3.Dot(fwd, delta) / distance;

            const float A = 31.3573f;
            const float B = 3.51726f;
            const float C = 0.626957f;
            const float D = 15.6948f;

            float x = MathF.Abs(C - D * distance);
            float angleDeg = A - B * MathF.Log(MathF.Max(x, 1e-6f));
            if (angleDeg < 1f) angleDeg = 1f;
            if (angleDeg > 179f) angleDeg = 179f;

            float cosThreshold = MathF.Cos(angleDeg * (MathF.PI / 180f));
            return cosAngle >= cosThreshold;

            static Vector3 RotationToDirection(Vector2 rotation)
            {
                float yaw = rotation.X * (MathF.PI / 180f);
                float pitch = rotation.Y * (MathF.PI / 180f);

                float cp = MathF.Cos(pitch);
                float sp = MathF.Sin(pitch);
                float sy = MathF.Sin(yaw);
                float cy = MathF.Cos(yaw);

                var dir = new Vector3(
                    cp * sy,
                   -sp,
                    cp * cy
                );

                float lenSq = Vector3.Dot(dir, dir);
                if (lenSq > 0f && MathF.Abs(lenSq - 1f) > 1e-4f)
                {
                    float invLen = 1f / MathF.Sqrt(lenSq);
                    dir *= invLen;
                }
                return dir;
            }
        }

        #endregion
    }
}