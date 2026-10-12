using NUnit.Framework;

namespace ARPG.Tests
{
    public class CoinStreakTests
    {
        [Test]
        public void QuickPickups_Climb_AndSoften_APauseStartsOver()
        {
            var streak = CoinStreak.Next(0, 5f);
            Assert.AreEqual(0, streak, "a first coin after a pause");
            Assert.AreEqual(1f, CoinStreak.Pitch(streak));
            Assert.AreEqual(1f, CoinStreak.Volume(streak));
            var lastPitch = CoinStreak.Pitch(streak);
            for (var i = 1; i < 5; i++)
            {
                streak = CoinStreak.Next(streak, 0.3f);
                Assert.AreEqual(i, streak);
                Assert.Greater(CoinStreak.Pitch(streak), lastPitch, "each quick coin plays higher");
                Assert.Less(CoinStreak.Volume(streak), CoinStreak.Volume(streak - 1), "and softer");
                lastPitch = CoinStreak.Pitch(streak);
            }
            Assert.AreEqual(1f, CoinStreak.Pitch(CoinStreak.Next(streak, 0.3f)), "the run goes round again");
            Assert.AreEqual(0.6f, CoinStreak.Volume(20), 1e-5f, "never softer than 60 percent");
            Assert.AreEqual(0, CoinStreak.Next(streak, CoinStreak.StreakGap + 0.1f), "a pause starts it over");
        }
    }
}
