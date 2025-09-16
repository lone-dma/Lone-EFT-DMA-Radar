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

namespace EftDmaRadarLite.Unity
{
    public static class UnityTransformExtensions
    {
        private static readonly Vector3 _left = new Vector3(-1, 0, 0);
        private static readonly Vector3 _right = new(1, 0, 0);
        private static readonly Vector3 _up = new(0, 1, 0);
        private static readonly Vector3 _down = new(0, -1, 0);
        private static readonly Vector3 _forward = new(0, 0, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Left(this Quaternion q) =>
            q.Multiply(_left);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Right(this Quaternion q) =>
            q.Multiply(_right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Up(this Quaternion q) =>
            q.Multiply(_up);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Down(this Quaternion q) =>
            q.Multiply(_down);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Forward(this Quaternion q) =>
            q.Multiply(_forward);

        /// <summary>
        /// Convert Local Direction to World Direction.
        /// </summary>
        /// <param name="localDirection">Local Direction.</param>
        /// <returns>World Direction.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 TransformDirection(this Quaternion q, Vector3 localDirection)
        {
            return q.Multiply(localDirection);
        }

        /// <summary>
        /// Convert World Direction to Local Direction.
        /// </summary>
        /// <param name="worldDirection">World Direction.</param>
        /// <returns>Local Direction.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 InverseTransformDirection(this Quaternion q, Vector3 worldDirection)
        {
            return Quaternion.Conjugate(q).Multiply(worldDirection);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Multiply(this Quaternion q, Vector3 vector)
        {
            var m = Matrix4x4.CreateFromQuaternion(q);
            return Vector3.Transform(vector, m);
        }
    }
}
