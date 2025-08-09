using eft_dma_radar.Tarkov.Player;
using eft_dma_radar.UI.Skia;
using eft_dma_radar.Misc;
using eft_dma_radar.UI.Skia.Maps;

namespace eft_dma_radar.Tarkov.GameWorld.Explosives
{
    public sealed class MortarProjectile : IExplosiveItem
    {
        public static implicit operator ulong(MortarProjectile x) => x.Addr;
        private readonly ConcurrentDictionary<ulong, IExplosiveItem> _parent;

        public MortarProjectile(ulong baseAddr, ConcurrentDictionary<ulong, IExplosiveItem> parent)
        {
            _parent = parent;
            Addr = baseAddr;
            Refresh();
            if (!IsActive)
            {
                throw new InvalidOperationException("Already exploded!");
            }
        }

        public ulong Addr { get; }

        public bool IsActive { get; private set; }

        private Vector3 _position;
        public ref Vector3 Position => ref _position;

        public void Draw(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            if (!IsActive)
                return;
            var circlePosition = Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams);
            var size = 5f * App.Config.UI.UIScale;
            SKPaints.ShapeOutline.StrokeWidth = SKPaints.PaintExplosives.StrokeWidth + 2f * App.Config.UI.UIScale;
            canvas.DrawCircle(circlePosition, size, SKPaints.ShapeOutline); // Draw outline
            canvas.DrawCircle(circlePosition, size, SKPaints.PaintExplosives); // draw LocalPlayer marker
        }

        public void Refresh()
        {
            var artilleryProjectile = Memory.ReadValue<ArtilleryProjectile>(this, false);
            IsActive = artilleryProjectile.IsActive;
            if (IsActive)
            {
                _position = artilleryProjectile.Position;
            }
            else
            {
                _parent.TryRemove(this, out _);
            }
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
