using UnityEngine;

namespace ARPG
{
    /// <summary>Where a drop comes from. The drop table in Docs/03-itemization.md has more sources (zone chests,
    /// bosses, Abyss guardians), added as they exist.</summary>
    public enum LootSource
    {
        NormalEnemy,
        Champion,
        Elite,
    }

    /// <summary>
    /// Rolls drops with a seeded generator, so a run can be replayed (Docs/07-technical.md). It also holds the bad
    /// luck counter, which has to live as long as the character does. Pure: no scene dependency.
    /// </summary>
    public sealed class LootRoller
    {
        // Docs/03-itemization.md drop table.
        public const float NormalEnemyDropChance = 0.06f;
        public const float ChampionDropChance = 0.25f;
        public const float EliteDropChance = 1f;

        // Docs: Elite's rarity floor is Magic, or Rare with this chance.
        const float EliteRareFloorChance = 0.30f;

        // Docs/04-progression-and-economy.md: elite gold is 8 times the normal formula. Champion gold is not
        // specified, so it follows the normal formula until tuned.
        const float EliteGoldMultiplier = 8f;

        // Bad luck protection: the legendary weight doubles after this many kills without one, and triples after twice this.
        public const int BadLuckThreshold = 300;

        // Every slot that can drop, weighted evenly. No per-slot drop-table weighting yet (Docs/03-itemization.md
        // does not specify one); a real drop table can replace this later.
        static readonly ItemSlot[] DroppableSlots = { ItemSlot.Weapon, ItemSlot.Chest, ItemSlot.Helm };

        readonly System.Random random;

        public LootRoller(int seed) => random = new System.Random(seed);

        /// <summary>Kills since the last legendary drop. Every kill counts, whether or not it dropped anything.</summary>
        public int KillsSinceLegendary { get; private set; }

        /// <summary>
        /// The weight of one rarity for the current state. Magic Find multiplies the Magic, Rare and Legendary weights
        /// by one plus itself; bad luck protection multiplies the Legendary weight. Does not allocate.
        /// </summary>
        public static float RarityWeight(ItemRarity rarity, float magicFind, int killsSinceLegendary)
        {
            var find = 1f + Mathf.Max(0f, magicFind);
            switch (rarity)
            {
                case ItemRarity.Common: return 60f;
                case ItemRarity.Magic: return 30f * find;
                case ItemRarity.Rare: return 8.5f * find;
                default: return 1.5f * find * BadLuckMultiplier(killsSinceLegendary);
            }
        }

        /// <summary>All the rarity weights in the order Common, Magic, Rare, Legendary. For tests and tools; rolling uses <see cref="RarityWeight"/>.</summary>
        public static float[] RarityWeights(float magicFind, int killsSinceLegendary)
        {
            var weights = new float[4];
            for (var i = 0; i < weights.Length; i++)
                weights[i] = RarityWeight((ItemRarity)i, magicFind, killsSinceLegendary);
            return weights;
        }

        /// <summary>The legendary weight multiplier: 1 at first, 2 from 300 kills without one, 3 from 600.</summary>
        public static float BadLuckMultiplier(int killsSinceLegendary)
        {
            if (killsSinceLegendary >= BadLuckThreshold * 2)
                return 3f;
            return killsSinceLegendary >= BadLuckThreshold ? 2f : 1f;
        }

        /// <summary>
        /// Registers a kill and rolls whether it drops an item. Returns the item, or null for no drop. For a source
        /// that can drop more than one item (Elite), this is the first; use <see cref="RollDrops"/> for all of them.
        /// </summary>
        /// <param name="itemLevel">The item level of a drop, which equals the zone level.</param>
        /// <param name="magicFind">Magic Find as a fraction, 0.5 for 50 percent.</param>
        public Item RollDrop(LootSource source, int itemLevel, float magicFind)
        {
            var drops = RollDrops(source, itemLevel, magicFind);
            return drops.Count > 0 ? drops[0] : null;
        }

        /// <summary>
        /// Registers a kill and rolls its drops per <see cref="LootSource"/> (Docs/03-itemization.md): a normal
        /// enemy or Champion drops at most one item, an Elite 1 to 2, each at least the source's rarity floor
        /// (Common for a normal enemy, Magic for a Champion, Magic or 30 percent of the time Rare for an Elite).
        /// Empty when nothing dropped.
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<Item> RollDrops(LootSource source, int itemLevel, float magicFind)
        {
            KillsSinceLegendary++;

            if (random.NextDouble() >= DropChance(source))
                return System.Array.Empty<Item>();

            var floor = RarityFloor(source);
            var count = DropCount(source);
            var items = new Item[count];
            for (var i = 0; i < count; i++)
            {
                var rarity = RollRarity(magicFind);
                if (rarity < floor)
                    rarity = floor;
                if (rarity == ItemRarity.Legendary)
                    KillsSinceLegendary = 0;

                var slot = DroppableSlots[random.Next(DroppableSlots.Length)];
                var affixes = AffixRoller.Roll(slot, rarity, itemLevel, random);
                items[i] = new Item(slot, rarity, itemLevel, affixes);
            }
            return items;
        }

        /// <summary>
        /// Gold from a kill. Docs/04-progression-and-economy.md: a normal enemy drops 0.6 times its level to the power
        /// 1.3, an elite 8 times that. It is rounded, with a floor of 1 so a drop is never empty.
        /// </summary>
        public int RollGold(LootSource source, int level)
        {
            var amount = 0.6f * Mathf.Pow(Mathf.Max(1, level), 1.3f);
            if (source == LootSource.Elite)
                amount *= EliteGoldMultiplier;
            return Mathf.Max(1, Mathf.RoundToInt(amount));
        }

        public static float DropChance(LootSource source)
        {
            switch (source)
            {
                case LootSource.Champion: return ChampionDropChance;
                case LootSource.Elite: return EliteDropChance;
                default: return NormalEnemyDropChance;
            }
        }

        // Docs: Elite drops 1 to 2 items; every other source drops at most 1.
        int DropCount(LootSource source) => source == LootSource.Elite ? 1 + random.Next(2) : 1;

        ItemRarity RarityFloor(LootSource source)
        {
            switch (source)
            {
                case LootSource.Champion:
                    return ItemRarity.Magic;
                case LootSource.Elite:
                    // One roll for the whole drop, not per item: either every item from this kill floors at Rare or none do.
                    return random.NextDouble() < EliteRareFloorChance ? ItemRarity.Rare : ItemRarity.Magic;
                default:
                    return ItemRarity.Common;
            }
        }

        ItemRarity RollRarity(float magicFind)
        {
            const int rarityCount = 4;

            var total = 0f;
            for (var i = 0; i < rarityCount; i++)
                total += RarityWeight((ItemRarity)i, magicFind, KillsSinceLegendary);

            var roll = (float)random.NextDouble() * total;
            for (var i = 0; i < rarityCount; i++)
            {
                roll -= RarityWeight((ItemRarity)i, magicFind, KillsSinceLegendary);
                if (roll < 0f)
                    return (ItemRarity)i;
            }

            return ItemRarity.Common;
        }
    }
}
