﻿/*
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

using EftDmaRadarLite.Tarkov.Data.ProfileApi.Schema;
using Microsoft.AspNetCore.Http;
using System.Threading.RateLimiting;

namespace EftDmaRadarLite.Tarkov.Data.ProfileApi.Providers
{
    public sealed class EftApiTechProvider : IProfileApiProvider
    {
        /// <summary>
        /// Singleton instance.
        /// </summary>
        internal static readonly EftApiTechProvider Instance = new();

        private readonly HashSet<string> _skip = new(StringComparer.OrdinalIgnoreCase);
        private readonly TokenBucketRateLimiter _limiter = new(
            new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                TokensPerPeriod = 1,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1) / App.Config.ProfileApi.EftApiTech.RequestsPerMinute,
                QueueLimit = 0
            });

        public uint Priority { get; } = App.Config.ProfileApi.EftApiTech.Priority;

        public bool IsEnabled { get; } = App.Config.ProfileApi.EftApiTech.Enabled;

        public bool CanRun => (_limiter.GetStatistics()?.CurrentAvailablePermits ?? 0) > 0;

        private EftApiTechProvider() { }

        public bool CanLookup(string accountId) => !_skip.Contains(accountId);

        public async Task<EFTProfileResponse> GetProfileAsync(string accountId, CancellationToken ct)
        {
            try
            {
                using var lease = await _limiter.AcquireAsync(1, ct);
                if (!lease.IsAcquired)
                    return null; // Rate limit hit
                var client = App.HttpClientFactory.CreateClient("eft-api");
                using var response = await client.GetAsync($"api/profile/{accountId}", ct);
                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    MessageBox.Show(MainWindow.Instance, $"eft-api.tech returned {response.StatusCode}. Please make sure your Api Key and IP Address are set correctly.", nameof(EftApiTechProvider), MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
                {
                    _skip.Add(accountId);
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
                var result = JsonSerializer.Deserialize<ProfileData>(raw) ??
                    throw new InvalidOperationException("Failed to deserialize response");
                Debug.WriteLine($"[EftApiTechProvider] Got Profile '{accountId}'!");
                return new()
                {
                    Data = result,
                    Raw = raw,
                    LastUpdated = DateTimeOffset.FromUnixTimeMilliseconds(epoch)
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
