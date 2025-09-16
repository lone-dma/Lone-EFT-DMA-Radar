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

namespace EftDmaRadarLite.UI.ColorPicker
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
