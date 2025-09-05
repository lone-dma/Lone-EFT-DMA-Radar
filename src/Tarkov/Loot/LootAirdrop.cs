namespace EftDmaRadarLite.Tarkov.Loot
{
    public sealed class LootAirdrop : LootContainer
    {
        private static readonly IReadOnlyList<LootItem> _default = new List<LootItem>(1);
        public override string Name { get; } = "Airdrop";
        /// <summary>
        /// Constructor.
        /// </summary>
        public LootAirdrop() : base(_default)
        {
        }
    }
}