namespace ARPG
{
    /// <summary>Item rarity from Docs/03-itemization.md. Cursed is an endgame variant and comes later.</summary>
    public enum ItemRarity
    {
        Common,
        Magic,
        Rare,
        Legendary,
    }

    /// <summary>Equipment slots. Docs list ten; a slot is added when items for it exist.</summary>
    public enum ItemSlot
    {
        Weapon,
    }

    /// <summary>
    /// One item. It has a slot, a rarity and an item level, which equals the level of the zone it dropped in. Affixes
    /// come with the itemization step, so for now a weapon's damage depends on its item level alone.
    /// Items are compared by reference: two drops of the same kind are different items.
    /// </summary>
    public sealed class Item
    {
        public Item(ItemSlot slot, ItemRarity rarity, int itemLevel)
        {
            Slot = slot;
            Rarity = rarity;
            ItemLevel = itemLevel < 1 ? 1 : itemLevel;
        }

        public ItemSlot Slot { get; }

        public ItemRarity Rarity { get; }

        public int ItemLevel { get; }

        /// <summary>Average weapon damage, from the curve in Docs/03-itemization.md. Zero for items that are not weapons.</summary>
        public float WeaponAverageDamage => Slot == ItemSlot.Weapon ? CombatFormulas.WeaponAverageDamage(ItemLevel) : 0f;

        public override string ToString() => $"{Rarity} {Slot} (item level {ItemLevel})";
    }
}
