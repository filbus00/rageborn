using NUnit.Framework;

namespace ARPG.Tests
{
    public class LifePoolTests
    {
        [Test]
        public void StartsFull()
        {
            var life = new LifePool(100f);

            Assert.AreEqual(100f, life.Current, 1e-5f);
            Assert.AreEqual(1f, life.Fraction, 1e-5f);
            Assert.IsFalse(life.IsDead);
        }

        [Test]
        public void TakeDamage_ReducesLife_AndReportsAKillOnlyOnTheKillingHit()
        {
            var life = new LifePool(100f);

            Assert.IsFalse(life.TakeDamage(60f));
            Assert.AreEqual(40f, life.Current, 1e-5f);

            Assert.IsTrue(life.TakeDamage(60f));
            Assert.AreEqual(0f, life.Current, 1e-5f);
            Assert.IsTrue(life.IsDead);
        }

        [Test]
        public void TakeDamage_OnADeadPool_ReportsNoSecondKill()
        {
            var life = new LifePool(10f);
            life.TakeDamage(50f);

            Assert.IsFalse(life.TakeDamage(50f));
            Assert.AreEqual(0f, life.Current, 1e-5f);
        }

        [Test]
        public void TakeDamage_IgnoresNegativeAmounts()
        {
            var life = new LifePool(100f);

            life.TakeDamage(-30f);

            Assert.AreEqual(100f, life.Current, 1e-5f);
        }

        [Test]
        public void Heal_AddsButNeverExceedsMax()
        {
            var life = new LifePool(100f);
            life.TakeDamage(70f);

            life.Heal(20f);
            Assert.AreEqual(50f, life.Current, 1e-5f);

            life.Heal(500f);
            Assert.AreEqual(100f, life.Current, 1e-5f);
        }

        [Test]
        public void Heal_DoesNotReviveTheDead()
        {
            var life = new LifePool(100f);
            life.TakeDamage(500f);

            life.Heal(50f);

            Assert.IsTrue(life.IsDead);
        }

        [Test]
        public void SetFraction_ClampsAndScales()
        {
            var life = new LifePool(200f);

            life.SetFraction(0.25f);
            Assert.AreEqual(50f, life.Current, 1e-4f);

            life.SetFraction(3f);
            Assert.AreEqual(200f, life.Current, 1e-4f);

            life.SetFraction(-1f);
            Assert.AreEqual(0f, life.Current, 1e-4f);
        }

        [Test]
        public void SetFraction_CanReviveAfterDeath()
        {
            var life = new LifePool(100f);
            life.TakeDamage(500f);

            life.SetFraction(1f);

            Assert.IsFalse(life.IsDead);
            Assert.AreEqual(100f, life.Current, 1e-4f);
        }
    }
}
