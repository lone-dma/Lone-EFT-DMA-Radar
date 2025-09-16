/*
 * EFT DMA Radar Lite
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

using EftDmaRadarLite.DMA;
using EftDmaRadarLite.Tarkov.Loot;
using EftDmaRadarLite.UI.Skia;
using EftDmaRadarLite.UI.Skia.Maps;
using System.Net.Http.Headers;

namespace EftDmaRadarLite.Misc
{
    /// <summary>
    /// Extension methods go here.
    /// </summary>
    public static class GeneralExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindUtf16NullTerminatorIndex(this ReadOnlySpan<byte> span)
        {
            for (int i = 0; i < span.Length - 1; i += 2)
            {
                if (span[i] == 0 && span[i + 1] == 0)
                {
                    return i;
                }
            }
            return -1; // Not found
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindUtf16NullTerminatorIndex(this Span<byte> span)
        {
            for (int i = 0; i < span.Length - 1; i += 2)
            {
                if (span[i] == 0 && span[i + 1] == 0)
                {
                    return i;
                }
            }
            return -1; // Not found
        }

        /// <summary>
        /// Converts 'Degrees' to 'Radians'.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToRadians(this float degrees) =>
            MathF.PI / 180f * degrees;
        /// <summary>
        /// Converts 'Radians' to 'Degrees'.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToDegrees(this float radians) =>
            180f / MathF.PI * radians;
        /// <summary>
        /// Converts 'Radians' to 'Degrees'.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToDegrees(this Vector2 radians) =>
            180f / MathF.PI * radians;
        /// <summary>
        /// Converts 'Radians' to 'Degrees'.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToDegrees(this Vector3 radians) =>
            180f / MathF.PI * radians;
        /// <summary>
        /// Converts 'Degrees' to 'Radians'.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToRadians(this Vector2 degrees) =>
            MathF.PI / 180f * degrees;
        /// <summary>
        /// Converts 'Degrees' to 'Radians'.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToRadians(this Vector3 degrees) =>
            MathF.PI / 180f * degrees;

        /// <summary>
        /// Normalize angular degrees to 0-360.
        /// </summary>
        /// <param name="angle">Angle (degrees).</param>
        /// <returns>Normalized angle.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float NormalizeAngle(this float angle)
        {
            float modAngle = angle % 360.0f;

            if (modAngle < 0.0f)
                return modAngle + 360.0f;
            return modAngle;
        }
        /// <summary>
        /// Normalize angular degrees to 0-360.
        /// </summary>
        /// <param name="angle">Angle (degrees).</param>
        /// <returns>Normalized angle.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 NormalizeAngles(this Vector3 angles)
        {
            angles.X = angles.X.NormalizeAngle();
            angles.Y = angles.Y.NormalizeAngle();
            angles.Z = angles.Z.NormalizeAngle();
            return angles;
        }
        /// <summary>
        /// Normalize angular degrees to 0-360.
        /// </summary>
        /// <param name="angle">Angle (degrees).</param>
        /// <returns>Normalized angle.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 NormalizeAngles(this Vector2 angles)
        {
            angles.X = angles.X.NormalizeAngle();
            angles.Y = angles.Y.NormalizeAngle();
            return angles;
        }

        /// <summary>
        /// Custom implemenation to check if a float value is valid.
        /// This is the same as float.IsNormal() except it accepts 0 as a valid value.
        /// </summary>
        /// <param name="f">Float value to validate.</param>
        /// <returns>True if valid, otherwise False if invalid.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsNormalOrZero(this float f)
        {
            int bits = *(int*)&f & 0x7FFFFFFF; // Clears the sign bit
            return bits == 0 || (bits >= 0x00800000 && bits < 0x7F800000); // Allow 0, normal values, but not subnormal, infinity, or NaN
        }

        /// <summary>
        /// Checks if a Vector2 is valid.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNormal(this Vector2 v)
        {
            return float.IsNormal(v.X) && float.IsNormal(v.Y);
        }

        /// <summary>
        /// Checks if a Vector3 is valid.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNormal(this Vector3 v)
        {
            return float.IsNormal(v.X) && float.IsNormal(v.Y) && float.IsNormal(v.Z);
        }

        /// <summary>
        /// Checks if a Quaternion is valid.
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNormal(this Quaternion q)
        {
            return float.IsNormal(q.X) && float.IsNormal(q.Y) && float.IsNormal(q.Z) && float.IsNormal(q.W);
        }

        /// <summary>
        /// Checks if a Vector2 is valid or Zero.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNormalOrZero(this Vector2 v)
        {
            return v.X.IsNormalOrZero() && v.Y.IsNormalOrZero();
        }

        /// <summary>
        /// Checks if a Vector3 is valid or Zero.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNormalOrZero(this Vector3 v)
        {
            return v.X.IsNormalOrZero() && v.Y.IsNormalOrZero() && v.Z.IsNormalOrZero();
        }

        /// <summary>
        /// Checks if a Quaternion is valid or Zero.
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNormalOrZero(this Quaternion q)
        {
            return q.X.IsNormalOrZero() && q.Y.IsNormalOrZero() && q.Z.IsNormalOrZero() && q.W.IsNormalOrZero();
        }

        /// <summary>
        /// Validates a float for invalid values.
        /// </summary>
        /// <param name="q">Input Float.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfAbnormal(this float f)
        {
            if (!float.IsNormal(f))
                throw new ArgumentOutOfRangeException(nameof(f));
        }

        /// <summary>
        /// Validates a Quaternion for invalid values.
        /// </summary>
        /// <param name="q">Input Quaternion.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfAbnormal(this Quaternion q)
        {
            if (!q.IsNormal())
                throw new ArgumentOutOfRangeException(nameof(q));
        }
        /// <summary>
        /// Validates a Vector3 for invalid values.
        /// </summary>
        /// <param name="v">Input Vector3.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfAbnormal(this Vector3 v)
        {
            if (!v.IsNormal())
                throw new ArgumentOutOfRangeException(nameof(v));
        }
        /// <summary>
        /// Validates a Vector2 for invalid values.
        /// </summary>
        /// <param name="v">Input Vector2.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfAbnormal(this Vector2 v)
        {
            if (!v.IsNormal())
                throw new ArgumentOutOfRangeException(nameof(v));
        }

        /// <summary>
        /// Validates a float for invalid values.
        /// </summary>
        /// <param name="q">Input Float.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfAbnormalAndNotZero(this float f)
        {
            if (!f.IsNormalOrZero())
                throw new ArgumentOutOfRangeException(nameof(f));
        }

        /// <summary>
        /// Validates a Quaternion for invalid values.
        /// </summary>
        /// <param name="q">Input Quaternion.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfAbnormalAndNotZero(this Quaternion q)
        {
            if (!q.IsNormalOrZero())
                throw new ArgumentOutOfRangeException(nameof(q));
        }
        /// <summary>
        /// Validates a Vector3 for invalid values.
        /// </summary>
        /// <param name="v">Input Vector3.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfAbnormalAndNotZero(this Vector3 v)
        {
            if (!v.IsNormalOrZero())
                throw new ArgumentOutOfRangeException(nameof(v));
        }
        /// <summary>
        /// Validates a Vector2 for invalid values.
        /// </summary>
        /// <param name="v">Input Vector2.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfAbnormalAndNotZero(this Vector2 v)
        {
            if (!v.IsNormalOrZero())
                throw new ArgumentOutOfRangeException(nameof(v));
        }
        /// <summary>
        /// Calculate a normalized direction towards a destination position.
        /// </summary>
        /// <param name="source">Source position.</param>
        /// <param name="destination">Destination position.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 CalculateDirection(this Vector3 source, Vector3 destination)
        {
            // Calculate the direction from source to destination
            Vector3 direction = destination - source;

            // Normalize the direction vector
            return Vector3.Normalize(direction);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 AsVector2(this SKPoint point) =>
            Unsafe.BitCast<SKPoint, Vector2>(point);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SKPoint AsSKPoint(this Vector2 vector) =>
            Unsafe.BitCast<Vector2, SKPoint>(vector);
    }

    public static class WebExtensions
    {
        /// <summary>
        /// Parse a Retry header from an HTTP response and return the retry duration.
        /// </summary>
        /// <param name="retryHeader"></param>
        /// <returns></returns>
        public static TimeSpan GetRetryAfter(this RetryConditionHeaderValue retryHeader)
        {
            if (retryHeader?.Delta is TimeSpan ts)
            {
                return ts;
            }
            if (retryHeader?.Date is DateTimeOffset date)
            {
                return date.UtcDateTime - DateTimeOffset.UtcNow;
            }
            return TimeSpan.FromSeconds(2);
        }
    }

    public static class MemoryExtensions
    {
        /// <summary>
        /// Checks if an array contains a signature, and returns the offset where the signature occurs.
        /// </summary>
        /// <param name="array">Array to search in.</param>
        /// <param name="signature">Signature to search for. Must not be larger than array.</param>
        /// <param name="mask">Optional Signature Mask. x = check for match, ? = wildcard</param>
        /// <returns>Signature offset within array. -1 if not found.</returns>
        public static int FindSignatureOffset(this byte[] array, ReadOnlySpan<byte> signature, string mask = null)
        {
            ReadOnlySpan<byte> span = array.AsSpan();
            return span.FindSignatureOffset(signature, mask);
        }

        /// <summary>
        /// Checks if an array contains a signature, and returns the offset where the signature occurs.
        /// </summary>
        /// <param name="array">Array to search in.</param>
        /// <param name="signature">Signature to search for. Must not be larger than array.</param>
        /// <param name="mask">Optional Signature Mask. x = check for match, ? = wildcard</param>
        /// <returns>Signature offset within array. -1 if not found.</returns>
        public static int FindSignatureOffset(this Span<byte> array, ReadOnlySpan<byte> signature, string mask = null)
        {
            ReadOnlySpan<byte> span = array;
            return span.FindSignatureOffset(signature, mask);
        }

        /// <summary>
        /// Checks if an array contains a signature, and returns the offset where the signature occurs.
        /// </summary>
        /// <param name="array">Array to search in.</param>
        /// <param name="signature">Signature to search for. Must not be larger than array.</param>
        /// <param name="mask">Optional Signature Mask. x = check for match, ? = wildcard</param>
        /// <returns>Signature offset within array. -1 if not found.</returns>
        public static int FindSignatureOffset(this ReadOnlySpan<byte> array, ReadOnlySpan<byte> signature, string mask = null)
        {
            ArgumentOutOfRangeException.ThrowIfZero(array.Length, nameof(array));
            ArgumentOutOfRangeException.ThrowIfZero(signature.Length, nameof(signature));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(signature.Length, array.Length, nameof(signature));
            if (mask is not null && signature.Length != mask.Length)
                throw new ArgumentException("Mask Length does not match Signature length!");

            for (int i = 0; i <= array.Length - signature.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < signature.Length; j++)
                {
                    if (mask is not null && mask[j] == '?') // Skip on wildcard mask
                        continue;
                    // If any byte does not match, set found to false and break the inner loop.
                    if (array[i + j] != signature[j])
                    {
                        found = false;
                        break;
                    }
                }

                // If all bytes match, return the current index.
                if (found)
                {
                    return i;
                }
            }

            // If the signature is not found, return -1.
            return -1;
        }

        /// <summary>
        /// Checks if a Virtual Address is valid.
        /// </summary>
        /// <param name="va">Virtual Address to validate.</param>
        /// <returns>True if valid, otherwise False.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidVirtualAddress(this ulong va) =>
            MemDMA.IsValidVirtualAddress(va);

        /// <summary>
        /// Throws an exception if the Virtual Address is invalid.
        /// </summary>
        /// <param name="va">Virtual address to validate.</param>
        /// <exception cref="InvalidOperationException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfInvalidVirtualAddress(this ulong va)
        {
            if (!MemDMA.IsValidVirtualAddress(va))
                throw new InvalidOperationException($"Invalid Virtual Address 0x{va:X}");
        }

        /// <summary>
        /// Throws an exception if the Virtual Address is invalid.
        /// </summary>
        /// <param name="va">Virtual address to validate.</param>
        /// <exception cref="InvalidOperationException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfInvalidVirtualAddress(this ulong va, string message)
        {
            if (!MemDMA.IsValidVirtualAddress(va))
                throw new InvalidOperationException($"Invalid Virtual Address 0x{va:X} [{message}]");
        }
    }

    public static class GuiExtensions
    {
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

    public static class LootItemExtensions
    {
        /// <summary>
        /// Order loot (important first, then by price).
        /// </summary>
        /// <param name="loot"></param>
        /// <returns>Ordered loot.</returns>
        public static IEnumerable<LootItem> OrderLoot(this IEnumerable<LootItem> loot)
        {
            return loot
                .OrderByDescending(x => x.IsImportant || (App.Config.QuestHelper.Enabled && x.IsQuestCondition))
                .ThenByDescending(x => x.Price);
        }
    }
}
