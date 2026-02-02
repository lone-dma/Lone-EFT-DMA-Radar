/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
namespace LoneEftDmaRadar.UI.Maps
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
        /// <param name="canvasSize">Size of the canvas.</param>
        /// <param name="zoom">Zoom level.</param>
        /// <param name="localPlayerMapPos">Local player map position.</param>
        /// <returns></returns>
        EftMapParams GetParameters(SKSize canvasSize, int zoom, ref Vector2 localPlayerMapPos);
    }
}

