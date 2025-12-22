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

using LoneEftDmaRadar.Web.ProfileApi;
using Microsoft.Extensions.DependencyInjection;
using Polly.CircuitBreaker;
using System.Net.Http.Headers;
using System.Security.Authentication;

namespace LoneEftDmaRadar.Web.TarkovDev.Profiles
{
    public sealed class TarkovDevProfileProvider : IProfileApiProvider
    {
        private static readonly CircuitBreakerStateProvider _circuitBreakerStateProvider = new();

        static TarkovDevProfileProvider()
        {
            IProfileApiProvider.Register(new TarkovDevProfileProvider());
        }

        internal static void Configure(IServiceCollection services)
        {
            services.AddHttpClient(nameof(TarkovDevProfileProvider), client =>
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
                options.CircuitBreaker.StateProvider = _circuitBreakerStateProvider;
                options.CircuitBreaker.SamplingDuration = options.AttemptTimeout.Timeout * 2;
                options.CircuitBreaker.FailureRatio = 1.0;
                options.CircuitBreaker.MinimumThroughput = options.Retry.MaxRetryAttempts * 3;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromMinutes(1);
            });
        }

        private readonly ConcurrentDictionary<string, byte> _skip = new(StringComparer.OrdinalIgnoreCase);

        public uint Priority { get; } = Program.Config.ProfileApi.TarkovDev.Priority;

        public bool IsEnabled { get; } = Program.Config.ProfileApi.TarkovDev.Enabled;

        public bool CanRun { get; } = true;

        private TarkovDevProfileProvider() { }

        public bool CanLookup(string accountId) => _circuitBreakerStateProvider.CircuitState == CircuitState.Closed && !_skip.ContainsKey(accountId);

        public async Task<EFTProfileResponse> GetProfileAsync(string accountId, CancellationToken ct)
        {
            try
            {
                if (_skip.ContainsKey(accountId))
                {
                    return null;
                }
                var client = Program.HttpClientFactory.CreateClient(nameof(TarkovDevProfileProvider));
                using var response = await client.GetAsync($"profile/{accountId}.json", ct);
                string content = await response.Content.ReadAsStringAsync(ct);
                if (!response.IsSuccessStatusCode) // Handle errors
                {
                    if (response.StatusCode is HttpStatusCode.NotFound)
                    {
                        _skip.TryAdd(accountId, 0);
                    }
                    Logging.WriteLine($"[TarkovDevProvider] Failed to get Profile '{accountId}': [{response.StatusCode}] '{content}'");
                    return null;
                }
                using var jsonDoc = JsonDocument.Parse(content);
                long epoch = jsonDoc.RootElement.GetProperty("updated").GetInt64();
                var result = JsonSerializer.Deserialize<ProfileApiTypes.ProfileData>(content, Program.JsonOptions) ??
                    throw new InvalidOperationException("Failed to deserialize response");
                Logging.WriteLine($"[TarkovDevProvider] Got Profile '{accountId}'!");
                return new()
                {
                    Data = result,
                    Raw = content,
                    Updated = DateTimeOffset.FromUnixTimeMilliseconds(epoch)
                };
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"[TarkovDevProvider] Unhandled Exception: {ex}");
                return null;
            }
        }
    }
}
