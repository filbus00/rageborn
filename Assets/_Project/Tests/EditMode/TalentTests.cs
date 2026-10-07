using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ARPG.Tests
{
    public class TalentTests
    {
        static readonly string[] ClassSkills =
        {
            "SplitArrow", "PierceArrow", "HomingArrow", "ExplosiveArrow", "KillShot", "KnockbackShot", "Barrage", "WildFrenzy", "HuntersBreath",
        };

        [Test]
        public void She_StartsWithOnlyHerBow_AndEarnsAPointALevelFromTwo()
        {
            var talents = new TalentState();
            foreach (var skill in ClassSkills)
                Assert.IsFalse(talents.Knows(skill), skill);
            Assert.AreEqual(0, talents.Available(1));
            Assert.AreEqual(1, talents.Available(2));
            Assert.AreEqual(59, talents.Available(60));
        }

        [Test]
        public void Learning_ASkillNode_TeachesTheSkill_AndSpendsAPoint()
        {
            var talents = new TalentState();
            Assert.IsTrue(talents.Learn("pierce_arrow", 2));
            Assert.IsTrue(talents.Knows("PierceArrow"));
            Assert.AreEqual(0, talents.Available(2));
            Assert.IsFalse(talents.Learn("steady_aim", 2), "no points left");
            Assert.IsFalse(talents.Learn("pierce_arrow", 10), "a skill has one rank");
        }

        [Test]
        public void A_Row_OpensWithFivePointsInItsTree_AndARequirementMustBeFull()
        {
            var talents = new TalentState();
            var level = 30;
            Assert.IsNotNull(talents.Blocker(TalentRules.Get("lethal_shots"), level), "row 1 is shut with no points in the tree");
            for (var i = 0; i < 5; i++)
                Assert.IsTrue(talents.Learn("steady_aim", level));
            Assert.IsTrue(talents.Learn("lethal_shots", level));
            // Improved Pierce Arrow needs Pierce Arrow itself.
            Assert.IsFalse(talents.Learn("improved_pierce_arrow", level));
            Assert.IsTrue(talents.Learn("pierce_arrow", level));
            Assert.IsTrue(talents.Learn("improved_pierce_arrow", level));
            // Points in another tree do not open this one's rows.
            Assert.IsNotNull(talents.Blocker(TalentRules.Get("hunters_breath"), level));
        }

        [Test]
        public void Bonuses_AndImprovedSkills_AddUpByRank()
        {
            var talents = new TalentState();
            for (var i = 0; i < 3; i++)
                talents.Learn("steady_aim", 30);
            Assert.AreEqual(3f, talents.Bonuses.CriticalChance, 1e-4f);
            talents.Learn("steady_aim", 30);
            talents.Learn("steady_aim", 30);
            talents.Learn("pierce_arrow", 30);
            talents.Learn("improved_pierce_arrow", 30);
            talents.Learn("improved_pierce_arrow", 30);
            Assert.AreEqual(0.3f, talents.SkillDamage("PierceArrow"), 1e-4f);
            Assert.AreEqual(0f, talents.SkillDamage("SplitArrow"));
            talents.ResetAll();
            Assert.AreEqual(0, talents.Spent);
            Assert.AreEqual(0f, talents.Bonuses.CriticalChance);
            Assert.IsFalse(talents.Knows("PierceArrow"));
        }

        [Test]
        public void EveryClassSkill_IsTaughtOnce_AndEveryCapstoneCanBeReached()
        {
            foreach (var skill in ClassSkills)
                Assert.AreEqual(1, TalentRules.Nodes.Count(n => n.Skill == skill), skill);
            foreach (TalentTree tree in System.Enum.GetValues(typeof(TalentTree)))
            {
                var below = TalentRules.Nodes.Where(n => n.Tree == tree && n.Tier < TalentRules.Tiers - 1).Sum(n => n.MaxRanks);
                Assert.GreaterOrEqual(below, (TalentRules.Tiers - 1) * TalentRules.PointsPerTier, tree.ToString());
                Assert.IsTrue(TalentRules.Nodes.Any(n => n.Tree == tree && n.Tier == TalentRules.Tiers - 1), tree + " has a capstone");
            }
            var ids = new HashSet<string>();
            foreach (var node in TalentRules.Nodes)
            {
                Assert.IsTrue(ids.Add(node.Id), node.Id);
                Assert.IsTrue(node.Requires == null || TalentRules.Get(node.Requires) != null, node.Id);
                Assert.IsFalse(string.IsNullOrEmpty(TalentRules.Describe(node, 1)), node.Id);
            }
        }

        [Test]
        public void Talents_SurviveASaveAndLoad()
        {
            var session = new GameSession(1);
            for (var i = 0; i < 12; i++)
                session.GrantExperience(1000000);
            Assert.IsTrue(session.Talents.Learn("homing_arrow", session.Level));
            Assert.IsTrue(session.Talents.Learn("kindred_spirit", session.Level));
            var restored = SaveCodec.Restore(SaveCodec.Capture(session, 0), 1);
            Assert.IsTrue(restored.Talents.Knows("HomingArrow"));
            Assert.AreEqual(1, restored.Talents.RankOf("kindred_spirit"));
        }
    }
}
