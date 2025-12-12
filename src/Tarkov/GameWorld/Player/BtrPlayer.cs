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
using VmmSharpEx.Scatter;

namespace LoneEftDmaRadar.Tarkov.GameWorld.Player
{
    /// <summary>
    /// BTR Bot Operator.
    /// </summary>
    public sealed class BtrPlayer : ObservedPlayer
    {
        private readonly ulong _btrView;
        private readonly ulong _posAddr;
        private Vector3 _position;

        public override ref readonly Vector3 Position
        {
            get => ref _position;
        }
        public override string Name
        {
            get => "BTR";
            set { }
        }
        public BtrPlayer(ulong btrView, ulong playerBase) : base(playerBase)
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
        public override void OnRealtimeLoop(VmmScatter scatter)
        {
            scatter.PrepareReadValue<Vector3>(_posAddr);
            scatter.Completed += (sender, s) =>
            {
                if (s.ReadValue<Vector3>(_posAddr, out var position))
                    _position = position;
            };
        }
    }
}
