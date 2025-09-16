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

using System.Collections.Frozen;
using System.IO.Compression;

namespace EftDmaRadarLite.UI.Skia.Maps
{
    /// <summary>
    /// Maintains Map Resources for this application.
    /// </summary>
    internal static class EftMapManager
    {
        private static readonly Lock _sync = new();
        private static ZipArchive _zip;
        private static FrozenDictionary<string, EftMapConfig> _maps;

        /// <summary>
        /// Currently Loaded Map.
        /// </summary>
        public static IEftMap Map { get; private set; }

        /// <summary>
        /// Initialize this Module.
        /// ONLY CALL ONCE!
        /// </summary>
        public static void ModuleInit()
        {
            const string mapsPath = "Maps.bin";
            try
            {
                /// Load Maps
                var mapsStream = File.OpenRead(mapsPath);
                var zip = new ZipArchive(mapsStream, ZipArchiveMode.Read, false);
                var mapsBuilder = new Dictionary<string, EftMapConfig>(StringComparer.OrdinalIgnoreCase);
                foreach (var file in zip.Entries)
                {
                    if (file.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        using var stream = file.Open();
                        var config = JsonSerializer.Deserialize<EftMapConfig>(stream);
                        foreach (var id in config!.MapID)
                            mapsBuilder.Add(id, config);
                    }
                }
                _maps = mapsBuilder.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
                _zip = zip;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to Initialize Maps!", ex);
            }
        }

        /// <summary>
        /// Update the current map and load resources into Memory.
        /// </summary>
        /// <param name="mapId">Id of map to load.</param>
        /// <param name="map"></param>
        /// <exception cref="Exception"></exception>
        public static void LoadMap(string mapId)
        {
            lock (_sync)
            {
                try
                {
                    if (!_maps.TryGetValue(mapId, out var newMap))
                        newMap = _maps["default"];
                    Map?.Dispose();
                    Map = null;
                    Map = new EftSvgMap(_zip, mapId, newMap);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"ERROR loading '{mapId}'", ex);
                }
            }
        }
    }
}
