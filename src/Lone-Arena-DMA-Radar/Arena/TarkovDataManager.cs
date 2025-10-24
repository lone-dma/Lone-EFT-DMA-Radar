﻿/*
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

using LoneArenaDmaRadar.UI.Misc;
using LoneArenaDmaRadar.Web.TarkovDev.Data;
using System.Collections.Frozen;

namespace LoneArenaDmaRadar.Arena
{
    /// <summary>
    /// Manages Tarkov Dynamic Data (Items, Quests, etc).
    /// </summary>
    internal static class TarkovDataManager
    {
        private const string DATA_FILE_NAME = "data.json";
        private static readonly string _dataFile = Path.Combine(App.ConfigPath.FullName, DATA_FILE_NAME);

        /// <summary>
        /// Master items dictionary - mapped via BSGID String.
        /// </summary>
        public static FrozenDictionary<string, TarkovMarketItem> AllItems { get; private set; }

        #region Startup

        /// <summary>
        /// Call to start EftDataManager Module. ONLY CALL ONCE.
        /// </summary>
        /// <param name="loading">Loading UI Form.</param>
        /// <param name="defaultOnly">True if you want to load cached/default data only.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task ModuleInitAsync(LoadingWindow loading, bool defaultOnly = false)
        {
            try
            {
                var data = await GetDataAsync(loading, defaultOnly);
                AllItems = data.Items.Where(x => !x.Tags?.Contains("Static Container") ?? false)
                    .DistinctBy(x => x.BsgId, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(k => k.BsgId, v => v, StringComparer.OrdinalIgnoreCase)
                    .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"ERROR loading {DATA_FILE_NAME}", ex);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads Market data via several possible methods (cached,web,embedded resource).
        /// </summary>
        /// <returns>Collection of TarkovMarketItems.</returns>
        private static async Task<TarkovMarketData> GetDataAsync(LoadingWindow loading, bool defaultOnly)
        {
            TarkovMarketData data;
            string json = null;
            if (!defaultOnly &&
                (!File.Exists(_dataFile) ||
            File.GetLastWriteTime(_dataFile).AddHours(4) < DateTime.Now)) // only update every 4h
            {
                await loading.ViewModel.UpdateProgressAsync(loading.ViewModel.Progress, "Getting Updated Tarkov.Dev Data...");
                json = await GetUpdatedDataJsonAsync();
                if (json is not null)
                {
                    await File.WriteAllTextAsync(_dataFile, json);
                }
            }
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };
            if (json is null && File.Exists(_dataFile))
            {
                json = await File.ReadAllTextAsync(_dataFile);
            }
            json ??= await GetDefaultDataAsync();
            try
            {
                data = JsonSerializer.Deserialize<TarkovMarketData>(json, jsonOptions);
            }
            catch (JsonException)
            {
                File.Delete(_dataFile); // Delete data if json is corrupt.
                throw;
            }
            ArgumentNullException.ThrowIfNull(data, nameof(data));
            return data;
        }

        private static async Task<string> GetDefaultDataAsync()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LoneArenaDmaRadar.DEFAULT_DATA.json"))
            {
                var data = new byte[stream!.Length];
                await stream.ReadExactlyAsync(data);
                return Encoding.UTF8.GetString(data);
            }
        }

        /// <summary>
        /// Contacts the Loot Server for an updated Loot List.
        /// </summary>
        /// <returns>Json string of Loot List.</returns>
        private static async Task<string> GetUpdatedDataJsonAsync()
        {
            try
            {
                return await TarkovDevDataJob.GetUpdatedDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WARNING: Failed to retrieve updated Tarkov Market Data. Will use backup source(s).\n\n{ex}",
                    nameof(TarkovDataManager),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return null;
            }
        }

        #endregion

        #region Types

        public sealed class TarkovMarketData
        {
            [JsonPropertyName("items")]
            public List<TarkovMarketItem> Items { get; set; }
            [JsonPropertyName("tasks")]
            public List<TaskElement> Tasks { get; set; }
        }

        public partial class TaskElement
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("objectives")]
            public List<ObjectiveElement> Objectives { get; set; }

            public partial class ObjectiveElement
            {
                [JsonPropertyName("id")]
                public string Id { get; set; }

                [JsonPropertyName("type")]
                public string Type { get; set; }

                [JsonPropertyName("description")]
                public string Description { get; set; }

                [JsonPropertyName("requiredKeys")]
                public List<List<MarkerItemClass>> RequiredKeys { get; set; }

                [JsonPropertyName("maps")]
                public List<BasicDataElement> Maps { get; set; }

                [JsonPropertyName("zones")]
                public List<ZoneElement> Zones { get; set; }

                [JsonPropertyName("count")]
                public int Count { get; set; }

                [JsonPropertyName("foundInRaid")]
                public bool FoundInRaid { get; set; }

                [JsonPropertyName("item")]
                public MarkerItemClass Item { get; set; }

                [JsonPropertyName("questItem")]
                public ObjectiveQuestItem QuestItem { get; set; }

                [JsonPropertyName("markerItem")]
                public MarkerItemClass MarkerItem { get; set; }

                public class MarkerItemClass
                {
                    [JsonPropertyName("id")]
                    public string Id { get; set; }

                    [JsonPropertyName("name")]
                    public string Name { get; set; }

                    [JsonPropertyName("shortName")]
                    public string ShortName { get; set; }
                }

                public class ObjectiveQuestItem
                {
                    [JsonPropertyName("id")]
                    public string Id { get; set; }

                    [JsonPropertyName("name")]
                    public string Name { get; set; }

                    [JsonPropertyName("shortName")]
                    public string ShortName { get; set; }

                    [JsonPropertyName("normalizedName")]
                    public string NormalizedName { get; set; }

                    [JsonPropertyName("description")]
                    public string Description { get; set; }
                }

                public class ZoneElement
                {
                    [JsonPropertyName("id")]
                    public string Id { get; set; }

                    [JsonPropertyName("position")]
                    public PositionElement Position { get; set; }

                    [JsonPropertyName("map")]
                    public BasicDataElement Map { get; set; }
                }

                public class BasicDataElement
                {
                    [JsonPropertyName("id")]
                    public string Id { get; set; }

                    [JsonPropertyName("normalizedName")]
                    public string NormalizedName { get; set; }

                    [JsonPropertyName("name")]
                    public string Name { get; set; }
                }

                public class PositionElement
                {
                    [JsonPropertyName("y")]
                    public float Y { get; set; }

                    [JsonPropertyName("x")]
                    public float X { get; set; }

                    [JsonPropertyName("z")]
                    public float Z { get; set; }
                }
            }
        }

        #endregion
    }
}