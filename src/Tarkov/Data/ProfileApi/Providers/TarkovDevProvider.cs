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

        public async Task<EFTProfileResponse> GetProfileAsync(string accountId, CancellationToken ct)
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
                string json = await response.Content.ReadAsStringAsync(ct);
                var result = JsonSerializer.Deserialize<ProfileData>(json) ??
                    throw new InvalidOperationException("Failed to deserialize response");
                Debug.WriteLine($"[TarkovDevProvider] Got Profile '{accountId}'!");
                return new()
                {
                    Data = result,
                    Raw = json,
                    LastUpdated = DateTimeOffset.FromUnixTimeMilliseconds(result.Epoch)
                };
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
