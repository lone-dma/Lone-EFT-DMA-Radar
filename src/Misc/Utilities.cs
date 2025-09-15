﻿using System.Security.Cryptography;

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
        /// <param name="price">Price to convert to string format.</param>
        public static string FormatNumberKM(int price)
        {
            if (price >= 1000000)
                return (price / 1000000D).ToString("0.#") + "M";
            if (price >= 1000)
                return (price / 1000D).ToString("0") + "K";

            return price.ToString();
        }
    }
}
