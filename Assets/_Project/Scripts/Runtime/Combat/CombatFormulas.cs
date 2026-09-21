using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The base value curves and the hit formula from Docs/03-itemization.md. Pure functions with no scene
    /// dependency, so the balance simulator and the tests can use them. All values are starting points.
    /// </summary>
    public static class CombatFormulas
    {
        /// <summary>The share of damage armor can block at most.</summary>
        public const float MaxArmorReduction = 0.8f;

        /// <summary>Critical hits multiply damage by this plus the critical damage bonus.</summary>
        public const float BaseCriticalMultiplier = 1.5f;

        /// <summary>Average damage of a weapon of the given item level.</summary>
        public static float WeaponAverageDamage(int itemLevel) => 6f + 2.6f * Mathf.Pow(itemLevel, 1.35f);

        /// <summary>Damage of one hit from an enemy of the given level, before the player's armor.</summary>
        public static float EnemyHitDamage(int level) => 3f * Mathf.Pow(level, 1.45f);

        /// <summary>Life of a normal enemy of the given level.</summary>
        public static float EnemyLife(int level) => 8f * Mathf.Pow(level, 1.9f);

        /// <summary>
        /// Damage the player takes from one enemy hit: the enemy's level curve times an archetype multiplier, reduced
        /// by the player's armor. Docs: player armor uses the same formula against the attacker's level.
        /// </summary>
        public static float EnemyHitOnPlayer(int enemyLevel, float damageMultiplier, float playerArmor) =>
            EnemyHitDamage(enemyLevel) * damageMultiplier * (1f - ArmorReduction(playerArmor, enemyLevel));

        /// <summary>Base life of a character of the given level, before Vitality and gear.</summary>
        public static float CharacterBaseLife(int level) => 80f + 20f * level;

        /// <summary>Armor per equipment piece of the given item level.</summary>
        public static float BaseArmorPerPiece(int itemLevel) => 8f + 3.1f * itemLevel;

        /// <summary>The share of damage that armor blocks against an attacker of the given level, between 0 and 0.8.</summary>
        public static float ArmorReduction(float armor, int attackerLevel)
        {
            if (armor <= 0f)
                return 0f;

            var reduction = armor / (armor + 50f * attackerLevel + 400f);
            return Mathf.Min(reduction, MaxArmorReduction);
        }

        /// <summary>
        /// Damage of one hit, following the steps in Docs/03-itemization.md.
        /// </summary>
        /// <param name="weaponDamage">Average weapon damage.</param>
        /// <param name="skillMultiplier">Skill damage as a multiplier, 1.8 for 180 percent.</param>
        /// <param name="flatAdded">Flat damage added before the skill multiplier.</param>
        /// <param name="increasedSum">Sum of all increased percentages, 0.2 for plus 20 percent. They add together.</param>
        /// <param name="moreMultiplier">Product of all more multipliers. They multiply separately. 1 for none.</param>
        /// <param name="critical">Whether the hit is a critical hit.</param>
        /// <param name="criticalDamageBonus">Critical damage bonus, 0.25 for plus 25 percent.</param>
        /// <param name="targetArmor">Armor of the target.</param>
        /// <param name="targetLevel">Level of the target, which sets how much armor is worth.</param>
        public static float HitDamage(
            float weaponDamage, float skillMultiplier, float flatAdded, float increasedSum, float moreMultiplier,
            bool critical, float criticalDamageBonus, float targetArmor, int targetLevel)
        {
            var damage = (weaponDamage + flatAdded) * skillMultiplier;
            damage *= 1f + increasedSum;
            damage *= moreMultiplier;

            if (critical)
                damage *= BaseCriticalMultiplier + criticalDamageBonus;

            return damage * (1f - ArmorReduction(targetArmor, targetLevel));
        }
    }
}
