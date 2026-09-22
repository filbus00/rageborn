using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ARPG.Tests
{
    public class AffixRollerTests
    {
        [Test]
        public void Common_RollsNoAffixes()
        {
            var random = new Random(1);

            var affixes = AffixRoller.Roll(ItemSlot.Weapon, ItemRarity.Common, 60, random);

            Assert.IsEmpty(affixes);
        }

        [Test]
        public void Magic_RollsOnePrefixAndOneSuffix()
        {
            var random = new Random(2);

            var affixes = AffixRoller.Roll(ItemSlot.Weapon, ItemRarity.Magic, 60, random);

            var prefixes = 0;
            var suffixes = 0;
            foreach (var roll in affixes)
                if (AffixTable.Get(roll.Id).Kind == AffixKind.Prefix) prefixes++; else suffixes++;

            Assert.AreEqual(1, prefixes);
            Assert.AreEqual(1, suffixes);
        }

        [Test]
        public void Rare_RollsTwoOrThreeOfEachKind_OnASlotWithEnoughEligibleAffixes()
        {
            // Weapon has 2 eligible prefixes and 4 eligible suffixes, so a Rare weapon can reach the docs' 2-3 range
            // for suffixes; prefixes cap at the pool size of 2.
            for (var seed = 0; seed < 200; seed++)
            {
                var affixes = AffixRoller.Roll(ItemSlot.Weapon, ItemRarity.Rare, 60, new Random(seed));
                var prefixes = 0;
                var suffixes = 0;
                foreach (var roll in affixes)
                    if (AffixTable.Get(roll.Id).Kind == AffixKind.Prefix) prefixes++; else suffixes++;

                Assert.GreaterOrEqual(prefixes, 1);
                Assert.LessOrEqual(prefixes, 2, "the weapon prefix pool only has 2 entries");
                Assert.GreaterOrEqual(suffixes, 2);
                Assert.LessOrEqual(suffixes, 3);
            }
        }

        [Test]
        public void RolledAffixes_AreNeverRepeated_OnOneItem()
        {
            for (var seed = 0; seed < 200; seed++)
            {
                var affixes = AffixRoller.Roll(ItemSlot.Weapon, ItemRarity.Rare, 60, new Random(seed));
                var seen = new HashSet<AffixId>();
                foreach (var roll in affixes)
                    Assert.IsTrue(seen.Add(roll.Id), $"seed {seed} repeated {roll.Id}");
            }
        }

        [Test]
        public void RolledAffixes_OnlyComeFromTheSlotsEligiblePool()
        {
            for (var seed = 0; seed < 100; seed++)
            {
                var affixes = AffixRoller.Roll(ItemSlot.Chest, ItemRarity.Rare, 60, new Random(seed));
                foreach (var roll in affixes)
                    Assert.IsTrue(AffixTable.CanRollOn(roll.Id, ItemSlot.Chest), $"{roll.Id} cannot roll on Chest");
            }
        }

        [Test]
        public void ChestAtRare_OnlyRollsItsTwoEligiblePrefixes_NoSuffixes()
        {
            // The curated pool has no chest-eligible suffix yet (Docs/03-itemization.md's full suffix pool, e.g.
            // resistances, is not built). Chest items are prefixes only until it grows.
            var affixes = AffixRoller.Roll(ItemSlot.Chest, ItemRarity.Rare, 60, new Random(3));

            foreach (var roll in affixes)
                Assert.AreEqual(AffixKind.Prefix, AffixTable.Get(roll.Id).Kind);
            Assert.LessOrEqual(affixes.Length, 2);
        }

        [Test]
        public void AtItemLevelOne_OnlyTierFiveIsUnlocked()
        {
            Assert.AreEqual(5, AffixTable.BestUnlockedTier(1));

            for (var seed = 0; seed < 50; seed++)
            {
                var affixes = AffixRoller.Roll(ItemSlot.Weapon, ItemRarity.Rare, 1, new Random(seed));
                foreach (var roll in affixes)
                    Assert.AreEqual(5, roll.Tier);
            }
        }

        [Test]
        public void AtItemLevelSixty_EveryTierIsUnlocked()
        {
            Assert.AreEqual(1, AffixTable.BestUnlockedTier(60));
        }

        [TestCase(1)]
        [TestCase(14)]
        [TestCase(15)]
        [TestCase(29)]
        [TestCase(30)]
        [TestCase(44)]
        [TestCase(45)]
        [TestCase(59)]
        [TestCase(60)]
        public void BestUnlockedTier_MatchesTheDocsGates(int itemLevel)
        {
            var expected = itemLevel >= 60 ? 1 : itemLevel >= 45 ? 2 : itemLevel >= 30 ? 3 : itemLevel >= 15 ? 4 : 5;

            Assert.AreEqual(expected, AffixTable.BestUnlockedTier(itemLevel));
        }

        [Test]
        public void RolledValues_FallWithinTheTiersScaledRange()
        {
            for (var seed = 0; seed < 300; seed++)
            {
                var affixes = AffixRoller.Roll(ItemSlot.Weapon, ItemRarity.Rare, 60, new Random(seed));
                foreach (var roll in affixes)
                {
                    var definition = AffixTable.Get(roll.Id);
                    var tierScale = AffixTable.GetTier(roll.Tier);
                    var min = definition.T1Min * tierScale.LowShare;
                    var max = definition.T1Max * tierScale.HighShare;

                    Assert.GreaterOrEqual(roll.Value, min, $"seed {seed} {roll.Id} below its tier range");
                    Assert.LessOrEqual(roll.Value, max, $"seed {seed} {roll.Id} above its tier range");
                }
            }
        }

        [Test]
        public void TheSameSeed_RollsTheSameAffixes()
        {
            var a = AffixRoller.Roll(ItemSlot.Weapon, ItemRarity.Rare, 45, new Random(99));
            var b = AffixRoller.Roll(ItemSlot.Weapon, ItemRarity.Rare, 45, new Random(99));

            Assert.AreEqual(a.Length, b.Length);
            for (var i = 0; i < a.Length; i++)
            {
                Assert.AreEqual(a[i].Id, b[i].Id);
                Assert.AreEqual(a[i].Tier, b[i].Tier);
                Assert.AreEqual(a[i].Value, b[i].Value, 1e-6f);
            }
        }
    }
}
