namespace LoneEftDmaRadar.Misc.JSON
{
    public sealed class Vector3JsonConverter : JsonConverter<Vector3>
    {
        public static Vector3JsonConverter Instance { get; } = new Vector3JsonConverter();

        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected StartObject token");

            float x = 0, y = 0, z = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new Vector3(x, y, z);

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected PropertyName token");

                string propertyName = reader.GetString();
                reader.Read();

                switch (propertyName?.ToLowerInvariant())
                {
                    case "x":
                        x = reader.GetSingle();
                        break;
                    case "y":
                        y = reader.GetSingle();
                        break;
                    case "z":
                        z = reader.GetSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            throw new JsonException("Unexpected end of JSON");
        }

        public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.X);
            writer.WriteNumber("y", value.Y);
            writer.WriteNumber("z", value.Z);
            writer.WriteEndObject();
        }
    }
}
