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

    /// <summary>
    /// Equipment slots, the docs' ten. An item's own slot is its kind; <see cref="Ring2"/> and <see cref="OffHand"/> are
    /// only places to wear things, never an item's kind. Bows only since 2026-09-30 (Docs/03): <see cref="Weapon"/> is the
    /// short bow, <see cref="TwoHandWeapon"/> the longbow (both worn in the weapon place) and <see cref="Shield"/> the
    /// quiver (worn in the off-hand, beside either bow). The names stay from the Wrathborn's axes and shields because
    /// saves store them. New values go at the end: the order is the order worn items are saved in.
    /// </summary>
    public enum ItemSlot
    {
        Weapon,
        Chest,
        Helm,
        Gloves,
        Boots,
        Belt,
        Amulet,
        Ring,
        Ring2,
        OffHand,
        Shield,
        TwoHandWeapon,
    }

    /// <summary>
    /// The bows' numbers (Docs/03, the Wild Arrow's slots, proposed 2026-09-30, tuning): a short bow reaches 7.5; a
    /// longbow hits 1.3 times as hard at 0.8 times the speed (1.04 times the damage per second) and reaches 9, 1.5
    /// further; a quiver adds 5 percent attack speed, 2 more per look tier (9 at the top).
    /// </summary>
    public static class GripRules
    {
        public const float ShortBowReach = 7.5f;
        public const float LongbowDamageFactor = 1.3f;
        public const float LongbowAttackSpeedFactor = 0.8f;
        public const float LongbowReach = 1.5f;
        public const float QuiverAttackSpeedPercent = 5f;
        public const float QuiverAttackSpeedPerTier = 2f;
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

        /// <param name="reforges">How many times the Forge has reforged it, which raises the next reforge's cost.</param>
        /// <param name="tempers">How many times it has been tempered, at most <see cref="ForgeRules.MaxTempers"/>.</param>
        public Item(ItemSlot slot, ItemRarity rarity, int itemLevel, AffixRoll[] affixes = null, int reforges = 0, int tempers = 0)
        {
            Slot = slot;
            Rarity = rarity;
            ItemLevel = itemLevel < 1 ? 1 : itemLevel;
            Affixes = affixes ?? NoAffixes;
            Reforges = reforges < 0 ? 0 : reforges;
            Tempers = tempers < 0 ? 0 : tempers;
        }

        public ItemSlot Slot { get; }

        public ItemRarity Rarity { get; }

        public int ItemLevel { get; }

        /// <summary>The affixes rolled on this item. Empty for Common items.</summary>
        public System.Collections.Generic.IReadOnlyList<AffixRoll> Affixes { get; }

        /// <summary>Times reforged at the Forge (<see cref="ForgeRules"/>). Saved with the item.</summary>
        public int Reforges { get; }

        /// <summary>Times tempered at the Forge (<see cref="ForgeRules"/>). Saved with the item.</summary>
        public int Tempers { get; }

        /// <summary>Average weapon damage, from the curve in Docs/03-itemization.md. Zero for items that are not weapons.</summary>
        public float WeaponAverageDamage =>
            Slot == ItemSlot.Weapon ? CombatFormulas.WeaponAverageDamage(ItemLevel)
            : Slot == ItemSlot.TwoHandWeapon ? CombatFormulas.WeaponAverageDamage(ItemLevel) * GripRules.LongbowDamageFactor
            : 0f;

        public bool IsWeapon => Slot == ItemSlot.Weapon || Slot == ItemSlot.TwoHandWeapon;

        /// <summary>A quiver's attack speed in percent, by its look tier; zero for anything else.</summary>
        public float QuiverAttackSpeedPercent => Slot == ItemSlot.Shield
            ? GripRules.QuiverAttackSpeedPercent + GripRules.QuiverAttackSpeedPerTier * AppearanceRules.Tier(ItemLevel)
            : 0f;

        /// <summary>Base armor this piece grants from its item level alone, before affixes. Zero for slots that are
        /// not armor.</summary>
        public float ArmorValue => CombatFormulas.BaseArmorPerPiece(ItemLevel) * ArmorShare(Slot);

        /// <summary>How much of the armor curve a slot's base gives (Docs/03's proposed table: chest and helm in full,
        /// gloves and boots 60 percent, a belt 40; jewellery and quivers none).</summary>
        public static float ArmorShare(ItemSlot slot) =>
            slot == ItemSlot.Chest || slot == ItemSlot.Helm ? 1f
            : slot == ItemSlot.Gloves || slot == ItemSlot.Boots ? 0.6f
            : slot == ItemSlot.Belt ? 0.4f : 0f;

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
        public float MovementSpeedPercent => AffixSum(AffixId.MovementSpeed);
        public float DodgePercent => AffixSum(AffixId.DodgeChance);
        public float ExtraArrowPercent => AffixSum(AffixId.ExtraArrowChance);

        public override string ToString() => $"{Rarity} {Slot} (item level {ItemLevel}, {Affixes.Count} affixes)";
    }
}
