using eft_dma_radar.Tarkov.Data.TarkovMarket;

namespace eft_dma_radar.Tarkov.Loot
{
    /// <summary>
    /// Defines a static/dynamic spawn Quest Item (not normal loot).
    /// </summary>
    public sealed class QuestItem : LootItem
    {
        public QuestItem(TarkovMarketItem entry) : base(entry)
        {
        }

        public QuestItem(string id, string name) : base(id, name)
        {
        }
    }
}
