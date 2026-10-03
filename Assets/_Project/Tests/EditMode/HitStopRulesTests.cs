using NUnit.Framework;

namespace ARPG.Tests
{
    public class HitStopRulesTests
    {
        [Test]
        public void ANormalKill_NeverStopsTheGame() => Assert.IsFalse(HitStopRules.OnKill(EnemyRank.Normal, 100f));

        [Test]
        public void AnEliteKill_StopsIt() => Assert.IsTrue(HitStopRules.OnKill(EnemyRank.Elite, 5f));

        [Test]
        public void TwoBigKillsInASecond_StopItOnce() =>
            Assert.IsFalse(HitStopRules.OnKill(EnemyRank.Champion, HitStopRules.MinGapSeconds * 0.5f));
    }
}
