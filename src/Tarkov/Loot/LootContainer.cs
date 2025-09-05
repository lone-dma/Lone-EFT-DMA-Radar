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
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Name of container (example: AIRDROP).</param>
        public LootContainer(IReadOnlyList<LootItem> loot) : base(_defaultItem)
        {
            ArgumentNullException.ThrowIfNull(loot, nameof(loot));
            Loot = loot;
        }

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
        public IReadOnlyList<LootItem> Loot { get; }

        /// <summary>
        /// All Items inside this container that pass the current Loot Filter.
        /// Ordered by Important/Price Value.
        /// </summary>
        public IEnumerable<LootItem> FilteredLoot => Loot
            .Where(x => _filter(x))
            .OrderLoot();
    }
}