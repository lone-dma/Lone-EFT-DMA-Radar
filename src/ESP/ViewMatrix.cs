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

namespace EftDmaRadarLite.ESP
{
    /// <summary>
    /// Defines a transposed Matrix4x4 for ESP Operations (only contains necessary fields).
    /// </summary>
    public sealed class ViewMatrix
    {
        /// <summary>
        /// Zoom Levels for ESP View Matrix.
        /// </summary>
        public static ReadOnlyMemory<float> ZoomLevels { get; } = new float[]
        {
            1f, 2f, 5f, 10f
        };

        public float M44;
        public float M14;
        public float M24;

        public Vector3 Translation;
        public Vector3 Right;
        public Vector3 Up;

        public void Update(ref Matrix4x4 matrix) 
        {
            /// Transpose necessary fields
            M44 = matrix.M44;
            M14 = matrix.M41;
            M24 = matrix.M42;
            Translation.X = matrix.M14;
            Translation.Y = matrix.M24;
            Translation.Z = matrix.M34;
            Right.X = matrix.M11;
            Right.Y = matrix.M21;
            Right.Z = matrix.M31;
            Up.X = matrix.M12;
            Up.Y = matrix.M22;
            Up.Z = matrix.M32;
        }
    }
}
