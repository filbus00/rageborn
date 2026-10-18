using System.Collections.Generic;
using NUnit.Framework;

namespace ARPG.Tests
{
    /// <summary>The balance pass of 2026-10-17: the boons that multiply arrows offered less often, the unique boons, and
    /// steadier pressure from the enemies.</summary>
    public class UniqueBoonTests
    {
        [Test]
        public void TheMultiplyingBoons_AreOfferedLessOften_ButKeepTheirRanks()
        {
            Assert.AreEqual(3, TalentRules.Get("extra_arrow").MaxRanks);
            Assert.AreEqual(3, TalentRules.Get("extra_shot").MaxRanks);
            Assert.IsTrue(TalentRules.Get("extra_arrow").IsRare);
            Assert.IsFalse(TalentRules.Get("fire_arrow").IsRare);

            var counts = new Dictionary<string, int>();
            var random = new System.Random(5);
            var talents = new TalentState();
            for (var i = 0; i < 20000; i++)
                foreach (var id in BoonRules.Offer(talents, MetaProgress.BaseBoonTier, 1, random))
                    counts[id] = (counts.TryGetValue(id, out var c) ? c : 0) + 1;
            var ratio = (float)counts["extra_arrow"] / counts["fire_arrow"];
            Assert.AreEqual(TalentRules.RareWeight, ratio, 0.06f);
            var uncommon = (float)counts["ricochet"] / counts["fire_arrow"];
            Assert.AreEqual(TalentRules.UncommonWeight, uncommon, 0.08f);
        }

        [Test]
        public void AnOfferOfThree_IsStillThreeDifferentBoons()
        {
            var random = new System.Random(2);
            for (var i = 0; i < 200; i++)
            {
                var offer = BoonRules.Offer(new TalentState(), MetaProgress.BaseBoonTier, 3, random);
                Assert.AreEqual(3, offer.Count);
                CollectionAssert.AllItemsAreUnique(offer);
            }
        }

        [Test]
        public void TheUniqueBoons_AreOfferedFromTheFirstDepth_AndSetTheirBonus()
        {
            var effects = new Dictionary<string, PassiveEffect>
            {
                ["thunder_arrow"] = PassiveEffect.ThunderArrow,
                ["marked_prey"] = PassiveEffect.MarkedPrey,
                ["dive_volley"] = PassiveEffect.DiveVolley,
                ["vengeful_quiver"] = PassiveEffect.VengefulQuiver,
                ["wildfire"] = PassiveEffect.Wildfire,
                ["shatter"] = PassiveEffect.Shatter,
            };
            foreach (var pair in effects)
            {
                var node = TalentRules.Get(pair.Key);
                Assert.IsNotNull(node, pair.Key);
                Assert.AreEqual(pair.Value, node.Effect);
                Assert.Less(node.Tier, MetaProgress.BaseBoonTier, pair.Key);
            }
            var talents = new TalentState();
            Assert.IsFalse(BoonRules.CanTake(talents, TalentRules.Get("wildfire"), MetaProgress.BaseBoonTier), "needs Fire Arrow");
            Assert.IsTrue(talents.Grant("fire_arrow"));
            Assert.IsTrue(BoonRules.CanTake(talents, TalentRules.Get("wildfire"), MetaProgress.BaseBoonTier));
            Assert.IsTrue(talents.Grant("wildfire"));
            Assert.IsTrue(talents.Bonuses.Wildfire);
            Assert.IsFalse(talents.Bonuses.Shatter);
        }

        [Test]
        public void EveryNode_HasItsOwnPlaceOnTheScreen()
        {
            var seen = new HashSet<(TalentTree, int, int)>();
            foreach (var node in TalentRules.Nodes)
                Assert.IsTrue(seen.Add((node.Tree, node.Tier, node.Column)), node.Id);
        }

        [Test]
        public void ThunderArrow_ComesEveryEighthShot_AndThePreyIsTheToughest()
        {
            Assert.IsFalse(UniqueBoonRules.IsThunderShot(0));
            Assert.IsFalse(UniqueBoonRules.IsThunderShot(7));
            Assert.IsTrue(UniqueBoonRules.IsThunderShot(8));
            Assert.IsTrue(UniqueBoonRules.IsThunderShot(16));
            Assert.AreEqual(2, UniqueBoonRules.Toughest(new List<float> { 10f, 40f, 90f, 90f }));
            Assert.AreEqual(-1, UniqueBoonRules.Toughest(new List<float>()));
        }

        [Test]
        public void ADodgeCharge_CanBeGivenBack_UpToTheMost()
        {
            var charges = new DodgeCharges();
            Assert.IsTrue(charges.TryUse());
            charges.Refund();
            Assert.AreEqual(DodgeRules.MaxCharges, charges.Charges);
            charges.Refund();
            Assert.AreEqual(DodgeRules.MaxCharges, charges.Charges);
        }

        [Test]
        public void TheEnemies_ShootMoreOften_ButSofter_InBiggerPacks()
        {
            Assert.AreEqual(2f, EnemyRules.AttackSpeedFactor(EnemyRank.Normal, EnemyArchetype.Archer));
            Assert.AreEqual(1f, EnemyRules.ProjectileDamageFactor(EnemyRank.Normal));
            var settings = new DungeonSettings();
            Assert.AreEqual(8, settings.MinPackSize);
            Assert.AreEqual(15, settings.MaxPackSize);
        }

        [Test]
        public void TheDive_BurstsAndEasesOut_OverItsDistance()
        {
            // The owner, 2026-10-18: snappier.
            Assert.AreEqual(DodgeRules.MaxCharges, new DodgeCharges().Charges);
            Assert.Greater(DodgeRules.DiveSpeed(0f), 1f);
            Assert.Less(DodgeRules.DiveSpeed(1f), 1f);
            // The burst covers the dive's distance: the average share across it is 1.
            var sum = 0f;
            const int steps = 1000;
            for (var k = 0; k < steps; k++)
                sum += DodgeRules.DiveSpeed((k + 0.5f) / steps);
            Assert.AreEqual(1f, sum / steps, 1e-3f);
            Assert.LessOrEqual(DodgeRules.UntouchableSeconds, DodgeRules.Seconds);
            Assert.GreaterOrEqual(DodgeRules.AnimationSeconds, DodgeRules.Seconds);
        }
    }
}
