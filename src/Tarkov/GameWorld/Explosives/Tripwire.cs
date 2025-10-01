/*
 * EFT DMA Radar Lite
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

using EftDmaRadarLite.Unity;
using EftDmaRadarLite.Tarkov.Player;
using EftDmaRadarLite.UI.Skia;
using EftDmaRadarLite.Misc;
using EftDmaRadarLite.UI.Skia.Maps;
using VmmSharpEx.Scatter;

namespace EftDmaRadarLite.Tarkov.GameWorld.Explosives
{
    /// <summary>
    /// Represents a Tripwire (with attached Grenade) in Local Game World.
    /// </summary>
    public sealed class Tripwire : IExplosiveItem, IWorldEntity, IMapEntity
    {
        public static implicit operator ulong(Tripwire x) => x.Addr;
        private bool _isActive;
        private bool _destroyed;

        /// <summary>
        /// Base Address of Grenade Object.
        /// </summary>
        public ulong Addr { get; }

        public Tripwire(ulong baseAddr)
        {
            Addr = baseAddr;
            _position = Memory.ReadValue<Vector3>(baseAddr + Offsets.TripwireSynchronizableObject.ToPosition, false);
            _position.ThrowIfAbnormal("Tripwire Position");
        }

        public void OnRefresh(ScatterReadIndex index)
        {
            if (_destroyed)
            {
                return;
            }
            index.AddValueEntry<int>(0, this + Offsets.TripwireSynchronizableObject._tripwireState);
            index.Completed += (sender, x1) =>
            {
                if (x1.TryGetValue(0, out int nState))
                {
                    var state = (Enums.ETripwireState)nState;
                    _destroyed = state is Enums.ETripwireState.Exploded or Enums.ETripwireState.Inert;
                    _isActive = state is Enums.ETripwireState.Wait or Enums.ETripwireState.Active;
                }
            };
        }

        #region Interfaces

        private Vector3 _position;
        public ref Vector3 Position => ref _position;

        public void Draw(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            if (!_isActive)
                return;
            var circlePosition = Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams);
            var size = 5f * App.Config.UI.UIScale;
            SKPaints.ShapeOutline.StrokeWidth = SKPaints.PaintExplosives.StrokeWidth + 2f * App.Config.UI.UIScale;
            canvas.DrawCircle(circlePosition, size, SKPaints.ShapeOutline); // Draw outline
            canvas.DrawCircle(circlePosition, size, SKPaints.PaintExplosives); // draw LocalPlayer marker
        }

        #endregion
    }
}
