using NUnit.Framework;

namespace ARPG.Tests
{
    public class FiringPaceTests
    {
        [Test]
        public void FastBowWalksUntilTheNextShot() => Assert.AreEqual(0.4f, FiringPace.HoldFor(0.4f), 1e-5f);

        [Test]
        public void SlowBowRunsBetweenShots() => Assert.AreEqual(FiringPace.HoldSeconds, FiringPace.HoldFor(1.5f), 1e-5f);

        [Test]
        public void FiringIsAboutHalfSpeed()
        {
            var firing = new SlowDebuff();
            firing.Apply(FiringPace.MoveMultiplier, FiringPace.HoldFor(0.5f));
            Assert.AreEqual(0.5f, firing.Multiplier, 1e-5f);
            firing.Tick(0.6f);
            Assert.AreEqual(1f, firing.Multiplier, 1e-5f);
        }
    }
}
