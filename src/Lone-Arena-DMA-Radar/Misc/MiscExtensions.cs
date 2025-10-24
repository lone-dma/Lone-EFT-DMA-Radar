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

using System.Net.Http.Headers;

namespace LoneArenaDmaRadar.Misc
{
    /// <summary>
    /// Miscellaneous Extension Methods.
    /// </summary>
    public static class MiscExtensions
    {
        private static readonly JsonSerializerOptions _noIndents = new()
        {
            WriteIndented = false
        };

        /// <summary>
        /// Removes all unnecessary whitespace from a JSON string, producing a compact, minified representation.
        /// </summary>
        /// <param name="json">The JSON string to be minified. Must be a valid JSON document.</param>
        /// <returns>A minified JSON string with all insignificant whitespace removed.</returns>
        /// <exception cref="JsonException"></exception>
        public static string MinifyJson(this string json)
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            return System.Text.Json.JsonSerializer.Serialize(doc, _noIndents);
        }

        /// <summary>
        /// Checks if point A is within maxDist of point B.
        /// </summary>
        /// <param name="a">Point A.</param>
        /// <param name="b">Point B.</param>
        /// <param name="maxDist">Maximum distance between points.</param>
        /// <returns>TRUE if the squared distance between point A/B is within the squared distance of maxDist, otherwise FALSE.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool WithinDistance(this Vector3 a, Vector3 b, float maxDist)
            => Vector3.DistanceSquared(a, b) < maxDist * maxDist;

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
        public static void ThrowIfAbnormal(this float f, string paramName = null)
        {
            if (!float.IsNormal(f))
                throw new ArgumentOutOfRangeException(paramName);
        }

        /// <summary>
        /// Validates a Quaternion for invalid values.
        /// </summary>
        /// <param name="q">Input Quaternion.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfAbnormal(this Quaternion q, string paramName = null)
        {
            if (!q.IsNormal())
                throw new ArgumentOutOfRangeException(paramName);
        }
        /// <summary>
        /// Validates a Vector3 for invalid values.
        /// </summary>
        /// <param name="v">Input Vector3.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfAbnormal(this Vector3 v, string paramName = null)
        {
            if (!v.IsNormal())
                throw new ArgumentOutOfRangeException(paramName);
        }
        /// <summary>
        /// Validates a Vector2 for invalid values.
        /// </summary>
        /// <param name="v">Input Vector2.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfAbnormal(this Vector2 v, string paramName = null)
        {
            if (!v.IsNormal())
                throw new ArgumentOutOfRangeException(paramName);
        }

        /// <summary>
        /// Validates a float for invalid values.
        /// </summary>
        /// <param name="q">Input Float.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfAbnormalAndNotZero(this float f, string paramName = null)
        {
            if (!f.IsNormalOrZero())
                throw new ArgumentOutOfRangeException(paramName);
        }

        /// <summary>
        /// Validates a Quaternion for invalid values.
        /// </summary>
        /// <param name="q">Input Quaternion.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfAbnormalAndNotZero(this Quaternion q, string paramName = null)
        {
            if (!q.IsNormalOrZero())
                throw new ArgumentOutOfRangeException(paramName);
        }
        /// <summary>
        /// Validates a Vector3 for invalid values.
        /// </summary>
        /// <param name="v">Input Vector3.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfAbnormalAndNotZero(this Vector3 v, string paramName = null)
        {
            if (!v.IsNormalOrZero())
                throw new ArgumentOutOfRangeException(paramName);
        }
        /// <summary>
        /// Validates a Vector2 for invalid values.
        /// </summary>
        /// <param name="v">Input Vector2.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfAbnormalAndNotZero(this Vector2 v, string paramName = null)
        {
            if (!v.IsNormalOrZero())
                throw new ArgumentOutOfRangeException(paramName);
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
}
