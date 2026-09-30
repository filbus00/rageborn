using NUnit.Framework;

namespace ARPG.Tests
{
    public class FocusPoolTests
    {
        [Test]
        public void StartsAtTheGivenValue_ClampedToTheRange()
        {
            Assert.AreEqual(40f, new FocusPool(100f, 6f, 40f).Current, 1e-5f);
            Assert.AreEqual(100f, new FocusPool(100f, 6f, 500f).Current, 1e-5f);
            Assert.AreEqual(0f, new FocusPool(100f, 6f, -5f).Current, 1e-5f);
        }

        [Test]
        public void Tick_RegeneratesOverTime_AndStopsAtMax()
        {
            var pool = new FocusPool(100f, 6f, 0f);

            pool.Tick(2f);
            Assert.AreEqual(12f, pool.Current, 1e-4f);

            pool.Tick(1000f);
            Assert.AreEqual(100f, pool.Current, 1e-4f);
        }

        [Test]
        public void Gain_AddsButNeverExceedsMax()
        {
            var pool = new FocusPool(100f, 6f, 95f);

            pool.Gain(4f);
            Assert.AreEqual(99f, pool.Current, 1e-5f);

            pool.Gain(4f);
            Assert.AreEqual(100f, pool.Current, 1e-5f);
        }

        [Test]
        public void TrySpend_TakesTheCost_WhenAffordable()
        {
            var pool = new FocusPool(100f, 6f, 30f);

            Assert.IsTrue(pool.TrySpend(15f));
            Assert.AreEqual(15f, pool.Current, 1e-5f);
        }

        [Test]
        public void TrySpend_ChangesNothing_WhenTooExpensive()
        {
            var pool = new FocusPool(100f, 6f, 10f);

            Assert.IsFalse(pool.TrySpend(15f));
            Assert.AreEqual(10f, pool.Current, 1e-5f);
        }

        [Test]
        public void TrySpend_AllowsSpendingExactlyTheBalance()
        {
            var pool = new FocusPool(100f, 6f, 15f);

            Assert.IsTrue(pool.TrySpend(15f));
            Assert.AreEqual(0f, pool.Current, 1e-5f);
        }

        [Test]
        public void Defaults_FollowTheDocs_StartingFull()
        {
            var pool = new FocusPool();
            Assert.AreEqual(100f, pool.Current, 1e-5f, "full on every level load");
            Assert.IsTrue(pool.TrySpend(100f));
            pool.Tick(1f);
            Assert.AreEqual(6f, pool.Current, 1e-4f, "6 a second");
        }

        [Test]
        public void Gain_CountsAsCombat_AndRegenDoesNot()
        {
            var pool = new FocusPool(start: 0f);
            Assert.IsFalse(pool.InCombat);
            pool.Tick(1f);
            Assert.IsFalse(pool.InCombat, "regeneration is not combat");
            pool.Gain(FocusPool.PerBasicHit);
            Assert.IsTrue(pool.InCombat);
            pool.Tick(FocusPool.CombatSeconds + 0.1f);
            Assert.IsFalse(pool.InCombat);
        }

        [Test]
        public void Multipliers_ScaleRegenAndGainSeparately()
        {
            var pool = new FocusPool(start: 0f) { RegenMultiplier = 1.5f, GainMultiplier = 2f };
            pool.Tick(1f);
            Assert.AreEqual(9f, pool.Current, 1e-4f);
            pool.Gain(4f);
            Assert.AreEqual(17f, pool.Current, 1e-4f);
        }
    }
}
