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

using LiteDB;

namespace EftDmaRadarLite.Misc.Cache
{
    internal static class LocalCache
    {
        private static readonly string _dbPath = Path.Combine(App.ConfigPath.FullName, "cache.db");
        private static readonly LiteDatabase _db;

        static LocalCache()
        {
            _db = new LiteDatabase($"Filename={_dbPath};Connection=direct;Upgrade=true") ??
                throw new InvalidOperationException("Unable to load LocalCache DB.");
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            _db.Dispose();
        }

        /// <summary>
        /// Returns the Local DB collection for Player Profiles.
        /// </summary>
        /// <returns></returns>
        public static ILiteCollection<CachedPlayerProfile> GetProfileCollection()
        {
            var profiles = _db.GetCollection<CachedPlayerProfile>("profiles");
            profiles.EnsureIndex(x => x.Id, unique: true);
            return profiles;
        }

        /// <summary>
        /// Returns the Local DB collection for Twitch lookups.
        /// </summary>
        /// <returns></returns>
        public static ILiteCollection<CachedTwitchEntry> GetTwitchCollection()
        {
            var twitch = _db.GetCollection<CachedTwitchEntry>("twitch");
            twitch.EnsureIndex(x => x.Username, unique: true);
            return twitch;
        }
    }
}
