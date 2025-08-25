using eft_dma_radar.UI.Radar.ViewModels;
using eft_dma_radar.Unity;
using eft_dma_radar.Unity.Collections;
using VmmSharpEx.Scatter;

namespace eft_dma_radar.Tarkov.Player
{
    public class ObservedPlayer : PlayerBase
    {
        /// <summary>
        /// Player's Profile & Stats.
        /// </summary>
        public PlayerProfile Profile { get; }
        /// <summary>
        /// ObservedPlayerController for non-clientplayer players.
        /// </summary>
        private ulong ObservedPlayerController { get; }
        /// <summary>
        /// ObservedHealthController for non-clientplayer players.
        /// </summary>
        private ulong ObservedHealthController { get; }
        /// <summary>
        /// Player name.
        /// </summary>
        public override string Name
        {
            get => Profile?.Name ?? "Unknown";
            set
            {
                if (Profile is PlayerProfile profile)
                    profile.Name = value;
            }
        }
        /// <summary>
        /// Type of player unit.
        /// </summary>
        public override PlayerType Type
        {
            get => Profile?.Type ?? PlayerType.Default;
            protected set
            {
                if (Profile is PlayerProfile profile)
                    profile.Type = value;
            }
        }
        /// <summary>
        /// Player Alerts.
        /// </summary>
        public override string Alerts
        {
            get => Profile?.Alerts;
            protected set
            {
                if (Profile is PlayerProfile profile)
                    profile.Alerts = value;
            }
        }
        /// <summary>
        /// Twitch.tv Channel URL for this player (if available).
        /// </summary>
        public string TwitchChannelURL => Profile?.TwitchChannelURL;
        /// <summary>
        /// True if player is TTV Streaming.
        /// </summary>
        public bool IsStreaming => TwitchChannelURL is not null;
        /// <summary>
        /// Account UUID for Human Controlled Players.
        /// </summary>
        public override string AccountID
        {
            get
            {
                if (Profile?.AccountID is string id)
                    return id;
                return "";
            }
        }
        /// <summary>
        /// Group that the player belongs to.
        /// </summary>
        public override int GroupID
        {
            get => Profile?.GroupID ?? -1;
            protected set
            {
                if (Profile is PlayerProfile profile)
                    profile.GroupID = value;
            }
        }
        /// <summary>
        /// Player's Faction.
        /// </summary>
        public override Enums.EPlayerSide PlayerSide
        {
            get => Profile?.PlayerSide ?? Enums.EPlayerSide.Savage;
            protected set
            {
                if (Profile is PlayerProfile profile)
                    profile.PlayerSide = value;
            }
        }
        /// <summary>
        /// Player is Human-Controlled.
        /// </summary>
        public override bool IsHuman { get; }
        /// <summary>
        /// MovementContext / StateContext
        /// </summary>
        public override ulong MovementContext { get; }
        /// <summary>
        /// EFT.PlayerBody
        /// </summary>
        public override ulong Body { get; }
        /// <summary>
        /// Inventory Controller field address.
        /// </summary>
        public override ulong InventoryControllerAddr { get; }
        /// <summary>
        /// Hands Controller field address.
        /// </summary>
        public override ulong HandsControllerAddr { get; }
        /// <summary>
        /// Corpse field address..
        /// </summary>
        public override ulong CorpseAddr { get; }
        /// <summary>
        /// Player Rotation Field Address (view angles).
        /// </summary>
        public override ulong RotationAddress { get; }
        /// <summary>
        /// Player's Skeleton Bones.
        /// </summary>
        public override Skeleton Skeleton { get; }
        /// <summary>
        /// Player's Current Health Status
        /// </summary>
        public Enums.ETagStatus HealthStatus { get; private set; } = Enums.ETagStatus.Healthy;

        internal ObservedPlayer(ulong playerBase) : base(playerBase)
        {
            var localPlayer = Memory.LocalPlayer;
            ArgumentNullException.ThrowIfNull(localPlayer, nameof(localPlayer));
            ObservedPlayerController = Memory.ReadPtr(this + Offsets.ObservedPlayerView.ObservedPlayerController);
            ArgumentOutOfRangeException.ThrowIfNotEqual(this,
                Memory.ReadValue<ulong>(ObservedPlayerController + Offsets.ObservedPlayerController.Player),
                nameof(ObservedPlayerController));
            ObservedHealthController = Memory.ReadPtr(ObservedPlayerController + Offsets.ObservedPlayerController.HealthController);
            ArgumentOutOfRangeException.ThrowIfNotEqual(this,
                Memory.ReadValue<ulong>(ObservedHealthController + Offsets.ObservedHealthController.Player),
                nameof(ObservedHealthController));
            Body = Memory.ReadPtr(this + Offsets.ObservedPlayerView.PlayerBody);
            InventoryControllerAddr = ObservedPlayerController + Offsets.ObservedPlayerController.InventoryController;
            HandsControllerAddr = ObservedPlayerController + Offsets.ObservedPlayerController.HandsController;
            CorpseAddr = ObservedHealthController + Offsets.ObservedHealthController.PlayerCorpse;

            MovementContext = GetMovementContext();
            RotationAddress = ValidateRotationAddr(MovementContext + Offsets.ObservedMovementController.Rotation);
            /// Setup Transforms
            Skeleton = new Skeleton(this, GetTransformInternalChain);

            bool isAI = Memory.ReadValue<bool>(this + Offsets.ObservedPlayerView.IsAI);
            IsHuman = !isAI;
            Profile = new PlayerProfile(this, GetAccountID());
            // Get Group ID
            GroupID = GetGroupID();
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
                    int pscavNumber = Interlocked.Increment(ref _playerScavNumber);
                    Name = $"PScav{pscavNumber}";
                    Type = GroupID != -1 && GroupID == localPlayer.GroupID ?
                        PlayerType.Teammate : PlayerType.PScav;
                }
            }
            else if (IsPmc)
            {
                Name = "PMC";
                Type = GroupID != -1 && GroupID == localPlayer.GroupID ?
                    PlayerType.Teammate : PlayerType.PMC;
            }
            else
                throw new NotImplementedException(nameof(PlayerSide));
            if (IsHuman)
            {
                PlayerHistoryViewModel.Add(this); /// Log To Player History
            }
            if (IsHumanHostile) /// Special Players Check on Hostiles Only
            {
                if (MainWindow.Instance?.PlayerWatchlist?.ViewModel is PlayerWatchlistViewModel vm &&
                    vm.Watchlist.TryGetValue(AccountID, out var watchlistEntry)) // player is on watchlist
                {
                    Type = PlayerType.SpecialPlayer; // Flag watchlist player
                    UpdateAlerts($"[Watchlist] {watchlistEntry.Reason} @ {watchlistEntry.Timestamp}");
                }
            }
        }

        /// <summary>
        /// Get Player's Account ID.
        /// </summary>
        /// <returns>Account ID Numeric String.</returns>
        private string GetAccountID()
        {
            if (!IsHuman)
                return "AI";
            var idPTR = Memory.ReadPtr(this + Offsets.ObservedPlayerView.AccountId);
            return Memory.ReadUnityString(idPTR);
        }

        /// <summary>
        /// Gets player's Group Number.
        /// </summary>
        private int GetGroupID()
        {
            try
            {
                var grpIdPtr = Memory.ReadPtr(this + Offsets.ObservedPlayerView.GroupID);
                var grp = Memory.ReadUnityString(grpIdPtr);
                return _groups.GetGroup(grp);
            }
            catch { return -1; } // will return null if Solo / Don't have a team
        }

        /// <summary>
        /// Get Movement Context Instance.
        /// </summary>
        private ulong GetMovementContext()
        {
            var movementController = Memory.ReadPtrChain(ObservedPlayerController, Offsets.ObservedPlayerController.MovementController);
            return movementController;
        }

        /// <summary>
        /// Refresh Player Information.
        /// </summary>
        public override void OnRegRefresh(ScatterReadIndex index, IReadOnlySet<ulong> registered, bool? isActiveParam = null)
        {
            if (isActiveParam is not bool isActive)
                isActive = registered.Contains(this);
            if (isActive)
            {
                UpdateHealthStatus();
            }
            base.OnRegRefresh(index, registered, isActive);
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
                Debug.WriteLine($"ERROR updating Health Status for '{Name}': {ex}");
            }
        }

        /// <summary>
        /// Get the Transform Internal Chain for this Player.
        /// </summary>
        /// <param name="bone">Bone to lookup.</param>
        /// <returns>Array of offsets for transform internal chain.</returns>
        public override uint[] GetTransformInternalChain(Bones bone)
        {
            return new uint[] { Offsets.ObservedPlayerView.PlayerBody, Offsets.PlayerBody.SkeletonRootJoint, Offsets.DizSkinningSkeleton._values, UnityList<byte>.ArrOffset, UnityList<byte>.ArrStartOffset + (uint)bone * 0x8, 0x10 };
        }
    }
}
