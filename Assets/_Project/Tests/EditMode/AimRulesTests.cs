using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class AimRulesTests
    {
        [Test]
        public void Aim_FallsWithThePush()
        {
            Assert.AreEqual(1f, AimRules.Aim(0f));
            Assert.AreEqual(1f, AimRules.Aim(0.3f));
            Assert.AreEqual(0.8f, AimRules.Aim(0.5f));
            Assert.AreEqual(0.8f, AimRules.Aim(0.66f));
            Assert.AreEqual(0.6f, AimRules.Aim(0.8f));
            Assert.AreEqual(0.6f, AimRules.Aim(1f));
        }

        [Test]
        public void PerfectAim_NeverStrays()
        {
            Assert.AreEqual(0f, AimRules.StrayDegrees(1f, 0f));
            Assert.AreEqual(0f, AimRules.StrayDegrees(1f, 1f));
        }

        [Test]
        public void WorseAim_StraysWider_AndBothWays()
        {
            Assert.Greater(AimRules.MaxStrayDegrees(0.6f), AimRules.MaxStrayDegrees(0.8f));
            Assert.AreEqual(-AimRules.MaxStrayDegrees(0.6f), AimRules.StrayDegrees(0.6f, 0f), 1e-4f);
            Assert.AreEqual(AimRules.MaxStrayDegrees(0.6f), AimRules.StrayDegrees(0.6f, 1f), 1e-4f);
            Assert.AreEqual(0f, AimRules.StrayDegrees(0.6f, 0.5f), 1e-4f);
        }

        [TestCase(0.8f)]
        [TestCase(0.6f)]
        public void AtFiveUnits_AnOrdinaryEnemyIsHit_AboutAimPercentOfTheTime(float aim)
        {
            // Body 0.3 and arrow 0.3: the arrow hits while it passes within 0.6 of the enemy 5 units away.
            var hits = 0;
            const int rolls = 1000;
            for (var i = 0; i < rolls; i++)
            {
                var angle = AimRules.StrayDegrees(aim, (i + 0.5f) / rolls) * Mathf.Deg2Rad;
                if (Mathf.Abs(Mathf.Tan(angle) * 5f) <= 0.6f)
                    hits++;
            }
            Assert.AreEqual(aim, hits / (float)rolls, 0.02f);
        }

        [Test]
        public void TheFiringArc_WidensWithPace_InsideItsBackground()
        {
            // The owner, 2026-10-17: the partial circle shows the firing radius for the current movement speed.
            var standing = AimCone.FiringHalfDegrees(AimRules.Aim(0f));
            var walking = AimCone.FiringHalfDegrees(AimRules.Aim(0.5f));
            var running = AimCone.FiringHalfDegrees(AimRules.Aim(1f));
            Assert.AreEqual(AimCone.MinimumHalfDegrees, standing, 1e-4f);
            Assert.Greater(walking, standing);
            Assert.Greater(running, walking);
            Assert.Less(running, AimCone.BackgroundHalfDegrees);
        }
    }
}
