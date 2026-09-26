using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class TravelTests
    {
        static List<RoomShape> Library() => PlaceholderRooms.All.Select(r => RoomShape.Parse(r.name, r.layout)).ToList();

        static HashSet<Vector2Int> Flood(DungeonLayout layout, Vector2Int start)
        {
            var seen = new HashSet<Vector2Int> { start };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    var next = cell + step;
                    if (layout.IsFloor(next) && seen.Add(next))
                        queue.Enqueue(next);
                }
            }
            return seen;
        }

        [Test]
        public void EveryLevel_HasAWaypointInItsStartRoom_OffTheArrivalAndTheStairs()
        {
            var shapes = Library();
            for (var seed = 1; seed <= 30; seed++)
                for (var depth = 1; depth <= DungeonRules.LevelsPerAct; depth++)
                {
                    var layout = DungeonGenerator.Generate(seed, depth, shapes);
                    var label = $"seed {seed} depth {depth}";
                    var start = layout.Rooms.First(r => r.Kind == RoomKind.Start).Interior;
                    Assert.IsTrue(layout.IsFloor(layout.Waypoint), label);
                    Assert.IsTrue(start.Contains(layout.Waypoint), label + ": in the start room");
                    Assert.AreNotEqual(layout.StairsUp, layout.Waypoint, label);
                    // Arriving by the stairs must not land on it, or its list would never open for the first time.
                    var gap = Vector2Int.Max(layout.Waypoint - layout.ArrivalFromAbove, layout.ArrivalFromAbove - layout.Waypoint);
                    Assert.GreaterOrEqual(Mathf.Max(gap.x, gap.y), 3, label + ": clear of the arrival point");
                    Assert.IsTrue(Flood(layout, layout.ArrivalFromAbove).Contains(layout.Waypoint), label + ": reachable");
                }
        }

        [Test]
        public void TheWanderer_StandsOnFloorBesideTheChest_AndCanBeReached()
        {
            var shapes = Library();
            for (var seed = 1; seed <= 30; seed++)
            {
                var layout = DungeonGenerator.Generate(seed, DungeonRules.PortalTomeDepth, shapes);
                var label = $"seed {seed}";
                Assert.IsTrue(layout.HasWandererSpot, label);
                Assert.IsTrue(layout.IsFloor(layout.WandererSpot), label);
                Assert.AreNotEqual(layout.Chests[0], layout.WandererSpot, label);
                Assert.IsTrue(Flood(layout, layout.ArrivalFromAbove).Contains(layout.WandererSpot), label + ": reachable");
            }
        }

        [Test]
        public void ALevel_IsStillTheSameForItsSeed()
        {
            // The waypoint and the Wanderer spot draw no random numbers (read the generator: they use NearestFloor only),
            // which is why no generator version bump was needed. This pins the part a test can: same seed, same level.
            var shapes = Library();
            var a = DungeonGenerator.Generate(42, 3, shapes);
            DungeonGenerator.Generate(7, 5, shapes);
            var b = DungeonGenerator.Generate(42, 3, shapes);
            Assert.AreEqual(a.Packs.Count, b.Packs.Count);
            Assert.AreEqual(a.Packs[0].Cell, b.Packs[0].Cell);
            Assert.AreEqual(a.Waypoint, b.Waypoint);
            Assert.AreEqual(a.WandererSpot, b.WandererSpot);
        }

        [Test]
        public void Waypoints_ActivateOnce_AndAreKeptInOrder()
        {
            var session = new GameSession();
            var modified = 0;
            session.Modified += () => modified++;

            Assert.IsTrue(session.ActivateWaypoint(3));
            Assert.IsTrue(session.ActivateWaypoint(1));
            Assert.IsFalse(session.ActivateWaypoint(3));
            Assert.IsFalse(session.ActivateWaypoint(0), "town is not a waypoint");
            CollectionAssert.AreEqual(new[] { 1, 3 }, session.Waypoints.ToArray());
            Assert.AreEqual(2, modified);
        }

        [Test]
        public void OnePortalAtATime_AndItClosesWhenUsedBack()
        {
            var session = new GameSession();
            session.OpenPortal(2, new Vector2(3f, 4f));
            session.OpenPortal(4, new Vector2(-1f, 8f));
            Assert.AreEqual(4, session.PortalDepth);
            Assert.AreEqual(new Vector2(-1f, 8f), session.PortalPosition);
            session.ClosePortal();
            Assert.AreEqual(0, session.PortalDepth);
        }

        [Test]
        public void ARoundTrip_KeepsWaypointsTheTomeAndAnOpenPortal()
        {
            var session = new GameSession(3);
            session.ActivateWaypoint(1);
            session.ActivateWaypoint(4);
            session.GivePortalTome();
            session.OpenPortal(4, new Vector2(12.5f, -7.25f));

            var json = SaveCodec.ToJson(SaveCodec.Capture(session, 1));
            Assert.IsTrue(SaveCodec.TryParse(json, out var data, out var error), error);
            var loaded = SaveCodec.Restore(data, 1);

            CollectionAssert.AreEqual(new[] { 1, 4 }, loaded.Waypoints.ToArray());
            Assert.IsTrue(loaded.HasPortalTome);
            Assert.AreEqual(4, loaded.PortalDepth);
            Assert.AreEqual(new Vector2(12.5f, -7.25f), loaded.PortalPosition);
        }

        [Test]
        public void AVersionSevenSave_LoadsWithNoWaypointsTomeOrPortal()
        {
            const string versionSeven =
                "{\"version\":7,\"savedAtUnixMs\":1790193596882,\"gold\":10,\"lifeFraction\":1.0,\"level\":5," +
                "\"equipped\":[],\"backpack\":[],\"corpses\":[],\"killed\":[],\"openedChests\":[],\"materials\":[]," +
                "\"stickTaught\":true,\"forgeIntroduced\":true,\"seenLegendary\":false,\"legendaryHintShown\":false,\"playSeconds\":300.0}";

            Assert.IsTrue(SaveCodec.TryParse(versionSeven, out var data, out var error), error);
            var session = SaveCodec.Restore(data, 1);
            Assert.AreEqual(SaveData.CurrentVersion, data.version);
            Assert.IsEmpty(session.Waypoints);
            Assert.IsFalse(session.HasPortalTome);
            Assert.AreEqual(0, session.PortalDepth);
            Assert.AreEqual(5, session.Level);
        }
    }
}
