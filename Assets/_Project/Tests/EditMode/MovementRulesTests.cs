using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>The movement fixes of 2026-11-15: diagonals, responsiveness, the root, the arrows' speed.</summary>
    public class MovementRulesTests
    {
        static float ScreenSpeed(Vector2 stick, float blend)
        {
            var ground = IsoMath.StickToGround(stick);
            return IsoMath.GroundToWorld(ground * MovementRules.DirectionFactor(ground, blend)).magnitude;
        }

        [Test]
        public void OnTheScreen_SidewaysIsFastest_UpAndDownSlowest_WithoutTheFix()
        {
            Assert.AreEqual(1f, ScreenSpeed(Vector2.right, 0f), 1e-4f);
            Assert.AreEqual(0.5f, ScreenSpeed(Vector2.up, 0f), 1e-4f);
            var diagonal = ScreenSpeed(new Vector2(1f, 1f).normalized, 0f);
            Assert.Greater(diagonal, 0.5f);
            Assert.Less(diagonal, 1f);
        }

        [Test]
        public void HalfWay_RaisesDiagonalsAndUpAndDown_ButNotSideways()
        {
            Assert.AreEqual(1f, ScreenSpeed(Vector2.right, MovementRules.ScreenSpeedBlend), 1e-4f);
            Assert.AreEqual(0.707f, ScreenSpeed(Vector2.up, MovementRules.ScreenSpeedBlend), 1e-3f);
            Assert.AreEqual(0.707f, ScreenSpeed(Vector2.down, MovementRules.ScreenSpeedBlend), 1e-3f);
            var diagonal = ScreenSpeed(new Vector2(1f, 1f).normalized, MovementRules.ScreenSpeedBlend);
            Assert.AreEqual(0.795f, diagonal, 1e-3f);
            Assert.Greater(diagonal, ScreenSpeed(new Vector2(1f, 1f).normalized, 0f));
        }

        [Test]
        public void AnEvenSpeedOnScreen_IsWhatABlendOfOneGives()
        {
            foreach (var stick in new[] { Vector2.right, Vector2.up, new Vector2(1f, 1f).normalized, new Vector2(2f, -1f).normalized })
                Assert.AreEqual(1f, ScreenSpeed(stick, 1f), 1e-3f);
        }

        [Test]
        public void TheFactor_NeverShrinksHerSpeed_AndIsAtMostRootTwo()
        {
            for (var degrees = 0; degrees < 360; degrees += 15)
            {
                var d = new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));
                var factor = MovementRules.DirectionFactor(d);
                Assert.GreaterOrEqual(factor, 1f - 1e-4f, degrees.ToString());
                Assert.LessOrEqual(factor, Mathf.Sqrt(2f) + 1e-3f, degrees.ToString());
            }
            Assert.AreEqual(1f, MovementRules.DirectionFactor(Vector2.zero));
        }

        [Test]
        public void ARoot_IsAStrongSlow_NotAnOrdinaryOne()
        {
            Assert.IsTrue(MovementRules.IsRooted(true, 0.05f));
            Assert.IsFalse(MovementRules.IsRooted(true, 0.5f), "frozen is a slow");
            Assert.IsFalse(MovementRules.IsRooted(true, 0.6f));
            Assert.IsFalse(MovementRules.IsRooted(false, 0.05f), "over");
        }

        [Test]
        public void HerVelocityChangesAtTheSameRate_WhateverSlowsSheHas()
        {
            const float unslowed = 4.4f;
            var free = MovementRules.Acceleration(unslowed, unslowed, 0.08f);
            Assert.AreEqual(free, MovementRules.Acceleration(unslowed, unslowed * 0.65f, 0.08f), 1e-4f, "a shot's walk");
            Assert.AreEqual(free, MovementRules.Acceleration(unslowed, unslowed * 0.05f, 0.08f), 1e-4f, "a root");
            Assert.Greater(MovementRules.Acceleration(unslowed, unslowed * 1.25f, 0.08f), free, "faster with Momentum");
        }

        [Test]
        public void ARoot_StopsHerWithinAFrameOfSpeed_NotOverASecond()
        {
            // The old rule: 5.5 a second run down by 0.05 of the rate per step took about a second.
            var old = 5.5f;
            var steps = 0;
            var rate = (4.4f * 0.05f) / 0.08f * 0.02f;
            while (old > 0.2f && steps < 1000)
            {
                old = Mathf.MoveTowards(old, 0f, rate);
                steps++;
            }
            Assert.Greater(steps * 0.02f, 1f, "it did glide");
            var fixedRate = MovementRules.Acceleration(4.4f, 4.4f * 0.05f, 0.08f) * 0.02f;
            var speed = 5.5f;
            steps = 0;
            while (speed > 0.2f && steps < 1000)
            {
                speed = Mathf.MoveTowards(speed, 0f, fixedRate);
                steps++;
            }
            Assert.Less(steps * 0.02f, 0.12f, "and does not now");
        }

        [Test]
        public void TheArrows_Fly30PercentFaster()
        {
            Assert.AreEqual(14f, PlayerCombat.BaseArrowSpeed);
            Assert.AreEqual(18.2f, PlayerCombat.ArrowSpeed, 1e-4f);
            Assert.AreEqual(1.3f, PlayerCombat.ArrowSpeed / PlayerCombat.BaseArrowSpeed, 1e-4f);
        }
    }
}
