/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
namespace LoneEftDmaRadar.UI.ColorPicker
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
            writer.WriteStartObject();
            foreach (var kvp in value)
            {
                writer.WriteString(kvp.Key.ToString(), kvp.Value);
            }
            writer.WriteEndObject();
        }
    }
}

