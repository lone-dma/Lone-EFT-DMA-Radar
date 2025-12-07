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

namespace LoneEftDmaRadar.Web.TarkovDev.Data
{
    internal static class TarkovDevDataJob
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Retrieves updated Tarkov data from the Tarkov Dev GraphQL API and formats it into a JSON string.
        /// </summary>
        /// <returns>Json string of <see cref="OutgoingTarkovMarketData"/>.</returns>
        public static async Task<string> GetUpdatedDataAsync()
        {
            var json = await TarkovDevGraphQLApi.GetTarkovDataAsync();
            var data = JsonSerializer.Deserialize<TarkovDevDataQuery>(json, _jsonOptions) ??
                throw new InvalidOperationException("Failed to deserialize Tarkov data.");
            var result = new OutgoingTarkovMarketData
            {
                Items = ParseMarketData(data),
                Maps = data.Data.Maps,
                PlayerLevels = data.Data.PlayerLevels,
                Tasks = data.Data.Tasks
            };
            return JsonSerializer.Serialize(result);
        }

        private static List<OutgoingItem> ParseMarketData(TarkovDevDataQuery data)
        {
            var outgoingItems = new List<OutgoingItem>();
            foreach (var item in data.Data.Items)
            {
                int slots = item.Width * item.Height;
                outgoingItems.Add(new OutgoingItem
                {
                    ID = item.Id,
                    ShortName = item.ShortName,
                    Name = item.Name,
                    Categories = item.Categories?.Select(x => x.Name)?.ToList() ?? new(), // Flatten categories
                    TraderPrice = item.HighestVendorPrice,
                    FleaPrice = item.OptimalFleaPrice,
                    Slots = slots
                });
            }
            foreach (var container in data.Data.LootContainers)
            {
                outgoingItems.Add(new OutgoingItem
                {
                    ID = container.Id,
                    ShortName = container.Name,
                    Name = container.NormalizedName,
                    Categories = new() { "Static Container" },
                    TraderPrice = -1,
                    FleaPrice = -1,
                    Slots = 1
                });
            }
            return outgoingItems;
        }

        #region Outgoing JSON

        // This section duplicates some types, but this used to be on my web backend =D

        private sealed class OutgoingTarkovMarketData
        {
            [JsonPropertyName("items")]
            public List<OutgoingItem> Items { get; set; }

            [JsonPropertyName("maps")]
            public List<object> Maps { get; set; }

            [JsonPropertyName("playerLevels")]
            public List<object> PlayerLevels { get; set; }

            [JsonPropertyName("tasks")]
            public List<object> Tasks { get; set; }
        }

        private sealed class OutgoingItem
        {
            [JsonPropertyName("bsgID")]
            public string ID { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("shortName")]
            public string ShortName { get; set; }

            [JsonPropertyName("price")]
            public long TraderPrice { get; set; }
            [JsonPropertyName("fleaPrice")]
            public long FleaPrice { get; set; }
            [JsonPropertyName("slots")]
            public int Slots { get; set; }

            [JsonPropertyName("categories")]
            public List<string> Categories { get; set; }
        }
        #endregion

    }
}
