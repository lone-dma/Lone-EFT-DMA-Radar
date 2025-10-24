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

namespace LoneArenaDmaRadar.DMA
{
    internal static class MemoryExtensions
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
        /// <param name="paramName">Parameter name to pass in exception message.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void ThrowIfInvalidVirtualAddress(this ulong va, string paramName = null)
        {
            string errorMsg;
            if (paramName is not null)
            {
                errorMsg = $"Invalid Virtual Address 0x{va:X} [{paramName}]";
            }
            else
            {
                errorMsg = $"Invalid Virtual Address 0x{va:X}";
            }
            if (!MemDMA.IsValidVirtualAddress(va))
                throw new InvalidOperationException(errorMsg);
        }
    }
}
