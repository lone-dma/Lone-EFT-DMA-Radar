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

using EftDmaRadarLite.Misc.Cache;
using EftDmaRadarLite.Tarkov.Data.ProfileApi.Schema;

namespace EftDmaRadarLite.Tarkov.Data.ProfileApi.Providers
{
    public sealed class LocalProfileProvider : IProfileApiProvider
    {
        static LocalProfileProvider()
        {
            IProfileApiProvider.Register(new LocalProfileProvider());
        }

        private readonly HashSet<string> _skip = new(StringComparer.OrdinalIgnoreCase);

        public uint Priority { get; } = App.Config.ProfileApi.LocalProfile.Priority;

        public bool IsEnabled { get; } = App.Config.ProfileApi.LocalProfile.Enabled;

        public bool CanRun => true;

        private LocalProfileProvider() { }

        public bool CanLookup(string accountId) => !_skip.Contains(accountId);

        public Task<EFTProfileResponse> GetProfileAsync(string accountId, CancellationToken ct)
        {
            if (_skip.Contains(accountId))
            {
                return Task.FromResult<EFTProfileResponse>(null);
            }
            try
            {
                var acctIdLong = long.Parse(accountId); // Validate Account ID
                var cache = LocalCache.GetProfileCollection();
                if (cache.FindById(acctIdLong) is not CachedPlayerProfile cachedProfile)
                {
                    _skip.Add(accountId);
                    return Task.FromResult<EFTProfileResponse>(null);
                }
                try
                {
                    var data = JsonSerializer.Deserialize<ProfileData>(cachedProfile.Data) ??
                        throw new InvalidOperationException("Failed to deserialize cached data");
                    Debug.WriteLine($"[LocalProfileProvider] Got Profile '{accountId}'!");
                    var result = new EFTProfileResponse()
                    {
                        Data = data,
                        Raw = null, // Won't be re-cached so w/e
                        LastUpdated = DateTimeOffset.MinValue // Don't re-cache
                    };
                    return Task.FromResult(result);
                }
                catch
                {
                    _ = cache.Delete(acctIdLong); // Corrupted cache data, remove it
                    throw;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LocalProfileProvider] Failed to get profile: {ex}");
                return Task.FromResult<EFTProfileResponse>(null);
            }
        }
    }
}
