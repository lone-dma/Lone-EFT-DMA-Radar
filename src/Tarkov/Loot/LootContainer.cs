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

using EftDmaRadarLite.Misc;
using EftDmaRadarLite.Tarkov.Data.TarkovMarket;

namespace EftDmaRadarLite.Tarkov.Loot
{
    public class LootContainer : LootItem
    {
        private static readonly TarkovMarketItem _defaultItem = new();
        private static readonly Predicate<LootItem> _pTrue = x => { return true; };
        private Predicate<LootItem> _filter = _pTrue;

        public override string Name
        {
            get
            {
                var items = FilteredLoot;
                if (items is not null && items.Count() == 1)
                    return items.First().Name ?? "Loot";
                return "Loot";
            }
        }

        protected LootContainer() : base(_defaultItem) { }

        /// <summary>
        /// Update the filter for this container.
        /// </summary>
        /// <param name="filter">New filter to be set.</param>
        public void SetFilter(Predicate<LootItem> filter)
        {
            ArgumentNullException.ThrowIfNull(filter, nameof(filter));
            _filter = filter;
        }

        /// <summary>
        /// All items inside this Container (unfiltered/unordered).
        /// </summary>
        public ConcurrentDictionary<ulong, LootItem> Loot { get; } = new();

        /// <summary>
        /// All Items inside this container that pass the current Loot Filter.
        /// Ordered by Important/Price Value.
        /// </summary>
        public IEnumerable<LootItem> FilteredLoot => Loot.Values
            .Where(x => _filter(x))
            .OrderLoot();
    }
}