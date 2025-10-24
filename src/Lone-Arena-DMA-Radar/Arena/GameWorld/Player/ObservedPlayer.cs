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

using LoneArenaDmaRadar.Arena.Mono.Collections;
using LoneArenaDmaRadar.Arena.Unity.Structures;
using LoneArenaDmaRadar.Web.Twitch;
using VmmSharpEx.Scatter;

namespace LoneArenaDmaRadar.Arena.GameWorld.Player
{
    public class ObservedPlayer : AbstractPlayer
    {
        /// <summary>
        /// ObservedPlayerController for non-clientplayer players.
        /// </summary>
        private ulong ObservedPlayerController { get; }
        /// <summary>
        /// ObservedHealthController for non-clientplayer players.
        /// </summary>
        private ulong ObservedHealthController { get; }
        /// <summary>
        /// Twitch.tv Channel URL for this player (if available).
        /// </summary>
        public string TwitchChannelURL { get; private set; }
        /// <summary>
        /// True if player is TTV Streaming.
        /// </summary>
        public bool IsStreaming => TwitchChannelURL is not null;
        /// <summary>
        /// Player's Current Health Status
        /// </summary>
        public Enums.ETagStatus HealthStatus { get; private set; } = Enums.ETagStatus.Healthy;

        internal ObservedPlayer(ulong playerBase) : base(playerBase)
        {
            var cameraType = Memory.ReadValue<int>(this + Offsets.ObservedPlayerView.VisibleToCameraType);
            ArgumentOutOfRangeException.ThrowIfNotEqual(cameraType, (int)Enums.ECameraType.Default, nameof(cameraType));
            ObservedPlayerController = Memory.ReadPtr(this + Offsets.ObservedPlayerView.ObservedPlayerController);
            ArgumentOutOfRangeException.ThrowIfNotEqual(this,
                Memory.ReadValue<ulong>(ObservedPlayerController + Offsets.ObservedPlayerController.Player),
                nameof(ObservedPlayerController));
            ObservedHealthController = Memory.ReadPtr(ObservedPlayerController + Offsets.ObservedPlayerController.HealthController);
            ArgumentOutOfRangeException.ThrowIfNotEqual(this,
                Memory.ReadValue<ulong>(ObservedHealthController + Offsets.ObservedHealthController.Player),
                nameof(ObservedHealthController));
            CorpseAddr = ObservedHealthController + Offsets.ObservedHealthController.PlayerCorpse;

            AccountID = GetAccountID();
            IsFocused = CheckIfFocused();
            GroupID = GetTeamID();
            MovementContext = GetMovementContext();
            RotationAddress = ValidateRotationAddr(MovementContext + Offsets.ObservedMovementController.Rotation);
            /// Setup Transform
            Span<uint> tiOffsets = stackalloc uint[ObservedPlayer.TransformInternalChainCount];
            GetTransformInternalChain(Unity.Structures.Bones.HumanBase, tiOffsets);
            var tiRoot = Memory.ReadPtrChain(this, true, tiOffsets);
            SkeletonRoot = new UnityTransform(tiRoot);
            _ = SkeletonRoot.UpdatePosition();

            bool isAI = Memory.ReadValue<bool>(this + Offsets.ObservedPlayerView.IsAI);
            IsHuman = !isAI;
            if (isAI)
            {
                Name = "AI";
                Type = PlayerType.Bot;
            }
            else // Human Player
            {
                if (LocalGameWorld.MatchHasTeams)
                    ArgumentOutOfRangeException.ThrowIfEqual(GroupID, -1, nameof(GroupID));

                Name = GetName();
                Type = GroupID != -1 && GroupID == Memory.LocalPlayer.GroupID ?
                    PlayerType.Teammate : PlayerType.Player;
            }
            if (IsHumanHostile)
            {
                _ = RunTwitchLookupAsync(Name);
            }
        }

        /// <summary>
        /// Runs the Twitch Lookup for the Player's Nickname.
        /// </summary>
        private async Task RunTwitchLookupAsync(string nickname)
        {
            string twitchLogin = await TwitchService.LookupAsync(nickname);
            if (twitchLogin is not null)
            {
                TwitchChannelURL = $"https://twitch.tv/{twitchLogin}";
                Type = PlayerType.Streamer; // Flag streamers
            }
        }

        /// <summary>
        /// Get Player's Account ID.
        /// </summary>
        /// <returns>Account ID Numeric String.</returns>
        private string GetAccountID()
        {
            var idPTR = Memory.ReadPtr(this + Offsets.ObservedPlayerView.AccountId);
            return Memory.ReadUnicodeString(idPTR);
        }

        /// <summary>
        /// Gets player's Team ID.
        /// </summary>
        private int GetTeamID()
        {
            try
            {
                var inventoryController = Memory.ReadPtr(ObservedPlayerController + Offsets.ObservedPlayerController.InventoryController);
                return GetTeamID(inventoryController);
            }
            catch { return -1; }
        }

        /// <summary>
        /// Get Player Name.
        /// </summary>
        /// <returns>Player Name String.</returns>
        private string GetName()
        {
            var namePtr = Memory.ReadPtr(this + Offsets.ObservedPlayerView.NickName);
            var name = Memory.ReadUnicodeString(namePtr)?.Trim();
            if (string.IsNullOrEmpty(name))
                name = "default";
            return name;
        }

        /// <summary>
        /// Get Movement Context Instance.
        /// </summary>
        private ulong GetMovementContext()
        {
            var movementController = Memory.ReadPtrChain(ObservedPlayerController, true, Offsets.ObservedPlayerController.MovementController);
            return movementController;
        }

        /// <summary>
        /// Refresh Player Information.
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
                Debug.WriteLine($"ERROR updating Health Status for '{Name}': {ex}");
            }
        }

        /// <summary>
        /// Get the Transform Internal Chain for this Player.
        /// </summary>
        /// <param name="bone">Bone to lookup.</param>
        /// <param name="offsets">Buffer to receive offsets.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void GetTransformInternalChain(Bones bone, Span<uint> offsets)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(offsets.Length, AbstractPlayer.TransformInternalChainCount, nameof(offsets));
            offsets[0] = Offsets.ObservedPlayerView.PlayerBody;
            offsets[1] = Offsets.PlayerBody.SkeletonRootJoint;
            offsets[2] = Offsets.DizSkinningSkeleton._values;
            offsets[3] = MonoList<byte>.ArrOffset;
            offsets[4] = MonoList<byte>.ArrStartOffset + (uint)bone * 0x8;
            offsets[5] = 0x10;
        }
    }
}
