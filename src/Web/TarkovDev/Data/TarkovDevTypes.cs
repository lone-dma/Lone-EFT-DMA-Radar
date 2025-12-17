using LoneEftDmaRadar.Tarkov.GameWorld.Hazards;
using LoneEftDmaRadar.Tarkov.GameWorld.Quests;
using System.Collections.Frozen;
using static LoneEftDmaRadar.Web.TarkovDev.Data.TarkovDevTypes;
using static LoneEftDmaRadar.Web.TarkovDev.Data.TarkovDevTypes.TarkovDevDataQuery;

namespace LoneEftDmaRadar.Web.TarkovDev.Data
{
    public sealed class TarkovDevData
    {
        [JsonPropertyName("lootContainers")]
        [Obsolete("Raw Tarkov.Dev Data. Discarded after processing. Do not use.")]
        public List<BasicDataElement> TarkovDevContainers { get; set; }

        [JsonPropertyName("items")]
        [Obsolete("Raw Tarkov.Dev Data. Discarded after processing. Do not use.")]
        public List<ItemElement> TarkovDevItems { get; set; }

        [JsonPropertyName("items_clean")]
        public List<TarkovMarketItem> Items { get; set; }

        [JsonPropertyName("maps")]
        public List<MapElement> Maps { get; set; }

        [JsonPropertyName("playerLevels")]
        public List<PlayerLevelElement> PlayerLevels { get; set; }

        [JsonPropertyName("tasks")]
        public List<TaskElement> Tasks { get; set; }
    }

    public static class TarkovDevTypes
    {
        public sealed class TarkovDevDataQuery
        {
            [JsonPropertyName("warnings")]
            public List<WarningMessage> Warnings { get; set; }

            [JsonPropertyName("data")]
            public TarkovDevData Data { get; set; }

            public sealed class WarningMessage
            {
                [JsonPropertyName("message")]
                public string Message { get; set; }
            }

            public sealed class ItemElement
            {
                [JsonPropertyName("id")]
                public string Id { get; set; }

                [JsonPropertyName("name")]
                public string Name { get; set; }

                [JsonPropertyName("shortName")]
                public string ShortName { get; set; }

                [JsonPropertyName("width")]
                public int Width { get; set; }

                [JsonPropertyName("height")]
                public int Height { get; set; }

                [JsonPropertyName("basePrice")]
                public long BasePrice { get; set; }

                [JsonPropertyName("avg24hPrice")]
                public long? Avg24HPrice { get; set; }

                [JsonPropertyName("categories")]
                public List<CategoryElement> Categories { get; set; }

                [JsonPropertyName("sellFor")]
                public List<SellForElement> SellFor { get; set; }

                [JsonPropertyName("historicalPrices")]
                public List<HistoricalPrice> HistoricalPrices { get; set; }

                [JsonIgnore]
                public long HighestVendorPrice => SellFor?
                    .Where(x => x.Vendor.Name != "Flea Market" && x.PriceRub is long)?
                    .Select(x => x.PriceRub)?
                    .DefaultIfEmpty()?
                    .Max() ?? 0;

                [JsonIgnore]
                public long OptimalFleaPrice
                {
                    get
                    {
                        if (BasePrice == 0)
                            return 0;
                        if (Avg24HPrice is long avg24 && FleaTax.Calculate(avg24, BasePrice) < avg24)
                            return avg24;
                        return (long)(HistoricalPrices?
                            .Where(x => x.Price is long price && FleaTax.Calculate(price, BasePrice) < price)?
                            .Select(x => x.Price)?
                            .DefaultIfEmpty()?
                            .Average() ?? 0);
                    }
                }

                public sealed class HistoricalPrice
                {
                    [JsonPropertyName("price")]
                    public long? Price { get; set; }
                }

                public sealed class SellForElement
                {
                    [JsonPropertyName("priceRUB")]
                    public long? PriceRub { get; set; }

                    [JsonPropertyName("vendor")]
                    public CategoryElement Vendor { get; set; }
                }

                public sealed class CategoryElement
                {
                    [JsonPropertyName("name")]
                    public string Name { get; set; }
                }
            }
        }

        public sealed class BasicDataElement
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("normalizedName")]
            public string NormalizedName { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }
        }

        public partial class MapElement
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("nameId")]
            public string NameId { get; set; }

            [JsonPropertyName("extracts")]
            public List<ExtractElement> Extracts { get; set; } = new();

            [JsonPropertyName("transits")]
            public List<TransitElement> Transits { get; set; } = new();

            [JsonPropertyName("hazards")]
            public List<GenericWorldHazard> Hazards { get; set; } = new();
        }

        public partial class PlayerLevelElement
        {
            [JsonPropertyName("exp")]
            public int Exp { get; set; }

            [JsonPropertyName("level")]
            public int Level { get; set; }
        }

        public partial class ExtractElement
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("faction")]
            public string Faction { get; set; }

            [JsonPropertyName("position")]
            public Vector3 Position { get; set; }

            [JsonIgnore]
            public bool IsPmc => Faction?.Equals("pmc", StringComparison.OrdinalIgnoreCase) ?? false;
            [JsonIgnore]
            public bool IsShared => Faction?.Equals("shared", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public partial class TransitElement
        {
            [JsonPropertyName("description")]
            public string Description { get; set; }

            [JsonPropertyName("position")]
            public Vector3 Position { get; set; }
        }


        public partial class TaskElement
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("objectives")]
            public List<ObjectiveElement> Objectives { get; set; }

            public partial class ObjectiveElement
            {
                [JsonPropertyName("id")]
                public string Id { get; set; }

                [JsonPropertyName("type")]
#pragma warning disable IDE1006 // Naming Styles
                public string _type { get; set; }
#pragma warning restore IDE1006 // Naming Styles

                [JsonIgnore]
                private static readonly FrozenDictionary<string, QuestObjectiveType> _objectiveTypes =
                    new Dictionary<string, QuestObjectiveType>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["visit"] = QuestObjectiveType.Visit,
                        ["mark"] = QuestObjectiveType.Mark,
                        ["giveItem"] = QuestObjectiveType.GiveItem,
                        ["shoot"] = QuestObjectiveType.Shoot,
                        ["extract"] = QuestObjectiveType.Extract,
                        ["findQuestItem"] = QuestObjectiveType.FindQuestItem,
                        ["giveQuestItem"] = QuestObjectiveType.GiveQuestItem,
                        ["findItem"] = QuestObjectiveType.FindItem,
                        ["buildWeapon"] = QuestObjectiveType.BuildWeapon,
                        ["plantItem"] = QuestObjectiveType.PlantItem,
                        ["plantQuestItem"] = QuestObjectiveType.PlantQuestItem,
                        ["traderLevel"] = QuestObjectiveType.TraderLevel,
                        ["traderStanding"] = QuestObjectiveType.TraderStanding,
                        ["skill"] = QuestObjectiveType.Skill,
                        ["experience"] = QuestObjectiveType.Experience,
                        ["useItem"] = QuestObjectiveType.UseItem,
                        ["sellItem"] = QuestObjectiveType.SellItem,
                        ["taskStatus"] = QuestObjectiveType.TaskStatus,
                    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

                [JsonIgnore]
                public QuestObjectiveType Type =>
                    _objectiveTypes.TryGetValue(_type, out var type) ? type : QuestObjectiveType.Unknown;

                [JsonPropertyName("description")]
                public string Description { get; set; }

                [JsonPropertyName("requiredKeys")]
                public List<List<MarkerItemClass>> RequiredKeys { get; set; }

                [JsonPropertyName("maps")]
                public List<TaskMapElement> Maps { get; set; }

                [JsonPropertyName("zones")]
                public List<TaskZoneElement> Zones { get; set; }

                [JsonPropertyName("count")]
                public int Count { get; set; }

                [JsonPropertyName("foundInRaid")]
                public bool FoundInRaid { get; set; }

                [JsonPropertyName("item")]
                public MarkerItemClass Item { get; set; }

                [JsonPropertyName("questItem")]
                public ObjectiveQuestItem QuestItem { get; set; }

                [JsonPropertyName("markerItem")]
                public MarkerItemClass MarkerItem { get; set; }

                public class MarkerItemClass
                {
                    [JsonPropertyName("id")]
                    public string Id { get; set; }

                    [JsonPropertyName("name")]
                    public string Name { get; set; }

                    [JsonPropertyName("shortName")]
                    public string ShortName { get; set; }
                }

                public class ObjectiveQuestItem
                {
                    [JsonPropertyName("id")]
                    public string Id { get; set; }

                    [JsonPropertyName("name")]
                    public string Name { get; set; }

                    [JsonPropertyName("shortName")]
                    public string ShortName { get; set; }

                    [JsonPropertyName("normalizedName")]
                    public string NormalizedName { get; set; }

                    [JsonPropertyName("description")]
                    public string Description { get; set; }
                }

                public class TaskZoneElement
                {
                    [JsonPropertyName("id")]
                    public string Id { get; set; }

                    [JsonPropertyName("position")]
                    public Vector3 Position { get; set; }

                    [JsonPropertyName("map")]
                    public TaskMapElement Map { get; set; }
                }

                public class TaskMapElement
                {
                    [JsonPropertyName("nameId")]
                    public string NameId { get; set; }

                    [JsonPropertyName("normalizedName")]
                    public string NormalizedName { get; set; }

                    [JsonPropertyName("name")]
                    public string Name { get; set; }
                }
            }
        }
    }
}
