namespace eft_dma_radar.UI.ColorPicker
{
    public class ColorDictionaryConverter
        : JsonConverter<ConcurrentDictionary<ColorPickerOption, string>>
    {
        public override ConcurrentDictionary<ColorPickerOption, string> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            var dict = new ConcurrentDictionary<ColorPickerOption, string>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return dict;

                // propertyName is the enum name in your JSON
                string propertyName = reader.GetString();
                if (!Enum.TryParse<ColorPickerOption>(propertyName,
                                                     ignoreCase: true,
                                                     out var key))
                {
                    // skip the value for this unrecognized key
                    reader.Skip();
                    continue;
                }

                // move to the value token
                reader.Read();
                string value = reader.GetString()!;
                dict[key] = value;
            }

            throw new JsonException();
        }

        public override void Write(
            Utf8JsonWriter writer,
            ConcurrentDictionary<ColorPickerOption, string> value,
            JsonSerializerOptions options)
        {
            // just hand it back to the default serializer
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}
