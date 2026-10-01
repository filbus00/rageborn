using NUnit.Framework;

namespace ARPG.Tests
{
    /// <summary>The Wild Arrow's affixes 90 to 100 (Docs/03, built 2026-10-01).</summary>
    public class WildArrowAffixTests
    {
        static readonly AffixId[] New =
        {
            AffixId.ExtraPierce, AffixId.ForkChance, AffixId.BurnDamage, AffixId.DamagePerPierce, AffixId.FarDamage,
            AffixId.NearDamage, AffixId.ArrowSpeed, AffixId.PetDamage, AffixId.PetLife, AffixId.ExtraSkillArrow,
        };

        [Test]
        public void TheNewAffixes_HavePlayerFacingLabels()
        {
            foreach (var id in New)
                Assert.AreNotEqual(id.ToString(), ItemComparison.AffixLabel(id), $"{id} has no label");
        }

        [Test]
        public void FixedAffixes_AreAWholeOne_AtEveryTier()
        {
            for (var tier = AffixTable.BestTier; tier <= AffixTable.WorstTier; tier++)
            {
                Assert.AreEqual(1f, AffixRoller.RollValue(AffixId.ExtraPierce, tier, new System.Random(tier)));
                Assert.AreEqual(1f, AffixRoller.RollValue(AffixId.ExtraSkillArrow, tier, new System.Random(tier)));
            }
            Assert.IsFalse(ItemComparison.IsPercent(AffixId.ExtraPierce));
            Assert.IsTrue(ItemComparison.IsPercent(AffixId.ForkChance));
        }

        [Test]
        public void PierceAndSkillArrows_WaitForTheirItemLevel()
        {
            Assert.IsFalse(AffixTable.CanRollOn(AffixId.ExtraPierce, ItemSlot.Shield, 19));
            Assert.IsTrue(AffixTable.CanRollOn(AffixId.ExtraPierce, ItemSlot.Shield, 20));
            Assert.IsFalse(AffixTable.CanRollOn(AffixId.ExtraSkillArrow, ItemSlot.Gloves, 29));
            Assert.IsTrue(AffixTable.CanRollOn(AffixId.ExtraSkillArrow, ItemSlot.Gloves, 30));

            for (var seed = 0; seed < 300; seed++)
                foreach (var roll in AffixRoller.Roll(ItemSlot.Shield, ItemRarity.Rare, 12, new System.Random(seed)))
                    Assert.AreNotEqual(AffixId.ExtraPierce, roll.Id, "an item level 12 quiver cannot roll +1 pierce");
        }

        [Test]
        public void PointBlank_IsShortBowOnly_AndRangeIsLongbowOnly()
        {
            Assert.IsTrue(AffixTable.CanRollOn(AffixId.NearDamage, ItemSlot.Weapon));
            Assert.IsFalse(AffixTable.CanRollOn(AffixId.NearDamage, ItemSlot.TwoHandWeapon));
            Assert.IsTrue(AffixTable.CanRollOn(AffixId.FarDamage, ItemSlot.TwoHandWeapon));
            Assert.IsFalse(AffixTable.CanRollOn(AffixId.FarDamage, ItemSlot.Weapon));
        }

        [Test]
        public void TheNewAffixes_RollSomewhere_AndSumOverTheGear()
        {
            foreach (var id in New)
            {
                var found = false;
                foreach (ItemSlot slot in System.Enum.GetValues(typeof(ItemSlot)))
                    found |= AffixTable.CanRollOn(id, slot, 60);
                Assert.IsTrue(found, $"{id} rolls on no slot");
            }

            var amulet = new Item(ItemSlot.Amulet, ItemRarity.Magic, 10, new[] { new AffixRoll(AffixId.PetDamage, 5, 4f) });
            var belt = new Item(ItemSlot.Belt, ItemRarity.Magic, 10, new[] { new AffixRoll(AffixId.PetDamage, 5, 5f) });
            var equipment = EquipmentState.Empty.With(ItemSlot.Amulet, amulet).With(ItemSlot.Belt, belt);
            Assert.AreEqual(9f, equipment.AffixTotal(AffixId.PetDamage), 1e-4f);
        }
    }
}
