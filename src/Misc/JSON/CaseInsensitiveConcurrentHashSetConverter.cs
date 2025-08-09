namespace eft_dma_radar.Misc.JSON
{
    public sealed class CaseInsensitiveConcurrentHashSetConverter
        : JsonConverter<ConcurrentHashSet<string>>
    {
        public override ConcurrentHashSet<string> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            // read as a List<string>
            var items = JsonSerializer.Deserialize<List<string>>(ref reader, options);
            var set = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (items != null)
            {
                foreach (var s in items)
                    set.Add(s!);
            }
            return set;
        }

        public override void Write(
            Utf8JsonWriter writer,
            ConcurrentHashSet<string> value,
            JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var s in value)
                writer.WriteStringValue(s);
            writer.WriteEndArray();
        }
    }
}
