using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class DodgeTests
    {
        [Test]
        public void Charges_TwoThenEmpty_OneBackEveryThreeSeconds()
        {
            var charges = new DodgeCharges();
            Assert.IsTrue(charges.TryUse());
            Assert.IsTrue(charges.TryUse());
            Assert.IsFalse(charges.TryUse(), "two charges");
            charges.Tick(DodgeRules.RechargeSeconds - 0.1f);
            Assert.AreEqual(0, charges.Charges);
            charges.Tick(0.2f);
            Assert.AreEqual(1, charges.Charges);
            charges.Tick(DodgeRules.RechargeSeconds);
            Assert.AreEqual(2, charges.Charges);
            charges.Tick(10f);
            Assert.AreEqual(DodgeRules.MaxCharges, charges.Charges, "never past the most");
        }

        [Test]
        public void AQuickFlick_IsAFlick_OncePerTripOut()
        {
            var flick = new FlickDetector();
            flick.Begin(0f);
            Assert.IsFalse(flick.Feed(new Vector2(0.6f, 0f), 0.05f));
            Assert.IsTrue(flick.Feed(new Vector2(1.2f, 0f), 0.1f));
            Assert.IsFalse(flick.Feed(new Vector2(1.4f, 0f), 0.12f), "one flick per trip out");
            Assert.IsFalse(flick.Feed(new Vector2(0.1f, 0f), 0.5f));
            Assert.IsTrue(flick.Feed(new Vector2(0f, -1.1f), 0.6f), "back near the middle, a new flick");
        }

        [Test]
        public void ASlowPush_PastTheRing_IsNoFlick()
        {
            var flick = new FlickDetector();
            flick.Begin(0f);
            Assert.IsFalse(flick.Feed(new Vector2(0.5f, 0f), 0.2f));
            Assert.IsFalse(flick.Feed(new Vector2(0.9f, 0f), 0.4f));
            Assert.IsFalse(flick.Feed(new Vector2(1.3f, 0f), 0.6f));
        }

        [Test]
        public void TheDive_CoversItsDistance_InItsTime()
        {
            Assert.AreEqual(DodgeRules.Distance, DodgeRules.Speed * DodgeRules.Seconds, 1e-4f);
            Assert.LessOrEqual(DodgeRules.UntouchableSeconds, DodgeRules.Seconds);
            Assert.GreaterOrEqual(DodgeRules.AnimationSeconds, DodgeRules.Seconds, "she gets up after the travel");
        }
    }
}
