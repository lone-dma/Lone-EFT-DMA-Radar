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

using Collections.Pooled;
using LoneEftDmaRadar.DMA;
using LoneEftDmaRadar.Tarkov.GameWorld.Player;
using LoneEftDmaRadar.Tarkov.Unity;
using LoneEftDmaRadar.Tarkov.Unity.Structures;
using LoneEftDmaRadar.UI.Radar.Maps;
using LoneEftDmaRadar.UI.Skia;
using VmmSharpEx.Scatter;

namespace LoneEftDmaRadar.Tarkov.GameWorld.Explosives
{
    /// <summary>
    /// Represents a 'Hot' grenade in Local Game World.
    /// </summary>
    public sealed class Grenade : IExplosiveItem, IWorldEntity, IMapEntity
    {
        public static implicit operator ulong(Grenade x) => x.Addr;
        private readonly ConcurrentDictionary<ulong, IExplosiveItem> _parent;
        private readonly bool _isSmoke;
        private readonly UnityTransform _transform;

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
            if (type.Contains("SmokeGrenade"))
            {
                _isSmoke = true;
                return;
            }
            var ti = Memory.ReadPtrChain(baseAddr, false, UnitySDK.UnityOffsets.TransformChain);
            _transform = new UnityTransform(ti);
        }

        /// <summary>
        /// Get the updated Position of this Grenade.
        /// </summary>
        public void OnRefresh(VmmScatter scatter)
        {
            if (_isSmoke)
            {
                // Smokes never leave the list, don't remove
                return;
            }
            scatter.PrepareReadValue<bool>(this + Offsets.Throwable._isDestroyed);
            scatter.PrepareReadArray<UnityTransform.TrsX>(_transform.VerticesAddr, _transform.Count);
            scatter.Completed += (sender, x1) =>
            {
                if (x1.ReadValue<bool>(this + Offsets.Throwable._isDestroyed, out bool destroyed) && destroyed)
                {
                    // Remove from parent collection
                    _ = _parent.TryRemove(Addr, out _);
                    return;
                }
                if (x1.ReadArray<UnityTransform.TrsX>(_transform.VerticesAddr, _transform.Count) is PooledMemory<UnityTransform.TrsX> vertices)
                {
                    using (vertices)
                    {
                        _ = _transform.UpdatePosition(vertices.Span);
                    }
                }
            };
        }

        #region Interfaces

        public ref readonly Vector3 Position => ref _transform.Position;

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
