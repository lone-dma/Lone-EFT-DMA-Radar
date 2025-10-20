namespace LoneEftDmaRadar.Tarkov.Loot
{
    internal static class LootExtensions
    {
        /// <summary>
        /// Order loot (important first, then by price).
        /// </summary>
        /// <param name="loot"></param>
        /// <returns>Ordered loot.</returns>
        public static IEnumerable<LootItem> OrderLoot(this IEnumerable<LootItem> loot)
        {
            return loot
                .OrderByDescending(x => x.IsImportant || (App.Config.QuestHelper.Enabled && x.IsQuestCondition))
                .ThenByDescending(x => x.Price);
        }
    }
}
