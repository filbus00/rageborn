using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>Corner cases of the roguelite's pure rules (2026-10-18, overnight): the boon offer, the unique boons, the
    /// blade's bounce, Garrett's prices, the upgrades' costs, blueprints and the run summary. They pin today's behaviour.</summary>
    public class RogueliteEdgeTests
    {
        [Test]
        public void TheOffer_IsNeverLongerThanWhatIsOpen_AndRepeatsForTheSameSeed()
        {
            var talents = new TalentState();
            var open = TalentRules.Nodes.Count(n => BoonRules.CanTake(talents, n, MetaProgress.BaseBoonTier));
            var all = BoonRules.Offer(talents, MetaProgress.BaseBoonTier, open + 10, new System.Random(4));
            Assert.AreEqual(open, all.Count);
            CollectionAssert.AllItemsAreUnique(all);
            Assert.IsEmpty(BoonRules.Offer(talents, MetaProgress.BaseBoonTier, 0, new System.Random(4)));
            CollectionAssert.AreEqual(BoonRules.Offer(talents, MetaProgress.BaseBoonTier, 3, new System.Random(9)),
                BoonRules.Offer(talents, MetaProgress.BaseBoonTier, 3, new System.Random(9)));
        }

        [Test]
        public void AMaxedBoon_IsNotOfferedAgain()
        {
            var talents = new TalentState();
            var node = TalentRules.Get("fire_arrow");
            for (var i = 0; i < node.MaxRanks; i++)
                talents.Grant(node.Id);
            Assert.IsFalse(BoonRules.CanTake(talents, node, MetaProgress.BaseBoonTier));
            for (var seed = 0; seed < 100; seed++)
                CollectionAssert.DoesNotContain(BoonRules.Offer(talents, MetaProgress.BaseBoonTier, 4, new System.Random(seed)), node.Id);
        }

        [Test]
        public void ThunderAndPrey_AtTheirEdges()
        {
            Assert.IsFalse(UniqueBoonRules.IsThunderShot(0));
            Assert.IsFalse(UniqueBoonRules.IsThunderShot(-8));
            Assert.IsFalse(UniqueBoonRules.IsThunderShot(UniqueBoonRules.ThunderEvery - 1));
            Assert.IsTrue(UniqueBoonRules.IsThunderShot(UniqueBoonRules.ThunderEvery * 2));
            Assert.AreEqual(-1, UniqueBoonRules.Toughest(new List<float>()));
            Assert.AreEqual(1, UniqueBoonRules.Toughest(new List<float> { 5f, 9f, 9f, 2f }), "the first on a tie");
        }

        [Test]
        public void ABlade_InAnOpenSpot_TurnsStraightBack_AndInACorner_BothWays()
        {
            var velocity = new Vector2(6f, 3f);
            var back = ProjectileKinds.Bounce(Vector2.zero, velocity, c => true);
            Assert.AreEqual(-velocity.x, back.x, 1e-5f);
            Assert.AreEqual(-velocity.y, back.y, 1e-5f);
            // Near the right tip of a lone open cell, a step along either axis leaves it.
            var tip = new Vector2(0.4f, 0.5f);
            var corner = ProjectileKinds.Bounce(tip, velocity, c => c == IsoMath.GroundToCell(tip));
            Assert.AreEqual(-velocity.x, corner.x, 1e-5f);
            Assert.AreEqual(-velocity.y, corner.y, 1e-5f);
            Assert.AreEqual(Vector2.zero, ProjectileKinds.Bounce(Vector2.zero, Vector2.zero, c => true));
        }

        [Test]
        public void ABlade_KeepsItsSpeed_OffAWallOnOneSide()
        {
            // A wall above (ground y >= 1): flying up and right, it turns back in y only.
            bool Walkable(Vector2Int c) => IsoMath.CellToGround(c).y < 1f;
            var at = new Vector2(0f, 0.9f);
            Assert.IsTrue(Walkable(IsoMath.GroundToCell(at)));
            var velocity = new Vector2(2f, 8f);
            var bounced = ProjectileKinds.Bounce(at, velocity, Walkable);
            Assert.AreEqual(-velocity.y, bounced.y, 1e-5f);
            Assert.AreEqual(velocity.x, bounced.x, 1e-5f, "the open side keeps its way");
            Assert.AreEqual(velocity.magnitude, bounced.magnitude, 1e-4f);
        }

        [Test]
        public void Prices_AtTheirEdges()
        {
            Assert.AreEqual(0, SellRules.Price(null));
            Assert.AreEqual(0, SellRules.Price(new Item(ItemSlot.Ring, ItemRarity.Rare, 9, blueprint: true)));
            Assert.AreEqual(SellRules.Price(new Item(ItemSlot.Ring, ItemRarity.Common, 1)), SellRules.Price(new Item(ItemSlot.Ring, ItemRarity.Common, 0)),
                "an item level below 1 is priced as 1");
            Assert.GreaterOrEqual(SellRules.Price(new Item(ItemSlot.Ring, ItemRarity.Common, 1)), 1);
            var session = new GameSession(1);
            var ring = new Item(ItemSlot.Ring, ItemRarity.Magic, 12);
            Assert.AreEqual(Mathf.RoundToInt(SellRules.Price(ring) * SellRules.RogueliteFactor), session.SellPrice(ring));
        }

        [Test]
        public void EveryUpgrade_CostsMoreEachRank_InWholeTens_AndStopsAtItsLast()
        {
            foreach (var upgrade in MetaRules.All)
            {
                var last = 0;
                for (var rank = 0; rank < upgrade.MaxRanks; rank++)
                {
                    var cost = MetaRules.Cost(upgrade.Id, rank);
                    Assert.AreEqual(0, cost % 10, upgrade.Name);
                    Assert.Greater(cost, last, upgrade.Name + " rank " + rank);
                    last = cost;
                }
                var meta = new MetaProgress();
                for (var rank = 0; rank < upgrade.MaxRanks; rank++)
                    Assert.IsTrue(meta.Raise(upgrade.Id));
                Assert.IsTrue(meta.IsMaxed(upgrade.Id));
                Assert.IsFalse(meta.Raise(upgrade.Id), upgrade.Name);
                meta.Restore(upgrade.Id, upgrade.MaxRanks + 5);
                Assert.AreEqual(upgrade.MaxRanks, meta.Rank(upgrade.Id), "a saved rank above the last is capped");
            }
            Assert.AreEqual(300, MetaRules.Cost(MetaUpgradeId.Might, 0));
            Assert.AreEqual(600, MetaRules.Cost(MetaUpgradeId.Might, 1));
        }

        [Test]
        public void Blueprints_AtTheirEdges()
        {
            Assert.IsFalse(BlueprintRules.TryParse(null, out _, out _));
            Assert.IsFalse(BlueprintRules.TryParse("", out _, out _));
            Assert.IsFalse(BlueprintRules.TryParse("Chest", out _, out _));
            Assert.IsFalse(BlueprintRules.TryParse("Chest/-1", out _, out _));
            Assert.IsFalse(BlueprintRules.TryParse("Chest/" + ItemNames.LookCount(ItemSlot.Chest), out _, out _));
            Assert.IsTrue(BlueprintRules.TryParse(BlueprintRules.Key(ItemSlot.Chest, 1), out var kind, out var look));
            Assert.AreEqual(ItemSlot.Chest, kind);
            Assert.AreEqual(1, look);

            // Every kind known at a level: nothing more to roll there.
            var known = new HashSet<string>();
            foreach (var k in BlueprintRules.Kinds)
                known.Add(BlueprintRules.Key(k, ItemNames.LookIndex(k, 10)));
            Assert.IsNull(BlueprintRules.Roll(10, known, new System.Random(1)));

            Assert.Greater(BlueprintRules.ForgeCost(1), BlueprintRules.ForgeCost(0));
            var lastLook = ItemNames.LookCount(ItemSlot.Chest) - 1;
            Assert.AreEqual(System.Math.Max(ItemNames.LevelOfLook(ItemSlot.Chest, lastLook), 60), BlueprintRules.ForgeLevel(ItemSlot.Chest, lastLook, 60));
            Assert.AreEqual(ItemNames.LevelOfLook(ItemSlot.Chest, 0), BlueprintRules.ForgeLevel(ItemSlot.Chest, 0, 60));
        }

        [Test]
        public void TheSummary_SaysHowTheRunEnded_AndShowsHoursAndBosses()
        {
            Assert.AreEqual("0:00", RunSummary.Duration(0f));
            Assert.AreEqual("0:59", RunSummary.Duration(59.9f));
            Assert.AreEqual("1:01:05", RunSummary.Duration(3665f));

            var summary = new RunSummary { End = RunEnd.Won, Depth = 48, DeepestEver = 48, Stats = new RunStats { Kills = 10, Bosses = 8, Blueprints = 1 } };
            Assert.AreEqual("The Run Is Won", summary.Title);
            StringAssert.StartsWith("The First Watchman", summary.Headline);
            var lines = summary.Lines().ToList();
            Assert.IsTrue(lines.Any(l => l.Contains("bosses  8")));
            Assert.IsTrue(lines.Any(l => l.Contains("brought home")));
            Assert.IsTrue(lines.Any(l => l.Contains("new for Thorin")));

            var died = new RunSummary { End = RunEnd.Died, Depth = 3, Stats = new RunStats() };
            Assert.IsFalse(died.Lines().Any(l => l.Contains("Blueprints")), "no blueprint line when none were found");
            Assert.AreEqual("Home Through the Portal", new RunSummary { End = RunEnd.Portal, Stats = new RunStats() }.Title);
        }
    }
}
