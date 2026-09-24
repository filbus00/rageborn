using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class DungeonGeneratorTests
    {
        static readonly Vector2Int[] Steps = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };

        static List<RoomShape> Library() => PlaceholderRooms.All.Select(r => RoomShape.Parse(r.name, r.layout)).ToList();

        static IEnumerable<(int seed, int depth)> Cases()
        {
            for (var seed = 1; seed <= 40; seed++)
                for (var depth = 1; depth <= DungeonRules.LevelsPerAct; depth++)
                    yield return (seed * 7919, depth);
        }

        [Test]
        public void EveryPlaceholderRoom_Parses()
        {
            foreach (var (name, layout) in PlaceholderRooms.All)
                Assert.DoesNotThrow(() => RoomShape.Parse(name, layout), name);
        }

        [Test]
        public void Parse_ReadsRowsTopDown()
        {
            var shape = RoomShape.Parse("t", "#......\n.......\n.......\n.......\n.......\n.......\n.......");

            Assert.AreEqual(7, shape.Size);
            Assert.IsTrue(shape.IsBlocked(0, 6), "the first text row is the top row");
            Assert.IsFalse(shape.IsBlocked(0, 0));
        }

        [TestCase("........\n........\n........\n........\n........\n........\n........", "not square")]
        [TestCase(".......\n.......\n.......\n...x...\n.......\n.......\n.......", "unknown character")]
        [TestCase(".......\n.......\n.......\n......#\n.......\n.......\n.......", "blocks its doorway")]
        [TestCase(".......\n.#####.\n.#...#.\n.#...#.\n.#####.\n.......\n.......", "blocks its doorway")]
        [TestCase(".#......\n##......\n........\n........\n........\n........\n........\n........", "cannot be reached")]
        public void Parse_RejectsUnusableRooms(string text, string reason)
        {
            var e = Assert.Throws<FormatException>(() => RoomShape.Parse("bad", text));
            StringAssert.Contains(reason, e.Message);
        }

        [Test]
        public void TheSameSeed_BuildsTheSameLevel()
        {
            var a = DungeonGenerator.Generate(1234, 3, Library());
            var b = DungeonGenerator.Generate(1234, 3, Library());

            Assert.AreEqual(a.Bounds, b.Bounds);
            for (var x = a.Bounds.xMin; x < a.Bounds.xMax; x++)
                for (var y = a.Bounds.yMin; y < a.Bounds.yMax; y++)
                    Assert.AreEqual(a.Get(x, y), b.Get(x, y));
            CollectionAssert.AreEqual(a.Packs.Select(p => p.Cell), b.Packs.Select(p => p.Cell));
            CollectionAssert.AreEqual(a.Chests, b.Chests);
            Assert.AreEqual(a.StairsDown, b.StairsDown);
        }

        [Test]
        public void DifferentSeeds_BuildDifferentLevels()
        {
            var distinct = new HashSet<string>();
            for (var seed = 1; seed <= 20; seed++)
            {
                var layout = DungeonGenerator.Generate(seed, 1, Library());
                distinct.Add(string.Join(";", layout.Rooms.Select(r => r.Macro + r.Shape.Name)));
            }

            Assert.Greater(distinct.Count, 15);
        }

        [Test]
        public void EveryLevel_HasTheDocsRoomsAndGuarantees()
        {
            foreach (var (seed, depth) in Cases())
            {
                var layout = DungeonGenerator.Generate(seed, depth, Library());
                var label = $"seed {seed}, depth {depth}";

                Assert.That(layout.Rooms.Count, Is.InRange(7, 10), label + ": 5 to 8 rooms between start and exit");
                Assert.AreEqual(1, layout.Rooms.Count(r => r.Kind == RoomKind.Start), label);
                Assert.AreEqual(1, layout.Rooms.Count(r => r.Kind == RoomKind.Exit), label);
                Assert.GreaterOrEqual(layout.Rooms.Count(r => r.Kind == RoomKind.Elite), 1, label);
                Assert.GreaterOrEqual(layout.Chests.Count, 1, label + ": one guaranteed chest");
                Assert.GreaterOrEqual(layout.Packs.Count(p => p.Kind == PackKind.Elite), 1, label + ": one guaranteed elite pack");
                Assert.AreEqual(DungeonRules.EnemyLevel(depth), layout.EnemyLevel, label);
                Assert.AreEqual(depth < DungeonRules.LevelsPerAct, layout.HasStairsDown, label + ": the last level has no stairs down yet");
                Assert.IsFalse(layout.Packs.Any(p => layout.Rooms[p.Room].Kind == RoomKind.Start), label + ": the start room is empty");
            }
        }

        [Test]
        public void EveryFloorCell_IsReachable_AndEverythingStandsOnFloor()
        {
            foreach (var (seed, depth) in Cases())
            {
                var layout = DungeonGenerator.Generate(seed, depth, Library());
                var label = $"seed {seed}, depth {depth}";
                var reached = Flood(layout, layout.ArrivalFromAbove);

                var floor = 0;
                for (var x = layout.Bounds.xMin; x < layout.Bounds.xMax; x++)
                    for (var y = layout.Bounds.yMin; y < layout.Bounds.yMax; y++)
                        if (layout.Get(x, y) == DungeonCell.Floor)
                            floor++;
                Assert.AreEqual(floor, reached.Count, label + ": floor cut off from the arrival point");

                Assert.IsTrue(reached.Contains(layout.StairsUp), label);
                if (layout.HasStairsDown)
                {
                    Assert.IsTrue(reached.Contains(layout.StairsDown), label);
                    Assert.IsTrue(reached.Contains(layout.ArrivalFromBelow), label);
                }
                foreach (var chest in layout.Chests)
                    Assert.IsTrue(reached.Contains(chest), label);
                foreach (var pack in layout.Packs)
                    Assert.IsTrue(reached.Contains(pack.Cell), label);
            }
        }

        [Test]
        public void Floor_IsAlwaysEnclosedByWalls()
        {
            foreach (var (seed, depth) in Cases())
            {
                var layout = DungeonGenerator.Generate(seed, depth, Library());
                for (var x = layout.Bounds.xMin; x < layout.Bounds.xMax; x++)
                    for (var y = layout.Bounds.yMin; y < layout.Bounds.yMax; y++)
                    {
                        if (layout.Get(x, y) != DungeonCell.Floor)
                            continue;
                        // Checked by hand and asserted only on a failure: an assert per cell makes this test take half a minute.
                        for (var dx = -1; dx <= 1; dx++)
                            for (var dy = -1; dy <= 1; dy++)
                                if (layout.Get(x + dx, y + dy) == DungeonCell.Void)
                                    Assert.Fail($"seed {seed}, depth {depth}: floor at ({x}, {y}) touches the void");
                    }
            }
        }

        [Test]
        public void ArrivingNeverStartsAFight()
        {
            var settings = new DungeonSettings();
            foreach (var (seed, depth) in Cases())
            {
                var layout = DungeonGenerator.Generate(seed, depth, Library(), settings);
                foreach (var pack in layout.Packs)
                {
                    var aggro = pack.Kind == PackKind.Elite ? settings.EliteAggroRange : settings.NormalAggroRange;
                    // The nearest member can stand at the pack's radius from its center.
                    var needed = aggro + pack.Radius;
                    Assert.GreaterOrEqual(Ground(pack.Cell, layout.ArrivalFromAbove), needed, $"seed {seed}, depth {depth}: arriving from above");
                    if (layout.HasStairsDown)
                        Assert.GreaterOrEqual(Ground(pack.Cell, layout.ArrivalFromBelow), needed, $"seed {seed}, depth {depth}: arriving from below");
                }
            }
        }

        [Test]
        public void TheArrivalPoints_AreNextToTheirStairs_ButNotOnThem()
        {
            foreach (var (seed, depth) in Cases())
            {
                var layout = DungeonGenerator.Generate(seed, depth, Library());
                var up = Chebyshev(layout.StairsUp, layout.ArrivalFromAbove);
                Assert.That(up, Is.InRange(2, 5), $"seed {seed}, depth {depth}");
                if (layout.HasStairsDown)
                    Assert.That(Chebyshev(layout.StairsDown, layout.ArrivalFromBelow), Is.InRange(2, 5), $"seed {seed}, depth {depth}");
            }
        }

        [Test]
        public void LeavingTheStartRoom_NeverCrossesTheStairsUp()
        {
            // The arrival point is closer to every doorway of the start room than the stairs are, so walking toward a
            // door walks away from them.
            foreach (var (seed, depth) in Cases())
            {
                var layout = DungeonGenerator.Generate(seed, depth, Library());
                var room = layout.Rooms[0].Interior;
                var center = new Vector2(room.xMin + room.width / 2f, room.yMin + room.height / 2f);
                Assert.Less(Vector2.Distance(layout.ArrivalFromAbove, center), Vector2.Distance(layout.StairsUp, center),
                    $"seed {seed}, depth {depth}: the arrival point should be on the room's side of the stairs");
            }
        }

        [Test]
        public void EnemyLevel_SpreadsTheActOverItsSixLevels()
        {
            Assert.AreEqual(1, DungeonRules.EnemyLevel(1));
            Assert.AreEqual(12, DungeonRules.EnemyLevel(DungeonRules.LevelsPerAct));
            for (var depth = 2; depth <= DungeonRules.LevelsPerAct; depth++)
                Assert.Greater(DungeonRules.EnemyLevel(depth), DungeonRules.EnemyLevel(depth - 1));
        }

        [Test]
        public void EachDepth_HasItsOwnSeed()
        {
            var seeds = Enumerable.Range(1, DungeonRules.LevelsPerAct).Select(d => DungeonRules.LevelSeed(42, d)).ToList();

            Assert.AreEqual(seeds.Count, seeds.Distinct().Count());
            Assert.AreEqual(DungeonRules.LevelSeed(42, 3), DungeonRules.LevelSeed(42, 3));
        }

        [Test]
        public void ARoomTooBigForThePitch_IsRejected()
        {
            var huge = new RoomShape("huge", new bool[40, 40]);

            Assert.Throws<ArgumentException>(() => DungeonGenerator.Generate(1, 1, new[] { huge }));
        }

        static HashSet<Vector2Int> Flood(DungeonLayout layout, Vector2Int start)
        {
            var seen = new HashSet<Vector2Int> { start };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in Steps)
                {
                    var next = cell + step;
                    if (layout.IsFloor(next) && seen.Add(next))
                        queue.Enqueue(next);
                }
            }
            return seen;
        }

        static float Ground(Vector2Int a, Vector2Int b) => Vector2.Distance(IsoMath.CellToGround(a), IsoMath.CellToGround(b));

        static int Chebyshev(Vector2Int a, Vector2Int b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
    }
}
