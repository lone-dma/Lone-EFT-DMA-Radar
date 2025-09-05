using EftDmaRadarLite.Tarkov.Player;
using EftDmaRadarLite.UI.Skia.Maps;

namespace EftDmaRadarLite.UI.Radar
{
    /// <summary>
    /// Defines a Radar Map Mouseover Entity.
    /// </summary>
    public interface IMouseoverEntity : IMapEntity
    {
        /// <summary>
        /// Cached 'Mouseover' Position on the Radar GUI. Used for mouseover events.
        /// Uses zoomed coordinates and is refreshed on each render cycle.
        /// </summary>
        Vector2 MouseoverPosition { get; set; }

        void DrawMouseover(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer);
    }
}
