/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using LoneEftDmaRadar.Misc;
using LoneEftDmaRadar.Misc.JSON;
using System.Collections.Frozen;

namespace LoneEftDmaRadar.UI.Maps
{
    /// <summary>
    /// Maintains Map Resources for this application.
    /// </summary>
    internal static class EftMapManager
    {
        public const string MapsNamespace = "LoneEftDmaRadar.Resources.Maps";
        private static FrozenDictionary<string, EftMapConfig> _maps;

        /// <summary>
        /// Currently Loaded Map.
        /// </summary>
        public static IEftMap Map { get; private set; }

        static EftMapManager()
        {
            Memory.RaidStopped += Memory_RaidStopped;
        }

        private static void Memory_RaidStopped(object sender, EventArgs e)
        {
            RadarWindow.Dispatcher.InvokeAsync(() =>
            {
                Map?.Dispose();
                Map = null;
            });
        }

        /// <summary>
        /// Initialize this Module.
        /// ONLY CALL ONCE!
        /// </summary>
        public static async Task ModuleInitAsync()
        {
            try
            {
                /// Load Maps
                var mapsBuilder = new Dictionary<string, EftMapConfig>(StringComparer.OrdinalIgnoreCase);
                foreach (var resource in GetMapResourceNames())
                {
                    if (resource.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        using var stream = Utilities.OpenResource(resource);
                        var config = await JsonSerializer.DeserializeAsync(stream, AppJsonContext.Default.EftMapConfig);
                        foreach (var id in config!.MapID)
                            mapsBuilder.Add(id, config);
                    }
                }
                _maps = mapsBuilder.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to Initialize Maps!", ex);
            }
        }

        private static IEnumerable<string> GetMapResourceNames()
        {
            return Assembly
                .GetExecutingAssembly()
                .GetManifestResourceNames()
                .Where(name => name.StartsWith(MapsNamespace, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks the requested map ID and loads the map if not loaded.
        /// Returns the loaded map.
        /// </summary>
        /// <remarks>
        /// NOT THREAD SAFE! Should be called from a single thread only.
        /// </remarks>
        /// <param name="mapId">Id of map to load.</param>
        /// <returns><see cref="IEftMap"/> instance if loaded, otherwise <see langword="null"/>.</returns>
        public static IEftMap LoadMap(string mapId)
        {
            try
            {
                if (Map?.ID?.Equals(mapId, StringComparison.OrdinalIgnoreCase) ?? false)
                    return Map;
                if (!_maps.TryGetValue(mapId, out var newMap))
                    throw new KeyNotFoundException($"Map ID '{mapId}' not found!");
                Map?.Dispose();
                Map = null;
                Map = new EftSvgMap(mapId, newMap);
                return Map;
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"ERROR loading '{mapId}': {ex}");
                return null;
            }
        }
    }
}

