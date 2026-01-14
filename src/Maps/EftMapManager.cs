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

using LoneEftDmaRadar.Misc;
using LoneEftDmaRadar.Misc.JSON;
using System.Collections.Frozen;

namespace LoneEftDmaRadar.Maps
{
    /// <summary>
    /// Maintains Map Resources for this application.
    /// </summary>
    internal static class EftMapManager
    {
        public const string MapsNamespace = "LoneEftDmaRadar.Maps.Resources";
        private static FrozenDictionary<string, EftMapConfig> _maps;

        /// <summary>
        /// Currently Loaded Map.
        /// </summary>
        public static IEftMap Map { get; private set; }

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
                var resources = GetMapResourceNames();
                foreach (var resource in resources)
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
                .Where(name => name.StartsWith(MapsNamespace, StringComparison.OrdinalIgnoreCase))
                .ToArray();
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

        /// <summary>
        /// Cleans up loaded map resources if loaded. Otherwise no-op.
        /// </summary>
        public static void Cleanup()
        {
            Map?.Dispose();
            Map = null;
        }
    }
}
