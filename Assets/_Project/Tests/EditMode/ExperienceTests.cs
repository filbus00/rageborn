using NUnit.Framework;

namespace ARPG.Tests
{
    public class ExperienceTests
    {
        // The table in Docs/04-progression-and-economy.md.
        [TestCase(1, 400)]
        [TestCase(5, 13797)]
        [TestCase(10, 63396)]
        [TestCase(15, 154689)]
        [TestCase(20, 291290)]
        [TestCase(30, 710766)]
        [TestCase(40, 1338419)]
        [TestCase(50, 2186724)]
        [TestCase(60, 3265824)]
        public void XpToNextLevel_MatchesTheDocsTable(int level, int expected)
        {
            Assert.AreEqual(expected, Experience.XpToNextLevel(level));
        }

        [TestCase(5, 28965)]
        [TestCase(10, 230974)]
        [TestCase(20, 1968881)]
        public void CumulativeXp_MatchesTheDocsTable(int level, int expected)
        {
            var total = 0;
            for (var n = 1; n <= level; n++)
                total += Experience.XpToNextLevel(n);

            // The docs' cumulative column rounds each level's number too; allow for where it rounded differently.
            Assert.AreEqual(expected, total, level);
        }

        [TestCase(1, 8)]
        [TestCase(5, 89)]
        [TestCase(10, 253)]
        [TestCase(30, 1315)]
        [TestCase(60, 3718)]
        public void EnemyXp_MatchesTheDocsTable(int enemyLevel, int expected)
        {
            Assert.AreEqual(expected, Experience.KillXp(enemyLevel, enemyLevel, EnemyRank.Normal));
        }

        [Test]
        public void AnElite_GivesEightTimesNormal_AndAChampion_ThreeTimes()
        {
            Assert.AreEqual(64, Experience.KillXp(1, 1, EnemyRank.Elite));
            Assert.AreEqual(24, Experience.KillXp(1, 1, EnemyRank.Champion));
        }

        [TestCase(10, 7, 1f)]
        [TestCase(10, 13, 1f)]
        [TestCase(10, 6, 0.88f)]
        [TestCase(10, 2, 0.40f)]
        [TestCase(20, 5, 0f)]
        [TestCase(10, 14, 1.08f)]
        [TestCase(10, 18, 1.40f)]
        [TestCase(10, 30, 1.5f)]
        public void LevelDifference_FullWithinThree_ThenTwelveDownOrEightUpPerLevel(int character, int enemy, float expected)
        {
            Assert.AreEqual(expected, Experience.LevelDifferenceMultiplier(character, enemy), 1e-5f);
        }

        [Test]
        public void Progress_StartsAtLevelOne_WithNoXp()
        {
            var progress = new CharacterProgress();

            Assert.AreEqual(1, progress.Level);
            Assert.AreEqual(0, progress.Xp);
            Assert.AreEqual(0f, progress.Fraction);
        }

        [Test]
        public void Progress_LevelsUp_AndCarriesTheRemainder()
        {
            var progress = new CharacterProgress();

            Assert.AreEqual(0, progress.Add(399));
            Assert.AreEqual(1, progress.Add(11));

            Assert.AreEqual(2, progress.Level);
            Assert.AreEqual(10, progress.Xp);
        }

        [Test]
        public void Progress_CanGainSeveralLevels_FromOneGrant()
        {
            var progress = new CharacterProgress();

            var gained = progress.Add(Experience.XpToNextLevel(1) + Experience.XpToNextLevel(2) + 5);

            Assert.AreEqual(2, gained);
            Assert.AreEqual(3, progress.Level);
            Assert.AreEqual(5, progress.Xp);
        }

        [Test]
        public void Progress_StopsAtTheLevelCap_AndDropsTheRest()
        {
            var progress = new CharacterProgress(Experience.MaxLevel - 1, 0);

            Assert.AreEqual(1, progress.Add(int.MaxValue));

            Assert.AreEqual(Experience.MaxLevel, progress.Level);
            Assert.IsTrue(progress.IsMaxLevel);
            Assert.AreEqual(0, progress.Xp);
            Assert.AreEqual(1f, progress.Fraction);
            Assert.AreEqual(0, progress.Add(1000));
        }

        [Test]
        public void Progress_FromASave_IsClampedIntoRange()
        {
            Assert.AreEqual(1, new CharacterProgress(0, 5).Level);
            Assert.AreEqual(Experience.MaxLevel, new CharacterProgress(99, 5).Level);
            Assert.AreEqual(0, new CharacterProgress(99, 5).Xp);
            Assert.AreEqual(399, new CharacterProgress(1, 1000).Xp);
            Assert.AreEqual(0, new CharacterProgress(1, -5).Xp);
        }

        [Test]
        public void GrantExperience_RaisesChangedAndLeveledUp_OnlyOnALevelUp()
        {
            var session = new GameSession();
            var changed = 0;
            var modified = 0;
            var leveledTo = 0;
            session.Changed += () => changed++;
            session.Modified += () => modified++;
            session.LeveledUp += level => leveledTo = level;

            session.GrantExperience(8);
            Assert.AreEqual(0, changed);
            Assert.AreEqual(1, modified, "plain XP must still be saved");
            Assert.AreEqual(0, leveledTo);

            session.GrantExperience(400);
            Assert.AreEqual(1, changed, "maximum life depends on level");
            Assert.AreEqual(2, leveledTo);
            Assert.AreEqual(2, session.Level);
        }

        [Test]
        public void ALevelUp_RefillsLife_ButPlainXpDoesNot()
        {
            var session = new GameSession();
            session.LifeFraction = 0.3f;

            session.GrantExperience(8);
            Assert.AreEqual(0.3f, session.LifeFraction);

            session.GrantExperience(400);
            Assert.AreEqual(1f, session.LifeFraction);
        }
    }
}
