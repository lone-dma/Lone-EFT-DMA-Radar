namespace eft_dma_radar.Misc.JSON
{
    public sealed class CaseInsensitiveConcurrentDictionaryConverter<TValue>
        : JsonConverter<ConcurrentDictionary<string, TValue>>
    {
        public override ConcurrentDictionary<string, TValue> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            var dic = (ConcurrentDictionary<string, TValue>)JsonSerializer
                .Deserialize(ref reader, typeToConvert, options);
            return new ConcurrentDictionary<string, TValue>(
                dic!, StringComparer.OrdinalIgnoreCase);
        }

        public override void Write(
            Utf8JsonWriter writer,
            ConcurrentDictionary<string, TValue> value,
            JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(
                writer, value, value.GetType(), options);
        }
    }
}
