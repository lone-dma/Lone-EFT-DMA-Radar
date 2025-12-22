using LiteDB;
using System.IO.Compression;

namespace LoneEftDmaRadar.Web.ProfileApi
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
        /// Raw JSON Data for <see cref="ProfileApiTypes.ProfileData"/>.
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
        /// Date/Time the data was cached.
        /// </summary>
        public DateTimeOffset Cached { get; set; }

        /// <summary>
        /// TRUE if the data was recently cached, otherwise FALSE.
        /// </summary>
        [BsonIgnore]
        public bool IsCachedRecent => DateTimeOffset.UtcNow - Cached < TimeSpan.FromDays(2);

        /// <summary>
        /// Attempt to deserialize the cached data into a <see cref="ProfileData"/> instance.
        /// </summary>
        /// <returns><see cref="ProfileData"/> instance.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="JsonException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public ProfileApiTypes.ProfileData ToProfileData()
        {
            return System.Text.Json.JsonSerializer.Deserialize<ProfileApiTypes.ProfileData>(this.Data, Program.JsonOptions) ??
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
