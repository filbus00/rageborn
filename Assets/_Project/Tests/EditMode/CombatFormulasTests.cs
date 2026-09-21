using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class CombatFormulasTests
    {
        [Test]
        public void WeaponAverageDamage_MatchesTheValuesQuotedInTheDocs()
        {
            // Docs/03: about 262 at item level 30 and about 660 at level 60.
            Assert.AreEqual(262f, CombatFormulas.WeaponAverageDamage(30), 1f);
            Assert.AreEqual(660f, CombatFormulas.WeaponAverageDamage(60), 1f);
            Assert.AreEqual(8.6f, CombatFormulas.WeaponAverageDamage(1), 1e-3f);
        }

        [Test]
        public void EnemyAndCharacterCurves_AtLevelOne()
        {
            Assert.AreEqual(8f, CombatFormulas.EnemyLife(1), 1e-4f);
            Assert.AreEqual(3f, CombatFormulas.EnemyHitDamage(1), 1e-4f);
            Assert.AreEqual(100f, CombatFormulas.CharacterBaseLife(1), 1e-4f);
            Assert.AreEqual(11.1f, CombatFormulas.BaseArmorPerPiece(1), 1e-4f);
        }

        [Test]
        public void EnemyCurves_GrowWithLevel()
        {
            Assert.AreEqual(8f * Mathf.Pow(10f, 1.9f), CombatFormulas.EnemyLife(10), 0.1f);
            Assert.Greater(CombatFormulas.EnemyLife(11), CombatFormulas.EnemyLife(10));
            Assert.Greater(CombatFormulas.EnemyHitDamage(11), CombatFormulas.EnemyHitDamage(10));
        }

        [Test]
        public void ArmorReduction_FollowsTheDocFormula()
        {
            // armor / (armor + 50 * level + 400) at level 10: 500 armor is 500 / 1400.
            Assert.AreEqual(500f / 1400f, CombatFormulas.ArmorReduction(500f, 10), 1e-5f);
            Assert.AreEqual(0f, CombatFormulas.ArmorReduction(0f, 10), 1e-6f);
            Assert.AreEqual(0f, CombatFormulas.ArmorReduction(-50f, 10), 1e-6f, "negative armor blocks nothing");
        }

        [Test]
        public void ArmorReduction_IsCappedAtEightyPercent()
        {
            // At level 1 the cap is reached at armor 1800: armor / (armor + 450) = 0.8.
            Assert.AreEqual(0.8f, CombatFormulas.ArmorReduction(1_000_000f, 1), 1e-6f);
            Assert.AreEqual(0.8f, CombatFormulas.ArmorReduction(2000f, 1), 1e-6f);
            Assert.AreEqual(1000f / 1450f, CombatFormulas.ArmorReduction(1000f, 1), 1e-5f, "below the cap it follows the formula");
        }

        [Test]
        public void ArmorReduction_ShrinksAsTheAttackerLevelRises()
        {
            Assert.Less(CombatFormulas.ArmorReduction(300f, 30), CombatFormulas.ArmorReduction(300f, 5));
        }

        [Test]
        public void HitDamage_PlainWeaponHit()
        {
            var damage = CombatFormulas.HitDamage(10f, 1f, 0f, 0f, 1f, false, 0f, 0f, 1);

            Assert.AreEqual(10f, damage, 1e-5f);
        }

        [Test]
        public void HitDamage_SkillMultiplierAppliesToWeaponAndFlatDamage()
        {
            // (weapon + flat) * multiplier: (10 + 5) * 1.8.
            var damage = CombatFormulas.HitDamage(10f, 1.8f, 5f, 0f, 1f, false, 0f, 0f, 1);

            Assert.AreEqual(27f, damage, 1e-4f);
        }

        [Test]
        public void HitDamage_IncreasedAddsTogether_AndMoreMultiplies()
        {
            // Increased 20 and 30 percent add to plus 50 percent; two more multipliers of 1.2 multiply to 1.44.
            var increasedOnly = CombatFormulas.HitDamage(100f, 1f, 0f, 0.5f, 1f, false, 0f, 0f, 1);
            var moreOnly = CombatFormulas.HitDamage(100f, 1f, 0f, 0f, 1.2f * 1.2f, false, 0f, 0f, 1);

            Assert.AreEqual(150f, increasedOnly, 1e-4f);
            Assert.AreEqual(144f, moreOnly, 1e-3f);
        }

        [Test]
        public void HitDamage_CriticalMultipliesByOnePointFivePlusTheBonus()
        {
            var normal = CombatFormulas.HitDamage(100f, 1f, 0f, 0f, 1f, false, 0.25f, 0f, 1);
            var critical = CombatFormulas.HitDamage(100f, 1f, 0f, 0f, 1f, true, 0.25f, 0f, 1);

            Assert.AreEqual(100f, normal, 1e-4f);
            Assert.AreEqual(175f, critical, 1e-3f);
        }

        [Test]
        public void HitDamage_ArmorIsAppliedLast()
        {
            // 100 damage against armor worth 25 percent leaves 75.
            var armor = 0.25f / (1f - 0.25f) * (50f * 5 + 400f);
            var damage = CombatFormulas.HitDamage(100f, 1f, 0f, 0f, 1f, false, 0f, armor, 5);

            Assert.AreEqual(75f, damage, 1e-2f);
        }

        [Test]
        public void LevelOneBasicHit_KillsALevelOneEnemyInOneHit()
        {
            // The docs aim for a normal enemy to die in 0.6 to 1.2 seconds; at 1.4 attacks a second, one hit does it.
            var hit = CombatFormulas.HitDamage(CombatFormulas.WeaponAverageDamage(1), 1f, 0f, 0f, 1f, false, 0f, 0f, 1);

            Assert.GreaterOrEqual(hit, CombatFormulas.EnemyLife(1));
        }
    }
}
