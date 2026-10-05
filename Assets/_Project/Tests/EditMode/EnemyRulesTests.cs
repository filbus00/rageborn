using NUnit.Framework;

namespace ARPG.Tests
{
    /// <summary>When an idle enemy wakes (EnemyRules.Wakes).</summary>
    public class EnemyRulesTests
    {
        [Test]
        public void Wakes_InSightWithinAggroRange()
        {
            Assert.IsTrue(EnemyRules.Wakes(5f, 5f, 7f));
            Assert.IsFalse(EnemyRules.Wakes(8f, 8f, 7f));
        }

        [Test]
        public void BehindAWall_WakesOnlyWithAShortRoute()
        {
            Assert.IsTrue(EnemyRules.Wakes(5f, 10f, 7f), "a short way round the corner");
            Assert.IsFalse(EnemyRules.Wakes(5f, 20f, 7f), "the next room, a long way round");
            Assert.IsFalse(EnemyRules.Wakes(3f, float.PositiveInfinity, 7f), "no route at all");
        }

        [Test]
        public void AWolf_LungesFromThreeToFiveAway_WhenItSeesHerAndIsReady()
        {
            Assert.IsTrue(EnemyRules.ShouldLunge(4f, 3f, 4f, true, 0f));
            Assert.IsTrue(EnemyRules.ShouldLunge(5f, 3f, 4f, true, 0f));
            Assert.IsFalse(EnemyRules.ShouldLunge(2f, 3f, 4f, true, 0f), "too close: it bites");
            Assert.IsFalse(EnemyRules.ShouldLunge(5.5f, 3f, 4f, true, 0f), "too far");
            Assert.IsFalse(EnemyRules.ShouldLunge(4f, 3f, 4f, false, 0f), "no sight");
            Assert.IsFalse(EnemyRules.ShouldLunge(4f, 3f, 4f, true, 1f), "cooling down");
            Assert.IsFalse(EnemyRules.ShouldLunge(4f, 3f, 0f, true, 0f), "not a charger");
        }

        [Test]
        public void Auras_DoNotStack()
        {
            Assert.AreEqual(1f, EnemyRules.AuraMultiplier(0f), 1e-6f);
            Assert.AreEqual(1.2f, EnemyRules.AuraMultiplier(0.2f), 1e-6f);
        }
    }
}
