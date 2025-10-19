/*
 * Lone EFT DMA Radar
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

namespace LoneEftDmaRadar.Misc.JSON
{
    public class SKRectJsonConverter : JsonConverter<SKRect>
    {
        public override SKRect Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected StartObject token for SKRect.");

            float left = 0, top = 0, right = 0, bottom = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new SKRect(left, top, right, bottom);

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected PropertyName token.");

                string propertyName = reader.GetString()!;
                reader.Read(); // Move to the value token.

                switch (propertyName)
                {
                    case nameof(SKRect.Left): left = reader.GetSingle(); break;
                    case nameof(SKRect.Top): top = reader.GetSingle(); break;
                    case nameof(SKRect.Right): right = reader.GetSingle(); break;
                    case nameof(SKRect.Bottom): bottom = reader.GetSingle(); break;
                    default: reader.Skip(); break;
                }
            }

            throw new JsonException("Unexpected end of JSON for SKRect.");
        }

        public override void Write(Utf8JsonWriter writer, SKRect value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber(nameof(SKRect.Left), value.Left);
            writer.WriteNumber(nameof(SKRect.Top), value.Top);
            writer.WriteNumber(nameof(SKRect.Right), value.Right);
            writer.WriteNumber(nameof(SKRect.Bottom), value.Bottom);
            writer.WriteEndObject();
        }
    }
}
