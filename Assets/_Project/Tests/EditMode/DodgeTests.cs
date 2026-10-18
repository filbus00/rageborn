using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class DodgeTests
    {
        [Test]
        public void TheDive_IsReadyAgainHalfASecondAfterIt()
        {
            // The owner, 2026-10-18: "Just put the dive on 0.5 sec reload timer".
            var charges = new DodgeCharges();
            Assert.IsTrue(charges.TryUse());
            Assert.IsFalse(charges.TryUse(), "one dive at a time");
            charges.Tick(DodgeRules.RechargeSeconds - 0.05f);
            Assert.AreEqual(0, charges.Charges);
            charges.Tick(0.1f);
            Assert.AreEqual(1, charges.Charges);
            Assert.AreEqual(0.5f, DodgeRules.RechargeSeconds, 1e-5f);
            charges.Tick(10f);
            Assert.AreEqual(DodgeRules.MaxCharges, charges.Charges, "never past the most");
        }

        [Test]
        public void LeavingTheRing_DivesOnlyInAFight_AndOnlyOnTheWayOut()
        {
            Assert.IsTrue(DodgeRules.Triggers(true, false, true));
            Assert.IsFalse(DodgeRules.Triggers(true, false, false), "out of a fight she only runs");
            Assert.IsFalse(DodgeRules.Triggers(true, true, true), "once per trip out of the ring");
            Assert.IsFalse(DodgeRules.Triggers(false, true, true));
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
