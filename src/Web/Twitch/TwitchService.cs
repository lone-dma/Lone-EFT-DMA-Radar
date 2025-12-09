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

using LiteDB;
using System.Text.RegularExpressions;
using TwitchLib.Api;
using TwitchLib.Api.Core;
using TwitchLib.Api.Core.Exceptions;

namespace LoneEftDmaRadar.Web.Twitch
{
    /// <summary>
    /// Twitch Integration Module.
    /// </summary>
    internal static partial class TwitchService
    {
        private static readonly ConcurrentDictionary<string, CachedTwitchEntry> _cache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly SemaphoreSlim _lock = new(1, 1);
        private static readonly IReadOnlyList<string> _ttvAppends = new List<string>
        {
            null, // plain name
            "tv",
            "ttv",
            "_tv",
            "_ttv",
            "tv_",
            "ttv_",
            "_twitch",
            "twitch",
            "_",
            "__"
        };
        private static readonly TwitchAPI _api;

        static TwitchService()
        {
            if (App.Config.TwitchApi.ClientId is not string clientId ||
                App.Config.TwitchApi.ClientSecret is not string clientSecret)
                return; // No Twitch API credentials configured
            var settings = new ApiSettings()
            {
                ClientId = clientId,
                Secret = clientSecret
            };
            _api = new TwitchAPI(settings: settings);
        }

        /// <summary>
        /// Takes an input username, and checks if the user is a Twitch Streamer.
        /// </summary>
        /// <param name="username">Player's in-game name.</param>
        /// <returns>Twitch Channel URL. Null if not streaming.</returns>
        public static async Task<string> LookupAsync(string username)
        {
            try
            {
                if (_api is null) // Twitch API is not configured
                    return null;

                if (string.IsNullOrWhiteSpace(username)) // Invalid input
                    return null;

                if (_cache.TryGetValue(username, out var cached) && !cached.IsExpired)
                {
                    if (cached.Channel is string cachedChannel)
                    {
                        return cachedChannel;
                    }
                    return null;
                }

                var replacedName = GetTTVName(username);

                // Exit early if they are apparently not a TTVer
                if (replacedName is null)
                    return null;

                Debug.WriteLine($"[Twitch] Checking {username}...");
                string channel = await LookupTwitchApiAsync(replacedName);
                _cache[username] = new CachedTwitchEntry()
                {
                    Channel = channel,
                    Timestamp = DateTimeOffset.UtcNow
                };

                return channel;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Takes an input Player Name and checks if it is a valid TV/TTV-like name.
        /// Removes twitch/ttv/tv tokens from the edges (with optional '_' or '-') and normalizes.
        /// Returns null if not a TTVer-like name.
        /// </summary>
        /// <param name="username">Player's Name</param>
        /// <returns>Normalized base name in lowercase, or null.</returns>
        private static string GetTTVName(string username)
        {
            // Fast bail-out for obvious non-TTV names
            if (!username.Contains("tv", StringComparison.OrdinalIgnoreCase) && // Also handles 'ttv'
                !username.Contains("twitch", StringComparison.OrdinalIgnoreCase))
                return null;

            // Precompiled regex to strip "twitch|ttv|tv" that appear at the start or end,
            // with optional '-' or '_' around them. Culture invariant and compiled for speed.
            // This version fixes edge cases better than the previous implementation particularly with _ - tokens.
            var stripped = StripUsername().Replace(username, string.Empty).Trim('_', '-');

            // If nothing changed or became empty, it's not considered a TTVer-like name
            if (stripped.Length == 0 || string.Equals(stripped, username, StringComparison.Ordinal))
                return null;

            return stripped;
        }

        // Pre-compute login combinations once per username instead of rebuilding list each time
        private static List<string> BuildLoginList(string username)
        {
            var logins = new List<string>(_ttvAppends.Count);
            foreach (var append in _ttvAppends)
            {
                logins.Add(append is null ? username : $"{username}{append}");
            }
            return logins;
        }

        /// <summary>
        /// Takes an Input Username and checks if they are live on any combination of channel URLs.
        /// </summary>
        /// <param name="username">User's Username (without TTV,etc.)</param>
        /// <returns>User's Twitch Login if LIVE, otherwise NULL.</returns>
        private static async Task<string> LookupTwitchApiAsync(string username)
        {
            await _lock.WaitAsync();
            try
            {
                var logins = BuildLoginList(username);
                var response = await _api.Helix.Streams.GetStreamsAsync(
                    first: 1,
                    userLogins: logins);
                return response.Streams.FirstOrDefault()?.UserLogin;
            }
            catch (BadRequestException)
            {
                return null;
            }
            finally
            {
                _lock.Release();
            }
        }

        private sealed class CachedTwitchEntry()
        {
            public string Channel { get; init; }
            public DateTimeOffset Timestamp { get; init; }

            public bool IsExpired => (DateTimeOffset.UtcNow - Timestamp) > TimeSpan.FromMinutes(10);
        }

        [GeneratedRegex(@"^[-_]*(?:twitch|ttv|tv)[-_]*|[-_]*(?:twitch|ttv|tv)[-_]*$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
        private static partial Regex StripUsername();
    }
}