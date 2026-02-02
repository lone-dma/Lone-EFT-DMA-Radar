/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using LoneEftDmaRadar.Tarkov.World.Player;
using LoneEftDmaRadar.UI.Loot;

namespace LoneEftDmaRadar.Web.TarkovDev
{
    /// <summary>
    /// Class JSON Representation of Tarkov Market Data.
    /// </summary>
    public sealed class TarkovMarketItem
    {
        /// <summary>
        /// Item ID.
        /// </summary>
        [JsonPropertyName("bsgID")]
        public string BsgId { get; set; } = "NULL";
        /// <summary>
        /// Item Full Name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "NULL";
        /// <summary>
        /// Item Short Name.
        /// </summary>
        [JsonPropertyName("shortName")]
        public string ShortName { get; set; } = "NULL";
        /// <summary>
        /// Highest Vendor Price.
        /// </summary>
        [JsonPropertyName("price")]
        public long TraderPrice { get; set; }
        /// <summary>
        /// Optimal Flea Market Price.
        /// </summary>
        [JsonPropertyName("fleaPrice")]
        public long FleaPrice { get; set; }
        /// <summary>
        /// Number of slots taken up in the inventory.
        /// </summary>
        [JsonPropertyName("slots")]
        public int Slots { get; set; } = 1;
        [JsonPropertyName("categories")]
        public HashSet<string> Tags { get; set; } = new HashSet<string>();
        /// <summary>
        /// True if this item is Important via the Filters.
        /// </summary>
        [JsonIgnore]
        public bool Important => CustomFilter?.Important ?? false;
        /// <summary>
        /// Checks if an item is important via several means.
        /// </summary>
        [JsonIgnore]
        public bool IsImportant
        {
            get
            {
                if (Blacklisted)
                    return false;
                return Important || (Program.Config.Loot.ShowWishlist && IsWishlisted) || (Program.Config.QuestHelper.Enabled && IsQuestHelperItem);
            }
        }
        /// <summary>
        /// True if this item is Blacklisted via the Filters.
        /// </summary>
        [JsonIgnore]
        public bool Blacklisted => CustomFilter?.Blacklisted ?? false;
        /// <summary>
        /// Is a Medical Item.
        /// </summary>
        [JsonIgnore]
        public bool IsMed => Tags.Contains("Meds");
        /// <summary>
        /// Is a Food Item.
        /// </summary>
        [JsonIgnore]
        public bool IsFood => Tags.Contains("Food and drink");
        /// <summary>
        /// Is a backpack.
        /// </summary>
        [JsonIgnore]
        public bool IsBackpack => Tags.Contains("Backpack");
        /// <summary>
        /// Is a Weapon Item.
        /// </summary>
        [JsonIgnore]
        public bool IsWeapon => Tags.Contains("Weapon");
        /// <summary>
        /// Is Currency (Roubles,etc.)
        /// </summary>
        [JsonIgnore]
        public bool IsCurrency => Tags.Contains("Money");
        /// <summary>
        /// Is on the wishlist.
        /// </summary>
        [JsonIgnore]
        public bool IsWishlisted => LocalPlayer.WishlistItems.ContainsKey(BsgId);
        /// <summary>
        /// Is a quest helper tracked item.
        /// </summary>
        [JsonIgnore]
        public bool IsQuestHelperItem => Memory.QuestManager?.ItemConditions?.ContainsKey(BsgId) ?? false;

        /// <summary>
        /// This field is set if this item has a special filter.
        /// </summary>
        [JsonIgnore]
        public LootFilterEntry CustomFilter { get; private set; }

        /// <summary>
        /// Set the Custom Filter for this item.
        /// </summary>
        public void SetFilter(LootFilterEntry filter)
        {
            if (filter?.Enabled ?? false)
                CustomFilter = filter;
            else
                CustomFilter = null;
        }

        public override string ToString() => Name;
    }
}

