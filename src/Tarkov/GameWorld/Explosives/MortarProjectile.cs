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

using EftDmaRadarLite.Tarkov.Player;
using EftDmaRadarLite.UI.Skia;
using EftDmaRadarLite.Misc;
using EftDmaRadarLite.UI.Skia.Maps;
using VmmSharpEx.Scatter;

namespace EftDmaRadarLite.Tarkov.GameWorld.Explosives
{
    public sealed class MortarProjectile : IExplosiveItem
    {
        public static implicit operator ulong(MortarProjectile x) => x.Addr;
        private readonly ConcurrentDictionary<ulong, IExplosiveItem> _parent;

        public MortarProjectile(ulong baseAddr, ConcurrentDictionary<ulong, IExplosiveItem> parent)
        {
            baseAddr.ThrowIfInvalidVirtualAddress(nameof(baseAddr));
            _parent = parent;
            Addr = baseAddr;
        }

        public ulong Addr { get; }

        private Vector3 _position;
        public ref Vector3 Position => ref _position;

        public void Draw(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            // removed isActive check
            var circlePosition = Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams);
            var size = 5f * App.Config.UI.UIScale;
            SKPaints.ShapeOutline.StrokeWidth = SKPaints.PaintExplosives.StrokeWidth + 2f * App.Config.UI.UIScale;
            canvas.DrawCircle(circlePosition, size, SKPaints.ShapeOutline); // Draw outline
            canvas.DrawCircle(circlePosition, size, SKPaints.PaintExplosives); // draw LocalPlayer marker
        }

        public void OnRefresh(ScatterReadIndex index)
        {
            index.AddValueEntry<ArtilleryProjectile>(0, this);
            index.Completed += (sender, x1) =>
            {
                if (x1.TryGetValue(0, out ArtilleryProjectile artilleryProjectile))
                {
                    if (artilleryProjectile.Position.IsNormal())
                    {
                        _position = artilleryProjectile.Position;
                    }
                    if (!artilleryProjectile.IsActive)
                    {
                        _parent.TryRemove(this, out _);
                    }
                }
            };
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        private readonly struct ArtilleryProjectile
        {
            [FieldOffset((int)Offsets.ArtilleryProjectileClient.Position)]
            public readonly Vector3 Position;
            [FieldOffset((int)Offsets.ArtilleryProjectileClient.IsActive)]
            public readonly bool IsActive;
        }
    }
}
