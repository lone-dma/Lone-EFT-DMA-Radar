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
using LoneEftDmaRadar.Web.ProfileApi.Schema;
using Microsoft.Extensions.DependencyInjection;
using Polly.CircuitBreaker;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Threading.RateLimiting;

namespace LoneEftDmaRadar.Web.EftApiTech
{
    public sealed class EftApiTechProvider : IProfileApiProvider
    {
        private static readonly CircuitBreakerStateProvider _circuitBreakerStateProvider = new();
        static EftApiTechProvider()
        {
            IProfileApiProvider.Register(new EftApiTechProvider());
        }

        internal static void Configure(IServiceCollection services)
        {
            services.AddHttpClient(nameof(EftApiTechProvider), client =>
            {
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("identity"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", App.Config.ProfileApi.EftApiTech.ApiKey);
                client.BaseAddress = new Uri("https://eft-api.tech/");
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
                options.CircuitBreaker.MinimumThroughput = 2;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromMinutes(1);
            });
        }

        private readonly ConcurrentDictionary<string, byte> _skip = new(StringComparer.OrdinalIgnoreCase);
        private readonly SlidingWindowRateLimiter _limiter = new(new SlidingWindowRateLimiterOptions()
        {
            AutoReplenishment = true,
            PermitLimit = App.Config.ProfileApi.EftApiTech.RequestsPerMinute,
            QueueLimit = 0,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 12
        });

        public uint Priority { get; } = App.Config.ProfileApi.EftApiTech.Priority;

        public bool IsEnabled { get; } = App.Config.ProfileApi.EftApiTech.Enabled;

        public bool CanRun => _circuitBreakerStateProvider.CircuitState == CircuitState.Closed && (_limiter.GetStatistics()?.CurrentAvailablePermits ?? 0) > 0;

        private EftApiTechProvider() { }

        public bool CanLookup(string accountId) => !_skip.ContainsKey(accountId);

        public async Task<EFTProfileResponse> GetProfileAsync(string accountId, CancellationToken ct)
        {
            try
            {
                if (_skip.ContainsKey(accountId))
                {
                    return null;
                }
                using var lease = await _limiter.AcquireAsync(1, ct);
                if (!lease.IsAcquired)
                    return null; // Rate limit hit
                var client = App.HttpClientFactory.CreateClient(nameof(EftApiTechProvider));
                using var response = await client.GetAsync($"api/profile/{accountId}", ct);
                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    MessageBox.Show(MainWindow.Instance, $"eft-api.tech returned '{response.StatusCode}'. Please make sure your Api Key and IP Address are set correctly.", nameof(EftApiTechProvider), MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
                {
                    _skip.TryAdd(accountId, 0);
                }
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync(ct);
                using var jsonDoc = JsonDocument.Parse(json);
                bool success = jsonDoc.RootElement.GetProperty("success").GetBoolean();
                if (!success)
                    throw new InvalidOperationException("Profile request was not successful.");
                var epoch = jsonDoc.RootElement.GetProperty("lastUpdated").GetProperty("epoch").GetInt64();
                var data = jsonDoc.RootElement.GetProperty("data");
                string raw = data.GetRawText();
                var result = JsonSerializer.Deserialize<ProfileData>(raw, IProfileApiProvider.JsonOptions) ??
                    throw new InvalidOperationException("Failed to deserialize response");
                Debug.WriteLine($"[EftApiTechProvider] Got Profile '{accountId}'!");
                return new()
                {
                    Data = result,
                    Raw = raw,
                    Updated = DateTimeOffset.FromUnixTimeMilliseconds(epoch)
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EftApiTechProvider] Failed to get profile: {ex}");
                return null;
            }
        }
    }
}
