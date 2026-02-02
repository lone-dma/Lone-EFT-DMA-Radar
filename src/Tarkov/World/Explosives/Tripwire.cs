/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using LoneEftDmaRadar.Misc;
using LoneEftDmaRadar.Tarkov.Unity;
using LoneEftDmaRadar.Tarkov.World.Player;
using LoneEftDmaRadar.UI.Maps;
using LoneEftDmaRadar.UI.Skia;
using VmmSharpEx.Extensions;
using VmmSharpEx.Scatter;

namespace LoneEftDmaRadar.Tarkov.World.Explosives
{
    /// <summary>
    /// Represents a Tripwire (with attached Grenade) in Game World.
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
            baseAddr.ThrowIfInvalidUserVA(nameof(baseAddr));
            Addr = baseAddr;
            _position = Memory.ReadValue<Vector3>(baseAddr + Offsets.TripwireSynchronizableObject.ToPosition, false);
            _position.ThrowIfAbnormal("Tripwire Position");
        }

        public void OnRefresh(VmmScatterManaged scatter)
        {
            if (_destroyed)
            {
                return;
            }
            scatter.PrepareReadValue<int>(this + Offsets.TripwireSynchronizableObject._tripwireState);
            scatter.Completed += (sender, s) =>
            {
                if (s.ReadValue(this + Offsets.TripwireSynchronizableObject._tripwireState, out int nState))
                {
                    var state = (Enums.ETripwireState)nState;
                    _destroyed = state is Enums.ETripwireState.Exploded or Enums.ETripwireState.Inert;
                    _isActive = state is Enums.ETripwireState.Wait or Enums.ETripwireState.Active;
                }
            };
        }

        #region Interfaces

        private readonly Vector3 _position;
        public ref readonly Vector3 Position => ref _position;

        public void Draw(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            if (!_isActive)
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

