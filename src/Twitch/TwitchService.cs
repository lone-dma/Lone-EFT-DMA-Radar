using System.Text.RegularExpressions;
using TwitchLib.Api;
using TwitchLib.Api.Core;
using TwitchLib.Api.Core.Exceptions;

namespace EftDmaRadarLite.Twitch
{
    internal static class TwitchService
    {
        private static readonly SemaphoreSlim _lock = new(1, 1);
        private static readonly IReadOnlyList<string> _ttvAppends = new List<string>()
        {
            null,
            "ttv",
            "tv",
            "_tv",
            "_ttv",
            "_"
        };
        private static readonly TwitchAPI _api;

        /// <summary>
        /// Persistent Cache Access.
        /// </summary>
        private static ConcurrentDictionary<string, CachedTwitchEntry> Cache { get; } = App.Config.Cache.TwitchService;

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
            // Cleanup Cache
            var expiredLookups = Cache.Where(x => x.Value.Expired);
            foreach (var expired in expiredLookups)
                Cache.TryRemove(expired.Key, out _);
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
                if (Cache.TryGetValue(username, out var cachedEntry) &&
                    !cachedEntry.Expired &&
                    cachedEntry.TwitchLogin is string cachedTwitchLogin)
                {
                    Debug.WriteLine($"[Twitch] {username} is LIVE! (Cached)");
                    return cachedTwitchLogin;
                }

                var replacedName = GetTTVName(username)?.ToLower();

                // Exit early if they are apparently not a TTVer
                if (replacedName is null)
                    return null;

                Debug.WriteLine($"[Twitch] Checking {username}..."); // replacedName
                string result = await LookupTwitchApiAsync(replacedName);
                if (result is not null)
                    Debug.WriteLine($"[Twitch] {username} is LIVE!");

                Cache[username] = new CachedTwitchEntry()
                {
                    TwitchLogin = result
                };
                return result;
            }
            catch
            {
                return null;
            }
        }

        #region Internal API

        /// <summary>
        /// Takes an input username and attempts to normalize the username by removing the TV/TTV portions.
        /// If a user is *not* streaming, the return value should be the same as the input.
        /// </summary>
        /// <param name="username">Input username.</param>
        /// <param name="substring">Substring to check the input for.</param>
        /// <returns>Normalized TTV Name. ex: LoneSurvivorTTV --> LoneSurvivor</returns>
        private static string NormalizeTTVName(string username, string substring)
        {
            // Matches names with any number of _ or - before/after the target substring
            string pattern = $"^[_-]*{substring}[_-]*|[_-]*{substring}[_-]*$";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            return regex.Replace(username, "");
        }

        /// <summary>
        /// Checks a base string for a substring (Helper Method)
        /// </summary>
        /// <param name="baseString">Base string.</param>
        /// <param name="substring">Substring to check the base string for.</param>
        /// <returns>True if the base string contains the substring.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool StrContains(string baseString, string substring)
        {
            return baseString.Contains(substring, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Takes an input Player Name and checks if it is a valid TV/TTV Name.
        /// Checks several different combinations.
        /// Attempts to normalize the username by removing the "TV" portions.
        /// </summary>
        /// <param name="username">Player's Name</param>
        /// <returns>Player's Normalized TTV Name.</returns>
        private static string GetTTVName(string username)
        {
            // The regex will match _ and - at the start and end of usernames.
            // This check prevents a username like CoolDude_ or -CoolDude from being matched as a TTVer
            if (!StrContains(username, "ttv") && !StrContains(username, "tv") && !StrContains(username, "twitch"))
                return null;

            // Regex is really fast on strings as small as usernames
            // This just makes the overall logic flow flatter and more understandable
            var c1 = NormalizeTTVName(username, "ttv");
            var c2 = NormalizeTTVName(username, "tv");
            var c3 = NormalizeTTVName(username, "twitch");

            if (c1 != username) return c1;
            else if (c2 != username) return c2;
            else if (c3 != username) return c3;

            return null;
        }

        /// <summary>
        /// Takes an Input Username and checks if they are live on any combination of channel URLs.
        /// </summary>
        /// <param name="username">User's Username (without TTV,etc.)</param>
        /// <returns>User's Twitch Login if LIVE, otherwise NULL.</returns>
        private static async Task<string> LookupTwitchApiAsync(string username)
        {
            await _lock.WaitAsync(); // Only one request at a time
            try
            {
                /// Build API Request
                var logins = _ttvAppends.Select(x => $"{username}{x}").ToList();
                var response = await _api.Helix.Streams.GetStreamsAsync(
                    first: 1,
                    userLogins: logins);
                if (response.Streams.Length == 0)
                {
                    Cache[username] = new CachedTwitchEntry()
                    {
                        TwitchLogin = null
                    };
                    return null;
                }
                string twitchLogin = response.Streams.First().UserLogin;
                Cache[username] = new CachedTwitchEntry()
                {
                    TwitchLogin = twitchLogin
                };
                return twitchLogin;
            }
            catch (BadRequestException) // Fake TTVer
            {
                return null;
            }
            finally
            {
                _lock.Release();
            }
        }

        #endregion
    }
}