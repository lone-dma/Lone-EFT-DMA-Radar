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
