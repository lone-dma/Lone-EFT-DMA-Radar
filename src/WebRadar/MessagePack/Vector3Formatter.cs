using MessagePack;
using MessagePack.Formatters;

namespace EftDmaRadarLite.WebRadar.MessagePack
{
    public class Vector3Formatter : IMessagePackFormatter<Vector3>
    {
        public Vector3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            options.Security.DepthStep(ref reader);

            ArgumentOutOfRangeException.ThrowIfNotEqual(reader.ReadArrayHeader(), 3, nameof(reader));
            Vector3 result;
            result.X = reader.ReadSingle();
            result.Y = reader.ReadSingle();
            result.Z = reader.ReadSingle();

            reader.Depth--;
            return result;
        }

        public void Serialize(ref MessagePackWriter writer, Vector3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
        }
    }
}
