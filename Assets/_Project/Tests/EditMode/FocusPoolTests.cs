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
    }
}
