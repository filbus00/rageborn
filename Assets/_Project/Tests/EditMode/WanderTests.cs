using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>Packs that wander, and duos and trios roaming (2026-10-26).</summary>
    public class WanderTests
    {
        static List<RoomShape> Library() => PlaceholderRooms.All.Select(r => RoomShape.Parse(r.name, r.layout)).ToList();

        [Test]
        public void ATarget_LiesWithinTheRadius_AndSpreadsOverTheDisc()
        {
            var origin = new Vector2(10, 5);
            for (var i = 0; i < 50; i++)
                Assert.LessOrEqual(Vector2.Distance(origin, WanderRules.Target(origin, 3f, i / 49f, i * 0.37f % 1f)), 3f + 1e-4f);
            Assert.AreEqual(origin, WanderRules.Target(origin, 3f, 0f, 0.5f));
            Assert.AreEqual(3f, Vector2.Distance(origin, WanderRules.Target(origin, 3f, 1f, 0.2f)), 1e-4f);
        }

        [Test]
        public void ThePacksMiddle_DriftsAtItsSpeed_AndArrives()
        {
            var anchor = Vector2.zero;
            Assert.IsFalse(WanderRules.Step(ref anchor, new Vector2(4, 0), 1f));
            Assert.AreEqual(WanderRules.Speed, anchor.x, 1e-4f);
            for (var i = 0; i < 10; i++)
                WanderRules.Step(ref anchor, new Vector2(4, 0), 1f);
            Assert.AreEqual(new Vector2(4, 0), anchor);
        }

        [Test]
        public void Levels_HaveBigPacksWanderingALittle_AndDuosAndTriosRoaming()
        {
            foreach (var seed in new[] { 7919, 31, 4242 })
                foreach (var depth in new[] { 1, 3, 9 })
                {
                    var layout = DungeonGenerator.Generate(seed, depth, Library());
                    var label = $"seed {seed}, depth {depth}";
                    var roamers = layout.Packs.Where(p => p.Roams).ToList();
                    Assert.GreaterOrEqual(roamers.Count, 2, label);
                    Assert.IsTrue(roamers.All(p => p.Count >= 2 && p.Count <= 3), label);
                    Assert.IsTrue(layout.Packs.Where(p => !p.Roams).All(p => p.Wander == WanderRules.PackRadius), label);
                    Assert.IsTrue(layout.Packs.Any(p => !p.Roams && p.Count >= 8), label + ": the big packs stay");
                }
        }
    }
}
