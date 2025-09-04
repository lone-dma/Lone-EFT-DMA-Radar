using System.Security.Cryptography;

namespace eft_dma_radar.Misc
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
    }
}
