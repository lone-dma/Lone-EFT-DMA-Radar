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

using LoneEftDmaRadar.Web.TarkovDev.Data;
using System.Collections.Frozen;

namespace LoneEftDmaRadar.Tarkov
{
    /// <summary>
    /// Manages Tarkov Dynamic Data (Items, Quests, etc).
    /// </summary>
    public static class TarkovDataManager
    {
        private static readonly FileInfo _bakDataFile = new(Path.Combine(App.ConfigPath.FullName, "data.json.bak"));
        private static readonly FileInfo _tempDataFile = new(Path.Combine(App.ConfigPath.FullName, "data.json.tmp"));
        private static readonly FileInfo _dataFile = new(Path.Combine(App.ConfigPath.FullName, "data.json"));
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        /// <summary>
        /// Master items dictionary - mapped via BSGID String.
        /// </summary>
        public static FrozenDictionary<string, TarkovMarketItem> AllItems { get; private set; }

        /// <summary>
        /// Master containers dictionary - mapped via BSGID String.
        /// </summary>
        public static FrozenDictionary<string, TarkovMarketItem> AllContainers { get; private set; }

        /// <summary>
        /// Maps Data for Tarkov.
        /// </summary>
        public static FrozenDictionary<string, MapElement> MapData { get; private set; }
        /// <summary>
        /// XP Table for Tarkov.
        /// </summary>
        public static IReadOnlyDictionary<int, int> XPTable { get; private set; }

        #region Startup

        /// <summary>
        /// Call to start EftDataManager Module. ONLY CALL ONCE.
        /// </summary>
        /// <param name="loading">Loading UI Form.</param>
        /// <param name="defaultOnly">True if you want to load cached/default data only.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task ModuleInitAsync(bool defaultOnly = false)
        {
            try
            {
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"ERROR loading Game/Loot Data ({_dataFile.Name})", ex);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads Game/FilteredLoot Data and sets the static dictionaries.
        /// If updated data is needed, spawns a background task to retrieve it.
        /// </summary>
        /// <returns></returns>
        private static async Task LoadDataAsync()
        {
            if (_dataFile.Exists)
            {
                DateTime lastWriteTime = File.GetLastWriteTime(_dataFile.FullName);
                await LoadDiskDataAsync();
                if (lastWriteTime < DateTime.Now.Subtract(TimeSpan.FromHours(4))) // only update every 4h
                {
                    _ = Task.Run(LoadRemoteDataAsync); // Run continuations on the thread pool.
                }
            }
            else
            {
                await LoadDefaultDataAsync();
                _ = Task.Run(LoadRemoteDataAsync); // Run continuations on the thread pool.
            }
        }

        /// <summary>
        /// Sets the input <paramref name="data"/> into the static dictionaries.
        /// </summary>
        /// <param name="data">Data to be set.</param>
        private static void SetData(TarkovData data)
        {
            AllItems = data.Items.Where(x => !x.Tags?.Contains("Static Container") ?? false)
                .DistinctBy(x => x.BsgId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(k => k.BsgId, v => v, StringComparer.OrdinalIgnoreCase)
                .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
            AllContainers = data.Items.Where(x => x.Tags?.Contains("Static Container") ?? false)
                .DistinctBy(x => x.BsgId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(k => k.BsgId, v => v, StringComparer.OrdinalIgnoreCase)
                .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
            XPTable = data.PlayerLevels?.ToDictionary(x => x.Exp, x => x.Level) ?? new Dictionary<int, int>();
            var maps = data.Maps.ToDictionary(x => x.NameId, StringComparer.OrdinalIgnoreCase) ??
                new Dictionary<string, MapElement>(StringComparer.OrdinalIgnoreCase);
            maps.TryAdd("Terminal", new MapElement() // Preliminary terminal support
            {
                Name = "Terminal",
                NameId = "Terminal"
            });
            MapData = maps.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, MapElement>().ToFrozenDictionary();
        }

        /// <summary>
        /// Loads default embedded <see cref="TarkovData"/> and sets the static dictionaries.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private static async Task LoadDefaultDataAsync()
        {
            const string resource = "LoneEftDmaRadar.DEFAULT_DATA.json";
            using var dataStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource) ??
                throw new ArgumentNullException(resource);
            var data = await JsonSerializer.DeserializeAsync<TarkovData>(dataStream)
                ?? throw new InvalidOperationException($"Failed to deserialize {nameof(dataStream)}");
            SetData(data);
        }

        /// <summary>
        /// Loads <see cref="TarkovData"/> from disk and sets the static dictionaries.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private static async Task LoadDiskDataAsync()
        {
            var data = await TryLoadFromDiskAsync(_tempDataFile) ??
                await TryLoadFromDiskAsync(_dataFile) ??
                await TryLoadFromDiskAsync(_bakDataFile);
            if (data is null) // Internal soft failover
            {
                _dataFile.Delete();
                await LoadDefaultDataAsync();
                return;
            }
            SetData(data);

            static async Task<TarkovData> TryLoadFromDiskAsync(FileInfo file)
            {
                try
                {
                    if (!file.Exists)
                        return null;
                    using var dataStream = File.OpenRead(file.FullName);
                    return await JsonSerializer.DeserializeAsync<TarkovData>(dataStream, _jsonOptions) ??
                        throw new InvalidOperationException($"Failed to deserialize {nameof(dataStream)}");
                }
                catch
                {
                    return null; // Ignore errors, return null to indicate failure
                }
            }
        }

        /// <summary>
        /// Loads updated Game/FilteredLoot Data from the web and sets the static dictionaries.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private static async Task LoadRemoteDataAsync()
        {
            try
            {
                string dataJson = await TarkovDevDataJob.GetUpdatedDataAsync();
                ArgumentNullException.ThrowIfNull(dataJson, nameof(dataJson));
                await File.WriteAllTextAsync(_tempDataFile.FullName, dataJson);
                if (_dataFile.Exists)
                {
                    File.Replace(
                        sourceFileName: _tempDataFile.FullName,
                        destinationFileName: _dataFile.FullName,
                        destinationBackupFileName: _bakDataFile.FullName,
                        ignoreMetadataErrors: true);
                }
                else
                {
                    File.Copy(
                        sourceFileName: _tempDataFile.FullName,
                        destFileName: _bakDataFile.FullName,
                        overwrite: true);
                    File.Move(
                        sourceFileName: _tempDataFile.FullName,
                        destFileName: _dataFile.FullName,
                        overwrite: true);
                }
                var data = JsonSerializer.Deserialize<TarkovData>(dataJson, _jsonOptions) ??
                    throw new InvalidOperationException($"Failed to deserialize {nameof(dataJson)}");
                SetData(data);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    messageBoxText: $"An unhandled exception occurred while retrieving updated Game/Loot Data from the web: {ex}",
                    caption: App.Name,
                    button: MessageBoxButton.OK,
                    icon: MessageBoxImage.Warning,
                    defaultResult: MessageBoxResult.OK,
                    options: MessageBoxOptions.DefaultDesktopOnly);
            }
        }

        #endregion

        #region Types

        public sealed class TarkovData
        {
            [JsonPropertyName("items")]
            public List<TarkovMarketItem> Items { get; set; } = new();

            [JsonPropertyName("maps")]
            public List<MapElement> Maps { get; set; } = new();

            [JsonPropertyName("playerLevels")]
            public List<PlayerLevelElement> PlayerLevels { get; set; }
        }


        public class PositionElement
        {
            [JsonPropertyName("x")]
            public float X { get; set; }

            [JsonPropertyName("y")]
            public float Y { get; set; }

            [JsonPropertyName("z")]
            public float Z { get; set; }

            public Vector3 AsVector3() => new(X, Y, Z);
        }

        public partial class MapElement
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("nameId")]
            public string NameId { get; set; }

            [JsonPropertyName("extracts")]
            public List<ExtractElement> Extracts { get; set; } = new();

            [JsonPropertyName("transits")]
            public List<TransitElement> Transits { get; set; } = new();

            [JsonPropertyName("hazards")]
            public List<HazardElement> Hazards { get; set; } = new();
        }

        public partial class PlayerLevelElement
        {
            [JsonPropertyName("exp")]
            public int Exp { get; set; }

            [JsonPropertyName("level")]
            public int Level { get; set; }
        }

        public partial class HazardElement
        {
            [JsonPropertyName("hazardType")]
            public string HazardType { get; set; }

            [JsonPropertyName("position")]
            public PositionElement Position { get; set; }
        }

        public partial class ExtractElement
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("faction")]
            public string Faction { get; set; }

            [JsonPropertyName("position")]
            public PositionElement Position { get; set; }

            [JsonIgnore]
            public bool IsPmc => Faction?.Equals("pmc", StringComparison.OrdinalIgnoreCase) ?? false;
            [JsonIgnore]
            public bool IsShared => Faction?.Equals("shared", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public partial class TransitElement
        {
            [JsonPropertyName("description")]
            public string Description { get; set; }

            [JsonPropertyName("position")]
            public PositionElement Position { get; set; }
        }

        #endregion
    }
}