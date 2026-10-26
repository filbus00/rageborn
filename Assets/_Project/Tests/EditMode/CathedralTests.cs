using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>The crypt laid out as Diablo 1's cathedral (2026-10-26).</summary>
    public class CathedralTests
    {
        // The dungeon's themes, as before the forest (2026-10-26).
        [SetUp]
        public void ThemesBack() => DungeonRules.Forest = false;

        [TearDown]
        public void ForestAgain() => DungeonRules.Forest = true;

        static List<RoomShape> Library() => PlaceholderRooms.All.Select(r => RoomShape.Parse(r.name, r.layout)).ToList();

        static IEnumerable<(int seed, int depth)> Cases()
        {
            foreach (var seed in new[] { 7919, 31, 4242, 77, 1234, 99 })
                foreach (var depth in new[] { 1, 2, 3, 5, 9, 11 })
                    yield return (seed, depth);
        }

        [Test]
        public void TheCrypt_IsACathedral_ButNotItsBossLevels()
        {
            Assert.IsTrue(DungeonGenerator.Generate(7919, 1, Library()).Cathedral);
            Assert.IsTrue(DungeonGenerator.Generate(7919, 11, Library()).Cathedral);
            Assert.IsFalse(DungeonGenerator.Generate(7919, 6, Library()).Cathedral, "the Cinder Warden keeps his arena");
            Assert.IsFalse(DungeonGenerator.Generate(7919, 16, Library()).Cathedral, "the prison");
            Assert.IsFalse(DungeonGenerator.Generate(7919, 3, Library(), new DungeonSettings { Cathedral = false }).Cathedral);
        }

        [Test]
        public void Rooms_AreRectangles_ApartByAWall_AndNeverOverlap()
        {
            foreach (var (seed, depth) in Cases())
            {
                var layout = DungeonGenerator.Generate(seed, depth, Library());
                var label = $"seed {seed}, depth {depth}";
                var rooms = layout.Rooms.Select(r => r.Interior).ToList();
                for (var i = 0; i < rooms.Count; i++)
                    for (var j = i + 1; j < rooms.Count; j++)
                    {
                        var grown = new RectInt(rooms[i].xMin - DungeonGenerator.CathedralWall, rooms[i].yMin - DungeonGenerator.CathedralWall,
                            rooms[i].width + 2 * DungeonGenerator.CathedralWall, rooms[i].height + 2 * DungeonGenerator.CathedralWall);
                        Assert.IsFalse(grown.Overlaps(rooms[j]), label + ": rooms a wall apart at least");
                    }
                // The cells just outside a room are wall wherever no opening was made: its edges run straight.
                foreach (var r in rooms)
                    for (var x = r.xMin; x < r.xMax; x++)
                        Assert.AreNotEqual(DungeonCell.Void, layout.Get(x, r.yMin - 1), label);
            }
        }

        [Test]
        public void Rooms_AreJoined_SoTheExitCanBeWalkedTo()
        {
            foreach (var (seed, depth) in Cases())
            {
                var layout = DungeonGenerator.Generate(seed, depth, Library());
                var label = $"seed {seed}, depth {depth}";
                Assert.AreEqual(RoomKind.Start, layout.Rooms[0].Kind, label);
                Assert.AreEqual(1, layout.Rooms.Count(r => r.Kind == RoomKind.Exit), label);
                Assert.IsTrue(layout.HasStairsDown, label);
                // Every floor cell is reached from the start room (KeepReachable walls off the rest), so every room's
                // middle that is floor is reached: count how many rooms keep floor.
                var floorRooms = layout.Rooms.Count(r => Enumerable.Range(r.Interior.xMin, r.Interior.width)
                    .Any(x => Enumerable.Range(r.Interior.yMin, r.Interior.height).Any(y => layout.IsFloor(new Vector2Int(x, y)))));
                Assert.AreEqual(layout.Rooms.Count, floorRooms, label + ": no room walled off");
                Assert.Greater(layout.Packs.Count, 5, label);
            }
        }

        [Test]
        public void SomeRooms_OpenThroughArcades_AndSomeHallsHaveColumns()
        {
            var columns = 0;
            foreach (var (seed, depth) in Cases())
            {
                var layout = DungeonGenerator.Generate(seed, depth, Library());
                columns += layout.FreeStanding.Count;
            }
            Assert.Greater(columns, 50, "pillars of arcades and columns of halls stand free");
        }

        [Test]
        public void SharedWalls_AreFoundBetweenNeighboursOnly()
        {
            var a = new RectInt(10, 10, 8, 8);
            Assert.IsTrue(DungeonGenerator.Shared(a, new RectInt(19, 12, 6, 6), out var x0, out var x1, out var y0, out var y1));
            Assert.AreEqual((18, 19, 12, 18), (x0, x1, y0, y1));
            Assert.IsTrue(DungeonGenerator.Shared(a, new RectInt(12, 1, 6, 8), out _, out _, out _, out _));
            Assert.IsFalse(DungeonGenerator.Shared(a, new RectInt(20, 12, 6, 6), out _, out _, out _, out _), "too far");
            Assert.IsFalse(DungeonGenerator.Shared(a, new RectInt(19, 16, 6, 6), out _, out _, out _, out _), "two cells in common only");
        }

        [Test]
        public void Rooms_ShareAWall_OrStandWellApart_NeverBackToBack()
        {
            // The owner, 2026-10-26: "avoid making more than one block wide walls".
            var a = new RectInt(10, 10, 8, 8);
            Assert.IsTrue(DungeonGenerator.GapAllowed(a, new RectInt(19, 10, 6, 6)), "one cell: a shared wall");
            Assert.IsFalse(DungeonGenerator.GapAllowed(a, new RectInt(20, 10, 6, 6)), "two walls back to back");
            Assert.IsFalse(DungeonGenerator.GapAllowed(a, new RectInt(21, 10, 6, 6)), "a slit of dark between two walls");
            Assert.IsTrue(DungeonGenerator.GapAllowed(a, new RectInt(22, 10, 6, 6)));
            Assert.IsFalse(DungeonGenerator.GapAllowed(a, new RectInt(18, 10, 6, 6)), "touching");
        }

        [Test]
        public void NoWall_IsTwoCellsThickBetweenFloors()
        {
            foreach (var (seed, depth) in Cases())
            {
                var layout = DungeonGenerator.Generate(seed, depth, Library());
                // Measured between rooms' floors: a doorway beside a corner where three rooms meet has the corner's two
                // wall cells in line with it, which is no thick wall.
                bool Open(int x, int y) => layout.Get(x, y) == DungeonCell.Floor &&
                                           layout.Rooms.Any(r => r.Interior.Contains(new Vector2Int(x, y)));
                // A wall, not furniture or a column standing inside a room.
                bool Wall(int x, int y) => layout.Get(x, y) == DungeonCell.Wall &&
                                           !layout.Rooms.Any(r => r.Interior.Contains(new Vector2Int(x, y)));
                var b = layout.Bounds;
                var where = "";
                for (var x = b.xMin + 1; x < b.xMax - 3; x++)
                    for (var y = b.yMin + 1; y < b.yMax - 3; y++)
                    {
                        if (Open(x, y) && Wall(x + 1, y) && Wall(x + 2, y) && Open(x + 3, y))
                            where += $" x-run at {x},{y}";
                        if (Open(x, y) && Wall(x, y + 1) && Wall(x, y + 2) && Open(x, y + 3))
                            where += $" y-run at {x},{y}";
                    }
                Assert.AreEqual("", where, $"seed {seed}, depth {depth}: " + string.Join(" ", layout.Rooms.Select(r => r.Interior)));
            }
        }
    }
}
