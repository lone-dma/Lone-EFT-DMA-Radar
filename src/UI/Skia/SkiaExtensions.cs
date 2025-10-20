using LoneEftDmaRadar.UI.Radar.Maps;

namespace LoneEftDmaRadar.UI.Skia
{
    internal static class SkiaExtensions
    {
        /// <summary>
        /// Adjusts perceived brightness by changing HSL lightness, preserving hue/saturation and alpha.
        /// Black and white are returned unchanged.
        /// </summary>
        /// <param name="color">Source color.</param>
        /// <param name="amount">
        /// Brightness delta in [-1, 1]. Positive lightens (toward 1), negative darkens (toward 0).
        /// Values are clamped.
        /// </param>
        /// <returns>New color with adjusted brightness and original alpha.</returns>
        /// <remarks>
        /// Uses HSL for perceptual changes (less hue shift than RGB scaling). Early-returns for
        /// <see cref="SkiaSharp.SKColors.Black"/> and <see cref="SkiaSharp.SKColors.White"/>.
        /// </remarks>
        public static SKColor AdjustBrightness(this SKColor color, float amount)
        {
            if (color == SKColors.White || color == SKColors.Black) // Keep pure black/white as-is
                return color;
            amount = Math.Clamp(amount, -1f, 1f);
            // Keep alpha
            byte a = color.Alpha;

            // Normalize to 0..1
            float r = color.Red / 255f;
            float g = color.Green / 255f;
            float b = color.Blue / 255f;

            RgbToHsl(r, g, b, out float h, out float s, out float l);

            // Move L toward 1 (lighten) or 0 (darken) smoothly
            if (amount >= 0f)
                l = l + (1f - l) * amount;     // lighten
            else
                l = l * (1f + amount);         // darken  (amount is negative)

            HslToRgb(h, s, l, out r, out g, out b);

            return new SKColor(
                (byte)Math.Clamp((int)MathF.Round(r * 255f), 0, 255),
                (byte)Math.Clamp((int)MathF.Round(g * 255f), 0, 255),
                (byte)Math.Clamp((int)MathF.Round(b * 255f), 0, 255),
                a);

            // --- Helpers: RGB <-> HSL (all components in 0..1) ---
            static void RgbToHsl(float r, float g, float b, out float h, out float s, out float l)
            {
                float max = MathF.Max(r, MathF.Max(g, b));
                float min = MathF.Min(r, MathF.Min(g, b));
                l = (max + min) * 0.5f;

                if (MathF.Abs(max - min) < 1e-6f)
                {
                    h = 0f; s = 0f; return;
                }

                float d = max - min;
                s = l > 0.5f ? d / (2f - max - min) : d / (max + min);

                if (max == r) h = ((g - b) / d + (g < b ? 6f : 0f)) / 6f;
                else if (max == g) h = ((b - r) / d + 2f) / 6f;
                else h = ((r - g) / d + 4f) / 6f;
            }

            static void HslToRgb(float h, float s, float l, out float r, out float g, out float b)
            {
                if (s <= 1e-6f) { r = g = b = l; return; }

                float q = l < 0.5f ? l * (1f + s) : (l + s - l * s);
                float p = 2f * l - q;

                r = HueToRgb(p, q, h + 1f / 3f);
                g = HueToRgb(p, q, h);
                b = HueToRgb(p, q, h - 1f / 3f);
            }

            static float HueToRgb(float p, float q, float t)
            {
                if (t < 0f) t += 1f;
                if (t > 1f) t -= 1f;
                if (t < 1f / 6f) return p + (q - p) * 6f * t;
                if (t < 1f / 2f) return q;
                if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
                return p;
            }
        }

        /// <summary>
        /// Convert Unity Position (X,Y,Z) to an unzoomed Map Position..
        /// </summary>
        /// <param name="vector">Unity Vector3</param>
        /// <param name="map">Current Map</param>
        /// <returns>Unzoomed 2D Map Position.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToMapPos(this Vector3 vector, EftMapConfig map) =>
            new()
            {
                X = (map.X * map.SvgScale) + (vector.X * (map.Scale * map.SvgScale)),
                Y = (map.Y * map.SvgScale) - (vector.Z * (map.Scale * map.SvgScale))
            };

        /// <summary>
        /// Convert an Unzoomed Map Position to a Zoomed Map Position ready for 2D Drawing.
        /// </summary>
        /// <param name="mapPos">Unzoomed Map Position.</param>
        /// <param name="mapParams">Current Map Parameters.</param>
        /// <returns>Zoomed 2D Map Position.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SKPoint ToZoomedPos(this Vector2 mapPos, EftMapParams mapParams) =>
            new SKPoint
            {
                X = (mapPos.X - mapParams.Bounds.Left) * mapParams.XScale,
                Y = (mapPos.Y - mapParams.Bounds.Top) * mapParams.YScale
            };

        /// <summary>
        /// Gets a drawable 'Up Arrow'. IDisposable. Applies UI Scaling internally.
        /// </summary>
        public static SKPath GetUpArrow(this SKPoint point, float size = 6, float offsetX = 0, float offsetY = 0)
        {
            float x = point.X + offsetX;
            float y = point.Y + offsetY;

            size *= App.Config.UI.UIScale;
            var path = new SKPath();
            path.MoveTo(x, y);
            path.LineTo(x - size, y + size);
            path.LineTo(x + size, y + size);
            path.Close();

            return path;
        }

        /// <summary>
        /// Gets a drawable 'Down Arrow'. IDisposable. Applies UI Scaling internally.
        /// </summary>
        public static SKPath GetDownArrow(this SKPoint point, float size = 6, float offsetX = 0, float offsetY = 0)
        {
            float x = point.X + offsetX;
            float y = point.Y + offsetY;

            size *= App.Config.UI.UIScale;
            var path = new SKPath();
            path.MoveTo(x, y);
            path.LineTo(x - size, y - size);
            path.LineTo(x + size, y - size);
            path.Close();

            return path;
        }

        /// <summary>
        /// Draws a Mine/Explosive Marker on this zoomed location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawMineMarker(this SKPoint zoomedMapPos, SKCanvas canvas)
        {
            float length = 3.5f * App.Config.UI.UIScale;
            canvas.DrawLine(new SKPoint(zoomedMapPos.X - length, zoomedMapPos.Y + length), new SKPoint(zoomedMapPos.X + length, zoomedMapPos.Y - length), SKPaints.PaintExplosives);
            canvas.DrawLine(new SKPoint(zoomedMapPos.X - length, zoomedMapPos.Y - length), new SKPoint(zoomedMapPos.X + length, zoomedMapPos.Y + length), SKPaints.PaintExplosives);
        }

        /// <summary>
        /// Draws Mouseover Text (with backer) on this zoomed location.
        /// </summary>
        public static void DrawMouseoverText(this SKPoint zoomedMapPos, SKCanvas canvas, IEnumerable<string> lines)
        {

            float maxLength = 0;
            foreach (var line in lines)
            {
                var length = SKFonts.UIRegular.MeasureText(line);
                if (length > maxLength)
                    maxLength = length;
            }
            var backer = new SKRect
            {
                Bottom = zoomedMapPos.Y + ((lines.Count() * 12f) - 2) * App.Config.UI.UIScale,
                Left = zoomedMapPos.X + (9 * App.Config.UI.UIScale),
                Top = zoomedMapPos.Y - (9 * App.Config.UI.UIScale),
                Right = zoomedMapPos.X + (9 * App.Config.UI.UIScale) + maxLength + (6 * App.Config.UI.UIScale)
            };
            canvas.DrawRect(backer, SKPaints.PaintTransparentBacker); // Draw tooltip backer
            zoomedMapPos.Offset(11 * App.Config.UI.UIScale, 3 * App.Config.UI.UIScale);
            foreach (var line in lines) // Draw tooltip text
            {
                if (string.IsNullOrEmpty(line?.Trim()))
                    continue;
                canvas.DrawText(line,
                    zoomedMapPos,
                    SKTextAlign.Left,
                    SKFonts.UIRegular,
                    SKPaints.TextMouseover); // draw line text
                zoomedMapPos.Offset(0, 12f * App.Config.UI.UIScale);
            }

        }
    }
}
