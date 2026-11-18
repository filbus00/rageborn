using NUnit.Framework;

namespace ARPG.Tests
{
    public class BurnFxTests
    {
        [Test]
        public void NothingBurningThrowsNothing_AndMoreStacksThrowMore()
        {
            Assert.AreEqual(0f, BurnFxRules.Rate(0));
            Assert.Greater(BurnFxRules.Rate(1), 0f);
            Assert.Greater(BurnFxRules.Rate(2), BurnFxRules.Rate(1));
            Assert.Greater(BurnFxRules.Rate(3), BurnFxRules.Rate(2));
            // More stacks than the cap do not throw more.
            Assert.AreEqual(BurnFxRules.Rate(BurnStacks.MaxStacks), BurnFxRules.Rate(BurnStacks.MaxStacks + 4));
        }

        [Test]
        public void ABiggerEnemyBurnsWiderButNeverNarrowerThanASmallOne()
        {
            Assert.Greater(BurnFxRules.Width(2f), BurnFxRules.Width(1f));
            Assert.AreEqual(BurnFxRules.Width(0.5f), BurnFxRules.Width(0.1f));
        }

        [Test]
        public void BurnStacksCountTheirLiveStacks()
        {
            var burn = new BurnStacks();
            Assert.AreEqual(0, burn.Count);
            burn.Apply(10f);
            burn.Apply(10f);
            Assert.AreEqual(2, burn.Count);
            burn.Tick(BurnStacks.Seconds + 1f);
            Assert.AreEqual(0, burn.Count);
            Assert.IsFalse(burn.IsBurning);
        }
    }
}
