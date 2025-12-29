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

using LoneEftDmaRadar.Tarkov.GameWorld.Player.Helpers;
using LoneEftDmaRadar.Tarkov.Unity.Collections;
using LoneEftDmaRadar.Tarkov.Unity.Structures;
using VmmSharpEx.Scatter;

namespace LoneEftDmaRadar.Tarkov.GameWorld.Player
{
    public class ObservedPlayer : AbstractPlayer
    {
        #region Static Interface

        private static readonly ConcurrentDictionary<int, byte> _teammates = new();

        static ObservedPlayer()
        {
            Memory.RaidStopped += Memory_RaidStopped;
        }

        private static void Memory_RaidStopped(object sender, EventArgs e)
        {
            _teammates.Clear();
        }

        #endregion
        /// <summary>
        /// Player's Unique Id within this Raid Instance.
        /// </summary>
        public int Id { get; }
        /// <summary>
        /// Player's Current TarkovDevItems.
        /// </summary>
        public PlayerEquipment Equipment { get; }
        /// <summary>
        /// Address of InventoryController field.
        /// </summary>
        public ulong InventoryControllerAddr { get; }
        /// <summary>
        /// Hands Controller field address.
        /// </summary>
        public ulong HandsControllerAddr { get; }
        /// <summary>
        /// ObservedPlayerController for non-clientplayer players.
        /// </summary>
        private ulong ObservedPlayerController { get; }
        /// <summary>
        /// ObservedHealthController for non-clientplayer players.
        /// </summary>
        private ulong ObservedHealthController { get; }
        /// <summary>
        /// Player is Human-Controlled.
        /// </summary>
        public override bool IsHuman { get; }
        /// <summary>
        /// MovementContext / StateContext
        /// </summary>
        public override ulong MovementContext { get; }
        /// <summary>
        /// Corpse field address..
        /// </summary>
        public override ulong CorpseAddr { get; }
        /// <summary>
        /// Player Rotation Field Address (view angles).
        /// </summary>
        public override ulong RotationAddress { get; }
        /// <summary>
        /// Player's Current Health Status
        /// </summary>
        public Enums.ETagStatus HealthStatus { get; private set; } = Enums.ETagStatus.Healthy;
        /// <summary>
        /// True if the Player is a Teammate.
        /// </summary>
        public bool IsTeammate => _teammates.ContainsKey(Id);

        internal ObservedPlayer(ulong playerBase) : base(playerBase)
        {
            var localPlayer = Memory.LocalPlayer;
            ArgumentNullException.ThrowIfNull(localPlayer, nameof(localPlayer));
            ObservedPlayerController = Memory.ReadPtr(this + Offsets.ObservedPlayerView.ObservedPlayerController);
            ArgumentOutOfRangeException.ThrowIfNotEqual(this,
                Memory.ReadValue<ulong>(ObservedPlayerController + Offsets.ObservedPlayerController.PlayerView),
                nameof(ObservedPlayerController));
            InventoryControllerAddr = ObservedPlayerController + Offsets.ObservedPlayerController.InventoryController;
            HandsControllerAddr = ObservedPlayerController + Offsets.ObservedPlayerController.HandsController;
            ObservedHealthController = Memory.ReadPtr(ObservedPlayerController + Offsets.ObservedPlayerController.HealthController);
            ArgumentOutOfRangeException.ThrowIfNotEqual(this,
                Memory.ReadValue<ulong>(ObservedHealthController + Offsets.ObservedHealthController._player),
                nameof(ObservedHealthController));
            CorpseAddr = ObservedHealthController + Offsets.ObservedHealthController._playerCorpse;

            MovementContext = GetMovementContext();
            RotationAddress = ValidateRotationAddr(MovementContext + Offsets.ObservedPlayerStateContext.Rotation);
            /// Setup Transform
            var ti = Memory.ReadPtrChain(this, false, _transformInternalChain);
            SkeletonRoot = new UnityTransform(ti);
            _ = SkeletonRoot.UpdatePosition();

            bool isAI = Memory.ReadValue<bool>(this + Offsets.ObservedPlayerView.IsAI);
            IsHuman = !isAI;
            Id = GetPlayerId();
            /// Determine Player Type
            PlayerSide = (Enums.EPlayerSide)Memory.ReadValue<int>(this + Offsets.ObservedPlayerView.Side); // Usec,Bear,Scav,etc.
            if (!Enum.IsDefined(PlayerSide)) // Make sure PlayerSide is valid
                throw new ArgumentOutOfRangeException(nameof(PlayerSide));
            if (IsScav)
            {
                if (isAI)
                {
                    var voicePtr = Memory.ReadPtr(this + Offsets.ObservedPlayerView.Voice);
                    string voice = Memory.ReadUnityString(voicePtr);
                    var role = GetAIRoleInfo(voice);
                    Name = role.Name;
                    Type = role.Type;
                }
                else
                {
                    Name = $"PScav{Id}";
                    Type = IsTeammate ?
                        PlayerType.Teammate : PlayerType.PScav;
                }
            }
            else if (IsPmc)
            {
                Name = $"{PlayerSide}{Id}";
                Type = IsTeammate ?
                    PlayerType.Teammate : PlayerType.PMC;
            }
            else
                throw new NotImplementedException(nameof(PlayerSide));
            Equipment = new PlayerEquipment(this);
            GroupId = TryGetGroup(Id);
            if (GroupId == -100)
            {
                Type = PlayerType.Teammate;
            }
        }

        /// <summary>
        /// If the Player is Human, Toggle Teammate Status.
        /// </summary>
        public void ToggleTeammate()
        {
            bool isTeammate = Type == PlayerType.Teammate;
            if (!IsHuman)
                return;
            if (!isTeammate)
            {
                _teammates.TryAdd(Id, 0);
                Type = PlayerType.Teammate;
            }
            else
            {
                _teammates.TryRemove(Id, out _);
                Type = PlayerSide == Enums.EPlayerSide.Savage ? PlayerType.PScav : PlayerType.PMC;
            }
        }

        /// <summary>
        /// Assign this Player to a Group.
        /// </summary>
        /// <param name="groupId"></param>
        public void AssignGroup(int groupId)
        {
            GroupId = groupId;
            Logging.WriteLine($"Player '{Name}' assigned to Group {GroupId}.");
        }

        /// <summary>
        /// Assign this Player as a Teammate to <see cref="LocalPlayer"/>.
        /// </summary>
        public void AssignTeammate()
        {
            Type = PlayerType.Teammate;
            Logging.WriteLine($"Player '{Name}' assigned as Teammate.");
        }

        /// <summary>
        /// Get the Player's ID.
        /// </summary>
        /// <returns>Player Id or 0 if failed.</returns>
        private int GetPlayerId()
        {
            try
            {
                return Memory.ReadValue<int>(this + Offsets.ObservedPlayerView.Id);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Tries to get an existing Group Id (if possible).
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private int TryGetGroup(int id)
        {
            if (!IsPmc)
                return -1;
            if (Memory.LocalPlayer is not LocalPlayer localPlayer)
                return -1;
            if (Program.Config.Cache.Groups.TryGetValue(localPlayer.RaidId, out var groups))
            {
                if (groups.TryGetValue(id, out var group))
                {
                    return group;
                }
            }
            return -1;
        }

        /// <summary>
        /// Get Movement Context Instance.
        /// </summary>
        private ulong GetMovementContext()
        {
            var movementController = Memory.ReadPtrChain(ObservedPlayerController, true, Offsets.ObservedPlayerController.MovementController, Offsets.ObservedPlayerMovementController.ObservedPlayerStateContext);
            return movementController;
        }

        /// <summary>
        /// Sync Player Information.
        /// </summary>
        public override void OnRegRefresh(VmmScatter scatter, ISet<ulong> registered, bool? isActiveParam = null)
        {
            if (isActiveParam is not bool isActive)
                isActive = registered.Contains(this);
            if (isActive)
            {
                UpdateHealthStatus();
            }
            base.OnRegRefresh(scatter, registered, isActive);
        }

        /// <summary>
        /// Get Player's Updated Health Condition
        /// Only works in Online Mode.
        /// </summary>
        private void UpdateHealthStatus()
        {
            try
            {
                var tag = (Enums.ETagStatus)Memory.ReadValue<int>(ObservedHealthController + Offsets.ObservedHealthController.HealthStatus);
                if ((tag & Enums.ETagStatus.Dying) == Enums.ETagStatus.Dying)
                    HealthStatus = Enums.ETagStatus.Dying;
                else if ((tag & Enums.ETagStatus.BadlyInjured) == Enums.ETagStatus.BadlyInjured)
                    HealthStatus = Enums.ETagStatus.BadlyInjured;
                else if ((tag & Enums.ETagStatus.Injured) == Enums.ETagStatus.Injured)
                    HealthStatus = Enums.ETagStatus.Injured;
                else
                    HealthStatus = Enums.ETagStatus.Healthy;
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"ERROR updating Health Status for '{Name}': {ex}");
            }
        }

        private static readonly uint[] _transformInternalChain =
        [
            Offsets.ObservedPlayerView.PlayerBody,
            Offsets.PlayerBody.SkeletonRootJoint,
            Offsets.DizSkinningSkeleton._values,
            UnityList<byte>.ArrOffset,
            UnityList<byte>.ArrStartOffset + (uint)Bones.HumanBase * 0x8,
            0x10
        ];
    }
}
