/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using LoneEftDmaRadar.Tarkov.World.Loot;

namespace LoneEftDmaRadar.UI.Loot
{
    /// <summary>
    /// Enumerable FilteredLoot Filter Class.
    /// </summary>
    internal static class LootFilter
    {
        public static string SearchString;
        public static bool ShowMeds;
        public static bool ShowFood;
        public static bool ShowBackpacks;
        public static bool ShowQuestItems;

        /// <summary>
        /// Creates a loot filter based on current FilteredLoot Filter settings.
        /// </summary>
        /// <returns>FilteredLoot Filter Predicate.</returns>
        public static Predicate<LootItem> Create()
        {
            var search = SearchString?.Trim();
            bool usePrices = string.IsNullOrEmpty(search);
            if (usePrices)
            {
                Predicate<LootItem> p = item => // Default Predicate
                {
                    if (Program.Config.QuestHelper.Enabled && item.IsQuestHelperItem)
                        return true;
                    if (item is LootAirdrop)
                        return true;
                    if (!Program.Config.Loot.HideCorpses && item is LootCorpse)
                        return true;
                    return (item.IsRegularLoot || item.IsValuableLoot || item.IsImportant || (Program.Config.Loot.ShowWishlist && item.IsWishlisted)) ||
                                (ShowBackpacks && item.IsBackpack) ||
                                (ShowMeds && item.IsMeds) ||
                                (ShowFood && item.IsFood) ||
                                (ShowQuestItems && item.IsQuestItem);
                };
                return item =>
                {
                    return p(item);
                };
            }
            else // FilteredLoot Search
            {
                var names = search!.Split(',').Select(a => a.Trim()).ToList(); // Pooled wasnt working well here
                Predicate<LootItem> p = item => // Search Predicate
                {
                    if (item is LootAirdrop)
                        return true;
                    return names.Any(a => item.Name.Contains(a, StringComparison.OrdinalIgnoreCase));
                };
                return item =>
                {
                    return p(item);
                };
            }
        }
    }
}
