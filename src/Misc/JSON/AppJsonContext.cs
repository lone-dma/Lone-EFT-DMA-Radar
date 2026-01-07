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

using LoneEftDmaRadar.Tarkov.World.Hazards;
using LoneEftDmaRadar.Tarkov.World.Player;
using LoneEftDmaRadar.UI.ColorPicker;
using LoneEftDmaRadar.UI.Loot;
using LoneEftDmaRadar.UI.Maps;
using LoneEftDmaRadar.Web.TarkovDev;
using VmmSharpEx.Extensions.Input;

namespace LoneEftDmaRadar.Misc.JSON
{
    /// <summary>
    /// AOT-compatible JSON serializer context for the application's configuration types.
    /// </summary>
    [JsonSourceGenerationOptions(
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = [
            typeof(Vector2JsonConverter),
            typeof(Vector3JsonConverter),
            typeof(ColorDictionaryConverter),
            typeof(SKRectJsonConverter)
        ])]
    // Main config
    [JsonSerializable(typeof(EftDmaConfig))]
    // Sub-configs
    [JsonSerializable(typeof(DMAConfig))]
    [JsonSerializable(typeof(UIConfig))]
    [JsonSerializable(typeof(WebRadarConfig))]
    [JsonSerializable(typeof(LootConfig))]
    [JsonSerializable(typeof(ContainersConfig))]
    [JsonSerializable(typeof(LootFilterConfig))]
    [JsonSerializable(typeof(AimviewWidgetConfig))]
    [JsonSerializable(typeof(QuestHelperConfig))]
    [JsonSerializable(typeof(PersistentCache))]
    [JsonSerializable(typeof(RaidCache))]
    [JsonSerializable(typeof(MiscConfig))]
    // Player Types
    [JsonSerializable(typeof(AIRole))]
    // Loot filter types
    [JsonSerializable(typeof(UserLootFilter))]
    [JsonSerializable(typeof(LootFilterEntry))]
    [JsonSerializable(typeof(List<LootFilterEntry>))]
    // Enums
    [JsonSerializable(typeof(LootFilterEntryType))]
    [JsonSerializable(typeof(LootPriceMode))]
    [JsonSerializable(typeof(FpgaAlgo))]
    [JsonSerializable(typeof(ColorPickerOption))]
    [JsonSerializable(typeof(Win32VirtualKey))]
    // Dictionary types
    [JsonSerializable(typeof(ConcurrentDictionary<Win32VirtualKey, string>))]
    [JsonSerializable(typeof(ConcurrentDictionary<ColorPickerOption, string>))]
    [JsonSerializable(typeof(ConcurrentDictionary<string, byte>))]
    [JsonSerializable(typeof(ConcurrentDictionary<string, UserLootFilter>))]
    [JsonSerializable(typeof(ConcurrentDictionary<ulong, RaidCache>))]
    [JsonSerializable(typeof(ConcurrentDictionary<int, int>))]
    [JsonSerializable(typeof(ConcurrentDictionary<int, AIRole>))]
    [JsonSerializable(typeof(ConcurrentDictionary<int, byte>))]
    // SkiaSharp types
    [JsonSerializable(typeof(SKSize))]
    [JsonSerializable(typeof(SKRect))]
    // System.Numerics types
    [JsonSerializable(typeof(Vector2))]
    [JsonSerializable(typeof(Vector3))]
    // Collection types
    [JsonSerializable(typeof(HashSet<string>))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    // Primitive types (for converters)
    [JsonSerializable(typeof(byte))]
    // Map config types
    [JsonSerializable(typeof(EftMapConfig))]
    [JsonSerializable(typeof(EftMapConfig.Layer))]
    [JsonSerializable(typeof(List<EftMapConfig.Layer>))]
    [JsonSerializable(typeof(List<string>))]
    // TarkovDev API types
    [JsonSerializable(typeof(TarkovDevTypes.ApiResponse))]
    [JsonSerializable(typeof(TarkovDevTypes.DataElement))]
    [JsonSerializable(typeof(TarkovDevTypes.MessageElement))]
    [JsonSerializable(typeof(TarkovDevTypes.ItemElement))]
    [JsonSerializable(typeof(TarkovDevTypes.ItemElement.HistoricalPrice))]
    [JsonSerializable(typeof(TarkovDevTypes.ItemElement.SellForElement))]
    [JsonSerializable(typeof(TarkovDevTypes.ItemElement.CategoryElement))]
    [JsonSerializable(typeof(TarkovDevTypes.BasicDataElement))]
    [JsonSerializable(typeof(TarkovDevTypes.MapElement))]
    [JsonSerializable(typeof(TarkovDevTypes.ExtractElement))]
    [JsonSerializable(typeof(TarkovDevTypes.TransitElement))]
    [JsonSerializable(typeof(TarkovDevTypes.TaskElement))]
    [JsonSerializable(typeof(TarkovDevTypes.TaskElement.ObjectiveElement))]
    [JsonSerializable(typeof(TarkovDevTypes.TaskElement.ObjectiveElement.MarkerItemClass))]
    [JsonSerializable(typeof(TarkovDevTypes.TaskElement.ObjectiveElement.ObjectiveQuestItem))]
    [JsonSerializable(typeof(TarkovDevTypes.TaskElement.ObjectiveElement.TaskZoneElement))]
    [JsonSerializable(typeof(TarkovDevTypes.TaskElement.ObjectiveElement.TaskMapElement))]
    [JsonSerializable(typeof(TarkovMarketItem))]
    [JsonSerializable(typeof(GenericWorldHazard))]
    // TarkovDev list types
    [JsonSerializable(typeof(List<TarkovDevTypes.MessageElement>))]
    [JsonSerializable(typeof(List<TarkovDevTypes.ItemElement>))]
    [JsonSerializable(typeof(List<TarkovDevTypes.ItemElement.HistoricalPrice>))]
    [JsonSerializable(typeof(List<TarkovDevTypes.ItemElement.SellForElement>))]
    [JsonSerializable(typeof(List<TarkovDevTypes.ItemElement.CategoryElement>))]
    [JsonSerializable(typeof(List<TarkovDevTypes.BasicDataElement>))]
    [JsonSerializable(typeof(List<TarkovDevTypes.MapElement>))]
    [JsonSerializable(typeof(List<TarkovDevTypes.ExtractElement>))]
    [JsonSerializable(typeof(List<TarkovDevTypes.TransitElement>))]
    [JsonSerializable(typeof(List<TarkovDevTypes.TaskElement>))]
    [JsonSerializable(typeof(List<TarkovDevTypes.TaskElement.ObjectiveElement>))]
    [JsonSerializable(typeof(List<TarkovDevTypes.TaskElement.ObjectiveElement.MarkerItemClass>))]
    [JsonSerializable(typeof(List<List<TarkovDevTypes.TaskElement.ObjectiveElement.MarkerItemClass>>))]
    [JsonSerializable(typeof(List<TarkovDevTypes.TaskElement.ObjectiveElement.TaskZoneElement>))]
    [JsonSerializable(typeof(List<TarkovDevTypes.TaskElement.ObjectiveElement.TaskMapElement>))]
    [JsonSerializable(typeof(List<TarkovMarketItem>))]
    [JsonSerializable(typeof(List<GenericWorldHazard>))]
    // Misc
    [JsonSerializable(typeof(JsonDocument))]
    public partial class AppJsonContext : JsonSerializerContext
    {
    }
}
