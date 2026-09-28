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
    /// only places to wear things, never an item's kind: a second <see cref="Ring"/>, and in the off-hand a
    /// <see cref="Shield"/> or a one-handed <see cref="Weapon"/> (dual wield). A <see cref="TwoHandWeapon"/> is worn in
    /// the weapon place and empties the off-hand (Docs/03, the grips). New values go at the end: saves store names, but
    /// the order is the order worn items are saved in.
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
    /// The grips' numbers (Docs/03, decided Q10, the numbers tuning): dual wield swings alternate hands, each with its own
    /// weapon's damage, at 15 percent more attack speed; a shield blocks 12 percent of melee hits and projectiles, 4 more
    /// per look tier (20 at the top), capped at 50; a two-hander hits 1.6 times as hard at 0.85 times the speed (1.36 times
    /// the damage per second, paying for the empty off-hand) and reaches 0.3 further.
    /// </summary>
    public static class GripRules
    {
        public const float DualWieldAttackSpeed = 0.15f;
        public const float TwoHandDamageFactor = 1.6f;
        public const float TwoHandAttackSpeedFactor = 0.85f;
        public const float TwoHandReach = 0.3f;
        public const float BaseBlockPercent = 12f;
        public const float BlockPercentPerTier = 4f;
        public const float MaxBlock = 0.5f;
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
            : Slot == ItemSlot.TwoHandWeapon ? CombatFormulas.WeaponAverageDamage(ItemLevel) * GripRules.TwoHandDamageFactor
            : 0f;

        public bool IsWeapon => Slot == ItemSlot.Weapon || Slot == ItemSlot.TwoHandWeapon;

        /// <summary>A shield's block chance in percent, by its look tier; zero for anything else.</summary>
        public float BlockPercent => Slot == ItemSlot.Shield
            ? GripRules.BaseBlockPercent + GripRules.BlockPercentPerTier * AppearanceRules.Tier(ItemLevel)
            : 0f;

        /// <summary>Base armor this piece grants from its item level alone, before affixes. Zero for slots that are
        /// not armor.</summary>
        public float ArmorValue => CombatFormulas.BaseArmorPerPiece(ItemLevel) * ArmorShare(Slot);

        /// <summary>How much of the armor curve a slot's base gives (Docs/03's proposed table: chest and helm in full,
        /// gloves and boots 60 percent, a belt 40; jewellery none).</summary>
        public static float ArmorShare(ItemSlot slot) =>
            slot == ItemSlot.Chest || slot == ItemSlot.Helm || slot == ItemSlot.Shield ? 1f
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

        public override string ToString() => $"{Rarity} {Slot} (item level {ItemLevel}, {Affixes.Count} affixes)";
    }
}
