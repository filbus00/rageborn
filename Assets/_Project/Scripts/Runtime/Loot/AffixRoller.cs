using System.Collections.Generic;

namespace ARPG
{
    /// <summary>
    /// Rolls the affixes on a dropped item: how many of each kind for the rarity (Docs/03-itemization.md), which
    /// ones (no repeats on one item), their tier (gated by item level, picked uniformly among unlocked tiers) and
    /// their value (uniform within the tier's scaled range). Pure and seeded through the caller's
    /// <see cref="System.Random"/>, so drops stay reproducible.
    /// </summary>
    public static class AffixRoller
    {
        /// <summary>Prefixes and suffixes rolled for a rarity, before capping to what the slot has eligible. Docs:
        /// Common 0, Magic 1 and 1, Rare 2 to 3 and 2 to 3. Legendary reuses Rare's range until fixed legendary
        /// affixes and unique powers are built (Docs/03-itemization.md).</summary>
        public static (int prefixes, int suffixes) CountRange(ItemRarity rarity, System.Random random)
        {
            switch (rarity)
            {
                case ItemRarity.Common:
                    return (0, 0);
                case ItemRarity.Magic:
                    return (1, 1);
                default:
                    var prefixes = 2 + random.Next(2); // 2 or 3
                    var suffixes = 2 + random.Next(2);
                    return (prefixes, suffixes);
            }
        }

        public static AffixRoll[] Roll(ItemSlot slot, ItemRarity rarity, int itemLevel, System.Random random)
        {
            var (prefixCount, suffixCount) = CountRange(rarity, random);
            if (prefixCount == 0 && suffixCount == 0)
                return System.Array.Empty<AffixRoll>();

            var rolls = new List<AffixRoll>(prefixCount + suffixCount);
            RollKind(AffixKind.Prefix, prefixCount, slot, itemLevel, random, rolls);
            RollKind(AffixKind.Suffix, suffixCount, slot, itemLevel, random, rolls);
            return rolls.ToArray();
        }

        static void RollKind(AffixKind kind, int count, ItemSlot slot, int itemLevel, System.Random random, List<AffixRoll> into)
        {
            if (count <= 0)
                return;

            var eligible = EligibleIds(kind, slot, itemLevel);
            var picked = PickDistinct(eligible, count, random);
            foreach (var id in picked)
            {
                var tier = RollTier(itemLevel, random);
                var value = RollValue(id, tier, random, itemLevel);
                into.Add(new AffixRoll(id, tier, value));
            }
        }

        /// <summary>Every affix of a kind that can roll on a slot, in <see cref="AffixId"/> order.</summary>
        public static List<AffixId> EligibleIds(AffixKind kind, ItemSlot slot, int itemLevel = int.MaxValue)
        {
            var list = new List<AffixId>();
            foreach (AffixId id in System.Enum.GetValues(typeof(AffixId)))
                if (AffixTable.Get(id).Kind == kind && AffixTable.CanRollOn(id, slot, itemLevel))
                    list.Add(id);
            return list;
        }

        // Partial Fisher-Yates: picks up to `count` distinct entries from `from`, without mutating the caller's list.
        static List<AffixId> PickDistinct(List<AffixId> from, int count, System.Random random)
        {
            var pool = new List<AffixId>(from);
            var picked = new List<AffixId>(System.Math.Min(count, pool.Count));
            var take = System.Math.Min(count, pool.Count);
            for (var i = 0; i < take; i++)
            {
                var index = random.Next(pool.Count);
                picked.Add(pool[index]);
                pool.RemoveAt(index);
            }
            return picked;
        }

        static int RollTier(int itemLevel, System.Random random)
        {
            var best = AffixTable.BestUnlockedTier(itemLevel);
            // Tiers best..worst are unlocked in that range (best is the lowest number, i.e. fewest unlocked at low
            // item level). Pick uniformly among every unlocked tier: best (numerically) through worst (5).
            return best + random.Next(AffixTable.WorstTier - best + 1);
        }

        /// <summary>A value uniformly within the tier's range for this affix at an item level.</summary>
        public static float RollValue(AffixId id, int tier, System.Random random, int itemLevel = FullScaleLevel)
        {
            var (min, max) = ValueRange(id, tier, itemLevel);
            return min + (float)random.NextDouble() * (max - min);
        }

        /// <summary>
        /// The lowest and highest value an affix can roll at a tier: its T1 range scaled by the tier's shares, and for the
        /// flat affixes (weapon damage, life, armour) by the item level too (<see cref="LevelScale"/>).
        /// </summary>
        public static (float min, float max) ValueRange(AffixId id, int tier, int itemLevel = FullScaleLevel)
        {
            var definition = AffixTable.Get(id);
            if (definition.Fixed)
                return (definition.T1Min, definition.T1Min);
            var scale = AffixTable.GetTier(tier);
            var level = ScalesWithLevel(id) ? LevelScale(itemLevel) : 1f;
            return (definition.T1Min * scale.LowShare * level, definition.T1Max * scale.HighShare * level);
        }

        /// <summary>
        /// The owner, 2026-10-17: "the gear that drops on the first levels is too powerful". The flat affixes rolled the
        /// same numbers at item level 1 as at 60: a T5 +12 to +31 weapon damage on a bow of 8.6 at level 1 (two to four
        /// times its damage), +36 to +84 life on 100. They now grow with the item level, to their full range at
        /// <see cref="FullScaleLevel"/>: about a tenth of it at level 1. Percent affixes are unchanged. Claude's numbers.
        /// </summary>
        public const int FullScaleLevel = 60;
        public const int LevelScaleOffset = 5;

        public static bool ScalesWithLevel(AffixId id) => id == AffixId.FlatWeaponDamage || id == AffixId.Life || id == AffixId.Armor;

        public static float LevelScale(int itemLevel) =>
            System.Math.Min(1f, (System.Math.Max(1, itemLevel) + LevelScaleOffset) / (float)(FullScaleLevel + LevelScaleOffset));
    }
}
