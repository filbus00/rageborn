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
        public void EveryClassSkill_IsTaughtOnce_AndEveryNodeIsInTheTrees()
        {
            // The roguelite (2026-10-15): boons ignore the trees' points, so only the rows' bounds are checked.
            foreach (var skill in ClassSkills)
                Assert.AreEqual(1, TalentRules.Nodes.Count(n => n.Skill == skill), skill);
            foreach (var node in TalentRules.Nodes)
                Assert.IsTrue(node.Tier >= 0 && node.Tier < TalentRules.Tiers && node.Column >= 0 && node.Column < TalentRules.Columns, node.Id);
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
