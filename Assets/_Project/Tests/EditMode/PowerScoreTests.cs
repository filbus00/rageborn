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

            Assert.AreEqual(expected, PowerScore.Evaluate(equipment, 1).DamagePerSecond, 1e-3f);
        }

        [Test]
        public void CritsAndAttackSpeed_RaiseDps_AsCombatWouldAverageThem()
        {
            var plain = PowerScore.Evaluate(new EquipmentState(Weapon(10)), 1).DamagePerSecond;
            var withCrit = PowerScore.Evaluate(new EquipmentState(Weapon(10, new AffixRoll(AffixId.CriticalChance, 5, 10f))), 1).DamagePerSecond;
            var withSpeed = PowerScore.Evaluate(new EquipmentState(Weapon(10, new AffixRoll(AffixId.AttackSpeed, 5, 10f))), 1).DamagePerSecond;

            // 10 percent crit chance at the base 150 percent crit: 1 + 0.1 x 0.5 = 5 percent more.
            Assert.AreEqual(plain * 1.05f, withCrit, 1e-3f);
            Assert.AreEqual(plain * 1.10f, withSpeed, 1e-3f);
        }

        [Test]
        public void ArmorAndLife_RaiseEffectiveLife()
        {
            var bare = PowerScore.Evaluate(EquipmentState.Empty, 1).EffectiveLife;
            var armored = PowerScore.Evaluate(new EquipmentState(Chest(10)), 1).EffectiveLife;
            var lifeChest = PowerScore.Evaluate(new EquipmentState(Chest(10, new AffixRoll(AffixId.Life, 5, 50f))), 1).EffectiveLife;

            Assert.AreEqual(CombatFormulas.CharacterLife(1), bare, 1e-3f);
            Assert.Greater(armored, bare);
            Assert.Greater(lifeChest, armored);
        }

        [Test]
        public void Attributes_CountAsTheCharacterGrows()
        {
            // Level 1 has no attribute points (they come from level 2); level 10 has Might, Agility and their armor.
            var equipment = new EquipmentState(Weapon(10)).With(ItemSlot.Chest, Chest(10));
            var attributes = CharacterAttributes.At(10);
            var plain = PowerScore.Evaluate(equipment, 1);
            var grown = PowerScore.Evaluate(equipment, 10);

            Assert.Greater(attributes.Might, 0);
            Assert.AreEqual(PowerScore.BaseAttacksPerSecond * (1f + attributes.AttackSpeed), PowerScore.AttacksPerSecond(equipment, 10), 1e-4f);
            Assert.Greater(grown.DamagePerSecond, plain.DamagePerSecond * (1f + attributes.IncreasedDamage) * (1f + attributes.AttackSpeed) * 0.999f);
        }

        [Test]
        public void ThePassiveTree_ChangesWhatAnItemIsWorth()
        {
            var equipment = new EquipmentState(Weapon(10)).With(ItemSlot.Chest, Chest(10));
            var damageAffix = Weapon(10, new AffixRoll(AffixId.IncreasedDamage, 5, 20f));
            var tree = new PassiveBonuses();
            tree.IncreasedDamage = 1f;
            // Increased damage adds up, so with +100 percent from the tree another 20 percent is worth less.
            Assert.Less(PowerScore.Change(equipment, damageAffix, 10, tree), PowerScore.Change(equipment, damageAffix, 10));

            var brutal = new PassiveBonuses();
            brutal.CriticalDamage = 50f;
            var critWeapon = Weapon(10, new AffixRoll(AffixId.CriticalChance, 5, 10f));
            // Bigger crits make crit chance worth more.
            Assert.Greater(PowerScore.Change(equipment, critWeapon, 10, brutal), PowerScore.Change(equipment, critWeapon, 10));

            var life = new PassiveBonuses();
            life.LifePercent = 0.1f;
            Assert.AreEqual(PowerScore.Evaluate(equipment, 10).EffectiveLife * 1.1f, PowerScore.Evaluate(equipment, 10, life).EffectiveLife, 1e-2f);
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
