using UnityEngine;
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

        [Test]
        public void AWokenEnemy_TakesASecondAndAHalf_ABossNone()
        {
            // The owner, 2026-10-18: "give the a reaction-time of 1.5 seconds".
            Assert.AreEqual(1.5f, EnemyRules.ReactionFor(EnemyRank.Normal));
            Assert.AreEqual(1.5f, EnemyRules.ReactionFor(EnemyRank.Champion));
            Assert.AreEqual(1.5f, EnemyRules.ReactionFor(EnemyRank.Elite));
            Assert.AreEqual(0f, EnemyRules.ReactionFor(EnemyRank.Boss));
        }

        [Test]
        public void InView_ThePortraitViewShape_SidesCloserThanAggro()
        {
            // The phone's view: about 5.2 world units either side, 11.2 up and down from its centre.
            var half = new Vector2(5.2f, 11.2f);
            Assert.IsTrue(EnemyRules.InView(new Vector2(4f, 0f), half));
            Assert.IsFalse(EnemyRules.InView(new Vector2(5f, 0f), half), "an enemy at the side edge is not yet in");
            Assert.IsFalse(EnemyRules.InView(new Vector2(-7f, 0f), half), "aggro range to the side is off screen");
            Assert.IsTrue(EnemyRules.InView(new Vector2(0f, 9.5f), half));
            Assert.IsFalse(EnemyRules.InView(new Vector2(0f, 10.2f), half), "its body must show under the top edge");
            Assert.IsTrue(EnemyRules.InView(new Vector2(0f, -10.6f), half));
            Assert.IsFalse(EnemyRules.InView(new Vector2(0f, -11f), half));
        }
    }
}
