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
        /// <summary>
        /// Retrieves updated Tarkov data from the Tarkov Dev GraphQL API and formats it into a JSON string.
        /// </summary>
        /// <returns>Json string of <see cref="OutgoingTarkovMarketData"/>.</returns>
        public static async Task<TarkovDevData> GetUpdatedDataAsync()
        {
            var json = await TarkovDevGraphQLApi.GetTarkovDataAsync();
            var data = JsonSerializer.Deserialize<TarkovDevTypes.TarkovDevDataQuery>(json, App.JsonOptions) ??
                throw new InvalidOperationException("Failed to deserialize Tarkov data.");
            ProcessRawQuery(data);
            return data.Data;
        }

        private static void ProcessRawQuery(TarkovDevTypes.TarkovDevDataQuery query)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var cleanedItems = new List<TarkovMarketItem>();
            foreach (var item in query.Data.TarkovDevItems)
            {
                int slots = item.Width * item.Height;
                cleanedItems.Add(new TarkovMarketItem
                {
                    BsgId = item.Id,
                    ShortName = item.ShortName,
                    Name = item.Name,
                    Tags = item.Categories?.Select(x => x.Name)?.ToList() ?? new(), // Flatten categories
                    TraderPrice = item.HighestVendorPrice,
                    FleaPrice = item.OptimalFleaPrice,
                    Slots = slots
                });
            }
            foreach (var container in query.Data.TarkovDevContainers)
            {
                cleanedItems.Add(new TarkovMarketItem
                {
                    BsgId = container.Id,
                    ShortName = container.Name,
                    Name = container.NormalizedName,
                    Tags = new List<string>() { "Static Container" },
                    TraderPrice = -1,
                    FleaPrice = -1,
                    Slots = 1
                });
            }
            // Set result
            query.Data.Items = cleanedItems;
            // Null out processed query
            query.Data.TarkovDevItems = null;
            query.Data.TarkovDevContainers = null;
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
