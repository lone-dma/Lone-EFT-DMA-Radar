using SkiaSharp.Views.WPF;

namespace eft_dma_radar.UI.Skia.Maps
{
    public interface IEftMap : IDisposable
    {
        /// <summary>
        /// Raw Map ID for this Map.
        /// </summary>
        string ID { get; }

        /// <summary>
        /// Configuration for this Map.
        /// </summary>
        EftMapConfig Config { get; }

        /// <summary>
        /// Draw the Map on the provided Canvas.
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="playerHeight"></param>
        /// <param name="mapBounds"></param>
        /// <param name="windowBounds"></param>
        void Draw(SKCanvas canvas, float playerHeight, SKRect mapBounds, SKRect windowBounds);

        /// <summary>
        /// Get Parameters for this map.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="zoom"></param>
        /// <param name="localPlayerMapPos"></param>
        /// <returns></returns>
        EftMapParams GetParameters(SKGLElement control, int zoom, ref Vector2 localPlayerMapPos);
    }
}
