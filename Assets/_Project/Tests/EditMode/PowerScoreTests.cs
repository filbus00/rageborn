using NUnit.Framework;

namespace ARPG.Tests
{
    public class PowerScoreTests
    {
        static Item Weapon(int level, params AffixRoll[] affixes) => new Item(ItemSlot.Weapon, ItemRarity.Magic, level, affixes);
        static Item Chest(int level, params AffixRoll[] affixes) => new Item(ItemSlot.Chest, ItemRarity.Magic, level, affixes);

        [Test]
        public void Dps_MatchesTheHitFormula_AtTheBaseAttackRate()
        {
            var equipment = new EquipmentState(Weapon(10));
            var expected = CombatFormulas.WeaponAverageDamage(10) * PowerScore.BaseAttacksPerSecond;

            Assert.AreEqual(expected, PowerScore.Evaluate(equipment, 5).DamagePerSecond, 1e-3f);
        }

        [Test]
        public void CritsAndAttackSpeed_RaiseDps_AsCombatWouldAverageThem()
        {
            var plain = PowerScore.Evaluate(new EquipmentState(Weapon(10)), 5).DamagePerSecond;
            var withCrit = PowerScore.Evaluate(new EquipmentState(Weapon(10, new AffixRoll(AffixId.CriticalChance, 5, 10f))), 5).DamagePerSecond;
            var withSpeed = PowerScore.Evaluate(new EquipmentState(Weapon(10, new AffixRoll(AffixId.AttackSpeed, 5, 10f))), 5).DamagePerSecond;

            // 10 percent crit chance at the base 150 percent crit: 1 + 0.1 x 0.5 = 5 percent more.
            Assert.AreEqual(plain * 1.05f, withCrit, 1e-3f);
            Assert.AreEqual(plain * 1.10f, withSpeed, 1e-3f);
        }

        [Test]
        public void ArmorAndLife_RaiseEffectiveLife()
        {
            var bare = PowerScore.Evaluate(EquipmentState.Empty, 5).EffectiveLife;
            var armored = PowerScore.Evaluate(new EquipmentState(Chest(10)), 5).EffectiveLife;
            var lifeChest = PowerScore.Evaluate(new EquipmentState(Chest(10, new AffixRoll(AffixId.Life, 5, 50f))), 5).EffectiveLife;

            Assert.AreEqual(CombatFormulas.CharacterBaseLife(5), bare, 1e-3f);
            Assert.Greater(armored, bare);
            Assert.Greater(lifeChest, armored);
        }

        [Test]
        public void TheScore_WeighsDamageAndSurvivalEqually()
        {
            var snapshot = new PowerSnapshot(100f, 400f);

            Assert.AreEqual(200f, snapshot.Score, 1e-3f);
            // Ten percent more of either raises the score by the same amount.
            Assert.AreEqual(new PowerSnapshot(110f, 400f).Score, new PowerSnapshot(100f, 440f).Score, 1e-3f);
        }

        [Test]
        public void Change_IsPositive_ForABetterItem_AndNegative_ForAWorseOne()
        {
            var equipment = new EquipmentState(Weapon(10));

            Assert.Greater(PowerScore.Change(equipment, Weapon(20), 5), 0f);
            Assert.Less(PowerScore.Change(equipment, Weapon(3), 5), 0f);
            Assert.AreEqual(0f, PowerScore.Change(equipment, Weapon(10), 5), 1e-5f);
        }

        [Test]
        public void AnyWeapon_BeatsFightingUnarmed_AndAnyChest_BeatsNone()
        {
            Assert.IsTrue(PowerScore.IsUpgrade(EquipmentState.Empty, Weapon(1), 1));
            Assert.IsTrue(PowerScore.IsUpgrade(EquipmentState.Empty, Chest(1), 1));
        }

        [Test]
        public void ADefensiveItem_CanOutscoreAnOffensiveOne_WhenItAddsMore()
        {
            // Survival counts as much as damage: a chest with a big Life roll (about +54 percent effective life at
            // level 10, so about +24 percent power) beats a one-level weapon upgrade (about +12 percent damage, +6 power).
            var equipment = new EquipmentState(Weapon(10));
            var lifeChest = Chest(10, new AffixRoll(AffixId.Life, 5, 150f));

            Assert.Greater(PowerScore.Change(equipment, lifeChest, 10), PowerScore.Change(equipment, Weapon(11), 10));
        }
    }

    public class ItemComparisonTests
    {
        [Test]
        public void Lines_StartWithTheBaseStat_ThenEveryAffixEitherItemHas()
        {
            var candidate = new Item(ItemSlot.Weapon, ItemRarity.Rare, 12, new[] { new AffixRoll(AffixId.FlatWeaponDamage, 5, 10f) });
            var worn = new Item(ItemSlot.Weapon, ItemRarity.Magic, 8, new[] { new AffixRoll(AffixId.CriticalChance, 5, 3f) });

            var lines = ItemComparison.Lines(candidate, worn);

            Assert.AreEqual(3, lines.Count);
            Assert.AreEqual("Weapon damage", lines[0].Label);
            Assert.AreEqual(1, lines[0].Direction, "item level 12 hits harder than 8");
            Assert.AreEqual("Flat damage", lines[1].Label);
            Assert.AreEqual(10f, lines[1].Candidate);
            Assert.AreEqual(0f, lines[1].Equipped);
            Assert.AreEqual("Critical chance", lines[2].Label);
            Assert.AreEqual(-1, lines[2].Direction, "the new item lacks the worn one's crit");
            Assert.IsTrue(lines[2].Percent);
        }

        [Test]
        public void AgainstAnEmptySlot_AWeaponIsComparedWithFightingUnarmed()
        {
            var candidate = new Item(ItemSlot.Weapon, ItemRarity.Common, 1);

            var lines = ItemComparison.Lines(candidate, null);

            Assert.AreEqual(CombatFormulas.WeaponAverageDamage(0), lines[0].Equipped, 1e-4f);
        }

        [Test]
        public void ArmorPieces_CompareBaseArmor()
        {
            var lines = ItemComparison.Lines(new Item(ItemSlot.Helm, ItemRarity.Common, 5), new Item(ItemSlot.Helm, ItemRarity.Common, 9));

            Assert.AreEqual("Base armor", lines[0].Label);
            Assert.AreEqual(-1, lines[0].Direction);
        }

        [TestCase(1, "●●●●●")]
        [TestCase(3, "●●●○○")]
        [TestCase(5, "●○○○○")]
        public void TierDots_FillFiveForTheBestTier(int tier, string expected)
        {
            Assert.AreEqual(expected, ItemComparison.TierDots(tier));
        }
    }
}
