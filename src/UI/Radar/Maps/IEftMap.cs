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

using SkiaSharp.Views.WPF;

namespace LoneEftDmaRadar.UI.Radar.Maps
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
