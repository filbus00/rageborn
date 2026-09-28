using System;

namespace ARPG
{
    /// <summary>
    /// The affixes shipped so far, a curated slice of the 88 in Docs/03-itemization.md, hooked into stats the game
    /// already has (damage, armor, life, attack speed, crits, cooldowns, life on hit, movement speed, dodge), on the
    /// slots the docs list for each. The rest of the pool (resistances, sockets, Magic Find on gear) comes later.
    /// New ids go at the end: <see cref="AffixTable"/> indexes its definitions by id.
    /// </summary>
    public enum AffixId
    {
        // Prefixes.
        FlatWeaponDamage,
        IncreasedDamage,
        Life,
        Armor,

        // Suffixes.
        AttackSpeed,
        CriticalChance,
        CriticalDamage,
        LifeOnHit,
        CooldownReduction,
        MovementSpeed,
        DodgeChance,
    }

    public enum AffixKind
    {
        Prefix,
        Suffix,
    }

    /// <summary>One rolled affix on an item: which one, its tier (1 best to 5 worst, matching the docs' T1-T5) and
    /// its rolled value in the unit the docs table uses (a plain number for flat affixes, a percent number for
    /// percent affixes, e.g. 22 for +22 percent).</summary>
    public readonly struct AffixRoll
    {
        public AffixRoll(AffixId id, int tier, float value)
        {
            Id = id;
            Tier = tier;
            Value = value;
        }

        public AffixId Id { get; }
        public int Tier { get; }
        public float Value { get; }
    }

    /// <summary>
    /// The static data behind affixes: what an id means, its T1 range and which slots can roll it, plus the tier
    /// scaling and item level gates from Docs/03-itemization.md. Pure lookup, no rolling here (see
    /// <see cref="AffixRoller"/>).
    /// </summary>
    public static class AffixTable
    {
        public readonly struct Definition
        {
            public Definition(AffixKind kind, float t1Min, float t1Max, ItemSlot[] slots)
            {
                Kind = kind;
                T1Min = t1Min;
                T1Max = t1Max;
                Slots = slots;
            }

            public AffixKind Kind { get; }
            public float T1Min { get; }
            public float T1Max { get; }
            public ItemSlot[] Slots { get; }
        }

        public readonly struct Tier
        {
            public Tier(int minItemLevel, float lowShare, float highShare)
            {
                MinItemLevel = minItemLevel;
                LowShare = lowShare;
                HighShare = highShare;
            }

            public int MinItemLevel { get; }
            public float LowShare { get; }
            public float HighShare { get; }
        }

        // The slots each affix can roll on, from Docs/03's affix table (shields and the off-hand wait for their slot).
        static ItemSlot[] On(params ItemSlot[] slots) => slots;

        static readonly Definition[] Definitions =
        {
            new Definition(AffixKind.Prefix, 60f, 90f, On(ItemSlot.Weapon, ItemSlot.Ring, ItemSlot.Amulet, ItemSlot.Gloves)), // FlatWeaponDamage
            new Definition(AffixKind.Prefix, 18f, 26f, On(ItemSlot.Weapon, ItemSlot.Gloves, ItemSlot.Amulet)),                 // IncreasedDamage
            new Definition(AffixKind.Prefix, 180f, 240f, On(ItemSlot.Chest, ItemSlot.Helm, ItemSlot.Belt, ItemSlot.Boots)),    // Life
            new Definition(AffixKind.Prefix, 90f, 130f, On(ItemSlot.Chest, ItemSlot.Helm, ItemSlot.Gloves, ItemSlot.Boots)),   // Armor
            new Definition(AffixKind.Suffix, 7f, 11f, On(ItemSlot.Weapon, ItemSlot.Gloves, ItemSlot.Ring)),                    // AttackSpeed
            new Definition(AffixKind.Suffix, 4f, 7f, On(ItemSlot.Weapon, ItemSlot.Ring, ItemSlot.Amulet, ItemSlot.Gloves)),    // CriticalChance
            new Definition(AffixKind.Suffix, 20f, 30f, On(ItemSlot.Weapon, ItemSlot.Amulet, ItemSlot.Ring)),                   // CriticalDamage
            new Definition(AffixKind.Suffix, 4f, 8f, On(ItemSlot.Weapon, ItemSlot.Ring, ItemSlot.Gloves)),                     // LifeOnHit
            new Definition(AffixKind.Suffix, 5f, 9f, On(ItemSlot.Helm, ItemSlot.Amulet, ItemSlot.Ring)),                       // CooldownReduction
            new Definition(AffixKind.Suffix, 6f, 10f, On(ItemSlot.Boots)),                                                     // MovementSpeed
            new Definition(AffixKind.Suffix, 3f, 5f, On(ItemSlot.Boots, ItemSlot.Belt, ItemSlot.Ring)),                        // DodgeChance
        };

        // Index 0 unused so tier numbers (1-5) index directly.
        static readonly Tier[] Tiers =
        {
            default,
            new Tier(60, 0.90f, 1.00f), // T1
            new Tier(45, 0.75f, 0.90f), // T2
            new Tier(30, 0.55f, 0.75f), // T3
            new Tier(15, 0.35f, 0.55f), // T4
            new Tier(1, 0.20f, 0.35f),  // T5
        };

        public const int BestTier = 1;
        public const int WorstTier = 5;

        public static Definition Get(AffixId id) => Definitions[(int)id];

        public static Tier GetTier(int tier) => Tiers[tier];

        public static bool CanRollOn(AffixId id, ItemSlot slot) => Array.IndexOf(Get(id).Slots, slot) >= 0;

        /// <summary>The best (lowest-numbered) tier an item of this level has unlocked. Item level 1 only unlocks T5.</summary>
        public static int BestUnlockedTier(int itemLevel)
        {
            for (var tier = BestTier; tier <= WorstTier; tier++)
                if (itemLevel >= Tiers[tier].MinItemLevel)
                    return tier;
            return WorstTier;
        }
    }
}
