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

using LoneArenaDmaRadar.UI.Skia;
using SkiaSharp.Views.WPF;
using Svg.Skia;
using System.IO.Compression;

namespace LoneArenaDmaRadar.UI.Radar.Maps
{
    /// <summary>
    /// SVG Map Implementation.
    /// </summary>
    public sealed class EftSvgMap : IEFTMap
    {
        private static readonly SKSamplingOptions _sampling = new SKSamplingOptions(SKCubicResampler.Mitchell);
        private readonly EftMapConfig.LoadedLayer[] _layers;

        public string ID { get; }
        public EftMapConfig Config { get; }

        public EftSvgMap(ZipArchive zip, string id, EftMapConfig config)
        {
            ID = id;
            Config = config;
            var layers = new List<EftMapConfig.LoadedLayer>();
            try
            {
                using var paint = new SKPaint()
                {
                    IsAntialias = true
                };
                foreach (var layer in config.MapLayers) // Load resources for new map
                {
                    using var stream = zip.Entries.First(x => x.Name
                            .Equals(layer.Filename,
                                StringComparison.OrdinalIgnoreCase))
                        .Open();
                    using var svg = SKSvg.CreateFromStream(stream);
                    // Create an image info with the desired dimensions
                    var scaleInfo = new SKImageInfo(
                        (int)Math.Round(svg.Picture!.CullRect.Width * config.SvgScale),
                        (int)Math.Round(svg.Picture!.CullRect.Height * config.SvgScale));
                    // Rasterize the SVG to a surface
                    using (var surface = SKSurface.Create(scaleInfo))
                    {
                        // Clear the surface
                        surface.Canvas.Clear(SKColors.Transparent);
                        // Apply the scale and draw the SVG picture
                        surface.Canvas.Scale(config.SvgScale);
                        surface.Canvas.DrawPicture(svg.Picture, paint);
                        layers.Add(new EftMapConfig.LoadedLayer(surface.Snapshot(), layer));
                    }
                }
                _layers = layers.Order().ToArray();
            }
            catch
            {
                foreach (var layer in layers) // Unload any partially loaded layers
                {
                    layer.Dispose();
                }
                throw;
            }
        }

        public void Draw(SKCanvas canvas, float playerHeight, SKRect mapBounds, SKRect windowBounds)
        {
            var layers = _layers // Use overridden equality operators
                .Where(layer => layer.IsHeightInRange(playerHeight))
                .Order()
                .ToArray();
            foreach (var layer in layers)
            {
                SKPaint paint;
                if (layers.Length > 1 && layer != layers.Last() && !(layer.IsBaseLayer && layers.Any(x => !x.DimBaseLayer)))
                {
                    paint = SKPaints.PaintBitmapAlpha;
                }
                else
                {
                    paint = SKPaints.PaintBitmap;
                }
                canvas.DrawImage(
                    image: layer.Image, 
                    source: mapBounds, 
                    dest: windowBounds, 
                    paint: paint,
                    sampling: _sampling);
            }
        }

        /// <summary>
        /// Provides miscellaneous map parameters used throughout the entire render.
        /// </summary>
        public EftMapParams GetParameters(SKGLElement element, int zoom, ref Vector2 localPlayerMapPos)
        {
            var zoomWidth = _layers[0].Image.Width * (.01f * zoom);
            var zoomHeight = _layers[0].Image.Height * (.01f * zoom);

            // Get the size of the element using the CanvasSize property
            var canvasSize = element.CanvasSize;

            var bounds = new SKRect(localPlayerMapPos.X - zoomWidth / 2,
                    localPlayerMapPos.Y - zoomHeight / 2,
                    localPlayerMapPos.X + zoomWidth / 2,
                    localPlayerMapPos.Y + zoomHeight / 2)
                .AspectFill(canvasSize);

            return new EftMapParams
            {
                Map = Config,
                Bounds = bounds,
                XScale = canvasSize.Width / bounds.Width, // Set scale for this frame
                YScale = canvasSize.Height / bounds.Height // Set scale for this frame
            };
        }
        public EftMapParams GetParametersE(SKSize control, float zoom, ref Vector2 localPlayerMapPos)
        {
            var zoomWidth = _layers[0].Image.Width * (.01f * zoom);
            var zoomHeight = _layers[0].Image.Height * (.01f * zoom);

            var bounds = new SKRect(localPlayerMapPos.X - zoomWidth / 2,
                    localPlayerMapPos.Y - zoomHeight / 2,
                    localPlayerMapPos.X + zoomWidth / 2,
                    localPlayerMapPos.Y + zoomHeight / 2)
                .AspectFill(control);

            return new EftMapParams
            {
                Map = Config,
                Bounds = bounds,
                XScale = control.Width / bounds.Width, // Set scale for this frame
                YScale = control.Height / bounds.Height // Set scale for this frame
            };
        }
        public void Dispose()
        {
            for (int i = 0; i < _layers.Length; i++)
            {
                _layers[i]?.Dispose();
                _layers[i] = null;
            }
        }
    }
}