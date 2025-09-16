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

using System.Security.Cryptography;

namespace EftDmaRadarLite.Misc
{
    internal static class Utilities
    {
        /// <summary>
        /// Get a random password of a specified length.
        /// </summary>
        /// <param name="length">Password length.</param>
        /// <returns>Random alpha-numeric password.</returns>
        public static string GetRandomPassword(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            string pw = "";
            for (int i = 0; i < length; i++)
                pw += chars[RandomNumberGenerator.GetInt32(chars.Length)];
            return pw;
        }

        /// <summary>
        /// Format integer as a compact string with K/M suffixes.
        /// </summary>
        /// <param name="num">Integer to convert to string format.</param>
        public static string FormatNumberKM(int num)
        {
            if (num >= 1000000)
                return (num / 1000000D).ToString("0.#") + "M";
            if (num >= 1000)
                return (num / 1000D).ToString("0") + "K";

            return num.ToString();
        }
    }
}
