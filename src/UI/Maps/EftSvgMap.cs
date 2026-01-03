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

using Collections.Pooled;
using LoneEftDmaRadar.UI.Skia;
using Svg.Skia;
using System.IO.Compression;

namespace LoneEftDmaRadar.UI.Maps
{
    /// <summary>
    /// SVG map implementation that pre-rasterizes layers to SKImage bitmaps for fast rendering.
    /// Each layer is converted from vector to bitmap at load time, then drawn as a texture each frame.
    /// </summary>
    public sealed class EftSvgMap : IEftMap
    {
        private static readonly SKSamplingOptions _sampling = new(SKCubicResampler.Mitchell); // Slightly sharper than Linear but performs well still
        private readonly RasterLayer[] _layers;

        /// <summary>Raw map ID.</summary>
        public string ID { get; }
        /// <summary>Loaded configuration for this map instance.</summary>
        public EftMapConfig Config { get; }

        /// <summary>
        /// Construct a new map by loading each SVG layer from the supplied zip archive
        /// and pre-rasterizing them to SKImage bitmaps for fast rendering.
        /// </summary>
        /// <param name="zip">Archive containing the SVG layer files.</param>
        /// <param name="id">External map identifier.</param>
        /// <param name="config">Configuration describing layers and scaling.</param>
        /// <exception cref="InvalidOperationException">Thrown if any SVG fails to load.</exception>
        public EftSvgMap(ZipArchive zip, string id, EftMapConfig config)
        {
            ID = id;
            Config = config;

            var loaded = new List<RasterLayer>();
            try
            {
                foreach (var layerCfg in config.MapLayers)
                {
                    var entry = zip.Entries.First(x =>
                        x.Name.Equals(layerCfg.Filename, StringComparison.OrdinalIgnoreCase));

                    using var stream = entry.Open();

                    using var svg = new SKSvg();
                    if (svg.Load(stream) is null || svg.Picture is null)
                        throw new InvalidOperationException($"Failed to load SVG '{layerCfg.Filename}'.");

                    // Pre-rasterize the SVG to a bitmap for fast drawing
                    loaded.Add(new RasterLayer(svg.Picture, config.RasterScale, layerCfg));
                }

                _layers = loaded.Order().ToArray();
            }
            catch
            {
                foreach (var l in loaded) l.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Draw visible layers into the target canvas.
        /// Applies:
        ///  - Height filtering
        ///  - Map bounds → window bounds transform
        ///  - Configured SVG rasterScale
        ///  - Optional dimming of non-top layers
        /// </summary>
        /// <param name="canvas">Destination Skia canvas.</param>
        /// <param name="playerHeight">Current player Y height for layer filtering.</param>
        /// <param name="mapBounds">Logical source rectangle (in map coordinates) to show.</param>
        /// <param name="windowBounds">Destination rectangle inside the control.</param>
        public void Draw(SKCanvas canvas, float playerHeight, SKRect mapBounds, SKRect windowBounds)
        {
            if (_layers.Length == 0) return;

            using var visible = new PooledList<RasterLayer>(capacity: 8);
            foreach (var layer in _layers)
            {
                if (layer.IsHeightInRange(playerHeight))
                    visible.Add(layer);
            }

            if (visible.Count == 0) return;
            visible.Sort();

            float scaleX = windowBounds.Width / mapBounds.Width;
            float scaleY = windowBounds.Height / mapBounds.Height;

            canvas.Save();
            // Map coordinate system -> window region
            canvas.Translate(windowBounds.Left, windowBounds.Top);
            canvas.Scale(scaleX, scaleY);
            canvas.Translate(-mapBounds.Left, -mapBounds.Top);

            var front = visible[^1];
            foreach (var layer in visible)
            {
                bool dim = !Config.DisableDimming &&        // Make sure dimming is enabled globally
                           layer != front &&                // Make sure the current layer is not in front
                           !front.CannotDimLowerLayers;     // Don't dim the lower layers if the front layer has dimming disabled upon lower layers

                var paint = dim ? 
                    SKPaints.PaintBitmapAlpha : SKPaints.PaintBitmap;

                canvas.DrawImage(
                    image: layer.Image, 
                    x: 0, 
                    y: 0, 
                    sampling: _sampling,
                    paint: paint);
            }

            canvas.Restore();
        }

        /// <summary>
        /// Compute per-frame map parameters (bounds and scaling factors) based on the
        /// current zoom and player-centered position. Returns the rectangle of the map
        /// (in map coordinates) that should be displayed and the X/Y zoom rasterScale factors.
        /// </summary>
        /// <param name="canvasSize">Size of the rendering canvas.</param>
        /// <param name="zoom">Zoom percentage (e.g. 100 = 1:1).</param>
        /// <param name="localPlayerMapPos">Player map-space position (center target); value may be adjusted externally.</param>
        /// <returns>Computed parameters for rendering this frame.</returns>
        public EftMapParams GetParameters(SKSize canvasSize, int zoom, ref Vector2 localPlayerMapPos)
        {
            if (_layers.Length == 0)
            {
                return new EftMapParams
                {
                    Map = Config,
                    Bounds = SKRect.Empty,
                    XScale = 1f,
                    YScale = 1f
                };
            }

            var baseLayer = _layers[0];

            float fullWidth = baseLayer.RawWidth * Config.RasterScale;
            float fullHeight = baseLayer.RawHeight * Config.RasterScale;

            var zoomWidth = fullWidth * (0.01f * zoom);
            var zoomHeight = fullHeight * (0.01f * zoom);

            var bounds = new SKRect(
                localPlayerMapPos.X - zoomWidth * 0.5f,
                localPlayerMapPos.Y - zoomHeight * 0.5f,
                localPlayerMapPos.X + zoomWidth * 0.5f,
                localPlayerMapPos.Y + zoomHeight * 0.5f
            ).AspectFill(canvasSize);

            return new EftMapParams
            {
                Map = Config,
                Bounds = bounds,
                XScale = (float)canvasSize.Width / bounds.Width,
                YScale = (float)canvasSize.Height / bounds.Height
            };
        }

        /// <summary>
        /// Dispose all raster layers (releasing their SKImage resources).
        /// </summary>
        public void Dispose()
        {
            for (int i = 0; i < _layers.Length; i++)
                _layers[i].Dispose();
        }

        /// <summary>
        /// Internal wrapper for a single pre-rasterized map layer.
        /// Converts SKPicture to SKImage at construction for fast bitmap drawing.
        /// </summary>
        private sealed class RasterLayer : IComparable<RasterLayer>, IDisposable
        {
            private readonly SKImage _image;
            public readonly bool IsBaseLayer;
            public readonly bool CannotDimLowerLayers;
            public readonly float? MinHeight;
            public readonly float? MaxHeight;
            public readonly float RawWidth;
            public readonly float RawHeight;

            /// <summary>
            /// The pre-rasterized bitmap image for this layer.
            /// </summary>
            public SKImage Image => _image;

            /// <summary>
            /// Create a raster layer by converting the SKPicture to an SKImage bitmap.
            /// </summary>
            public RasterLayer(SKPicture picture, float rasterScale, EftMapConfig.Layer cfgLayer)
            {
                IsBaseLayer = cfgLayer.MinHeight is null && cfgLayer.MaxHeight is null;
                CannotDimLowerLayers = cfgLayer.CannotDimLowerLayers;
                MinHeight = cfgLayer.MinHeight;
                MaxHeight = cfgLayer.MaxHeight;

                var cullRect = picture.CullRect;
                RawWidth = cullRect.Width;
                RawHeight = cullRect.Height;

                int width = (int)Math.Ceiling(RawWidth * rasterScale);
                int height = (int)Math.Ceiling(RawHeight * rasterScale);

                var imageInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var surface = SKSurface.Create(imageInfo);
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);
                canvas.Scale(rasterScale, rasterScale);
                using var paint = new SKPaint { IsAntialias = true }; // AA before we raster, why not
                canvas.DrawPicture(picture, paint);
                _image = surface.Snapshot();
            }

            /// <summary>
            /// Determines whether the provided height is inside this layer's vertical range.
            /// Base layers always return true.
            /// </summary>
            public bool IsHeightInRange(float h)
            {
                if (IsBaseLayer) return true;
                if (MinHeight.HasValue && h < MinHeight.Value) return false;
                if (MaxHeight.HasValue && h > MaxHeight.Value) return false;
                return true;
            }

            /// <summary>
            /// Ordering: base layers first, then ascending MinHeight, then ascending MaxHeight.
            /// </summary>
            public int CompareTo(RasterLayer other)
            {
                if (other is null) return -1;
                if (IsBaseLayer && !other.IsBaseLayer)
                    return -1;
                if (!IsBaseLayer && other.IsBaseLayer)
                    return 1;

                var thisMin = MinHeight ?? float.MinValue;
                var otherMin = other.MinHeight ?? float.MinValue;
                int cmp = thisMin.CompareTo(otherMin);
                if (cmp != 0) return cmp;

                var thisMax = MaxHeight ?? float.MaxValue;
                var otherMax = other.MaxHeight ?? float.MaxValue;
                return thisMax.CompareTo(otherMax);
            }

            /// <summary>Dispose the rasterized image.</summary>
            public void Dispose() => _image.Dispose();
        }
    }
}
