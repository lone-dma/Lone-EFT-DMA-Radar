namespace EftDmaRadarLite.Tarkov.Data.ProfileApi.Schema
{
    public sealed class EFTProfileResponse
    {
        public ProfileData Data { get; init; }
        public string Raw { get; init; }
        public DateTimeOffset LastUpdated { get; init; }
    }
}
