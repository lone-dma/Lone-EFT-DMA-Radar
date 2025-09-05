using EftDmaRadarLite.Misc;
using EftDmaRadarLite.Tarkov.Data.ProfileApi.Schema;

namespace EftDmaRadarLite.Tarkov.Data.ProfileApi.Providers
{
    public sealed class TarkovDevProvider : IProfileApiProvider
    {
        static TarkovDevProvider()
        {
            IProfileApiProvider.Register(new TarkovDevProvider());
        }

        private readonly HashSet<string> _skip = new(StringComparer.OrdinalIgnoreCase);
        private readonly TimeSpan _rate = TimeSpan.FromMinutes(1) / App.Config.ProfileApi.TarkovDev.RequestsPerMinute;
        private DateTimeOffset _nextRun = DateTimeOffset.MinValue;
        private TimeSpan _rateLimit;

        public uint Priority { get; } = App.Config.ProfileApi.TarkovDev.Priority;

        public bool IsEnabled { get; } = App.Config.ProfileApi.TarkovDev.Enabled;

        public bool CanRun => DateTimeOffset.UtcNow > _nextRun;

        private TarkovDevProvider() { }

        public bool CanLookup(string accountId) => !_skip.Contains(accountId);

        public async Task<ProfileData> GetProfileAsync(string accountId, CancellationToken ct)
        {
            if (_skip.Contains(accountId))
            {
                return null;
            }
            try
            {
                string uri = $"https://players.tarkov.dev/profile/{accountId}.json";
                var client = App.HttpClientFactory.CreateClient("default");
                using var response = await client.GetAsync(uri, ct);
                if (response.StatusCode is HttpStatusCode.NotFound)
                {
                    _skip.Add(accountId);
                }
                else if (response.StatusCode is HttpStatusCode.TooManyRequests)
                {
                    _rateLimit = response.Headers.RetryAfter.GetRetryAfter();
                }
                response.EnsureSuccessStatusCode(); // Handles 429 TooManyRequests
                using var stream = await response.Content.ReadAsStreamAsync(ct);
                var result = await JsonSerializer.DeserializeAsync<ProfileData>(
                    utf8Json: stream,
                    cancellationToken: ct);
                ArgumentNullException.ThrowIfNull(result, nameof(result));
                Debug.WriteLine($"[TarkovDevProvider] Got Profile '{accountId}'!");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TarkovDevProvider] Failed to get profile: {ex}");
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
