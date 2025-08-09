using System.Net.Http.Json;

namespace eft_dma_radar.Tarkov.Data.TarkovMarket
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
            var client = App.HttpClientFactory.CreateClient("resilient");
            using var response = await client.PostAsJsonAsync(
                requestUri: "https://api.tarkov.dev/graphql", 
                value: query);
            response.EnsureSuccessStatusCode();
            return await JsonSerializer.DeserializeAsync<TarkovDevQuery>(await response.Content.ReadAsStreamAsync(), _jsonOptions);
        }
    }
}
