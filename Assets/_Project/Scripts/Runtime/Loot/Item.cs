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
        Chest,
        Helm,
    }

    /// <summary>
    /// One item. It has a slot, a rarity, an item level (equal to the level of the zone it dropped in) and the
    /// affixes rolled for it (empty for Common, which is base stat only). A weapon's damage and an armor piece's
    /// protection come from item level alone; affixes add on top. Items are compared by reference: two drops of the
    /// same kind are different items.
    /// </summary>
    public sealed class Item
    {
        static readonly AffixRoll[] NoAffixes = System.Array.Empty<AffixRoll>();

        public Item(ItemSlot slot, ItemRarity rarity, int itemLevel, AffixRoll[] affixes = null)
        {
            Slot = slot;
            Rarity = rarity;
            ItemLevel = itemLevel < 1 ? 1 : itemLevel;
            Affixes = affixes ?? NoAffixes;
        }

        public ItemSlot Slot { get; }

        public ItemRarity Rarity { get; }

        public int ItemLevel { get; }

        /// <summary>The affixes rolled on this item. Empty for Common items.</summary>
        public System.Collections.Generic.IReadOnlyList<AffixRoll> Affixes { get; }

        /// <summary>Average weapon damage, from the curve in Docs/03-itemization.md. Zero for items that are not weapons.</summary>
        public float WeaponAverageDamage => Slot == ItemSlot.Weapon ? CombatFormulas.WeaponAverageDamage(ItemLevel) : 0f;

        /// <summary>Base armor this piece grants from its item level alone, before affixes. Zero for slots that are
        /// not armor.</summary>
        public float ArmorValue => Slot == ItemSlot.Chest || Slot == ItemSlot.Helm ? CombatFormulas.BaseArmorPerPiece(ItemLevel) : 0f;

        /// <summary>Sum of every rolled affix matching this id. Zero when the item has none.</summary>
        public float AffixSum(AffixId id)
        {
            var total = 0f;
            for (var i = 0; i < Affixes.Count; i++)
                if (Affixes[i].Id == id)
                    total += Affixes[i].Value;
            return total;
        }

        public float FlatWeaponDamageBonus => AffixSum(AffixId.FlatWeaponDamage);
        public float IncreasedDamagePercent => AffixSum(AffixId.IncreasedDamage);
        public float LifeBonus => AffixSum(AffixId.Life);
        public float ArmorAffixBonus => AffixSum(AffixId.Armor);
        public float AttackSpeedPercent => AffixSum(AffixId.AttackSpeed);
        public float CriticalChancePercent => AffixSum(AffixId.CriticalChance);
        public float CriticalDamagePercent => AffixSum(AffixId.CriticalDamage);
        public float LifeOnHit => AffixSum(AffixId.LifeOnHit);
        public float CooldownReductionPercent => AffixSum(AffixId.CooldownReduction);

        public override string ToString() => $"{Rarity} {Slot} (item level {ItemLevel}, {Affixes.Count} affixes)";
    }
}
