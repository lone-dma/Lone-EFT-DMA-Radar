using EftDmaRadarLite.Tarkov.Player;
using EftDmaRadarLite.Unity;

namespace EftDmaRadarLite.UI.Skia.Maps
{
    /// <summary>
    /// Defines an entity that can be drawn on the 2D Radar Map.
    /// </summary>
    public interface IMapEntity : IWorldEntity
    {
        /// <summary>
        /// Draw this Entity on the Radar Map.
        /// </summary>
        /// <param name="canvas">SKCanvas instance to draw on.</param>
        void Draw(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer);
    }
}
