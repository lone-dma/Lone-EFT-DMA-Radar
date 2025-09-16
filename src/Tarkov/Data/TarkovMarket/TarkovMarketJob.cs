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

namespace EftDmaRadarLite.Tarkov.Data.TarkovMarket
{
    internal static class TarkovMarketJob
    {

        public static async Task<string> GetUpdatedMarketDataAsync()
        {
            try
            {
                var data = await TarkovDevCore.QueryTarkovDevAsync();
                var result = new TarkovMarketData
                {
                    Items = ParseMarketData(data),
                    Tasks = data.Data.Tasks
                };
                return JsonSerializer.Serialize(result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{nameof(TarkovMarketJob)} [FAIL]: {ex}");
                throw;
            }
        }

        private static List<OutgoingItem> ParseMarketData(TarkovDevQuery data)
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
            foreach (var questItem in data.Data.QuestItems)
            {
                outgoingItems.Add(new OutgoingItem
                {
                    ID = questItem.Id,
                    ShortName = $"Q_{questItem.ShortName}",
                    Name = $"Q_{questItem.ShortName}",
                    Categories = new() { "Quest Item" },
                    TraderPrice = -1,
                    FleaPrice = -1,
                    Slots = 1
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
        private sealed class TarkovMarketData
        {
            [JsonPropertyName("items")]
            public List<OutgoingItem> Items { get; set; }
            [JsonPropertyName("tasks")]
            public List<TaskElement> Tasks { get; set; }
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
