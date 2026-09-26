using NUnit.Framework;

namespace ARPG.Tests
{
    public class BossTests
    {
        [TestCase(1f, 1)]
        [TestCase(0.67f, 1)]
        [TestCase(0.66f, 2)]
        [TestCase(0.34f, 2)]
        [TestCase(0.33f, 3)]
        [TestCase(0.01f, 3)]
        public void Phases_SplitTheLifeBarInThirds(float lifeFraction, int phase)
        {
            Assert.AreEqual(phase, BossPhases.PhaseFor(lifeFraction));
        }

        [Test]
        public void Stagger_FillsFromHitCount_NotDamage_AndStopsTheBossWhenFull()
        {
            var meter = new StaggerMeter();
            var hitsToFill = UnityEngine.Mathf.CeilToInt(StaggerMeter.Full / StaggerMeter.PerHit);

            for (var i = 0; i < hitsToFill - 1; i++)
                Assert.IsFalse(meter.AddHit());
            Assert.IsFalse(meter.IsStaggered);

            Assert.IsTrue(meter.AddHit(), "the filling hit reports the stagger");
            Assert.IsTrue(meter.IsStaggered);
            Assert.AreEqual(1f, meter.Fraction);
        }

        [Test]
        public void Stagger_LastsItsTime_ThenTheMeterStartsEmpty()
        {
            var meter = new StaggerMeter();
            while (!meter.AddHit())
            {
            }

            meter.Tick(StaggerMeter.StaggerSeconds - 0.1f);
            Assert.IsTrue(meter.IsStaggered);
            Assert.IsFalse(meter.AddHit(), "hits during a stagger do not count toward the next");

            meter.Tick(0.2f);
            Assert.IsFalse(meter.IsStaggered);
            Assert.AreEqual(0f, meter.Value);
        }

        [Test]
        public void ABoss_GivesSixtyTimesNormalXp()
        {
            Assert.AreEqual(480, Experience.KillXp(1, 1, EnemyRank.Boss));
        }

        [Test]
        public void ABoss_AlwaysDropsFourToSixItems_RareOrBetter()
        {
            var roller = new LootRoller(11);
            for (var i = 0; i < 200; i++)
            {
                var items = roller.RollDrops(LootSource.Boss, 12, 0f);
                Assert.That(items.Count, Is.InRange(LootRoller.MinBossItems, LootRoller.MaxBossItems));
                foreach (var item in items)
                    Assert.GreaterOrEqual(item.Rarity, ItemRarity.Rare);
            }
        }

        [Test]
        public void ABoss_DropsALegendary_AboutAThirdOfTheTime()
        {
            var roller = new LootRoller(21);
            var withLegendary = 0;
            const int kills = 2000;
            for (var i = 0; i < kills; i++)
            {
                foreach (var item in roller.RollDrops(LootSource.Boss, 12, 0f))
                    if (item.Rarity == ItemRarity.Legendary)
                    {
                        withLegendary++;
                        break;
                    }
            }

            // The docs' 35 percent chance, plus the ordinary legendary weight on the other items.
            var share = (float)withLegendary / kills;
            Assert.That(share, Is.InRange(0.35f, 0.5f));
        }

        [Test]
        public void BossGold_IsTwentyTimesTheNormalFormula()
        {
            var roller = new LootRoller(0);

            // 0.6 x 10^1.3 = 11.97; twenty times that is 239.4, rounded once at the end.
            Assert.AreEqual(239, roller.RollGold(LootSource.Boss, 10));
        }
    }
}
