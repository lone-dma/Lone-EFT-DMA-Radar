using Collections.Pooled;
using EftDmaRadarLite.Tarkov.Loot;
using EftDmaRadarLite.UI.Radar;

namespace EftDmaRadarLite.UI.Loot
{
    /// <summary>
    /// Enumerable Loot Filter Class.
    /// </summary>
    internal static class LootFilter
    {
        public static string SearchString;
        public static bool ShowMeds;
        public static bool ShowFood;
        public static bool ShowBackpacks;

        private static bool ShowQuestItems => App.Config.QuestHelper.Enabled;

        /// <summary>
        /// Creates a loot filter based on current Loot Filter settings.
        /// </summary>
        /// <returns>Loot Filter Predicate.</returns>
        public static Predicate<LootItem> Create()
        {
            var search = SearchString?.Trim();
            bool usePrices = string.IsNullOrEmpty(search);
            if (usePrices)
            {
                Predicate<LootItem> p = x => // Default Predicate
                {
                    return (x.IsRegularLoot || x.IsValuableLoot || x.IsImportant || x.IsWishlisted) ||
                                (ShowQuestItems && x.IsQuestCondition) ||
                                (ShowBackpacks && x.IsBackpack) ||
                                (ShowMeds && x.IsMeds) ||
                                (ShowFood && x.IsFood);
                };
                return item =>
                {
                    if (item is LootAirdrop)
                    {
                        return true;
                    }
                    if (item is LootCorpse)
                    {
                        return true;
                    }
                    if (p(item))
                    {
                        if (item is LootContainer container)
                        {
                            container.SetFilter(p);
                        }
                        return true;
                    }
                    return false;
                };
            }
            else // Loot Search
            {
                using var names = search!.Split(',').Select(a => a.Trim()).ToPooledList();
                Predicate<LootItem> p = x => // Search Predicate
                {
                    return names.Any(a => x.Name.Contains(a, StringComparison.OrdinalIgnoreCase));
                };
                return item =>
                {
                    if (item is LootAirdrop)
                    {
                        return true;
                    }
                    if (item.ContainsSearchPredicate(p))
                    {
                        if (item is LootContainer container)
                        {
                            container.SetFilter(p);
                        }
                        return true;
                    }
                    return false;
                };
            }
        }
    }
}