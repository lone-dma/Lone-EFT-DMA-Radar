using eft_dma_radar.Tarkov.Player;
using eft_dma_radar.Unity;

namespace eft_dma_radar.UI.Skia.Maps
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
