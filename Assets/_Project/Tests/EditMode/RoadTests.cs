using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class RoadTests
    {
        [Test]
        public void EveryStretch_IsOneWalkableHall_FromTheBottomBeaconToTheRiftAndTheWayOn()
        {
            for (var stretch = 1; stretch <= RoadRules.Rifts + 1; stretch++)
                foreach (var seed in new[] { 1, 77, 4242 })
                {
                    var road = RoadGenerator.Generate(seed, stretch);
                    var layout = road.Layout;
                    var reached = Flood(layout, layout.ArrivalFromAbove);
                    var name = $"stretch {stretch}, seed {seed}";
                    Assert.IsTrue(reached.Contains(road.Rift), name + ": the rift");
                    Assert.IsTrue(reached.Contains(layout.StairsUp), name + ": the way back down");
                    Assert.IsTrue(reached.Contains(layout.Waypoint), name + ": the waystone");
                    if (layout.HasStairsDown)
                        Assert.IsTrue(reached.Contains(layout.StairsDown), name + ": the way on");
                    foreach (var chest in layout.Chests)
                        Assert.IsTrue(reached.Contains(chest), name + ": a chest");
                    Assert.AreEqual(DungeonRules.IsBossDepth(stretch), layout.HasBossArena, name + ": a boss every sixth");
                }
        }

        [Test]
        public void TheHall_GoesUpTheScreen_AboutSeventyUnitsAStretch_AndTheEndlessOneFarLonger()
        {
            var road = RoadGenerator.Generate(5, 3);
            var points = road.Path.Points;
            for (var i = 1; i < points.Count; i++)
                Assert.Greater(points[i].y, points[i - 1].y, "every leg climbs");
            Assert.That(road.Path.Length, Is.InRange(55f, 100f));
            Assert.Less(road.Path.Progress(IsoMath.CellToGround(road.Layout.ArrivalFromAbove)), 5f, "she arrives at the bottom");
            Assert.AreEqual(road.Path.Length, road.Path.Progress(IsoMath.CellToGround(road.Rift)), 1f, "the rift is the top");
            Assert.Greater(RoadGenerator.Generate(5, RoadRules.Rifts + 1).Path.Length, 300f);
        }

        [Test]
        public void ThePath_MeasuresHowFarUp_AndFindsPointsAlongIt()
        {
            var path = new RoadPath(new List<Vector2> { Vector2.zero, new Vector2(0f, 10f), new Vector2(10f, 10f) });
            Assert.AreEqual(20f, path.Length, 1e-4f);
            Assert.AreEqual(5f, path.Progress(new Vector2(1f, 5f)), 1e-4f);
            Assert.AreEqual(15f, path.Progress(new Vector2(5f, 12f)), 1e-4f);
            Assert.AreEqual(new Vector2(0f, 7f), path.PointAt(7f));
            Assert.AreEqual(new Vector2(4f, 10f), path.PointAt(14f));
            Assert.AreEqual(new Vector2(10f, 10f), path.PointAt(99f));
        }

        [Test]
        public void TheStream_GrowsStrongerAndDenserTowardTheRift_WithNoTimer()
        {
            Assert.AreEqual(DungeonRules.EnemyLevel(4), RoadRules.PackLevel(4, 0f, 0f));
            Assert.AreEqual(DungeonRules.EnemyLevel(4) + 3, RoadRules.PackLevel(4, 1f, 70f));
            Assert.AreEqual(3, RoadRules.PackSize(0f));
            Assert.AreEqual(5, RoadRules.PackSize(1f));
            Assert.Less(RoadRules.MaxAwake(0f), RoadRules.MaxAwake(1f));
            Assert.Greater(RoadRules.SurgeSeconds(0f), RoadRules.SurgeSeconds(1f));
            Assert.AreEqual(PackKind.Normal, RoadRules.RollKind(1, 0f, 0.99f));
            Assert.AreEqual(PackKind.WithChampion, RoadRules.RollKind(1, 1f, 0.01f), "no elites on the first stretch");
            Assert.AreEqual(PackKind.Elite, RoadRules.RollKind(8, 1f, 0.01f));
            // The endless stretch keeps rising up the hall.
            Assert.Greater(RoadRules.PackLevel(RoadRules.Rifts + 1, 1f, 240f), RoadRules.PackLevel(RoadRules.Rifts + 1, 1f, 24f));
        }

        [Test]
        public void ClosedRifts_StayClosed_AndTheTownLeadsToTheOpenOne()
        {
            Assert.IsTrue(RoadRules.IsClosed(3, 5));
            Assert.IsFalse(RoadRules.IsClosed(5, 5));
            Assert.IsFalse(RoadRules.IsClosed(RoadRules.Rifts + 1, RoadRules.Rifts + 1), "the endless rift never closes");
            Assert.AreEqual(1, RoadRules.StretchFromTown(0));
            Assert.AreEqual(7, RoadRules.StretchFromTown(7));
            Assert.AreEqual(RoadRules.Rifts + 1, RoadRules.StretchFromTown(99));
        }

        [Test]
        public void Falling_CostsATenthOfHerGold_AndKeepsHerGear()
        {
            var session = new GameSession();
            session.AddGold(250);
            var worn = session.Equipment;
            Assert.AreEqual(25, session.FallOnRoad());
            Assert.AreEqual(225, session.Gold);
            Assert.AreEqual(worn, session.Equipment);
            Assert.AreEqual(1f, session.LifeFraction);
        }

        static HashSet<Vector2Int> Flood(DungeonLayout layout, Vector2Int start)
        {
            var seen = new HashSet<Vector2Int> { start };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            var steps = new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in steps)
                {
                    var next = cell + step;
                    if (layout.Get(next) == DungeonCell.Floor && seen.Add(next))
                        queue.Enqueue(next);
                }
            }
            return seen;
        }
    }
}
