using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>The fun boons of 2026-11-15 (the owner: "lets add some fun boons", "add all of them as rare boons").</summary>
    public class FunBoonTests
    {
        static readonly Dictionary<string, PassiveEffect> Effects = new Dictionary<string, PassiveEffect>
        {
            ["dragon_fire"] = PassiveEffect.DragonFire,
            ["storm_quiver"] = PassiveEffect.StormQuiver,
            ["frost_trail"] = PassiveEffect.FrostTrail,
            ["boomerang_arrow"] = PassiveEffect.BoomerangArrow,
            ["rain_of_arrows"] = PassiveEffect.RainOfArrows,
            ["ghost_archer"] = PassiveEffect.GhostArcher,
            ["bone_arrows"] = PassiveEffect.BoneArrows,
            ["dread_howl"] = PassiveEffect.DreadHowl,
            ["last_arrow"] = PassiveEffect.LastArrow,
        };

        [Test]
        public void EveryFunBoon_IsRare_OneRank_AndOfferedFromTheFirstDepth()
        {
            Assert.AreEqual(9, Effects.Count);
            foreach (var pair in Effects)
            {
                var node = TalentRules.Get(pair.Key);
                Assert.IsNotNull(node, pair.Key);
                Assert.AreEqual(pair.Value, node.Effect, pair.Key);
                Assert.AreEqual(1, node.MaxRanks, pair.Key);
                Assert.IsTrue(node.IsRare, pair.Key);
                Assert.AreEqual(TalentRules.RareWeight, node.Weight, pair.Key);
                Assert.IsTrue(BoonRules.CanTake(new TalentState(), node, MetaProgress.BaseBoonTier), pair.Key + " from the first depth");
                Assert.IsFalse(string.IsNullOrEmpty(node.Text), pair.Key);
            }
        }

        [Test]
        public void TakingOne_SetsItsBonus_AndOnlyIts()
        {
            var talents = new TalentState();
            Assert.IsFalse(talents.Bonuses.DragonFire);
            Assert.IsTrue(talents.Grant("dragon_fire"));
            Assert.IsTrue(talents.Bonuses.DragonFire);
            Assert.IsFalse(talents.Bonuses.StormQuiver);
            Assert.IsFalse(talents.Grant("dragon_fire"), "one rank");
            foreach (var id in Effects.Keys)
                talents.Grant(id);
            var b = talents.Bonuses;
            Assert.IsTrue(b.StormQuiver && b.FrostTrail && b.BoomerangArrow && b.RainOfArrows && b.GhostArcher && b.BoneArrows && b.DreadHowl && b.LastArrow);
        }

        [Test]
        public void DragonFire_ComesEveryFourthShot_TheStormEveryFifth()
        {
            Assert.IsFalse(FunBoonRules.IsDragonShot(0));
            Assert.IsFalse(FunBoonRules.IsDragonShot(3));
            Assert.IsTrue(FunBoonRules.IsDragonShot(4));
            Assert.IsTrue(FunBoonRules.IsDragonShot(8));
            Assert.IsFalse(FunBoonRules.IsStormShot(4));
            Assert.IsTrue(FunBoonRules.IsStormShot(5));
            Assert.IsTrue(FunBoonRules.IsStormShot(10));
        }

        [Test]
        public void TheCone_ReachesItsLength_WithinItsAngle()
        {
            var origin = new Vector2(2f, 3f);
            var aim = Vector2.right;
            Assert.IsTrue(FunBoonRules.InCone(origin, aim, origin, 0.3f), "on her");
            Assert.IsTrue(FunBoonRules.InCone(origin, aim, origin + new Vector2(7.5f, 0f), 0.3f));
            Assert.IsTrue(FunBoonRules.InCone(origin, aim, origin + new Vector2(8.2f, 0f), 0.3f), "the body's radius counts");
            Assert.IsFalse(FunBoonRules.InCone(origin, aim, origin + new Vector2(8.5f, 0f), 0.3f), "past its length");
            Assert.IsTrue(FunBoonRules.InCone(origin, aim, origin + new Vector2(4f, 0.8f), 0.3f), "11 degrees off");
            Assert.IsFalse(FunBoonRules.InCone(origin, aim, origin + new Vector2(4f, 1.5f), 0.3f), "20 degrees off");
            Assert.IsFalse(FunBoonRules.InCone(origin, aim, origin + new Vector2(-1f, 0f), 0.3f), "behind her");
        }

        [Test]
        public void TheLastArrow_FliesOnce_BelowAThird_WhileSheLives()
        {
            Assert.IsFalse(FunBoonRules.LastArrowReady(false, 0.5f));
            Assert.IsTrue(FunBoonRules.LastArrowReady(false, 0.3f));
            Assert.IsFalse(FunBoonRules.LastArrowReady(true, 0.3f), "spent this level");
            Assert.IsFalse(FunBoonRules.LastArrowReady(false, 0f), "dead");
        }

        [Test]
        public void BoneArrows_LoadToFive_AndFanToEitherSide()
        {
            var held = 0;
            for (var i = 0; i < 8; i++)
                held = FunBoonRules.Loaded(held);
            Assert.AreEqual(FunBoonRules.BoneMax, held);
            Assert.AreEqual(FunBoonRules.BoneFanDegrees, FunBoonRules.BoneAngle(1));
            Assert.AreEqual(-FunBoonRules.BoneFanDegrees, FunBoonRules.BoneAngle(2));
            Assert.AreEqual(2f * FunBoonRules.BoneFanDegrees, FunBoonRules.BoneAngle(3));
            Assert.AreEqual(-2f * FunBoonRules.BoneFanDegrees, FunBoonRules.BoneAngle(4));
        }
    }
}
