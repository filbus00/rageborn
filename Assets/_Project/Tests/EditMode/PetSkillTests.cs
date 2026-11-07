using NUnit.Framework;

namespace ARPG.Tests
{
    /// <summary>The wolf's skills, boons since 2026-11-07, and Lyriel's training.</summary>
    public class PetSkillTests
    {
        [Test]
        public void EverySkill_IsABoonOfBeastMastery_OfferedFromTheFirstDepth()
        {
            foreach (var skill in PetSkillRules.All)
            {
                var node = TalentRules.Get(skill.Boon);
                Assert.IsNotNull(node, skill.Name);
                Assert.AreEqual(TalentTree.BeastMastery, node.Tree, skill.Name);
                Assert.AreEqual(PetSkillRules.MaxRanks, node.MaxRanks, skill.Name);
                Assert.IsTrue(node.AnyDepth, skill.Name);
            }
            Assert.IsTrue(BoonRules.CanTake(new TalentState(), TalentRules.Get("alpha_wolf"), MetaProgress.BaseBoonTier));
        }

        [Test]
        public void ASkillsRank_IsItsBoonsRank_AndEndsWithTheRun()
        {
            var session = new GameSession(2);
            Assert.AreEqual(0, session.Pets.ActiveRank(PetSkillId.Pounce));
            session.Talents.Grant("wolf_pounce");
            session.Talents.Grant("wolf_pounce");
            Assert.AreEqual(2, session.Pets.ActiveRank(PetSkillId.Pounce));
            Assert.AreEqual(0, session.NextRun(3).Pets.ActiveRank(PetSkillId.Pounce));
            Assert.AreEqual("Every 8 seconds the wolf leaps at an enemy up to 6 away, its bite chilling it 2 times.",
                TalentRules.Describe(TalentRules.Get("wolf_pounce"), 2));
        }

        [Test]
        public void BloodFeast_NeedsRendFirst()
        {
            var talents = new TalentState();
            Assert.IsFalse(BoonRules.CanTake(talents, TalentRules.Get("wolf_feast"), MetaProgress.BaseBoonTier));
            talents.Grant("wolf_rend");
            Assert.IsTrue(BoonRules.CanTake(talents, TalentRules.Get("wolf_feast"), MetaProgress.BaseBoonTier));
        }

        [Test]
        public void AlphaWolf_AndLyrielsTraining_StrengthenTheWolf()
        {
            var session = new GameSession(2);
            session.Talents.Grant("alpha_wolf");
            Assert.AreEqual(0.4f, session.Talents.Bonuses.PetDamage, 1e-5f);
            Assert.AreEqual(0.4f, session.Talents.Bonuses.PetLife, 1e-5f);
            session.AddGold(10000);
            Assert.IsTrue(session.BuyUpgrade(MetaUpgradeId.SharpFangs));
            Assert.IsTrue(session.BuyUpgrade(MetaUpgradeId.ThickCoat));
            Assert.AreEqual(0.55f, session.Talents.Bonuses.PetDamage, 1e-5f);
            Assert.AreEqual(0.6f, session.Talents.Bonuses.PetLife, 1e-5f);
        }
    }
}
