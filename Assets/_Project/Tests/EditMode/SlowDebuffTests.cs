using NUnit.Framework;

namespace ARPG.Tests
{
    public class SlowDebuffTests
    {
        [Test]
        public void AFreshDebuff_IsNotActive_AndHasNoMultiplier()
        {
            var slow = new SlowDebuff();

            Assert.IsFalse(slow.IsActive);
            Assert.AreEqual(1f, slow.Multiplier, 1e-6f);
        }

        [Test]
        public void Apply_MakesItActive_AtTheGivenMultiplier()
        {
            var slow = new SlowDebuff();

            slow.Apply(0.5f, 1.5f);

            Assert.IsTrue(slow.IsActive);
            Assert.AreEqual(0.5f, slow.Multiplier, 1e-6f);
        }

        [Test]
        public void Apply_Clamps_ToZeroAndOne()
        {
            var slow = new SlowDebuff();

            slow.Apply(-0.5f, 1f);
            Assert.AreEqual(0f, slow.Multiplier, 1e-6f);

            var slow2 = new SlowDebuff();
            slow2.Apply(1.5f, 1f);
            Assert.AreEqual(1f, slow2.Multiplier, 1e-6f);
        }

        [Test]
        public void ReapplyingAWeakerSlow_KeepsTheStrongerOne()
        {
            var slow = new SlowDebuff();
            slow.Apply(0.4f, 1f); // stronger (lower multiplier)

            slow.Apply(0.8f, 1f); // weaker: must not overwrite

            Assert.AreEqual(0.4f, slow.Multiplier, 1e-6f);
        }

        [Test]
        public void ReapplyingAStrongerSlow_ReplacesTheWeakerOne()
        {
            var slow = new SlowDebuff();
            slow.Apply(0.8f, 1f);

            slow.Apply(0.3f, 1f);

            Assert.AreEqual(0.3f, slow.Multiplier, 1e-6f);
        }

        [Test]
        public void Reapplying_ExtendsDuration_ToTheLonger_RatherThanAdding()
        {
            var slow = new SlowDebuff();
            slow.Apply(0.5f, 1f);
            slow.Tick(0.9f); // 0.1 s left

            slow.Apply(0.5f, 0.5f); // shorter than what's left extended to: max(0.1, 0.5) = 0.5, not 0.6

            Assert.IsTrue(slow.IsActive);
            slow.Tick(0.49f);
            Assert.IsTrue(slow.IsActive, "0.5 - 0.49 still just barely active");
            slow.Tick(0.02f);
            Assert.IsFalse(slow.IsActive);
        }

        [Test]
        public void Tick_CountsDown_ThenExpiresBackToNoSlow()
        {
            var slow = new SlowDebuff();
            slow.Apply(0.5f, 1f);

            slow.Tick(0.6f);
            Assert.IsTrue(slow.IsActive);
            Assert.AreEqual(0.5f, slow.Multiplier, 1e-6f, "still slowed while active");

            slow.Tick(0.6f);
            Assert.IsFalse(slow.IsActive);
            Assert.AreEqual(1f, slow.Multiplier, 1e-6f, "back to full speed once it expires");
        }

        [Test]
        public void ANewWeakerSlow_AfterExpiry_IsNotBlockedByTheOldStrongerOne()
        {
            var slow = new SlowDebuff();
            slow.Apply(0.2f, 0.5f);
            slow.Tick(1f); // well past expiry

            slow.Apply(0.9f, 1f);

            Assert.AreEqual(0.9f, slow.Multiplier, 1e-6f, "the expired slow must not keep winning as \"stronger\"");
        }

        [Test]
        public void Tick_OnAnInactiveDebuff_DoesNothing()
        {
            var slow = new SlowDebuff();

            slow.Tick(1f);

            Assert.IsFalse(slow.IsActive);
            Assert.AreEqual(1f, slow.Multiplier, 1e-6f);
        }
    }
}
