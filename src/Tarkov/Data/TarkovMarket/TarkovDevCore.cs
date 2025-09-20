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

using System.Net.Http.Json;

namespace EftDmaRadarLite.Tarkov.Data.TarkovMarket
{
    internal static class TarkovDevCore
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static async Task<TarkovDevQuery> QueryTarkovDevAsync()
        {
            var query = new Dictionary<string, string>
            {
                { "query",
                """
                {
                    items { 
                        id 
                        name 
                        shortName 
                        width 
                        height 
                        sellFor { 
                            vendor { 
                                name 
                            } 
                            priceRUB 
                        } 
                        basePrice 
                        avg24hPrice 
                        historicalPrices { 
                            price 
                        } 
                        categories { 
                            name 
                        } 
                    }
                    questItems { 
                        id shortName 
                    }
                    lootContainers { 
                        id 
                        normalizedName 
                        name 
                    }
                    tasks {
                        id
                        name
                        objectives {
                            id
                            type
                            description
                            maps {
                                id
                                name
                                normalizedName
                            }
                            ... on TaskObjectiveItem {
                                item {
                                id
                                name
                                shortName
                                }
                                zones {
                                id
                                map {
                                    id
                                    normalizedName
                                    name
                                }
                                position {
                                    y
                                    x
                                    z
                                }
                                }
                                requiredKeys {
                                id
                                name
                                shortName
                                }
                                count
                                foundInRaid
                            }
                            ... on TaskObjectiveMark {
                                id
                                description
                                markerItem {
                                id
                                name
                                shortName
                                }
                                maps {
                                id
                                normalizedName
                                name
                                }
                                zones {
                                id
                                map {
                                    id
                                    normalizedName
                                    name
                                }
                                position {
                                    y
                                    x
                                    z
                                }
                                }
                                requiredKeys {
                                id
                                name
                                shortName
                                }
                            }
                            ... on TaskObjectiveQuestItem {
                                id
                                description
                                requiredKeys {
                                id
                                name
                                shortName
                                }
                                maps {
                                id
                                normalizedName
                                name
                                }
                                zones {
                                id
                                map {
                                    id
                                    normalizedName
                                    name
                                }
                                position {
                                    y
                                    x
                                    z
                                }
                                }
                                requiredKeys {
                                id
                                name
                                shortName
                                }
                                questItem {
                                    id
                                    name
                                    shortName
                                    normalizedName
                                    description
                                }
                                count
                            }
                            ... on TaskObjectiveBasic {
                                id
                                description
                                requiredKeys {
                                id
                                name
                                shortName
                                }
                                maps {
                                id
                                normalizedName
                                name
                                }
                                zones {
                                id
                                map {
                                    id
                                    normalizedName
                                    name
                                }
                                position {
                                    y
                                    x
                                    z
                                }
                                }
                                requiredKeys {
                                id
                                name
                                shortName
                                }
                            }
                        }
                    }
                }
                """
                }
            };
            var client = App.HttpClientFactory.CreateClient("default");
            using var response = await client.PostAsJsonAsync(
                requestUri: "https://api.tarkov.dev/graphql", 
                value: query);
            response.EnsureSuccessStatusCode();
            return await JsonSerializer.DeserializeAsync<TarkovDevQuery>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
        }
    }
}
