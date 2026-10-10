using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class DungeonThemeTests
    {
        static List<RoomShape> Library() => PlaceholderRooms.All.Select(r => RoomShape.Parse(r.name, r.layout)).ToList();

        [Test]
        public void TheCryptRunsToTheTidewife_ThenCaves()
        {
            Assert.AreEqual(DungeonTheme.Crypt, DungeonRules.ThemeAt(1));
            Assert.AreEqual(DungeonTheme.Crypt, DungeonRules.ThemeAt(12));
            Assert.AreEqual(DungeonTheme.Cave, DungeonRules.ThemeAt(13));
            Assert.AreEqual(DungeonTheme.Cave, DungeonRules.ThemeAt(24));
            Assert.AreEqual(DungeonTheme.Crypt, DungeonGenerator.Generate(7919, 4, Library()).Theme);
            Assert.AreEqual(DungeonTheme.Cave, DungeonGenerator.Generate(7919, 16, Library()).Theme);
        }

        [Test]
        public void CryptWalls_AreNotAllRightAngles()
        {
            // Cut corners and round rooms leave wall cells that the floor meets on two touching sides.
            for (var seed = 1; seed <= 10; seed++)
            {
                var layout = DungeonGenerator.Generate(seed * 7919, 5, Library());
                bool Open(int x, int y) => layout.Get(x, y) == DungeonCell.Floor;
                var diagonals = 0;
                for (var x = layout.Bounds.xMin; x < layout.Bounds.xMax; x++)
                    for (var y = layout.Bounds.yMin; y < layout.Bounds.yMax; y++)
                        if (layout.Get(x, y) == DungeonCell.Wall && ThemePieces.Diagonal(Open, x, y) != null)
                            diagonals++;
                Assert.Greater(diagonals, 40, $"seed {seed}");
            }
        }

        [Test]
        public void CaveTerraces_AreRingedByLedges_AndReachedByTheirSteps()
        {
            var withTerraces = 0;
            for (var seed = 1; seed <= 12; seed++)
            {
                var layout = DungeonGenerator.Generate(seed * 7919, 17, Library());
                var label = $"seed {seed}";
                if (layout.Raised.Count > 0)
                    withTerraces++;
                foreach (var cell in layout.Ledges)
                    Assert.AreEqual(DungeonCell.Wall, layout.Get(cell), label + ": a ledge blocks the way");
                foreach (var pair in layout.TerraceStairs)
                {
                    Assert.IsTrue(layout.IsFloor(pair.Key), label + ": steps are walked on");
                    Assert.IsTrue(layout.IsFloor(pair.Key + pair.Value), label + ": the way down is floor");
                }
                // Every raised cell is reached from the arrival, through the steps.
                var seen = new HashSet<Vector2Int> { layout.ArrivalFromAbove };
                var queue = new Queue<Vector2Int>(seen);
                while (queue.Count > 0)
                {
                    var cell = queue.Dequeue();
                    foreach (var step in RoomShape.Sides)
                        if (layout.IsFloor(cell + step) && seen.Add(cell + step))
                            queue.Enqueue(cell + step);
                }
                foreach (var cell in layout.Raised.Where(layout.IsFloor))
                    Assert.IsTrue(seen.Contains(cell), label + $": raised {cell} cut off");
            }
            Assert.GreaterOrEqual(withTerraces, 10, "most cave levels have terraces");
        }

        [Test]
        public void Stairs_StandInAFarWall_WithFloorBeforeThem()
        {
            var inWalls = 0;
            var total = 0;
            foreach (var depth in new[] { 2, 5, 9, 14, 20 })
                for (var seed = 1; seed <= 8; seed++)
                {
                    var layout = DungeonGenerator.Generate(seed * 7919, depth, Library());
                    foreach (var (cell, into) in new[] { (layout.StairsUp, layout.StairsUpInto), (layout.StairsDown, layout.StairsDownInto) })
                    {
                        if (cell == layout.StairsDown && !layout.HasStairsDown)
                            continue;
                        total++;
                        Assert.IsTrue(layout.IsFloor(cell), $"depth {depth}, seed {seed}: the stairs' cell is floor");
                        if (into == Vector2Int.zero)
                            continue;
                        inWalls++;
                        Assert.IsTrue(into == Vector2Int.right || into == Vector2Int.up, "a far wall, seen face on");
                        Assert.AreEqual(DungeonCell.Wall, layout.Get(cell + into));
                        Assert.IsTrue(layout.IsFloor(cell - into), "room to walk up to them");
                    }
                }
            Assert.GreaterOrEqual(inWalls, total * 9 / 10, "nearly all stairs stand in a wall");
        }

        [Test]
        public void Pieces_FollowTheTheme()
        {
            bool Nothing(int x, int y) => false;
            Assert.AreEqual("crypt_pillar_1", ThemePieces.Wall(DungeonTheme.Crypt, Nothing, 0, 0, false, true, false, 0));
            Assert.AreEqual("crypt_wall_low_1", ThemePieces.Wall(DungeonTheme.Crypt, Nothing, 0, 0, true, false, false, 0));
            Assert.AreEqual("cave_ledge_1", ThemePieces.Wall(DungeonTheme.Cave, Nothing, 0, 0, false, true, true, 0));
            Assert.AreEqual("stalagmite_1", ThemePieces.Wall(DungeonTheme.Cave, Nothing, 0, 0, false, true, false, 0));
            Assert.AreEqual("cave_wall_low_1", ThemePieces.Wall(DungeonTheme.Cave, Nothing, 0, 0, true, false, false, 0));
            Assert.AreEqual("cave_stairs_ny", ThemePieces.Stairs(Vector2Int.down));
            // Floor toward -x and -y: the wall fills the far half, facing the camera.
            Assert.AreEqual("b", ThemePieces.Diagonal((x, y) => x < 0 && y == 0 || y < 0 && x == 0, 0, 0));
            Assert.AreEqual("f", ThemePieces.Diagonal((x, y) => x > 0 && y == 0 || y > 0 && x == 0, 0, 0));
            for (var hash = 0; hash < 40; hash++)
                Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(ThemePieces.Wall(DungeonTheme.Crypt, Nothing, 0, 0, false, false, false, hash), "^crypt_wall_[1-5]$"));
        }
    }
}
