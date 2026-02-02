/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using LoneEftDmaRadar.Tarkov.World.Player.Helpers;
using VmmSharpEx.Scatter;

namespace LoneEftDmaRadar.Tarkov.World.Player
{
    /// <summary>
    /// BTR Bot Operator.
    /// </summary>
    public sealed class BtrPlayer : ObservedPlayer
    {
        private readonly ulong _btrView;
        private readonly ulong _posAddr;
        private Vector3 _position = new(9999, 0, 9999);

        public override ref readonly Vector3 Position
        {
            get => ref _position;
        }
        public override string Name
        {
            get => "BTR";
        }
        public BtrPlayer(ulong btrView, ulong playerBase, GameWorld gameWorld) : base(playerBase, gameWorld)
        {
            _btrView = btrView;
            _posAddr = _btrView + Offsets.BTRView._previousPosition;
            Type = PlayerType.AIRaider;
        }

        /// <summary>
        /// Set the position of the BTR.
        /// Give this function it's own unique Index.
        /// </summary>
        /// <param name="index">Scatter read index to read off of.</param>
        public override void OnRealtimeLoop(VmmScatterManaged scatter)
        {
            scatter.PrepareReadValue<Vector3>(_posAddr);
            scatter.Completed += (sender, s) =>
            {
                if (s.ReadValue<Vector3>(_posAddr, out var position) && position != default)
                {
                    _position = position;
                }
            };
        }
    }
}

