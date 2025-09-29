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

using EftDmaRadarLite.Tarkov.Data.ProfileApi;
using EftDmaRadarLite.Tarkov.Data.ProfileApi.Schema;
using LiteDB;
using System.IO.Compression;

namespace EftDmaRadarLite.Misc.Cache
{
    public class EftProfileDto
    {
        /// <summary>
        /// Player Account ID.
        /// </summary>
        [BsonId]
        public long Id { get; init; }
        [BsonField("Data")]
        private byte[] _data;
        /// <summary>
        /// Raw JSON Data for <see cref="ProfileData"/>.
        /// </summary>
        [BsonIgnore]
        public string Data
        {
            get => Decompress(_data);
            set => _data = Compress(value);
        }
        /// <summary>
        /// Date/Time of the profile data. This may be older than the time it was cached at.
        /// </summary>
        public DateTimeOffset Updated { get; set; }
        /// <summary>
        /// Date/Time the data was cached locally.
        /// </summary>
        public DateTimeOffset Cached { get; set; }

        /// <summary>
        /// TRUE if the data was recently cached, otherwise FALSE.
        /// </summary>
        [BsonIgnore]
        public bool IsCachedRecent => DateTimeOffset.UtcNow - Cached < TimeSpan.FromDays(1);

        /// <summary>
        /// Attempt to deserialize the cached data into a <see cref="ProfileData"/> instance.
        /// </summary>
        /// <returns><see cref="ProfileData"/> instance.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="JsonException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public ProfileData ToProfileData()
        {
            return System.Text.Json.JsonSerializer.Deserialize<ProfileData>(this.Data, IProfileApiProvider.JsonOptions) ??
                throw new InvalidOperationException($"Failed to deserialize ProfileData from {nameof(EftProfileDto)}.");
        }

        private static byte[] Compress(string text)
        {
            if (text is null)
                return null;
            var inputBytes = Encoding.UTF8.GetBytes(text);
            using var output = new MemoryStream();
            using (var brotli = new BrotliStream(output, CompressionLevel.Optimal, leaveOpen: true))
            {
                brotli.Write(inputBytes);
            }
            return output.ToArray();
        }

        private static string Decompress(byte[] compressed)
        {
            if (compressed is null)
                return null;
            using var input = new MemoryStream(compressed);
            using var brotli = new BrotliStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            brotli.CopyTo(output);
            return Encoding.UTF8.GetString(output.ToArray());
        }
    }
}
