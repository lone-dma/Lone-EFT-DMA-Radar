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
    /// Represents a 'Hot' grenade in Local Game World.
    /// </summary>
    public sealed class Grenade : IExplosiveItem, IWorldEntity, IMapEntity
    {
        public static implicit operator ulong(Grenade x) => x.Addr;
        private static readonly uint[] _toPosChain =
            ObjectClass.To_GameObject.Concat(new uint[] { GameObject.ComponentsOffset, 0x8, 0x38 }).ToArray();
        private readonly ConcurrentDictionary<ulong, IExplosiveItem> _parent;
        private readonly bool _isSmoke;
        private readonly ulong _posAddr;

        /// <summary>
        /// Base Address of Grenade Object.
        /// </summary>
        public ulong Addr { get; }

        public Grenade(ulong baseAddr, ConcurrentDictionary<ulong, IExplosiveItem> parent)
        {
            baseAddr.ThrowIfInvalidVirtualAddress(nameof(baseAddr));
            Addr = baseAddr;
            _parent = parent;
            var type = ObjectClass.ReadName(baseAddr, 64, false);
            if (type == "SmokeGrenade")
            {
                _isSmoke = true;
                return;
            }
            _posAddr = Memory.ReadPtrChain(baseAddr, false, _toPosChain) + 0x90;
        }

        /// <summary>
        /// Get the updated Position of this Grenade.
        /// </summary>
        public void OnRefresh(ScatterReadIndex index)
        {
            if (_isSmoke)
            {
                // Smokes never leave the list, don't remove
                return;
            }
            index.AddValueEntry<Vector3>(0, _posAddr);
            index.AddValueEntry<bool>(1, this + Offsets.Grenade.IsDestroyed);
            index.Completed += (sender, x1) =>
            {
                if (x1.TryGetValue(0, out Vector3 pos) && pos.IsNormal())
                {
                    _position = pos;
                }
                if (x1.TryGetValue(1, out bool isDestroyed) && isDestroyed)
                {
                    _parent.TryRemove(this, out _);
                }
            };
        }

        #region Interfaces

        private Vector3 _position;
        public ref Vector3 Position => ref _position;

        public void Draw(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            if (_isSmoke)
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
