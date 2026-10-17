using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>Momentum shots (2026-10-17; cover was removed the same day).</summary>
    public class MomentumShotTests
    {
        [Test]
        public void Momentum_SpeedsHerShots_AndSendsArrowsFurther()
        {
            Assert.AreEqual(0f, MomentumShots.AttackSpeed(0));
            Assert.AreEqual(0.3f, MomentumShots.AttackSpeed(StanceStacks.MaxStacks), 1e-5f);
            Assert.AreEqual(1.2f, MomentumShots.ArrowFactor(StanceStacks.MaxStacks), 1e-5f);
        }
    }
}
