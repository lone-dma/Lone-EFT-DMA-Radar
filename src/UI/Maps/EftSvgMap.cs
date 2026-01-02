/*
 * Lone EFT DMA Radar
 * Bought to you by Lone (Lone DMA)
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
    /// SVG map implementation with tiling/mip map system for optimized rendering.
    /// Uses high-resolution tiles when zoomed in and lower-resolution images when zoomed out.
    /// </summary>
    public sealed class EftSvgMap : IEftMap
    {
        /// <summary>
        /// Number of mip levels to generate (0 = full res, 1 = half, 2 = quarter, etc.)
        /// </summary>
        private const int MipLevelCount = 4;

        private readonly MipMapLayer[] _layers;

        /// <summary>Raw map ID.</summary>
        public string ID { get; }
        /// <summary>Loaded configuration for this map instance.</summary>
        public EftMapConfig Config { get; }

        /// <summary>
        /// Construct a new map by loading each SVG layer from the supplied zip archive
        /// and creating a mip-mapped tiled representation for efficient rendering.
        /// </summary>
        /// <param name="zip">Archive containing the SVG layer files.</param>
        /// <param name="id">External map identifier.</param>
        /// <param name="config">Configuration describing layers and scaling.</param>
        /// <exception cref="InvalidOperationException">Thrown if any SVG fails to load.</exception>
        public EftSvgMap(ZipArchive zip, string id, EftMapConfig config)
        {
            ID = id;
            Config = config;

            var loaded = new List<MipMapLayer>();
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

                    // Create mip-mapped tiled layer
                    loaded.Add(new MipMapLayer(svg.Picture, config.RasterScale, layerCfg));
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
        /// Draw visible layers into the target canvas using appropriate mip level based on zoom.
        /// </summary>
        /// <param name="canvas">Destination Skia canvas.</param>
        /// <param name="playerHeight">Current player Y height for layer filtering.</param>
        /// <param name="mapBounds">Logical source rectangle (in map coordinates) to show.</param>
        /// <param name="windowBounds">Destination rectangle inside the control.</param>
        public void Draw(SKCanvas canvas, float playerHeight, SKRect mapBounds, SKRect windowBounds)
        {
            if (_layers.Length == 0) return;

            using var visible = new PooledList<MipMapLayer>(capacity: 8);
            foreach (var layer in _layers)
            {
                if (layer.IsHeightInRange(playerHeight))
                    visible.Add(layer);
            }

            if (visible.Count == 0) return;
            visible.Sort();

            float scaleX = windowBounds.Width / mapBounds.Width;
            float scaleY = windowBounds.Height / mapBounds.Height;

            // Determine mip level based on zoom (how much of the map we're showing)
            var baseLayer = _layers[0];
            int mipLevel = SelectMipLevel(Program.Config.UI.Zoom);

            canvas.Save();
            // Map coordinate system -> window region
            canvas.Translate(windowBounds.Left, windowBounds.Top);
            canvas.Scale(scaleX, scaleY);
            canvas.Translate(-mapBounds.Left, -mapBounds.Top);

            var front = visible[^1];
            foreach (var layer in visible)
            {
                bool dim = !Config.DisableDimming &&
                           layer != front &&
                           !front.CannotDimLowerLayers;

                var paint = dim ?
                    SKPaints.PaintBitmapAlpha : SKPaints.PaintBitmap;

                layer.Draw(canvas, mapBounds, mipLevel, paint);
            }

            canvas.Restore();
        }

        /// <summary>
        /// Select the appropriate mip level based on zoom.
        /// Lower zoom (zoomed in) = higher detail (lower mip level).
        /// Higher zoom (zoomed out) = lower detail (higher mip level).
        /// </summary>
        private static int SelectMipLevel(int zoom)
        {
            // mip 0 (full res) [Zoomed In]
            // mip 1 (half res)
            // mip 2 (quarter res)
            // mip 3 (eighth res) [Zoomed Out]

            if (zoom <= 50)
                return 0;
            if (zoom <= 80)
                return 1;
            if (zoom <= 120)
                return 2;
            return 3;
        }

        /// <summary>
        /// Compute per-frame map parameters (bounds and scaling factors) based on the
        /// current zoom and player-centered position.
        /// </summary>
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

            float fullWidth = baseLayer.FullWidth;
            float fullHeight = baseLayer.FullHeight;

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
        /// Dispose all mip map layers.
        /// </summary>
        public void Dispose()
        {
            for (int i = 0; i < _layers.Length; i++)
                _layers[i].Dispose();
        }

        /// <summary>
        /// A single map layer with multiple mip levels for efficient rendering at different zoom levels.
        /// The highest resolution level (mip 0) uses tiles for memory efficiency when zoomed in.
        /// Lower resolution levels use single images for fast rendering when zoomed out.
        /// </summary>
        private sealed class MipMapLayer : IComparable<MipMapLayer>, IDisposable
        {
            /// <summary>
            /// Represents a single mip level as a rasterized image.
            /// </summary>
            private sealed class MipLevel : IDisposable
            {
                /// <summary>Rasterized image for this mip level.</summary>
                public readonly SKImage Image;
                /// <summary>Width of this mip level in pixels.</summary>
                public readonly int Width;
                /// <summary>Height of this mip level in pixels.</summary>
                public readonly int Height;
                /// <summary>Scale factor from SVG to this mip level.</summary>
                public readonly float Scale;

                public MipLevel(SKPicture picture, int width, int height, float scale)
                {
                    Width = width;
                    Height = height;
                    Scale = scale;

                    var imageInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
                    using var surface = SKSurface.Create(imageInfo);
                    var canvas = surface.Canvas;
                    canvas.Clear(SKColors.Transparent);
                    canvas.Scale(scale, scale);
                    canvas.DrawPicture(picture);
                    Image = surface.Snapshot();
                }

                public void Dispose()
                {
                    Image?.Dispose();
                }
            }

            private readonly MipLevel[] _mipLevels;
            public readonly bool IsBaseLayer;
            public readonly bool CannotDimLowerLayers;
            public readonly float? MinHeight;
            public readonly float? MaxHeight;
            public readonly float RawWidth;
            public readonly float RawHeight;
            public readonly float FullWidth;
            public readonly float FullHeight;

            /// <summary>
            /// Create a mip-mapped layer with multiple resolution levels.
            /// </summary>
            public MipMapLayer(SKPicture picture, float rasterScale, EftMapConfig.Layer cfgLayer)
            {
                IsBaseLayer = cfgLayer.MinHeight is null && cfgLayer.MaxHeight is null;
                CannotDimLowerLayers = cfgLayer.CannotDimLowerLayers;
                MinHeight = cfgLayer.MinHeight;
                MaxHeight = cfgLayer.MaxHeight;

                var cullRect = picture.CullRect;
                RawWidth = cullRect.Width;
                RawHeight = cullRect.Height;

                // Full resolution dimensions
                int fullWidth = (int)Math.Ceiling(RawWidth * rasterScale);
                int fullHeight = (int)Math.Ceiling(RawHeight * rasterScale);
                FullWidth = fullWidth;
                FullHeight = fullHeight;

                // Generate mip levels - all as single images (no tiling)
                _mipLevels = new MipLevel[MipLevelCount];
                for (int level = 0; level < MipLevelCount; level++)
                {
                    float levelScale = rasterScale / (1 << level); // Divide by 2^level
                    int levelWidth = Math.Max(1, fullWidth >> level);
                    int levelHeight = Math.Max(1, fullHeight >> level);

                    _mipLevels[level] = new MipLevel(picture, levelWidth, levelHeight, levelScale);
                }
            }

            /// <summary>
            /// Draw this layer using the specified mip level.
            /// </summary>
            public void Draw(SKCanvas canvas, SKRect mapBounds, int mipLevel, SKPaint paint)
            {
                mipLevel = Math.Clamp(mipLevel, 0, _mipLevels.Length - 1);
                var mip = _mipLevels[mipLevel];

                if (mip.Image is null)
                    return;

                // Scale from mip level coordinates to full resolution coordinates
                float scale = FullWidth / mip.Width;
                canvas.Save();
                canvas.Scale(scale, scale);
                canvas.DrawImage(mip.Image, 0, 0, paint);
                canvas.Restore();
            }

            /// <summary>
            /// Determines whether the provided height is inside this layer's vertical range.
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
            public int CompareTo(MipMapLayer other)
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

            /// <summary>Dispose all mip levels.</summary>
            public void Dispose()
            {
                foreach (var mip in _mipLevels)
                    mip.Dispose();
            }
        }
    }
}
