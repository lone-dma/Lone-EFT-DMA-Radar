using LoneEftDmaRadar.UI.ColorPicker;

namespace LoneEftDmaRadar.Misc.JSON
{
    [JsonSourceGenerationOptions(
    WriteIndented = true,
    Converters = [
        typeof(Vector2JsonConverter),
            typeof(Vector3JsonConverter),
            typeof(ColorDictionaryConverter),
            typeof(SKRectJsonConverter)
    ])]
    [JsonSerializable(typeof(EftDmaConfig))]
    public partial class AppConfigJsonContext : JsonSerializerContext
    {
    }
}
