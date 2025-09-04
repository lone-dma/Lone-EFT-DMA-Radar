using MessagePack;
using MessagePack.Formatters;

namespace eft_dma_radar.WebRadar.MessagePack
{
    public class Vector2Formatter : IMessagePackFormatter<Vector2>
    {
        public Vector2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            options.Security.DepthStep(ref reader);

            ArgumentOutOfRangeException.ThrowIfNotEqual(reader.ReadArrayHeader(), 2, nameof(reader));
            Vector2 result;
            result.X = reader.ReadSingle();
            result.Y = reader.ReadSingle();

            reader.Depth--;
            return result;
        }

        public void Serialize(ref MessagePackWriter writer, Vector2 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.X);
            writer.Write(value.Y);
        }
    }
}
