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

            var eligible = EligibleIds(kind, slot);
            var picked = PickDistinct(eligible, count, random);
            foreach (var id in picked)
            {
                var tier = RollTier(itemLevel, random);
                var value = RollValue(id, tier, random);
                into.Add(new AffixRoll(id, tier, value));
            }
        }

        static List<AffixId> EligibleIds(AffixKind kind, ItemSlot slot)
        {
            var list = new List<AffixId>();
            foreach (AffixId id in System.Enum.GetValues(typeof(AffixId)))
                if (AffixTable.Get(id).Kind == kind && AffixTable.CanRollOn(id, slot))
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

        static float RollValue(AffixId id, int tier, System.Random random)
        {
            var definition = AffixTable.Get(id);
            var scale = AffixTable.GetTier(tier);
            var min = definition.T1Min * scale.LowShare;
            var max = definition.T1Max * scale.HighShare;
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
