using System;
using UnityEngine;

namespace ARPG
{
    /// <summary>The salvage materials from Docs/04-progression-and-economy.md, one per rarity. They stack without limit.</summary>
    public enum CraftingMaterial
    {
        Ash,
        Cinders,
        Bloodstone,
        Soulglass,
    }

    /// <summary>What a Forge action costs: gold plus some of one material.</summary>
    public readonly struct ForgeCost
    {
        public ForgeCost(int gold, CraftingMaterial material, int amount)
        {
            Gold = gold;
            Material = material;
            Amount = amount;
        }

        public int Gold { get; }
        public CraftingMaterial Material { get; }
        public int Amount { get; }
    }

    /// <summary>
    /// The Forge (Docs/03-itemization.md, Forge): salvage, and the three crafting actions that work with what exists
    /// so far. Reforge turns one chosen affix of a Rare into a new random affix of the same kind at the same tier (the
    /// user's reading of "same tier band", 2026-09-26); it may land on the stat it replaced. Reroll values rerolls the
    /// numbers of every affix on a Legendary within their tiers. Temper raises one affix a tier, keeping where its value
    /// sat in its range, at most three times per item. Each action returns a new item; the original is untouched.
    /// Socket, Imprint and Transmog wait for gems, legendary powers and appearances. Pure.
    ///
    /// Tuning values, which the docs leave open: the salvage yields, the material amounts, the gold multipliers on the
    /// docs' gold curve, and the elite and boss material drops.
    /// </summary>
    public static class ForgeRules
    {
        public const int MaxTempers = 3;

        /// <summary>Docs: each reforge on the same item raises its material cost by 25 percent. The docs also make the
        /// gold rise with each reroll, so it escalates the same way.</summary>
        public const float ReforgeEscalation = 1.25f;

        public const int ReforgeBloodstone = 2;
        public const int RerollSoulglass = 1;
        public const int TemperSoulglass = 2;

        // Docs/04: Forge costs use the gold drop curve times an action multiplier.
        public const float ReforgeGoldMultiplier = 10f;
        public const float RerollGoldMultiplier = 15f;
        public const float TemperGoldMultiplier = 25f;

        /// <summary>Docs/04: Bloodstone also comes from elites and Soulglass from bosses, not only from salvage.</summary>
        public const int EliteBloodstone = 1;
        public const int BossSoulglass = 3;

        /// <summary>The material a rarity salvages into (Docs/03: Ash, Cinders, Bloodstone, Soulglass).</summary>
        public static CraftingMaterial MaterialOf(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return CraftingMaterial.Ash;
                case ItemRarity.Magic: return CraftingMaterial.Cinders;
                case ItemRarity.Rare: return CraftingMaterial.Bloodstone;
                default: return CraftingMaterial.Soulglass;
            }
        }

        /// <summary>What salvaging an item gives: 2 of Ash, Cinders or Bloodstone, 1 Soulglass for a Legendary.</summary>
        public static (CraftingMaterial material, int amount) SalvageYield(Item item)
        {
            var material = MaterialOf(item.Rarity);
            return (material, material == CraftingMaterial.Soulglass ? 1 : 2);
        }

        /// <summary>The docs' gold curve at an item level, 0.6 times level to the power 1.3, before an action's multiplier.</summary>
        public static float GoldCurve(int itemLevel) => 0.6f * Mathf.Pow(Mathf.Max(1, itemLevel), 1.3f);

        static int Gold(int itemLevel, float multiplier) => Mathf.Max(1, Mathf.RoundToInt(GoldCurve(itemLevel) * multiplier));

        // --- Reforge ---------------------------------------------------------------------------------------------

        public static bool CanReforge(Item item) => item != null && item.Rarity == ItemRarity.Rare && item.Affixes.Count > 0;

        public static ForgeCost ReforgeCost(Item item)
        {
            var escalation = Mathf.Pow(ReforgeEscalation, item.Reforges);
            return new ForgeCost(
                Mathf.CeilToInt(Gold(item.ItemLevel, ReforgeGoldMultiplier) * escalation - 0.0001f),
                CraftingMaterial.Bloodstone,
                Mathf.CeilToInt(ReforgeBloodstone * escalation - 0.0001f));
        }

        /// <summary>
        /// A copy with the chosen affix replaced by a random affix of the same kind that the slot can roll and the item
        /// does not already have elsewhere, at the same tier with a fresh value. Its reforge count goes up by one.
        /// </summary>
        public static Item Reforge(Item item, int affixIndex, System.Random random)
        {
            if (!CanReforge(item))
                throw new InvalidOperationException($"Only a Rare with affixes can be reforged, not {item}.");
            CheckIndex(item, affixIndex);

            var old = item.Affixes[affixIndex];
            var candidates = AffixRoller.EligibleIds(AffixTable.Get(old.Id).Kind, item.Slot, item.ItemLevel);
            for (var i = 0; i < item.Affixes.Count; i++)
                if (i != affixIndex)
                    candidates.Remove(item.Affixes[i].Id);

            var id = candidates[random.Next(candidates.Count)];
            var affixes = Copy(item);
            affixes[affixIndex] = new AffixRoll(id, old.Tier, AffixRoller.RollValue(id, old.Tier, random));
            return new Item(item.Slot, item.Rarity, item.ItemLevel, affixes, item.Reforges + 1, item.Tempers, item.Legendary);
        }

        // --- Reroll values ---------------------------------------------------------------------------------------

        public static bool CanReroll(Item item) => item != null && item.Rarity == ItemRarity.Legendary && item.Affixes.Count > 0;

        public static ForgeCost RerollCost(Item item) =>
            new ForgeCost(Gold(item.ItemLevel, RerollGoldMultiplier), CraftingMaterial.Soulglass, RerollSoulglass);

        /// <summary>A copy with every affix's value rolled again within its tier. Affixes and tiers stay.</summary>
        public static Item Reroll(Item item, System.Random random)
        {
            if (!CanReroll(item))
                throw new InvalidOperationException($"Only a Legendary with affixes can be rerolled, not {item}.");

            var affixes = Copy(item);
            for (var i = 0; i < affixes.Length; i++)
                affixes[i] = new AffixRoll(affixes[i].Id, affixes[i].Tier, AffixRoller.RollValue(affixes[i].Id, affixes[i].Tier, random));
            return new Item(item.Slot, item.Rarity, item.ItemLevel, affixes, item.Reforges, item.Tempers, item.Legendary);
        }

        // --- Temper ----------------------------------------------------------------------------------------------

        /// <summary>Whether the item has a temper left at all. Which affix can take it is <see cref="CanTemper(Item, int)"/>.</summary>
        public static bool CanTemper(Item item) => item != null && item.Affixes.Count > 0 && item.Tempers < MaxTempers;

        public static bool CanTemper(Item item, int affixIndex) =>
            CanTemper(item) && affixIndex >= 0 && affixIndex < item.Affixes.Count && item.Affixes[affixIndex].Tier > AffixTable.BestTier;

        public static ForgeCost TemperCost(Item item) =>
            new ForgeCost(Gold(item.ItemLevel, TemperGoldMultiplier), CraftingMaterial.Soulglass, TemperSoulglass);

        /// <summary>
        /// A copy with one affix a tier better. Its value keeps its place in the range: an affix at the top of T3 lands
        /// at the top of T2, so tempering never loses anything to a bad roll. The temper count goes up by one.
        /// </summary>
        public static Item Temper(Item item, int affixIndex)
        {
            if (!CanTemper(item, affixIndex))
                throw new InvalidOperationException($"Affix {affixIndex} of {item} cannot be tempered.");

            var old = item.Affixes[affixIndex];
            var (oldMin, oldMax) = AffixRoller.ValueRange(old.Id, old.Tier);
            var tier = old.Tier - 1;
            var (newMin, newMax) = AffixRoller.ValueRange(old.Id, tier);
            var place = oldMax > oldMin ? Mathf.Clamp01((old.Value - oldMin) / (oldMax - oldMin)) : 0f;

            var affixes = Copy(item);
            affixes[affixIndex] = new AffixRoll(old.Id, tier, newMin + place * (newMax - newMin));
            return new Item(item.Slot, item.Rarity, item.ItemLevel, affixes, item.Reforges, item.Tempers + 1, item.Legendary);
        }

        static AffixRoll[] Copy(Item item)
        {
            var affixes = new AffixRoll[item.Affixes.Count];
            for (var i = 0; i < affixes.Length; i++)
                affixes[i] = item.Affixes[i];
            return affixes;
        }

        static void CheckIndex(Item item, int affixIndex)
        {
            if (affixIndex < 0 || affixIndex >= item.Affixes.Count)
                throw new ArgumentOutOfRangeException(nameof(affixIndex));
        }
    }
}
