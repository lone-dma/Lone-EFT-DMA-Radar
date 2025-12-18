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

using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Authentication;

namespace LoneEftDmaRadar.Web.TarkovDev.Data
{
    internal static class TarkovDevGraphQLApi
    {
        internal static void Configure(IServiceCollection services)
        {
            services.AddHttpClient(nameof(TarkovDevGraphQLApi), client =>
            {
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("identity"));
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
            {
                SslOptions = new()
                {
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                },
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.Brotli | DecompressionMethods.GZip | DecompressionMethods.Deflate
            })
            .AddStandardResilienceHandler(options =>
            {
                // Add retry logic for 403 responses -> sometimes tarkov.dev returns 403 for no reason but works immediately on retry
                var origShouldHandle = options.Retry.ShouldHandle;
                options.Retry.ShouldHandle = args =>
                {
                    if (args.Outcome.Result is HttpResponseMessage response && response.StatusCode == HttpStatusCode.Forbidden)
                        return ValueTask.FromResult(true);

                    return origShouldHandle(args);
                };
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(100);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.SamplingDuration = options.AttemptTimeout.Timeout * 2;
            });
        }

        /// <summary>
        /// Retrieves updated data from the Tarkov.Dev GraphQL API and returns the <see cref="TarkovDevTypes.DataElement"/>.
        /// </summary>
        public static async Task<TarkovDevTypes.DataElement> GetTarkovDataAsync()
        {
            using var response = await QueryTarkovDevAsync();
            response.EnsureSuccessStatusCode();
            var query = await JsonSerializer.DeserializeAsync<TarkovDevTypes.ApiResponse>(await response.Content.ReadAsStreamAsync(), App.JsonOptions) ??
                throw new InvalidOperationException("Failed to deserialize Tarkov.Dev Query Response.");
            ProcessRawQuery(query);
            return query.Data;
        }

        private static void ProcessRawQuery(TarkovDevTypes.ApiResponse query)
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

        private static async Task<HttpResponseMessage> QueryTarkovDevAsync()
        {
            var query = new Dictionary<string, string>
            {
                { "query",
                """
                {
                    maps {
                        name
                        nameId
                        extracts {
                            name
                            faction
                            position {x,y,z}
                        }
                        transits {
                            description
                            position {x,y,z}
                        }
                        hazards {
                          hazardType
                          position {
                            x
                            y
                            z
                          }
                        }
                    }
                    playerLevels {
                        exp
                        level
                    }
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
                          nameId
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
                              nameId
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
                            nameId
                            normalizedName
                            name
                          }
                          zones {
                            id
                            map {
                              nameId
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
                            nameId
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
                            nameId
                            normalizedName
                            name
                          }
                          zones {
                            id
                            map {
                              nameId
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
            var client = App.HttpClientFactory.CreateClient(nameof(TarkovDevGraphQLApi));
            return await client.PostAsJsonAsync(
                requestUri: "https://api.tarkov.dev/graphql",
                value: query);
        }
    }
}
