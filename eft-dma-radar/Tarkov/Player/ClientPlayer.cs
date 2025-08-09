using eft_dma_radar.Unity;
using eft_dma_radar.Unity.Collections;
using System;
using static SDK.Offsets;

namespace eft_dma_radar.Tarkov.Player
{
    public class ClientPlayer : PlayerBase
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
        /// Procedural Weapon Animation
        /// </summary>
        public ulong PWA { get; }
        /// <summary>
        /// PlayerInfo Address (GClass1044)
        /// </summary>
        public ulong Info { get; }
        /// <summary>
        /// Player name.
        /// </summary>
        public override string Name { get; set; }
        /// <summary>
        /// Account UUID for Human Controlled Players.
        /// </summary>
        public override string AccountID { get; }
        /// <summary>
        /// Group that the player belongs to.
        /// </summary>
        public override int GroupID { get; protected set; } = -1;
        /// <summary>
        /// Player's Faction.
        /// </summary>
        public override Enums.EPlayerSide PlayerSide { get; protected set; }
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

        internal ClientPlayer(ulong playerBase) : base(playerBase)
        {
            Profile = Memory.ReadPtr(this + Offsets.Player.Profile);
            CharacterController = Memory.ReadPtr(this + Offsets.Player._characterController);
            Info = Memory.ReadPtr(Profile + Offsets.Profile.Info);
            PWA = Memory.ReadPtr(this + Offsets.Player.ProceduralWeaponAnimation);
            Body = Memory.ReadPtr(this + Offsets.Player._playerBody);
            InventoryControllerAddr = this + Offsets.Player._inventoryController;
            HandsControllerAddr = this + Offsets.Player._handsController;
            CorpseAddr = this + Offsets.Player.Corpse;

            AccountID = GetAccountID();
            GroupID = GetGroupID();
            MovementContext = GetMovementContext();
            RotationAddress = ValidateRotationAddr(MovementContext + Offsets.MovementContext._rotation);
            /// Setup Transforms
            Skeleton = new Skeleton(this, GetTransformInternalChain);
            /// Determine Player Type
            PlayerSide = (Enums.EPlayerSide)Memory.ReadValue<int>(Info + Offsets.PlayerInfo.Side); // Usec,Bear,Scav,etc.
            if (!Enum.IsDefined(PlayerSide)) // Make sure PlayerSide is valid
                throw new ArgumentOutOfRangeException(nameof(PlayerSide));
            if (this is LocalPlayer) // Handled in derived class
                return;

            bool isAI = Memory.ReadValue<int>(Info + Offsets.PlayerInfo.RegistrationDate) == 0;
            if (IsScav)
            {
                if (isAI)
                {
                    IsHuman = false;
                    Name = "AI";
                    Type = PlayerType.AIScav;
                }
                else
                {
                    IsHuman = true;
                    Name = "PScav";
                    Type = PlayerType.PScav;
                }
            }
            else if (IsPmc)
            {
                IsHuman = true;
                Name = "PMC";
                Type = PlayerType.PMC;
            }
            else
                throw new NotImplementedException(nameof(PlayerSide));
        }

        /// <summary>
        /// Get Player's Account ID.
        /// </summary>
        /// <returns>Account ID Numeric String.</returns>
        private string GetAccountID()
        {
            var idPTR = Memory.ReadPtr(Profile + Offsets.Profile.AccountId);
            return Memory.ReadUnityString(idPTR);
        }

        /// <summary>
        /// Gets player's Group Number.
        /// </summary>
        private int GetGroupID()
        {
            try
            {
                var grpIdPtr = Memory.ReadPtr(Info + Offsets.PlayerInfo.GroupId);
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
        /// <returns>Array of offsets for transform internal chain.</returns>
        public override uint[] GetTransformInternalChain(Bones bone)
        {
            return new uint[] { Offsets.Player._playerBody, PlayerBody.SkeletonRootJoint, DizSkinningSkeleton._values, MemList<byte>.ArrOffset, MemList<byte>.ArrStartOffset + (uint)bone * 0x8, 0x10 };
        }
    }
}
