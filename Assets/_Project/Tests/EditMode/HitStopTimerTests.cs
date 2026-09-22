using NUnit.Framework;

namespace ARPG.Tests
{
    public class HitStopTimerTests
    {
        [Test]
        public void AFreshTimer_IsNotActive()
        {
            var timer = new HitStopTimer();

            Assert.IsFalse(timer.IsActive);
            Assert.AreEqual(0f, timer.RemainingSeconds);
        }

        [Test]
        public void Trigger_MakesItActive_ForTheRequestedSeconds()
        {
            var timer = new HitStopTimer();

            timer.Trigger(0.05f);

            Assert.IsTrue(timer.IsActive);
            Assert.AreEqual(0.05f, timer.RemainingSeconds, 1e-6f);
        }

        [Test]
        public void Trigger_WhileActive_ExtendsToTheLonger_RatherThanStacking()
        {
            var timer = new HitStopTimer();
            timer.Trigger(0.05f);

            timer.Trigger(0.03f); // shorter: no change
            Assert.AreEqual(0.05f, timer.RemainingSeconds, 1e-6f);

            timer.Trigger(0.08f); // longer: extends to it, not 0.05 + 0.08
            Assert.AreEqual(0.08f, timer.RemainingSeconds, 1e-6f);
        }

        [Test]
        public void Tick_CountsDown_AndReturnsFalse_WhileStillActive()
        {
            var timer = new HitStopTimer();
            timer.Trigger(0.05f);

            var ended = timer.Tick(0.02f);

            Assert.IsFalse(ended);
            Assert.IsTrue(timer.IsActive);
            Assert.AreEqual(0.03f, timer.RemainingSeconds, 1e-6f);
        }

        [Test]
        public void Tick_ReturnsTrue_ExactlyOnTheTickThatEndsIt()
        {
            var timer = new HitStopTimer();
            timer.Trigger(0.05f);

            Assert.IsFalse(timer.Tick(0.02f));
            Assert.IsFalse(timer.Tick(0.02f));
            Assert.IsTrue(timer.Tick(0.02f), "0.02+0.02+0.02 = 0.06 exceeds the 0.05 requested");

            Assert.IsFalse(timer.IsActive);
            Assert.AreEqual(0f, timer.RemainingSeconds);
        }

        [Test]
        public void Tick_OnlyReturnsTrueOnce_NotOnEveryTickAfterItEnded()
        {
            var timer = new HitStopTimer();
            timer.Trigger(0.05f);

            timer.Tick(0.1f); // ends it, well past zero
            var endedAgain = timer.Tick(0.1f); // idle now; must not re-report ending

            Assert.IsFalse(endedAgain);
        }

        [Test]
        public void ALargeTick_CanEndItInOneStep_EvenFarPastZero()
        {
            var timer = new HitStopTimer();
            timer.Trigger(0.05f);

            var ended = timer.Tick(5f); // a huge unscaled delta, as if many real seconds passed in one frame

            Assert.IsTrue(ended);
            Assert.AreEqual(0f, timer.RemainingSeconds, "clamped, not left negative");
        }

        [Test]
        public void Tick_OnAnIdleTimer_DoesNothing()
        {
            var timer = new HitStopTimer();

            var ended = timer.Tick(1f);

            Assert.IsFalse(ended);
            Assert.IsFalse(timer.IsActive);
        }

        [Test]
        public void TriggerAgain_AfterItEnded_StartsAFreshCountdown()
        {
            var timer = new HitStopTimer();
            timer.Trigger(0.05f);
            timer.Tick(0.1f);
            Assert.IsFalse(timer.IsActive);

            timer.Trigger(0.02f);

            Assert.IsTrue(timer.IsActive);
            Assert.AreEqual(0.02f, timer.RemainingSeconds, 1e-6f);
        }
    }
}
