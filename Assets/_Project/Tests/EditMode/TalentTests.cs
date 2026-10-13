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

    }
}
