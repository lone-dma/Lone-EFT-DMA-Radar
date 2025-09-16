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

namespace EftDmaRadarLite.Tarkov.Data.TarkovMarket
{
    public partial class TarkovDevQuery
    {
        [JsonPropertyName("warnings")]
        public List<WarningMessage> Warnings { get; set; }

        [JsonPropertyName("data")]
        public TarkovDevData Data { get; set; }

        public class TarkovDevData
        {
            [JsonPropertyName("questItems")]
            public List<QuestItemElement> QuestItems { get; set; }

            [JsonPropertyName("lootContainers")]
            public List<BasicDataElement> LootContainers { get; set; }

            [JsonPropertyName("tasks")]
            public List<TaskElement> Tasks { get; set; }

            [JsonPropertyName("items")]
            public List<ItemElement> Items { get; set; }
        }
        public class WarningMessage
        {
            [JsonPropertyName("message")]
            public string Message { get; set; }
        }
    }

    public partial class ItemElement
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

        public class HistoricalPrice
        {
            [JsonPropertyName("price")]
            public long? Price { get; set; }
        }

        public class SellForElement
        {
            [JsonPropertyName("priceRUB")]
            public long? PriceRub { get; set; }

            [JsonPropertyName("vendor")]
            public CategoryElement Vendor { get; set; }
        }

        public class CategoryElement
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
        }
    }

    public class BasicDataElement
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("normalizedName")]
        public string NormalizedName { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class QuestItemElement
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("shortName")]
        public string ShortName { get; set; }
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
            public string Type { get; set; }

            [JsonPropertyName("description")]
            public string Description { get; set; }

            [JsonPropertyName("requiredKeys")]
            public List<List<MarkerItemClass>> RequiredKeys { get; set; }

            [JsonPropertyName("maps")]
            public List<BasicDataElement> Maps { get; set; }

            [JsonPropertyName("zones")]
            public List<ZoneElement> Zones { get; set; }

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

            public class ZoneElement
            {
                [JsonPropertyName("id")]
                public string Id { get; set; }

                [JsonPropertyName("position")]
                public PositionElement Position { get; set; }

                [JsonPropertyName("map")]
                public BasicDataElement Map { get; set; }
            }

            public class PositionElement
            {
                [JsonPropertyName("y")]
                public float Y { get; set; }

                [JsonPropertyName("x")]
                public float X { get; set; }

                [JsonPropertyName("z")]
                public float Z { get; set; }
            }
        }
    }
}
