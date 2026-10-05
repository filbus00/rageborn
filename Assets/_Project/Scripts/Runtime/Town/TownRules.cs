using UnityEngine;

namespace ARPG
{
    /// <summary>The people who come to town as the player goes deeper (the owner, 2026-10-05: "New NPCs arrive").</summary>
    public enum Newcomer
    {
        StashKeeper,
        Healer,
        Gambler,
        Trainer,
    }

    /// <summary>
    /// When each newcomer arrives and where they stand (the owner's answer of 2026-10-05: a stash keeper at depth 2, a
    /// healer at 4, a gambler at 8, a trainer at 12, by the deepest depth the character has reached). Their cells are
    /// near the square, kept clear of the town's buildings (TownLayout). Pure.
    /// </summary>
    public static class Newcomers
    {
        public static readonly Newcomer[] All = { Newcomer.StashKeeper, Newcomer.Healer, Newcomer.Gambler, Newcomer.Trainer };

        public static int ArrivalDepth(Newcomer who) => who switch
        {
            Newcomer.StashKeeper => 2,
            Newcomer.Healer => 4,
            Newcomer.Gambler => 8,
            _ => 12,
        };

        public static bool HasArrived(Newcomer who, int deepestDepth) => deepestDepth >= ArrivalDepth(who);

        public static string Name(Newcomer who) => who switch
        {
            Newcomer.StashKeeper => "Stash Keeper",
            Newcomer.Healer => "Healer",
            Newcomer.Gambler => "Gambler",
            _ => "Trainer",
        };

        /// <summary>The cell each stands on.</summary>
        public static Vector2Int Cell(Newcomer who) => who switch
        {
            Newcomer.StashKeeper => new Vector2Int(-3, -3),
            Newcomer.Healer => new Vector2Int(1, 4),
            Newcomer.Gambler => new Vector2Int(-7, 6),
            _ => new Vector2Int(8, -2),
        };

        /// <summary>The newcomer whose arrival depth this is, if reaching it brings one (for the arrival notice).</summary>
        public static bool TryArrivingAt(int depth, out Newcomer who)
        {
            foreach (var candidate in All)
                if (ArrivalDepth(candidate) == depth)
                {
                    who = candidate;
                    return true;
                }
            who = default;
            return false;
        }
    }

    /// <summary>
    /// The Gambler (Diablo 2's Gheed): an item of a chosen kind for gold, its rarity rolled with better odds than a drop
    /// (Magic 78 percent, Rare 20, Legendary 2), at an item level from the deepest depth reached. Prices grow with that
    /// item level. Tuning, Claude's (2026-10-05). Pure.
    /// </summary>
    public static class GambleRules
    {
        public const float MagicWeight = 78f;
        public const float RareWeight = 20f;
        public const float LegendaryWeight = 2f;

        /// <summary>The kinds offered, one row each (the bow kinds and quiver count, rings once).</summary>
        public static readonly ItemSlot[] Offered =
        {
            ItemSlot.Weapon, ItemSlot.TwoHandWeapon, ItemSlot.Shield, ItemSlot.Helm, ItemSlot.Chest, ItemSlot.Gloves,
            ItemSlot.Belt, ItemSlot.Boots, ItemSlot.Amulet, ItemSlot.Ring,
        };

        /// <summary>The item level of what the Gambler sells: the enemies' level at the deepest depth reached.</summary>
        public static int ItemLevel(int deepestDepth) => DungeonRules.EnemyLevel(Mathf.Max(1, deepestDepth));

        /// <summary>A gamble's price: 60 gold plus 25 an item level, half again for a longbow or chest, more for
        /// jewellery.</summary>
        public static int Price(ItemSlot slot, int itemLevel)
        {
            var basePrice = 60f + 25f * Mathf.Max(1, itemLevel);
            var factor = slot switch
            {
                ItemSlot.TwoHandWeapon or ItemSlot.Chest => 1.5f,
                ItemSlot.Amulet or ItemSlot.Ring => 1.8f,
                _ => 1f,
            };
            return Mathf.RoundToInt(basePrice * factor / 5f) * 5;
        }

        /// <summary>The rarity of a gamble from a roll in 0..1.</summary>
        public static ItemRarity Rarity(double roll)
        {
            var total = MagicWeight + RareWeight + LegendaryWeight;
            var r = roll * total;
            if (r < LegendaryWeight)
                return ItemRarity.Legendary;
            if (r < LegendaryWeight + RareWeight)
                return ItemRarity.Rare;
            return ItemRarity.Magic;
        }
    }

    /// <summary>The Trainer's respec (Docs/05: the trainer resets skill points; stat points too, the owner's pick of
    /// 2026-10-05): every spent point back for a fee that grows with the character's level. Tuning. Pure.</summary>
    public static class RespecRules
    {
        public static int SkillsPrice(int characterLevel) => 50 * Mathf.Max(1, characterLevel);

        public static int StatsPrice(int characterLevel) => 50 * Mathf.Max(1, characterLevel);
    }
}
