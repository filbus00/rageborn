namespace ARPG
{
    /// <summary>
    /// What the merchant in town pays for gear (the owner, 2026-10-02: a merchant who buys unwanted gear, so every drop
    /// is worth something). A price grows with rarity and item level; a named legendary is worth a little more. Selling
    /// all of a rarity at once leaves out upgrades and named legendaries, so a bulk sale never loses something the player
    /// would want. First guesses, measured against gold from kills (<see cref="LootRoller.RollGold"/>: about 4 a normal
    /// kill at level 5, 30 at level 20): a Magic item is worth two or three kills. Pure.
    /// </summary>
    public static class SellRules
    {
        /// <summary>Gold per rarity before the item level, Common to Legendary.</summary>
        public const int CommonBase = 2, MagicBase = 6, RareBase = 15, LegendaryBase = 40;

        /// <summary>Each item level adds this fraction of the base.</summary>
        public const float PerItemLevel = 0.15f;

        /// <summary>A named legendary (Docs/03) is worth this much more than its rarity alone.</summary>
        public const float NamedLegendaryMultiplier = 1.5f;

        /// <summary>The rarities offered for a sale of all at once. Legendaries are sold one by one.</summary>
        public static readonly ItemRarity[] BulkRarities = { ItemRarity.Common, ItemRarity.Magic, ItemRarity.Rare };

        public static int Price(Item item)
        {
            if (item == null)
                return 0;
            float price = Base(item.Rarity) * (1f + PerItemLevel * System.Math.Max(1, item.ItemLevel));
            if (item.Legendary != LegendaryId.None)
                price *= NamedLegendaryMultiplier;
            return System.Math.Max(1, (int)System.Math.Round(price));
        }

        /// <summary>Whether a sale of all of a rarity takes this item: that rarity, not an upgrade, not a named legendary.</summary>
        public static bool InBulkSale(Item item, ItemRarity rarity, bool isUpgrade) =>
            item != null && item.Rarity == rarity && !isUpgrade && item.Legendary == LegendaryId.None;

        static int Base(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return CommonBase;
                case ItemRarity.Magic: return MagicBase;
                case ItemRarity.Rare: return RareBase;
                default: return LegendaryBase;
            }
        }
    }
}
