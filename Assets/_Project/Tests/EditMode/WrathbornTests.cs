using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class WrathbornTests
    {
        // --- Rage ---

        [Test]
        public void Rage_StartsEmpty_AndIsBuiltByHitting()
        {
            var rage = new RagePool();
            Assert.AreEqual(0f, rage.Current);

            rage.Gain(RagePool.PerBasicHit);
            rage.Gain(RagePool.PerHitTaken);
            Assert.AreEqual(9f, rage.Current, 1e-4f);
        }

        [Test]
        public void Rage_IsCappedAtItsMaximum()
        {
            var rage = new RagePool();
            for (var i = 0; i < 50; i++)
                rage.Gain(RagePool.PerBasicHit);
            Assert.AreEqual(RagePool.DefaultMax, rage.Current);
        }

        [Test]
        public void Rage_HoldsForThreeSeconds_ThenDrains()
        {
            var rage = new RagePool(start: 0f);
            rage.Gain(50f);
            Tick(rage, 2.9f);
            Assert.AreEqual(50f, rage.Current, 1e-3f, "no drain inside the delay");

            Tick(rage, 1.1f); // 1 s past the delay, give or take a step
            Assert.AreEqual(45f, rage.Current, 0.2f);

            Tick(rage, 20f);
            Assert.AreEqual(0f, rage.Current, "drains to empty, never below");
        }

        [Test]
        public void Combat_HoldsOffTheDrain()
        {
            var rage = new RagePool();
            rage.Gain(50f);
            for (var i = 0; i < 10; i++)
            {
                Tick(rage, 2f);
                rage.MarkCombat();
            }
            Assert.AreEqual(50f, rage.Current, 1e-3f);
        }

        [Test]
        public void Spending_NeedsTheWholeCost()
        {
            var rage = new RagePool();
            rage.Gain(15f);
            Assert.IsFalse(rage.TrySpend(20f));
            Assert.AreEqual(15f, rage.Current);
            Assert.IsTrue(rage.TrySpend(10f));
            Assert.AreEqual(5f, rage.Current, 1e-4f);
        }

        static void Tick(RagePool rage, float seconds)
        {
            const float step = 0.05f;
            for (var t = 0f; t < seconds - 1e-4f; t += step)
                rage.Tick(step);
        }

        // --- Stillness and Momentum ---

        static void Run(StanceStacks stance, float seconds, bool moving)
        {
            const float step = 0.02f;
            var steps = Mathf.RoundToInt(seconds / step);
            for (var i = 0; i < steps; i++)
                stance.Tick(step, moving);
        }

        [Test]
        public void Momentum_BuildsOnePerSixTenthsOfASecond_UpToFive()
        {
            var stance = new StanceStacks();
            Run(stance, 0.5f, true);
            Assert.AreEqual(0, stance.Momentum);
            Run(stance, 0.2f, true);
            Assert.AreEqual(1, stance.Momentum);
            Run(stance, 5f, true);
            Assert.AreEqual(5, stance.Momentum);
            Assert.AreEqual(1.25f, stance.MoveSpeedMultiplier, 1e-4f);
            Assert.AreEqual(0.25f, stance.DodgeChance, 1e-4f);
        }

        [Test]
        public void Momentum_SurvivesAShortStop_AndIsLostAfterOnePointTwoSeconds()
        {
            var stance = new StanceStacks();
            Run(stance, 3.5f, true);
            Run(stance, 1f, false);
            Assert.AreEqual(5, stance.Momentum, "a short stop keeps it");
            Run(stance, 0.3f, false);
            Assert.AreEqual(0, stance.Momentum);
        }

        [Test]
        public void Stillness_BuildsWhileStanding_AndIsLostOnMoving()
        {
            var stance = new StanceStacks();
            Run(stance, 0.9f, false);
            Assert.AreEqual(2, stance.Stillness);
            Assert.AreEqual(0.12f, stance.IncreasedDamage, 1e-4f);
            Assert.AreEqual(0.08f, stance.DamageReduction, 1e-4f);

            Run(stance, 0.04f, true);
            Assert.AreEqual(0, stance.Stillness);
        }

        [Test]
        public void BullRush_AddsMomentum_UpToTheCap()
        {
            var stance = new StanceStacks();
            stance.AddMomentum(3);
            Assert.AreEqual(3, stance.Momentum);
            stance.AddMomentum(4);
            Assert.AreEqual(StanceStacks.MaxStacks, stance.Momentum);
        }

        // --- Unlocks and triggers ---

        [Test]
        public void TheFirstFourSkills_UnlockAtOneTwoFourAndSix()
        {
            var levels = new[] { 1, 2, 4, 6 };
            Assert.IsTrue(SkillRules.IsUnlocked(levels[0], 1));
            Assert.IsFalse(SkillRules.IsUnlocked(levels[1], 1));
            Assert.IsTrue(SkillRules.IsUnlocked(levels[3], 6));

            CollectionAssert.AreEqual(new[] { 1 }, SkillRules.NewlyUnlocked(levels, 1, 2));
            CollectionAssert.IsEmpty(SkillRules.NewlyUnlocked(levels, 2, 3));
            // A big XP grant can jump several levels: every skill in between is announced.
            CollectionAssert.AreEqual(new[] { 2, 3 }, SkillRules.NewlyUnlocked(levels, 3, 7));
        }

        [Test]
        public void ACharge_OnlyTargetsWhatIsAheadAndInItsDistanceBand()
        {
            var origin = Vector2.zero;
            var east = Vector2.right;
            Assert.IsTrue(SkillRules.IsAhead(origin, east, new Vector2(4f, 1f), 3f, 6f));
            Assert.IsFalse(SkillRules.IsAhead(origin, east, new Vector2(2f, 0f), 3f, 6f), "too close");
            Assert.IsFalse(SkillRules.IsAhead(origin, east, new Vector2(7f, 0f), 3f, 6f), "too far");
            Assert.IsFalse(SkillRules.IsAhead(origin, east, new Vector2(3f, 3f), 3f, 6f), "45 degrees off the path");
            Assert.IsFalse(SkillRules.IsAhead(origin, Vector2.zero, new Vector2(4f, 0f), 3f, 6f), "standing still");
        }

        [Test]
        public void ACharge_RunsThroughItsTarget_ButNotFurtherThanItsLimit()
        {
            Assert.AreEqual(5.5f, SkillRules.ChargeDistance(4f, 7.5f), 1e-4f);
            Assert.AreEqual(7.5f, SkillRules.ChargeDistance(6.8f, 7.5f), 1e-4f);
        }

        [Test]
        public void Slows_Clear_ForAPooledEnemysRespawn()
        {
            var slow = new SlowDebuff();
            slow.Apply(0.7f, 2f);
            slow.Clear();
            Assert.AreEqual(1f, slow.Multiplier);
            Assert.IsFalse(slow.IsActive);
        }
    }
}
