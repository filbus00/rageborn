using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class EliteAffixTests
    {
        [Test]
        public void APack_AlwaysRollsAnAttackToDodge_AndMoreDeeper()
        {
            var random = new System.Random(7);
            foreach (var depth in new[] { 1, 6, 7, 18, 19, 24 })
                for (var i = 0; i < 200; i++)
                {
                    var roll = EliteAffixRules.Roll(random, depth);
                    Assert.IsTrue((roll & EliteAffixRules.Attacks) != 0, $"depth {depth}: an attack");
                    Assert.AreEqual(EliteAffixRules.Count(depth), EliteAffixRules.CountOf(roll), $"depth {depth}");
                }
            Assert.AreEqual(1, EliteAffixRules.Count(1));
            Assert.AreEqual(2, EliteAffixRules.Count(7));
            Assert.AreEqual(3, EliteAffixRules.Count(19));
        }

        [Test]
        public void AffixesOpen_ByDepth()
        {
            var random = new System.Random(3);
            for (var i = 0; i < 500; i++)
            {
                var top = EliteAffixRules.Roll(random, 1);
                Assert.AreEqual(EliteModifiers.None, top & (EliteModifiers.Desecrator | EliteModifiers.ArcaneBeam | EliteModifiers.FireChains));
            }
            var seen = EliteModifiers.None;
            for (var i = 0; i < 2000; i++)
                seen |= EliteAffixRules.Roll(random, 24);
            foreach (var (modifier, _) in EliteAffixRules.Pool)
                Assert.IsTrue((seen & modifier) != 0, modifier + " rolls at the bottom");
        }

        [Test]
        public void EveryAffix_HasAName_AndTheLabelListsThem()
        {
            foreach (var (modifier, _) in EliteAffixRules.Pool)
                Assert.IsNotEmpty(EliteAffixRules.Name(modifier), modifier.ToString());
            Assert.AreEqual("Molten  Mortar", EliteAffixRules.Names(EliteModifiers.Mortar | EliteModifiers.Molten));
        }

        [Test]
        public void DistanceToSegment_MeasuresToTheNearestPoint()
        {
            Assert.AreEqual(1f, EliteAffixRules.DistanceToSegment(new Vector2(0f, 1f), new Vector2(-2f, 0f), new Vector2(2f, 0f)), 1e-5f);
            Assert.AreEqual(1f, EliteAffixRules.DistanceToSegment(new Vector2(3f, 0f), new Vector2(-2f, 0f), new Vector2(2f, 0f)), 1e-5f);
        }
    }
}
