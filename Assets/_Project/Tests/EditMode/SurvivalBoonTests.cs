using NUnit.Framework;

namespace ARPG.Tests
{
    /// <summary>The three boons of 2026-10-19 (the owner: "The game is too hard as it is now. Add 3 more powerfull boons").
    /// </summary>
    public class SurvivalBoonTests
    {
        [Test]
        public void Executioner_FinishesTheBadlyHurt_ButNotABoss()
        {
            Assert.IsTrue(SurvivalBoonRules.Executes(15f, 100f, EnemyRank.Normal));
            Assert.IsTrue(SurvivalBoonRules.Executes(15f, 100f, EnemyRank.Elite));
            Assert.IsFalse(SurvivalBoonRules.Executes(25f, 100f, EnemyRank.Normal), "above a fifth");
            Assert.IsFalse(SurvivalBoonRules.Executes(15f, 100f, EnemyRank.Boss));
            Assert.IsFalse(SurvivalBoonRules.Executes(-3f, 100f, EnemyRank.Normal), "already dead");
        }

        [Test]
        public void ThickHide_AndBloodDrinker_DoWhatTheySay()
        {
            Assert.AreEqual(75f, SurvivalBoonRules.Taken(100f), 1e-4f);
            Assert.AreEqual(4f, SurvivalBoonRules.Healed(100f), 1e-4f);
            Assert.AreEqual(0f, SurvivalBoonRules.Healed(-5f));
        }

        [Test]
        public void TheNewBoons_AreOfferedFromTheFirstDepth()
        {
            foreach (var id in new[] { "executioner", "thick_hide", "blood_drinker" })
            {
                var node = TalentRules.Get(id);
                Assert.IsNotNull(node, id);
                Assert.Less(node.Tier, 2, id);
            }
        }
    }
}
