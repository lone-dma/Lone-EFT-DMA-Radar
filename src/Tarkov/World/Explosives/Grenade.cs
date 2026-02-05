/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using LoneEftDmaRadar.Tarkov.Unity;
using LoneEftDmaRadar.Tarkov.Unity.Structures;
using LoneEftDmaRadar.Tarkov.World.Player;
using LoneEftDmaRadar.UI.Maps;
using LoneEftDmaRadar.UI.Skia;
using VmmSharpEx.Extensions;
using VmmSharpEx.Scatter;

namespace LoneEftDmaRadar.Tarkov.World.Explosives
{
    /// <summary>
    /// Represents a 'Hot' grenade in Game World.
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
            baseAddr.ThrowIfInvalidUserVA(nameof(baseAddr));
            Addr = baseAddr;
            _parent = parent;
            var type = ObjectClass.ReadName(baseAddr, 64, false);
            if (type.Contains("SmokeGrenade"))
            {
                _isSmoke = true;
                return;
            }
            var ti = Memory.ReadPtrChain(baseAddr, false, UnityOffsets.TransformChain);
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
                if (x1.ReadPooled<UnityTransform.TrsX>(_transform.VerticesAddr, _transform.Count) is IMemoryOwner<UnityTransform.TrsX> vertices)
                {
                    using (vertices)
                    {
                        _ = _transform.UpdatePosition(vertices.Memory.Span);
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
            const float size = 5f;
            SKPaints.ShapeOutline.StrokeWidth = SKPaints.PaintExplosives.StrokeWidth + 2f;
            canvas.DrawCircle(circlePosition, size, SKPaints.ShapeOutline); // Draw outline
            canvas.DrawCircle(circlePosition, size, SKPaints.PaintExplosives); // draw LocalPlayer marker
        }

        #endregion
    }
}

