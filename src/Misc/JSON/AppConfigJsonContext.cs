/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
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

