/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using LoneEftDmaRadar.Tarkov.Unity.Collections;
using LoneEftDmaRadar.Tarkov.Unity.Structures;

namespace LoneEftDmaRadar.Tarkov.World.Player
{
    public class ClientPlayer : AbstractPlayer
    {
        /// <summary>
        /// EFT.Profile Address
        /// </summary>
        public ulong Profile { get; }
        /// <summary>
        /// PlayerInfo Address (GClass1044)
        /// </summary>
        public ulong Info { get; }
        /// <summary>
        /// Player name.
        /// </summary>
        public override string Name
        {
            get => "AI";
        }
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
        /// Corpse field address..
        /// </summary>
        public override ulong CorpseAddr { get; }
        /// <summary>
        /// Player Rotation Field Address (view angles).
        /// </summary>
        public override ulong RotationAddress { get; }

        internal ClientPlayer(ulong playerBase) : base(playerBase)
        {
            Profile = Memory.ReadPtr(this + Offsets.Player.Profile);
            Info = Memory.ReadPtr(Profile + Offsets.Profile.Info);
            CorpseAddr = this + Offsets.Player.Corpse;
            PlayerSide = (Enums.EPlayerSide)Memory.ReadValue<int>(Info + Offsets.PlayerInfo.Side);
            if (!Enum.IsDefined<Enums.EPlayerSide>(PlayerSide))
                throw new ArgumentOutOfRangeException(nameof(PlayerSide));

            MovementContext = GetMovementContext();
            RotationAddress = ValidateRotationAddr(MovementContext + Offsets.MovementContext._rotation);
            /// Setup Transform
            var ti = Memory.ReadPtrChain(this, false, _transformInternalChain);
            SkeletonRoot = new UnityTransform(ti);
            _ = SkeletonRoot.UpdatePosition();
        }

        /// <summary>
        /// Get Movement Context Instance.
        /// </summary>
        private ulong GetMovementContext()
        {
            var movementContext = Memory.ReadPtr(this + Offsets.Player.MovementContext);
            var player = Memory.ReadPtr(movementContext + Offsets.MovementContext._player, false);
            if (player != this)
                throw new ArgumentOutOfRangeException(nameof(movementContext));
            return movementContext;
        }

        private static readonly uint[] _transformInternalChain =
        [
            Offsets.Player._playerBody,
            Offsets.PlayerBody.SkeletonRootJoint,
            Offsets.DizSkinningSkeleton._values,
            UnityList<byte>.ArrOffset,
            UnityList<byte>.ArrStartOffset + (uint)Bones.HumanBase * 0x8,
            0x10
        ];
    }
}

