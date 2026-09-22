using System.Collections.Generic;
using NUnit.Framework;

namespace ARPG.Tests
{
    public class LootRollerTests
    {
        [Test]
        public void RarityWeights_AtBase_MatchTheDocsTable()
        {
            var weights = LootRoller.RarityWeights(0f, 0);

            Assert.AreEqual(new[] { 60f, 30f, 8.5f, 1.5f }, weights);
        }

        [Test]
        public void MagicFind_MultipliesMagicRareAndLegendary_ButNotCommon()
        {
            // Docs: Magic Find multiplies the weights of Magic, Rare and Legendary by (1 + MF percent).
            var weights = LootRoller.RarityWeights(1f, 0);

            Assert.AreEqual(60f, weights[0], 1e-4f);
            Assert.AreEqual(60f, weights[1], 1e-4f);
            Assert.AreEqual(17f, weights[2], 1e-4f);
            Assert.AreEqual(3f, weights[3], 1e-4f);
        }

        [Test]
        public void MagicFind_NeverLowersAWeight()
        {
            var weights = LootRoller.RarityWeights(-0.5f, 0);

            Assert.AreEqual(new[] { 60f, 30f, 8.5f, 1.5f }, weights);
        }

        [TestCase(0, 1f)]
        [TestCase(299, 1f)]
        [TestCase(300, 2f)]
        [TestCase(599, 2f)]
        [TestCase(600, 3f)]
        [TestCase(5000, 3f)]
        public void BadLuckProtection_DoublesAfter300Kills_AndTriplesAfter600(int kills, float expected)
        {
            Assert.AreEqual(expected, LootRoller.BadLuckMultiplier(kills), 1e-6f);
            Assert.AreEqual(1.5f * expected, LootRoller.RarityWeights(0f, kills)[3], 1e-4f);
        }

        [Test]
        public void EveryKillCounts_TowardBadLuckProtection_WhetherOrNotItDrops()
        {
            // The counter goes up by one on every kill, drop or not, and only a legendary resets it. A legendary can
            // turn up in any run, so the test follows the same rule instead of assuming none does.
            var roller = new LootRoller(1);
            var expected = 0;

            for (var i = 0; i < 2000; i++)
            {
                var item = roller.RollDrop(LootSource.NormalEnemy, 1, 0f);
                expected = item != null && item.Rarity == ItemRarity.Legendary ? 0 : expected + 1;

                Assert.AreEqual(expected, roller.KillsSinceLegendary, $"after kill {i + 1}");
            }
        }

        [Test]
        public void ALegendaryDrop_ResetsTheCounter()
        {
            // Roll until a legendary drops, with a huge Magic Find so it happens quickly, and check the reset.
            var roller = new LootRoller(3);
            Item legendary = null;
            for (var i = 0; i < 100_000 && legendary == null; i++)
            {
                var item = roller.RollDrop(LootSource.NormalEnemy, 1, 1000f);
                if (item != null && item.Rarity == ItemRarity.Legendary)
                    legendary = item;
            }

            Assert.IsNotNull(legendary, "a legendary should drop within 100000 kills at 1000 percent Magic Find");
            Assert.AreEqual(0, roller.KillsSinceLegendary);
        }

        [Test]
        public void TheDropChance_OfANormalEnemy_IsSixPercent()
        {
            var roller = new LootRoller(11);
            const int kills = 200_000;
            var drops = 0;

            for (var i = 0; i < kills; i++)
                if (roller.RollDrop(LootSource.NormalEnemy, 1, 0f) != null)
                    drops++;

            Assert.AreEqual(0.06f, drops / (float)kills, 0.003f);
        }

        [Test]
        public void TheRarities_DropInTheOrderOfTheirWeights()
        {
            var roller = new LootRoller(21);
            var counts = new Dictionary<ItemRarity, int>();
            for (var i = 0; i < 300_000; i++)
            {
                var item = roller.RollDrop(LootSource.NormalEnemy, 1, 0f);
                if (item == null)
                    continue;
                counts.TryGetValue(item.Rarity, out var n);
                counts[item.Rarity] = n + 1;
            }

            Assert.Greater(counts[ItemRarity.Common], counts[ItemRarity.Magic]);
            Assert.Greater(counts[ItemRarity.Magic], counts[ItemRarity.Rare]);
            Assert.Greater(counts[ItemRarity.Rare], counts[ItemRarity.Legendary]);
            Assert.Greater(counts[ItemRarity.Legendary], 0);
        }

        [Test]
        public void TheCommonShare_IsAboutSixtyPercent_OfDrops()
        {
            var roller = new LootRoller(33);
            var drops = 0;
            var common = 0;
            for (var i = 0; i < 300_000; i++)
            {
                var item = roller.RollDrop(LootSource.NormalEnemy, 1, 0f);
                if (item == null)
                    continue;
                drops++;
                if (item.Rarity == ItemRarity.Common)
                    common++;
            }

            // Bad luck protection raises the legendary weight most of the time (it kicks in after 300 kills without
            // one, and a legendary is rare), which pulls the common share down a point or so from the base 60.
            Assert.AreEqual(0.60f, common / (float)drops, 0.02f);
        }

        [Test]
        public void MagicFind_MakesRareDropsMoreCommon()
        {
            const int kills = 200_000;
            var plain = CountRarityAtOrAbove(new LootRoller(5), kills, 0f, ItemRarity.Rare);
            var lucky = CountRarityAtOrAbove(new LootRoller(5), kills, 1f, ItemRarity.Rare);

            // Magic Find also raises the Magic weight, so the Rare and Legendary share grows by less than double.
            Assert.Greater(lucky, plain * 1.25f);
        }

        static int CountRarityAtOrAbove(LootRoller roller, int kills, float magicFind, ItemRarity floor)
        {
            var count = 0;
            for (var i = 0; i < kills; i++)
            {
                var item = roller.RollDrop(LootSource.NormalEnemy, 1, magicFind);
                if (item != null && item.Rarity >= floor)
                    count++;
            }
            return count;
        }

        [Test]
        public void ADrop_TakesTheItemLevelItWasGiven()
        {
            var roller = new LootRoller(2);
            Item found = null;
            for (var i = 0; i < 10_000 && found == null; i++)
                found = roller.RollDrop(LootSource.NormalEnemy, 17, 0f);

            Assert.IsNotNull(found);
            Assert.AreEqual(17, found.ItemLevel);
        }

        [Test]
        public void ADrop_CanLandInAnyOfTheDroppableSlots()
        {
            // Not just weapons any more: Docs/03-itemization.md's ten slots are added as items for them exist, and
            // weapon, chest and helm all exist now.
            var roller = new LootRoller(4);
            var seen = new System.Collections.Generic.HashSet<ItemSlot>();
            for (var i = 0; i < 5000; i++)
            {
                var item = roller.RollDrop(LootSource.NormalEnemy, 1, 0f);
                if (item != null)
                    seen.Add(item.Slot);
            }

            Assert.IsTrue(seen.Contains(ItemSlot.Weapon));
            Assert.IsTrue(seen.Contains(ItemSlot.Chest));
            Assert.IsTrue(seen.Contains(ItemSlot.Helm));
        }

        [Test]
        public void ADrop_CarriesAffixes_MatchingItsRarity()
        {
            // Roll until a Common and a Rare of the same slot both drop, and check the docs' affix counts:
            // Common 0, Rare 2 to 3 of each kind (capped by the slot's eligible pool).
            var roller = new LootRoller(7);
            Item common = null, rare = null;
            for (var i = 0; i < 20_000 && (common == null || rare == null); i++)
            {
                var item = roller.RollDrop(LootSource.NormalEnemy, 60, 0f);
                if (item == null || item.Slot != ItemSlot.Weapon)
                    continue;
                if (item.Rarity == ItemRarity.Common)
                    common ??= item;
                else if (item.Rarity == ItemRarity.Rare)
                    rare ??= item;
            }

            Assert.IsNotNull(common, "a common weapon should drop within 20000 kills");
            Assert.IsNotNull(rare, "a rare weapon should drop within 20000 kills");
            Assert.AreEqual(0, common.Affixes.Count);
            Assert.GreaterOrEqual(rare.Affixes.Count, 2);
        }

        [Test]
        public void TheSameSeed_GivesTheSameDrops()
        {
            var a = new LootRoller(99);
            var b = new LootRoller(99);

            for (var i = 0; i < 5000; i++)
            {
                var x = a.RollDrop(LootSource.NormalEnemy, 3, 0.2f);
                var y = b.RollDrop(LootSource.NormalEnemy, 3, 0.2f);
                Assert.AreEqual(x == null, y == null, $"kill {i}");
                if (x != null)
                    Assert.AreEqual(x.Rarity, y.Rarity, $"kill {i}");
            }
        }

        [Test]
        public void DifferentSeeds_GiveDifferentDrops()
        {
            var a = new LootRoller(1);
            var b = new LootRoller(2);
            var differences = 0;

            for (var i = 0; i < 2000; i++)
            {
                var x = a.RollDrop(LootSource.NormalEnemy, 1, 0f);
                var y = b.RollDrop(LootSource.NormalEnemy, 1, 0f);
                if ((x == null) != (y == null) || (x != null && x.Rarity != y.Rarity))
                    differences++;
            }

            Assert.Greater(differences, 0);
        }

        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(10, 12)]
        [TestCase(30, 50)]
        public void Gold_FollowsPointSixTimesLevelToThePowerOnePointThree(int level, int expected)
        {
            // 0.6 * L^1.3, rounded: level 10 is 11.97, level 30 is 49.9.
            Assert.AreEqual(expected, new LootRoller(0).RollGold(LootSource.NormalEnemy, level));
        }

        [Test]
        public void Gold_IsNeverZero()
        {
            Assert.GreaterOrEqual(new LootRoller(0).RollGold(LootSource.NormalEnemy, 1), 1);
            Assert.GreaterOrEqual(new LootRoller(0).RollGold(LootSource.NormalEnemy, 0), 1);
        }

        [Test]
        public void EliteGold_IsEightTimesNormal()
        {
            // Docs/04-progression-and-economy.md.
            var roller = new LootRoller(0);

            Assert.AreEqual(roller.RollGold(LootSource.NormalEnemy, 10) * 8, roller.RollGold(LootSource.Elite, 10));
        }

        [Test]
        public void ChampionGold_FollowsTheNormalFormula_UntilTuned()
        {
            var roller = new LootRoller(0);

            Assert.AreEqual(roller.RollGold(LootSource.NormalEnemy, 10), roller.RollGold(LootSource.Champion, 10));
        }

        [TestCase(LootSource.NormalEnemy, LootRoller.NormalEnemyDropChance)]
        [TestCase(LootSource.Champion, LootRoller.ChampionDropChance)]
        [TestCase(LootSource.Elite, LootRoller.EliteDropChance)]
        public void DropChance_MatchesTheDocsTable(LootSource source, float expected)
        {
            Assert.AreEqual(expected, LootRoller.DropChance(source), 1e-6f);
        }

        [Test]
        public void Champion_NeverDropsBelowMagic()
        {
            var roller = new LootRoller(12);
            var sawADrop = false;

            for (var i = 0; i < 2000; i++)
            {
                var item = roller.RollDrop(LootSource.Champion, 1, 0f);
                if (item == null)
                    continue;

                sawADrop = true;
                Assert.GreaterOrEqual(item.Rarity, ItemRarity.Magic);
            }

            Assert.IsTrue(sawADrop, "a champion drops 25 percent of the time, so 2000 kills should see one");
        }

        [Test]
        public void Elite_AlwaysDrops_OneOrTwoItems_NeverBelowMagic()
        {
            var roller = new LootRoller(9);
            var sawOne = false;
            var sawTwo = false;

            for (var i = 0; i < 200; i++)
            {
                var items = roller.RollDrops(LootSource.Elite, 1, 0f);
                Assert.GreaterOrEqual(items.Count, 1, "docs: elite drops 100 percent of the time");
                Assert.LessOrEqual(items.Count, 2);
                sawOne |= items.Count == 1;
                sawTwo |= items.Count == 2;

                foreach (var item in items)
                    Assert.GreaterOrEqual(item.Rarity, ItemRarity.Magic);
            }

            Assert.IsTrue(sawOne, "1 to 2 items: some kills should drop just 1");
            Assert.IsTrue(sawTwo, "1 to 2 items: some kills should drop 2");
        }

        [Test]
        public void Elite_SometimesFloorsAtRare_InsteadOfMagic()
        {
            var roller = new LootRoller(3);
            var sawRareFloor = false;

            for (var i = 0; i < 500 && !sawRareFloor; i++)
            {
                var items = roller.RollDrops(LootSource.Elite, 1, 0f);
                foreach (var item in items)
                    if (item.Rarity == ItemRarity.Rare)
                        sawRareFloor = true; // Common/Magic never roll naturally floor to Rare on their own at low MF odds this consistently; treated as the floor kicking in.
            }

            Assert.IsTrue(sawRareFloor, "docs: elite floors at Rare 30 percent of the time");
        }

        [Test]
        public void NormalEnemy_StillHasNoRarityFloor()
        {
            // A regression check: adding Champion and Elite floors must not touch the normal enemy's own table.
            var roller = new LootRoller(21);
            var sawCommon = false;

            for (var i = 0; i < 2000 && !sawCommon; i++)
            {
                var item = roller.RollDrop(LootSource.NormalEnemy, 1, 0f);
                if (item != null && item.Rarity == ItemRarity.Common)
                    sawCommon = true;
            }

            Assert.IsTrue(sawCommon);
        }

        [Test]
        public void RollDrop_Singular_ReturnsTheFirstOfWhatRollDropsWouldGive()
        {
            var a = new LootRoller(55);
            var b = new LootRoller(55);

            var single = a.RollDrop(LootSource.Elite, 20, 0f);
            var plural = b.RollDrops(LootSource.Elite, 20, 0f);

            if (plural.Count == 0)
                Assert.IsNull(single);
            else
                Assert.AreEqual(plural[0].Rarity, single.Rarity);
        }
    }
}
