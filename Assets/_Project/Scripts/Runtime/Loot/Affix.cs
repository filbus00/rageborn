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

        // The quiver's own (Docs/03, 2026-09-30): the chance a basic shot looses a second arrow at another enemy.
        ExtraArrowChance,

        // Ailments (Docs/03, decided 2026-10-01): the chance an arrow sets its target burning, or chills it.
        IgniteChance,
        ChillChance,

        // The Wild Arrow's own (Docs/03, "New and changed affixes", 90 to 100; built 2026-10-01).
        ExtraPierce,        // 90: plus 1 pierce on every arrow (fixed, from item level 20)
        ForkChance,         // 91: chance an arrow forks on its first hit
        BurnDamage,         // 93: burns deal more
        DamagePerPierce,    // 94: increased damage per enemy already pierced
        FarDamage,          // 95: increased damage to enemies more than 6 away
        NearDamage,         // 96: increased damage to enemies within 3 (point blank)
        ArrowSpeed,         // 97: arrows fly faster
        PetDamage,          // 98
        PetLife,            // 99
        ExtraSkillArrow,    // 100: plus 1 arrow to Split and Homing Arrow (fixed, from item level 30)
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
            public Definition(AffixKind kind, float t1Min, float t1Max, ItemSlot[] slots, bool isFixed = false, int minItemLevel = 0)
            {
                Kind = kind;
                T1Min = t1Min;
                T1Max = t1Max;
                Slots = slots;
                Fixed = isFixed;
                MinItemLevel = minItemLevel;
            }

            public AffixKind Kind { get; }
            public float T1Min { get; }
            public float T1Max { get; }
            public ItemSlot[] Slots { get; }

            /// <summary>The value is T1Min at every tier (a whole extra pierce or arrow, Docs/03's "Fixed 1").</summary>
            public bool Fixed { get; }

            /// <summary>The lowest item level that can roll it (Docs/03: plus 1 pierce from 20, plus 1 arrow from 30).</summary>
            public int MinItemLevel { get; }
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

        // The slots each affix can roll on, from Docs/03's affix table. The quiver (the Shield kind, bows only since
        // 2026-09-30) rolls flat damage and life, and attack speed, crits and its own extra arrow (Docs/03, the Wild Arrow's slots).
        static ItemSlot[] On(params ItemSlot[] slots)
        {
            // A two-handed weapon rolls what a weapon rolls.
            if (System.Array.IndexOf(slots, ItemSlot.Weapon) < 0)
                return slots;
            var withTwoHand = new ItemSlot[slots.Length + 1];
            slots.CopyTo(withTwoHand, 0);
            withTwoHand[slots.Length] = ItemSlot.TwoHandWeapon;
            return withTwoHand;
        }

        static readonly Definition[] Definitions =
        {
            new Definition(AffixKind.Prefix, 60f, 90f, On(ItemSlot.Weapon, ItemSlot.Ring, ItemSlot.Amulet, ItemSlot.Gloves, ItemSlot.Shield)), // FlatWeaponDamage
            new Definition(AffixKind.Prefix, 18f, 26f, On(ItemSlot.Weapon, ItemSlot.Gloves, ItemSlot.Amulet)),                 // IncreasedDamage
            new Definition(AffixKind.Prefix, 180f, 240f, On(ItemSlot.Chest, ItemSlot.Helm, ItemSlot.Belt, ItemSlot.Boots, ItemSlot.Shield)),    // Life
            new Definition(AffixKind.Prefix, 90f, 130f, On(ItemSlot.Chest, ItemSlot.Helm, ItemSlot.Gloves, ItemSlot.Boots)),   // Armor
            new Definition(AffixKind.Suffix, 7f, 11f, On(ItemSlot.Weapon, ItemSlot.Gloves, ItemSlot.Ring, ItemSlot.Shield)),   // AttackSpeed
            new Definition(AffixKind.Suffix, 4f, 7f, On(ItemSlot.Weapon, ItemSlot.Ring, ItemSlot.Amulet, ItemSlot.Gloves, ItemSlot.Shield)), // CriticalChance
            new Definition(AffixKind.Suffix, 20f, 30f, On(ItemSlot.Weapon, ItemSlot.Amulet, ItemSlot.Ring, ItemSlot.Shield)),  // CriticalDamage
            new Definition(AffixKind.Suffix, 4f, 8f, On(ItemSlot.Weapon, ItemSlot.Ring, ItemSlot.Gloves)),                     // LifeOnHit
            new Definition(AffixKind.Suffix, 5f, 9f, On(ItemSlot.Helm, ItemSlot.Amulet, ItemSlot.Ring)),                       // CooldownReduction
            new Definition(AffixKind.Suffix, 6f, 10f, On(ItemSlot.Boots)),                                                     // MovementSpeed
            new Definition(AffixKind.Suffix, 3f, 5f, On(ItemSlot.Boots, ItemSlot.Belt, ItemSlot.Ring)),                        // DodgeChance
            new Definition(AffixKind.Suffix, 8f, 15f, On(ItemSlot.Shield)),                                                    // ExtraArrowChance
            new Definition(AffixKind.Suffix, 8f, 15f, On(ItemSlot.Weapon, ItemSlot.Shield, ItemSlot.Gloves)),                  // IgniteChance
            new Definition(AffixKind.Suffix, 8f, 15f, On(ItemSlot.Weapon, ItemSlot.Shield, ItemSlot.Gloves)),                  // ChillChance
            new Definition(AffixKind.Suffix, 1f, 1f, On(ItemSlot.Shield), true, 20),                                          // ExtraPierce
            new Definition(AffixKind.Suffix, 10f, 20f, On(ItemSlot.Shield, ItemSlot.Weapon)),                                  // ForkChance
            new Definition(AffixKind.Prefix, 20f, 35f, On(ItemSlot.Weapon, ItemSlot.Gloves, ItemSlot.Amulet)),                 // BurnDamage
            new Definition(AffixKind.Prefix, 6f, 10f, On(ItemSlot.TwoHandWeapon, ItemSlot.Shield)),                           // DamagePerPierce
            new Definition(AffixKind.Prefix, 12f, 20f, On(ItemSlot.TwoHandWeapon, ItemSlot.Helm)),                            // FarDamage
            new Definition(AffixKind.Prefix, 12f, 20f, new[] { ItemSlot.Weapon, ItemSlot.Gloves }),                           // NearDamage (short bow only)
            new Definition(AffixKind.Suffix, 15f, 25f, On(ItemSlot.Weapon, ItemSlot.Shield)),                                  // ArrowSpeed
            new Definition(AffixKind.Prefix, 15f, 25f, On(ItemSlot.Amulet, ItemSlot.Ring, ItemSlot.Belt)),                     // PetDamage
            new Definition(AffixKind.Prefix, 15f, 25f, On(ItemSlot.Chest, ItemSlot.Belt)),                                     // PetLife
            new Definition(AffixKind.Suffix, 1f, 1f, On(ItemSlot.Gloves, ItemSlot.Shield), true, 30),                          // ExtraSkillArrow
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

        /// <summary>Whether an item of this level can roll it: the slot, and the affix's own minimum item level.</summary>
        public static bool CanRollOn(AffixId id, ItemSlot slot, int itemLevel) => CanRollOn(id, slot) && itemLevel >= Get(id).MinItemLevel;

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
