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
using LoneArenaDmaRadar.Arena.Mono.Collections;
using LoneArenaDmaRadar.Arena.Unity;
using LoneArenaDmaRadar.Arena.Unity.Structures;
using LoneArenaDmaRadar.DMA;
using LoneArenaDmaRadar.Misc;
using LoneArenaDmaRadar.UI.Radar.Maps;
using LoneArenaDmaRadar.UI.Skia;
using VmmSharpEx.Scatter;
using static LoneArenaDmaRadar.Arena.Unity.Structures.UnityTransform;

namespace LoneArenaDmaRadar.Arena.GameWorld.Player
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
            lock (_focusedPlayers)
            {
                _focusedPlayers.Clear();
            }
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
            var className = ObjectClass.ReadName(playerBase, 64);
            if (className != "ArenaObservedPlayerView")
                throw new ArgumentOutOfRangeException(nameof(className));
            var player = new ObservedPlayer(playerBase);
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
        /// Player name.
        /// </summary>
        public virtual string Name { get; protected set; }

        /// <summary>
        /// Account UUID for Human Controlled Players.
        /// </summary>
        public string AccountID { get; protected set; }

        /// <summary>
        /// Group that the player belongs to.
        /// </summary>
        public int GroupID { get; protected set; } = -1;

        /// <summary>
        /// Player is Human-Controlled.
        /// </summary>
        public bool IsHuman { get; protected set; }

        /// <summary>
        /// MovementContext / StateContext
        /// </summary>
        public ulong MovementContext { get; protected set; }

        /// <summary>
        /// Corpse field address..
        /// </summary>
        public ulong CorpseAddr { get; protected set; }

        /// <summary>
        /// Player Rotation Field Address (view angles).
        /// </summary>
        public ulong RotationAddress { get; protected set; }

        /// <summary>
        /// True if the Player is Active (in the player list).
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Type of player unit.
        /// </summary>
        public virtual PlayerType Type { get; protected set; }

        /// <summary>
        /// Player's Rotation in Local Game World.
        /// </summary>
        public Vector2 Rotation { get; private set; }

        /// <summary>
        /// Player's Map Rotation (with 90 degree correction applied).
        /// </summary>
        public float MapRotation
        {
            get
            {
                float mapRotation = Rotation.X; // Cache value
                mapRotation -= 90f;
                while (mapRotation < 0f)
                    mapRotation += 360f;

                return mapRotation;
            }
        }

        /// <summary>
        /// Corpse field value.
        /// </summary>
        public ulong? Corpse { get; private set; }

        /// <summary>
        /// Player's Skeleton Root.
        /// </summary>
        public UnityTransform SkeletonRoot { get; protected set; }

        /// <summary>
        /// TRUE if critical memory reads (position/rotation) have failed.
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        /// True if player is being focused via Right-Click (UI).
        /// </summary>
        public bool IsFocused { get; protected set; }

        #endregion

        #region Boolean Getters

        /// <summary>
        /// Player is AI-Controlled.
        /// </summary>
        public bool IsAI => !IsHuman;
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
        /// Player is AI Controlled and Alive/Active.
        /// </summary>
        public bool IsAIActive => IsAI && IsActive && IsAlive;

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

        /// <summary>
        /// Returns the Team ID of a player based on their Armband Color.
        /// </summary>
        /// <param name="inventoryController">Player's Inventory Controller.</param>
        /// <returns>Team ID. -1 if not found.</returns>
        protected static int GetTeamID(ulong inventoryController)
        {
            var inventory = Memory.ReadPtr(inventoryController + Offsets.InventoryController.Inventory);
            var equipment = Memory.ReadPtr(inventory + Offsets.Inventory.Equipment);
            var slots = Memory.ReadPtr(equipment + Offsets.Equipment.Slots);
            using var slotsArray = MonoArray<ulong>.Create(slots);

            foreach (var slotPtr in slotsArray)
            {
                var slotNamePtr = Memory.ReadPtr(slotPtr + Offsets.Slot.ID);
                string name = Memory.ReadUnicodeString(slotNamePtr);
                if (name == "ArmBand")
                {
                    var containedItem = Memory.ReadPtr(slotPtr + Offsets.Slot.ContainedItem);
                    var itemTemplate = Memory.ReadPtr(containedItem + Offsets.LootItem.Template);
                    var idPtr = Memory.ReadValue<MongoID>(itemTemplate + Offsets.ItemTemplate._id);
                    string id = idPtr.ReadString();

                    if (id == "63615c104bc92641374a97c8")
                        return (int)Enums.ArmbandColorType.red;
                    else if (id == "63615bf35cb3825ded0db945")
                        return (int)Enums.ArmbandColorType.fuchsia;
                    else if (id == "63615c36e3114462cd79f7c1")
                        return (int)Enums.ArmbandColorType.yellow;
                    else if (id == "63615bfc5cb3825ded0db947")
                        return (int)Enums.ArmbandColorType.green;
                    else if (id == "63615bc6ff557272023d56ac")
                        return (int)Enums.ArmbandColorType.azure;
                    else if (id == "63615c225cb3825ded0db949")
                        return (int)Enums.ArmbandColorType.white;
                    else if (id == "63615be82e60050cb330ef2f")
                        return (int)Enums.ArmbandColorType.blue;
                    else
                        return -1;
                }
            }

            return -1;
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
            IsActive = true;
        }

        /// <summary>
        /// Executed on each Realtime Loop.
        /// </summary>
        /// <param name="index">Scatter read index dedicated to this player.</param>
        public virtual void OnRealtimeLoop(VmmScatter scatter)
        {
            scatter.PrepareReadValue<Vector2>(RotationAddress); // Rotation
            scatter.PrepareReadArray<TrsX>(SkeletonRoot.VerticesAddr, SkeletonRoot.Count); // Transform Vertices

            scatter.Completed += (sender, s) =>
            {
                bool p1 = false;
                bool p2 = false;
                if (s.ReadValue<Vector2>(RotationAddress, out var rotation))
                    p1 = SetRotation(ref rotation);
                if (s.ReadArray<TrsX>(SkeletonRoot.VerticesAddr, SkeletonRoot.Count) is PooledMemory<TrsX> vertices)
                {
                    using (vertices)
                    {
                        try
                        {
                            try
                            {
                                _ = SkeletonRoot.UpdatePosition(vertices.Span);
                                p2 = true;
                            }
                            catch (Exception ex) // Attempt to re-allocate Transform on error
                            {
                                Debug.WriteLine($"ERROR getting Player '{Name}' SkeletonRoot Position: {ex}");
                                var newRoot = new UnityTransform(SkeletonRoot.TransformInternal, false);
                                SkeletonRoot = newRoot;
                            }
                        }
                        catch
                        {
                        }
                    }
                }

                IsError = !(p1 && p2);
            };
        }

        /// <summary>
        /// Executed on each Transform Validation Loop.
        /// </summary>
        /// <param name="round1">Index (round 1)</param>
        /// <param name="round2">Index (round 2)</param>
        public void OnValidateTransforms(VmmScatter round1, VmmScatter round2)
        {
            round1.PrepareReadPtr(SkeletonRoot.TransformInternal + UnitySDK.TransformInternal.TransformAccess); // Bone Hierarchy
            round1.Completed += (sender, s1) =>
            {
                if (s1.ReadPtr(SkeletonRoot.TransformInternal + UnitySDK.TransformInternal.TransformAccess, out var tra))
                {
                    round2.PrepareReadPtr(tra + UnitySDK.TransformAccess.Vertices); // Vertices Ptr
                    round2.Completed += (sender, x2) =>
                    {
                        if (x2.ReadPtr(tra + UnitySDK.TransformAccess.Vertices, out var verticesPtr))
                        {
                            if (SkeletonRoot.VerticesAddr != verticesPtr) // check if any addr changed
                            {
                                Debug.WriteLine($"WARNING - SkeletonRoot Transform has changed for Player '{Name}'");
                                var newRoot = new UnityTransform(SkeletonRoot.TransformInternal, false);
                                SkeletonRoot = newRoot;
                            }
                        }
                    };
                }
            };
        }

        /// <summary>
        /// Set player rotation (Direction/Pitch)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual bool SetRotation(ref Vector2 rotation)
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

        #region Interfaces

        public virtual ref readonly Vector3 Position => ref SkeletonRoot.Position;
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
                    string name = Name ?? "<Unknown>";
                    string health = null;
                    if (this is ObservedPlayer observed)
                    {
                        health = observed.HealthStatus is Enums.ETagStatus.Healthy
                            ? null
                            : $" ({observed.HealthStatus})"; // Only display abnormal health status
                    }
                    using var lines = new PooledList<string>();
                    lines.Add($"{name}{health}");
                    lines.Add($"H: {height:n0} D: {dist:n0}");
                    if (IsError)
                        lines[0] = "ERROR"; // In case POS stops updating, let us know!

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

            float scale = 1.65f * App.Config.UI.UIScale;

            canvas.Save();
            canvas.Translate(point.X, point.Y);
            canvas.Scale(scale, scale);
            canvas.RotateDegrees(MapRotation);

            SKPaints.ShapeOutline.StrokeWidth = paints.Item1.StrokeWidth * 1.3f;
            // Draw the pill
            canvas.DrawPath(_playerPill, SKPaints.ShapeOutline); // outline
            canvas.DrawPath(_playerPill, paints.Item1);

            var aimlineLength = this == localPlayer ? 
                App.Config.UI.AimLineLength : 0;
            // High Alert -> Check if aiming at Local Player
            if (App.Config.UI.HighAlert &&
                !IsFriendly && 
                this.IsFacingTarget(localPlayer))
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
                case PlayerType.Player:
                    return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintPlayer, SKPaints.TextPlayer);
                case PlayerType.Bot:
                    return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintBot, SKPaints.TextBot);
                case PlayerType.Streamer:
                    return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintStreamer, SKPaints.TextStreamer);
                default:
                    return new ValueTuple<SKPaint, SKPaint>(SKPaints.PaintPlayer, SKPaints.TextPlayer);
            }
        }

        public void DrawMouseover(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            if (this == localPlayer)
                return;
            using var lines = new PooledList<string>();
            string health = null;
            if (this is ObservedPlayer observed)
                health = observed.HealthStatus is Enums.ETagStatus.Healthy
                    ? null
                    : $" ({observed.HealthStatus.ToString()})"; // Only display abnormal health status
            if (this is ObservedPlayer obs && obs.IsStreaming) // Streamer Notice
                lines.Add("[LIVE TTV - Double Click]");
            string name = Name ?? "<Unknown>";
            if (IsHostileActive) // Enemy Players, display information
            {
                lines.Add($"{name}{health} {AccountID}".Trim());
                string g = null;
                if (GroupID != -1)
                    g = $" G:{GroupID} ";
                lines.Add(g);
            }
            else if (!IsAlive)
            {
                lines.Add($"{Type.ToString()}:{name}");
                string g = null;
                if (GroupID != -1)
                    g = $"G:{GroupID} ";
                if (g is not null) lines.Add(g);
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
        public bool IsFacingTarget(AbstractPlayer target)
        {
            Vector3 delta = target.Position - this.Position;

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

        #region Focused Players
        private static readonly HashSet<string> _focusedPlayers = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Toggles this player's focused status.
        /// Only applies to Human Controlled Players.
        /// </summary>
        public void ToggleFocus()
        {
            if (this is not ObservedPlayer || !this.IsHumanActive)
                return;
            string id = this.AccountID;
            if (string.IsNullOrEmpty(id))
                return;
            lock (_focusedPlayers)
            {
                bool isFocused = _focusedPlayers.Contains(id);
                if (isFocused)
                    _focusedPlayers.Remove(id);
                else
                    _focusedPlayers.Add(id);
                IsFocused = !isFocused;
            }
        }

        /// <summary>
        /// Check if this Player was focused prior (from a different round,etc.)
        /// </summary>
        /// <returns>True if focused, otherwise False.</returns>
        protected bool CheckIfFocused()
        {
            string id = this.AccountID?.Trim();
            if (string.IsNullOrEmpty(id))
                return false;
            lock (_focusedPlayers)
            {
                return _focusedPlayers.Contains(id);
            }
        }
        #endregion
    }
}