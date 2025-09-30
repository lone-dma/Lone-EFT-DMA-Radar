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

using EftDmaRadarLite.Misc;
using EftDmaRadarLite.Tarkov.Data.ProfileApi.Schema;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;

namespace EftDmaRadarLite.Tarkov.Data.ProfileApi.Providers
{
    public sealed class TarkovDevProvider : IProfileApiProvider
    {
        static TarkovDevProvider()
        {
            IProfileApiProvider.Register(new TarkovDevProvider());
        }

        internal static void Configure(IServiceCollection services)
        {
            services.AddHttpClient(nameof(TarkovDevProvider), client =>
            {
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("identity"));
                client.BaseAddress = new Uri("https://players.tarkov.dev/");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
            {
                SslOptions = new()
                {
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                },
                AutomaticDecompression = DecompressionMethods.Brotli | DecompressionMethods.GZip | DecompressionMethods.Deflate
            })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.ShouldRetryAfterHeader = true;
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(20);
                options.CircuitBreaker.SamplingDuration = options.AttemptTimeout.Timeout * 2;
                options.CircuitBreaker.FailureRatio = 1.0;
                options.CircuitBreaker.MinimumThroughput = 2;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromMinutes(1);
            });
        }

        private readonly ConcurrentDictionary<string, byte> _skip = new(StringComparer.OrdinalIgnoreCase);

        public uint Priority { get; } = App.Config.ProfileApi.TarkovDev.Priority;

        public bool IsEnabled { get; } = App.Config.ProfileApi.TarkovDev.Enabled;

        public bool CanRun { get; } = true;

        private TarkovDevProvider() { }

        public bool CanLookup(string accountId) => !_skip.ContainsKey(accountId);

        public async Task<EFTProfileResponse> GetProfileAsync(string accountId)
        {
            if (_skip.ContainsKey(accountId))
            {
                return null;
            }
            try
            {
                var client = App.HttpClientFactory.CreateClient(nameof(TarkovDevProvider));
                using var response = await client.GetAsync($"profile/{accountId}.json");
                if (response.StatusCode is HttpStatusCode.NotFound)
                {
                    _skip.TryAdd(accountId, 0);
                }
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(json);
                long epoch = jsonDoc.RootElement.GetProperty("updated").GetInt64();
                var result = JsonSerializer.Deserialize<ProfileData>(json, IProfileApiProvider.JsonOptions) ??
                    throw new InvalidOperationException("Failed to deserialize response");
                Debug.WriteLine($"[TarkovDevProvider] Got Profile '{accountId}'!");
                return new()
                {
                    Data = result,
                    Raw = json,
                    Updated = DateTimeOffset.FromUnixTimeMilliseconds(epoch)
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TarkovDevProvider] Failed to get profile: {ex}");
                return null;
            }
        }
    }
}
