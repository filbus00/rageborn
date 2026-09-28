using UnityEngine;

namespace ARPG
{
    /// <summary>What a set of gear is worth to the character: expected damage per second, effective life, and the
    /// single power score made from them.</summary>
    public readonly struct PowerSnapshot
    {
        public PowerSnapshot(float damagePerSecond, float effectiveLife)
        {
            DamagePerSecond = damagePerSecond;
            EffectiveLife = effectiveLife;
        }

        /// <summary>Expected basic-attack damage per second against an unarmored target, crits averaged in.</summary>
        public float DamagePerSecond { get; }

        /// <summary>Maximum life divided by what armor lets through from an attacker of the character's level.</summary>
        public float EffectiveLife { get; }

        /// <summary>
        /// The geometric mean of the two, so damage and survival count equally and neither can be ignored: 10 percent
        /// more of either raises the score by about 5 percent.
        /// </summary>
        public float Score => Mathf.Sqrt(Mathf.Max(0f, DamagePerSecond) * Mathf.Max(0f, EffectiveLife));
    }

    /// <summary>
    /// The power score from Docs/03-itemization.md: "a single number computed from an item's contribution to damage per
    /// second, effective life and the class's main tags", used for the upgrade arrows and, later, the loot filter's
    /// Smart preset. It is a comparison aid; tooltips always show the raw affixes. Built from the same formulas combat
    /// uses (<see cref="CombatFormulas"/>, the basic attack's 1.4 per second), so an arrow cannot disagree with a fight.
    /// It counts the character's attributes (from its level) and, when given, its passive tree: they add to the same
    /// pools as gear (increased damage, crit, attack speed, armor, life), so they change which item is better
    /// (2026-09-28). Not counted: what depends on the fight (Stillness and Momentum, buffs, Rage, Bloodied Edge, Berserker),
    /// class tags, Life on Hit and Cooldown Reduction, which depend on how often the character is hit and casts. Pure.
    /// </summary>
    public static class PowerScore
    {
        /// <summary>Docs: attacks per second starts at 1.4, the same default <see cref="PlayerCombat"/> uses.</summary>
        public const float BaseAttacksPerSecond = 1.4f;

        /// <summary>Docs/02's hard cap on dodge, as <see cref="PlayerHealth"/> applies it.</summary>
        public const float MaxDodge = 0.5f;

        public static PowerSnapshot Evaluate(EquipmentState equipment, int characterLevel, PassiveBonuses tree = null)
        {
            characterLevel = Mathf.Max(1, characterLevel);
            var attributes = CharacterAttributes.At(characterLevel);

            // Unarmored target: armor scales every hit by the same factor, so it cannot change which gear is better.
            // The same sums as PlayerCombat.Damage and Strike: Might's damage counts, since the basic attack is melee.
            var increased = equipment.IncreasedDamagePercent / 100f + attributes.IncreasedDamage + (tree?.IncreasedDamage ?? 0f);
            var hit = CombatFormulas.HitDamage(
                equipment.WeaponDamage, 1f, equipment.FlatWeaponDamageBonus, increased, 1f,
                false, 0f, 0f, characterLevel);
            var critChance = Mathf.Clamp01((equipment.CriticalChancePercent + attributes.CriticalChance + (tree?.CriticalChance ?? 0f)) / 100f);
            var critMultiplier = CombatFormulas.BaseCriticalMultiplier +
                                 (equipment.CriticalDamagePercent + (tree?.CriticalDamage ?? 0f)) / 100f;
            var expectedHit = hit * (1f + critChance * (critMultiplier - 1f));
            var dps = expectedHit * AttacksPerSecond(equipment, characterLevel, tree);

            // As PlayerHealth: life and armor with the tree's percentages and Might's armor; dodge (attributes and the
            // tree, capped) spares that share of hits.
            var maxLife = (CombatFormulas.CharacterLife(characterLevel) + equipment.TotalLifeBonus) * (1f + (tree?.LifePercent ?? 0f));
            var armor = (equipment.TotalArmor + attributes.Armor) * (1f + (tree?.ArmorPercent ?? 0f));
            var dodge = Mathf.Min(MaxDodge, attributes.Dodge + (tree?.Dodge ?? 0f) + equipment.DodgePercent / 100f);
            var effectiveLife = maxLife / (1f - CombatFormulas.ArmorReduction(armor, characterLevel)) / (1f - dodge);

            return new PowerSnapshot(dps, effectiveLife);
        }

        /// <summary>Basic attacks a second with gear, attributes and the tree, as <see cref="PlayerCombat"/> times them.</summary>
        public static float AttacksPerSecond(EquipmentState equipment, int characterLevel, PassiveBonuses tree = null) =>
            BaseAttacksPerSecond * (1f + equipment.AttackSpeedPercent / 100f + CharacterAttributes.At(Mathf.Max(1, characterLevel)).AttackSpeed +
                                    (tree?.AttackSpeed ?? 0f));

        /// <summary>The character's own gear, level and tree.</summary>
        public static PowerSnapshot Evaluate(GameSession session) =>
            Evaluate(session.Equipment, session.Level, session.PassiveTree.Bonuses);

        /// <summary>
        /// Where <paramref name="candidate"/> would be worn: its own slot, or for a ring an empty hand, else the hand
        /// where it raises the score more (so it replaces the weaker ring).
        /// </summary>
        public static ItemSlot PlaceFor(EquipmentState equipment, Item candidate, int characterLevel, PassiveBonuses tree = null)
        {
            var places = EquipmentState.PlacesFor(candidate.Slot);
            if (places.Length == 1)
                return places[0];
            foreach (var place in places)
                if (equipment.Get(place) == null)
                    return place;
            var best = places[0];
            var bestScore = float.MinValue;
            foreach (var place in places)
            {
                var score = Evaluate(equipment.With(place, candidate), characterLevel, tree).Score;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = place;
                }
            }
            return best;
        }

        /// <summary>The gear as it would be with <paramref name="candidate"/> equipped where <see cref="PlaceFor"/> puts it.</summary>
        public static PowerSnapshot WithItem(EquipmentState equipment, Item candidate, int characterLevel, PassiveBonuses tree = null) =>
            Evaluate(equipment.With(PlaceFor(equipment, candidate, characterLevel, tree), candidate), characterLevel, tree);

        /// <summary>
        /// How much equipping <paramref name="candidate"/> would change the power score, as a fraction: 0.12 for 12
        /// percent better, negative for worse. Positive is an upgrade arrow.
        /// </summary>
        public static float Change(EquipmentState equipment, Item candidate, int characterLevel, PassiveBonuses tree = null) =>
            ChangeAt(equipment, PlaceFor(equipment, candidate, characterLevel, tree), candidate, characterLevel, tree);

        /// <summary>The same change, with the item worn in a given place.</summary>
        public static float ChangeAt(EquipmentState equipment, ItemSlot place, Item candidate, int characterLevel, PassiveBonuses tree = null)
        {
            var now = Evaluate(equipment, characterLevel, tree).Score;
            var then = Evaluate(equipment.With(place, candidate), characterLevel, tree).Score;
            return now > 0f ? then / now - 1f : 0f;
        }

        /// <summary>Changes smaller than this (half a percent) show as neither better nor worse.</summary>
        public const float SameThreshold = 0.005f;

        public static bool IsUpgrade(EquipmentState equipment, Item candidate, int characterLevel, PassiveBonuses tree = null) =>
            Change(equipment, candidate, characterLevel, tree) > SameThreshold;
    }
}
