using System.Linq;
using NUnit.Framework;

namespace ARPG.Tests
{
    /// <summary>The run summary (2026-10-15).</summary>
    public class RunSummaryTests
    {
        [Test]
        public void ADeath_SaysWhereAndWhatWasLost_AndANewRecordIsNamed()
        {
            var session = new GameSession(1);
            session.ReachDepth(1);
            session.ReachDepth(2);
            session.Run.Kills = 40;
            session.Run.Coins = 350;
            session.Run.Items = 3;
            session.Run.Seconds = 125f;
            session.Talents.Grant("fire_arrow");
            session.Talents.Grant("extra_arrow");
            session.Talents.Grant("extra_arrow");
            var next = session.NextRun(2, false);
            var summary = RunSummary.Of(session, RunEnd.Died, 0, next);
            Assert.AreEqual("The Run Is Over", summary.Title);
            StringAssert.Contains("depth 2", summary.Headline);
            StringAssert.Contains("new deepest", summary.Headline);
            var lines = summary.Lines().ToList();
            Assert.IsTrue(lines.Any(l => l.Contains("Time  2:05")));
            Assert.IsTrue(lines.Any(l => l.Contains("Enemies killed  40")));
            Assert.IsFalse(lines.Any(l => l.Contains("brought home")), "nothing comes home from a death");
            CollectionAssert.AreEquivalent(new[] { ("Extra Arrow", 2), ("Fire Arrow", 1) }, summary.Boons);
            Assert.AreEqual(0, next.Run.Kills, "the next run starts at zero");
        }

        [Test]
        public void APortal_CountsWhatCameHome_AndTheBlueprintsLearned()
        {
            var session = new GameSession(1);
            session.ReachDepth(1);
            session.Inventory.TryAdd(new Item(ItemSlot.Ring, ItemRarity.Rare, 3));
            session.Inventory.TryAdd(new Item(ItemSlot.Chest, ItemRarity.Rare, 9, blueprint: true));
            session.Run.Blueprints = 1;
            var learned = session.LearnCarriedBlueprints();
            var next = session.NextRun(2, true);
            var summary = RunSummary.Of(session, RunEnd.Portal, learned, next);
            Assert.AreEqual("Home Through the Portal", summary.Title);
            var lines = summary.Lines().ToList();
            Assert.IsTrue(lines.Any(l => l.Contains("new for Thorin  1")));
            Assert.IsTrue(lines.Any(l => l.Contains("brought home")));
            Assert.Greater(summary.ItemsHome, 1);
        }

        [Test]
        public void TheRunsNumbers_SurviveTheSave()
        {
            var session = new GameSession(1);
            session.ReachDepth(1);
            session.Run.Kills = 12;
            session.Run.Bosses = 1;
            session.Run.Seconds = 61f;
            var loaded = SaveCodec.Restore(SaveCodec.Capture(session, 0), 3);
            Assert.AreEqual(12, loaded.Run.Kills);
            Assert.AreEqual(1, loaded.Run.Bosses);
            Assert.AreEqual(61f, loaded.Run.Seconds, 0.01f);
            Assert.AreEqual("1:01:01", RunSummary.Duration(3661f));
        }
    }
}
