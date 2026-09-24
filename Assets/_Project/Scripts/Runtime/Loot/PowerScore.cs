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
    /// Not counted yet: class tags (the Wrathborn's skills are not designed), Life on Hit and Cooldown Reduction, which
    /// depend on how often the character is hit and casts. Pure.
    /// </summary>
    public static class PowerScore
    {
        /// <summary>Docs: attacks per second starts at 1.4, the same default <see cref="PlayerCombat"/> uses.</summary>
        public const float BaseAttacksPerSecond = 1.4f;

        public static PowerSnapshot Evaluate(EquipmentState equipment, int characterLevel)
        {
            characterLevel = Mathf.Max(1, characterLevel);

            // Unarmored target: armor scales every hit by the same factor, so it cannot change which gear is better.
            var hit = CombatFormulas.HitDamage(
                equipment.WeaponDamage, 1f, equipment.FlatWeaponDamageBonus, equipment.IncreasedDamagePercent / 100f, 1f,
                false, 0f, 0f, characterLevel);
            var critChance = Mathf.Clamp01(equipment.CriticalChancePercent / 100f);
            var critMultiplier = CombatFormulas.BaseCriticalMultiplier + equipment.CriticalDamagePercent / 100f;
            var expectedHit = hit * (1f + critChance * (critMultiplier - 1f));
            var dps = expectedHit * BaseAttacksPerSecond * (1f + equipment.AttackSpeedPercent / 100f);

            var maxLife = CombatFormulas.CharacterBaseLife(characterLevel) + equipment.TotalLifeBonus;
            var effectiveLife = maxLife / (1f - CombatFormulas.ArmorReduction(equipment.TotalArmor, characterLevel));

            return new PowerSnapshot(dps, effectiveLife);
        }

        /// <summary>The gear as it would be with <paramref name="candidate"/> equipped in its slot.</summary>
        public static PowerSnapshot WithItem(EquipmentState equipment, Item candidate, int characterLevel) =>
            Evaluate(equipment.With(candidate.Slot, candidate), characterLevel);

        /// <summary>
        /// How much equipping <paramref name="candidate"/> would change the power score, as a fraction: 0.12 for 12
        /// percent better, negative for worse. Positive is an upgrade arrow.
        /// </summary>
        public static float Change(EquipmentState equipment, Item candidate, int characterLevel)
        {
            var now = Evaluate(equipment, characterLevel).Score;
            var then = WithItem(equipment, candidate, characterLevel).Score;
            return now > 0f ? then / now - 1f : 0f;
        }

        /// <summary>Changes smaller than this (half a percent) show as neither better nor worse.</summary>
        public const float SameThreshold = 0.005f;

        public static bool IsUpgrade(EquipmentState equipment, Item candidate, int characterLevel) =>
            Change(equipment, candidate, characterLevel) > SameThreshold;
    }
}
