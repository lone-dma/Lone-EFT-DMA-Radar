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

namespace EftDmaRadarLite.Tarkov.Data.ProfileApi
{
    /// <summary>
    /// Defines an interface for a Profile API provider.
    /// Providers should be implemented in a thread-safe manner.
    /// </summary>
    public interface IProfileApiProvider
    {
        /// <summary>
        /// Default JSON Options for all Profile API providers and related services.
        /// </summary>
        internal static JsonSerializerOptions JsonOptions { get; } = new()
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        private static readonly ConcurrentBag<IProfileApiProvider> _providers = new();
        /// <summary>
        /// All Profile API providers.
        /// </summary>
        public static IEnumerable<IProfileApiProvider> AllProviders => _providers;

        /// <summary>
        /// True if the provider is enabled.
        /// </summary>
        bool IsEnabled { get; }
        /// <summary>
        /// True if the provider can run at this time.
        /// </summary>
        bool CanRun { get; }
        /// <summary>
        /// Priority of this data. Lower values indicate a better quality provider.
        /// </summary>
        uint Priority { get; }

        /// <summary>
        /// See if the provider can lookup a profile by account ID, and hasn't previously returned NOT FOUND, etc. that indicates you should not try again.
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        bool CanLookup(string accountId);

        /// <summary>
        /// Lookup a profile by account ID.
        /// Must not throw exceptions.
        /// </summary>
        /// <param name="accountId">Account id of player profile to lookup.</param>
        /// <param name="ct">Cancellation Token that will be signalled when no longer in a raid.</param>
        /// <returns>Player profile result. NULL if not found or an error occurred.</returns>
        Task<EFTProfileResponse> GetProfileAsync(string accountId, CancellationToken ct);

        /// <summary>
        /// Add a provider to the collection.
        /// </summary>
        /// <param name="provider">Provider to add.</param>
        protected static void Register(IProfileApiProvider provider) => _providers.Add(provider);
    }
}
