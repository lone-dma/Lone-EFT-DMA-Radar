/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using System.Security.Cryptography;
using VmmSharpEx.Extensions;

namespace LoneEftDmaRadar.Misc
{
    internal static class Utilities
    {
        /// <summary>
        /// Opens an embedded resource stream from the executing assembly.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static Stream OpenResource(string name)
        {
            return Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream(name)
                ?? throw new InvalidOperationException($"Resource '{name}' not found!");
        }

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

        public static void DumpClassNames(ulong thisClass, uint maxOffset)
        {
            var sb = new StringBuilder();
            for (uint offset = 0x10; offset < maxOffset; offset += 0x8)
            {
                try
                {
                    var childClass = Memory.ReadValue<ulong>(thisClass + offset);
                    if (childClass.IsValidUserVA())
                    {
                        var namePtr = Memory.ReadPtrChain(childClass, true, 0x0, 0x10);
                        var name = Memory.ReadUtf8String(namePtr, 128, true);
                        sb.AppendLine($"[{offset:X}] {name}");
                    }
                }
                catch { }
            }
            Logging.WriteLine(sb.ToString());
            Environment.Exit(0);
        }
    }
}

