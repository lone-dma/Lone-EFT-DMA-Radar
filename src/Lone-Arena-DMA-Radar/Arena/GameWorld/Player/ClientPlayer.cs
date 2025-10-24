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
using static SDK.Offsets;

namespace LoneArenaDmaRadar.Arena.GameWorld.Player
{
    public class ClientPlayer : AbstractPlayer
    {
        /// <summary>
        /// EFT.Profile Address
        /// </summary>
        public ulong Profile { get; }
        /// <summary>
        /// ICharacterController
        /// </summary>
        public ulong CharacterController { get; }
        /// <summary>
        /// PlayerInfo Address (GClass1044)
        /// </summary>
        public ulong Info { get; }

        internal ClientPlayer(ulong playerBase) : base(playerBase)
        {
            Profile = Memory.ReadPtr(this + Offsets.Player.Profile);
            Info = Memory.ReadPtr(Profile + Offsets.Profile.Info);
            Body = Memory.ReadPtr(this + Offsets.Player._playerBody);
            CorpseAddr = this + Offsets.Player.Corpse;

            AccountID = GetAccountID();
            GroupID = GetTeamID();
            if (LocalGameWorld.MatchHasTeams)
                ArgumentOutOfRangeException.ThrowIfEqual(GroupID, -1, nameof(GroupID)); 
            MovementContext = GetMovementContext();
            RotationAddress = ValidateRotationAddr(MovementContext + Offsets.MovementContext._rotation);
            /// Setup Transform
            Span<uint> tiOffsets = stackalloc uint[ClientPlayer.TransformInternalChainCount];
            GetTransformInternalChain(Unity.Structures.Bones.HumanBase, tiOffsets);
            var tiRoot = Memory.ReadPtrChain(this, true, tiOffsets);
            SkeletonRoot = new UnityTransform(tiRoot);
            _ = SkeletonRoot.UpdatePosition();

            if (this is LocalPlayer) // Handled in derived class
                return;

            IsHuman = true;
            Name = GetName();
            Type = PlayerType.Player;
        }

        /// <summary>
        /// Get Player Name.
        /// </summary>
        /// <returns>Player Name String.</returns>
        private string GetName()
        {
            var namePtr = Memory.ReadPtr(Info + Offsets.PlayerInfo.Nickname);
            var name = Memory.ReadUnicodeString(namePtr)?.Trim();
            if (string.IsNullOrEmpty(name))
                name = "default";
            return name;
        }

        /// <summary>
        /// Gets player's Team ID.
        /// </summary>
        private int GetTeamID()
        {
            try
            {
                var inventoryController = Memory.ReadPtr(this + Offsets.Player._inventoryController);
                return GetTeamID(inventoryController);
            }
            catch { return -1; }
        }

        /// <summary>
        /// Get Player's Account ID.
        /// </summary>
        /// <returns>Account ID Numeric String.</returns>
        private string GetAccountID()
        {
            var idPTR = Memory.ReadPtr(Profile + Offsets.Profile.AccountId);
            return Memory.ReadUnicodeString(idPTR);
        }

        /// <summary>
        /// Get Movement Context Instance.
        /// </summary>
        private ulong GetMovementContext()
        {
            var movementContext = Memory.ReadPtr(this + Offsets.Player.MovementContext);
            var player = Memory.ReadPtr(movementContext + Offsets.MovementContext.Player, false);
            if (player != this)
                throw new ArgumentOutOfRangeException(nameof(movementContext));
            return movementContext;
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
            offsets[0] = Offsets.Player._playerBody;
            offsets[1] = PlayerBody.SkeletonRootJoint;
            offsets[2] = DizSkinningSkeleton._values;
            offsets[3] = MonoList<byte>.ArrOffset;
            offsets[4] = MonoList<byte>.ArrStartOffset + (uint)bone * 0x8;
            offsets[5] = 0x10;
        }
    }
}
