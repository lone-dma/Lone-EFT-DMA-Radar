/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using LoneEftDmaRadar.Misc.JSON;

namespace LoneEftDmaRadar.Web.WebRadar.Data
{
    /// <summary>
    /// AOT-compatible JSON serializer context for Web Radar types.
    /// </summary>
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = [typeof(Vector2JsonConverter), typeof(Vector3JsonConverter)])]
    [JsonSerializable(typeof(WebRadarUpdate))]
    [JsonSerializable(typeof(WebRadarPlayer))]
    [JsonSerializable(typeof(WebRadarPlayer[]))]
    [JsonSerializable(typeof(WebPlayerType))]
    [JsonSerializable(typeof(Vector2))]
    [JsonSerializable(typeof(Vector3))]
    public partial class WebRadarJsonContext : JsonSerializerContext
    {
    }
}

