using eft_dma_radar.Misc;
using eft_dma_radar.Tarkov.Data.ProfileApi;
using eft_dma_radar.Tarkov.Data.ProfileApi.Schema;
using System.Net.Http;

namespace eft_dma_radar.Tarkov.Data.ProfileApi.Providers
{
    public sealed class EftApiTechProvider : IProfileApiProvider
    {
        static EftApiTechProvider()
        {
            IProfileApiProvider.Register(new EftApiTechProvider());
        }

        private readonly HashSet<string> _skip = new(StringComparer.OrdinalIgnoreCase);
        private readonly TimeSpan _rate = TimeSpan.FromMinutes(1) / App.Config.ProfileApi.EftApiTech.RequestsPerMinute;
        private DateTimeOffset _nextRun = DateTimeOffset.MinValue;
        private TimeSpan _rateLimit;

        public uint Priority { get; } = App.Config.ProfileApi.EftApiTech.Priority;

        public bool IsEnabled { get; } = App.Config.ProfileApi.EftApiTech.Enabled;

        public bool CanRun => DateTimeOffset.UtcNow > _nextRun;

        private EftApiTechProvider() { }

        public bool CanLookup(string accountId) => !_skip.Contains(accountId);

        public async Task<ProfileData> GetProfileAsync(string accountId, CancellationToken ct)
        {
            try
            {
                string uri = $"https://eft-api.tech/api/profile/{accountId}?includeOnlyPmcStats=true";
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                request.Headers.Add("Authorization", $"Bearer {App.Config.ProfileApi.EftApiTech.ApiKey}");
                var client = App.HttpClientFactory.CreateClient("default");
                using var response = await client.SendAsync(request, ct);
                if (response.StatusCode is HttpStatusCode.Unauthorized)
                {
                    MessageBox.Show(MainWindow.Instance, "eft-api.tech returned 401 UNAUTHORIZED. Please make sure your Api Key and IP Address are set correctly.", nameof(EftApiTechProvider), MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
                {
                    _skip.Add(accountId);
                }
                else if (response.StatusCode is HttpStatusCode.TooManyRequests)
                {
                    _rateLimit = response.Headers.RetryAfter.GetRetryAfter();
                }
                response.EnsureSuccessStatusCode(); // Handles 429 TooManyRequests
                using var stream = await response.Content.ReadAsStreamAsync(ct);
                var result = await JsonSerializer.DeserializeAsync<EftApiTechResponse>(
                    utf8Json: stream,
                    cancellationToken: ct);
                ArgumentNullException.ThrowIfNull(result, nameof(result));
                if (!result.Success)
                    throw new InvalidOperationException("Profile request was not successful.");
                Debug.WriteLine($"[EftApiTechProvider] Got Profile '{accountId}'!");
                return result.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EftApiTechProvider] Failed to get profile: {ex}");
                return null;
            }
            finally
            {
                _nextRun = DateTimeOffset.UtcNow + _rate + _rateLimit;
                _rateLimit = TimeSpan.Zero; // Reset rate limit after use
            }
        }
    }
}
