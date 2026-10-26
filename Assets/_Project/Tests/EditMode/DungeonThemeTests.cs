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
        public void TheCryptRunsToTheTidewife_ThenThePrison_TheLibrary_AndCaves()
        {
            Assert.AreEqual(DungeonTheme.Crypt, DungeonRules.ThemeAt(1));
            Assert.AreEqual(DungeonTheme.Crypt, DungeonRules.ThemeAt(12));
            Assert.AreEqual(DungeonTheme.Prison, DungeonRules.ThemeAt(13));
            Assert.AreEqual(DungeonTheme.Prison, DungeonRules.ThemeAt(24));
            Assert.AreEqual(DungeonTheme.Library, DungeonRules.ThemeAt(25));
            Assert.AreEqual(DungeonTheme.Library, DungeonRules.ThemeAt(36));
            Assert.AreEqual(DungeonTheme.Cave, DungeonRules.ThemeAt(37));
            Assert.AreEqual(DungeonTheme.Cave, DungeonRules.ThemeAt(48));
            Assert.AreEqual(DungeonTheme.Crypt, DungeonGenerator.Generate(7919, 4, Library()).Theme);
            Assert.AreEqual(DungeonTheme.Prison, DungeonGenerator.Generate(7919, 16, Library()).Theme);
            Assert.AreEqual(DungeonTheme.Library, DungeonGenerator.Generate(7919, 28, Library()).Theme);
            Assert.AreEqual(DungeonTheme.Cave, DungeonGenerator.Generate(7919, 40, Library()).Theme);
        }

        [Test]
        public void PrisonHalls_HoldCages_StandingFree_WithTheirInsidesReached()
        {
            var withCages = 0;
            for (var seed = 1; seed <= 8; seed++)
            {
                var layout = DungeonGenerator.Generate(seed * 7919, 16, Library());
                if (layout.FreeStanding.Count >= 30)
                    withCages++;
                // Every floor cell, the cages' insides too, is reached from the arrival.
                var seen = new HashSet<Vector2Int> { layout.ArrivalFromAbove };
                var queue = new Queue<Vector2Int>(seen);
                while (queue.Count > 0)
                {
                    var cell = queue.Dequeue();
                    foreach (var step in RoomShape.Sides)
                        if (layout.IsFloor(cell + step) && seen.Add(cell + step))
                            queue.Enqueue(cell + step);
                }
                var b = layout.Bounds;
                for (var x = b.xMin; x < b.xMax; x++)
                    for (var y = b.yMin; y < b.yMax; y++)
                        if (layout.IsFloor(new Vector2Int(x, y)))
                            Assert.IsTrue(seen.Contains(new Vector2Int(x, y)), $"seed {seed}: ({x}, {y}) cut off");
            }
            Assert.GreaterOrEqual(withCages, 6, "most prison levels have cages");
        }

        [Test]
        public void CryptWalls_AreNotAllRightAngles()
        {
            // Cut corners and round rooms leave wall cells that the floor meets on two touching sides.
            for (var seed = 1; seed <= 10; seed++)
            {
                // The open halls' crypt; the cathedral's walls are all right angles (2026-10-26).
                var layout = DungeonGenerator.Generate(seed * 7919, 5, Library(), new DungeonSettings { Cathedral = false });
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
        public void LibraryHalls_HaveAislesOfShelves_StandingFree()
        {
            // A shelf: a free-standing wall cell in a row (a wall neighbour along x or y). Most library levels have many.
            var withAisles = 0;
            for (var seed = 1; seed <= 8; seed++)
            {
                var layout = DungeonGenerator.Generate(seed * 7919, 28, Library());
                var shelves = 0;
                foreach (var cell in layout.FreeStanding)
                    if (layout.Get(cell + Vector2Int.right) == DungeonCell.Wall || layout.Get(cell + Vector2Int.up) == DungeonCell.Wall)
                        shelves++;
                if (shelves >= 30)
                    withAisles++;
            }
            Assert.GreaterOrEqual(withAisles, 6, "most library levels have aisles of shelves");
        }

        [Test]
        public void CaveTerraces_AreRingedByLedges_AndReachedByTheirSteps()
        {
            var withTerraces = 0;
            for (var seed = 1; seed <= 12; seed++)
            {
                var layout = DungeonGenerator.Generate(seed * 7919, 41, Library());
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
            Assert.AreEqual("cave_stairs_ny", ThemePieces.Stairs(DungeonTheme.Cave, Vector2Int.down));
            Assert.AreEqual("crypt_stairs_nx", ThemePieces.Stairs(DungeonTheme.Crypt, Vector2Int.left));
            Assert.AreEqual("crypt_chasm", ThemePieces.Chasm(false, false));
            Assert.AreEqual("crypt_chasm_xy", ThemePieces.Chasm(true, true));
            Assert.AreEqual("crypt_chasm_y", ThemePieces.Chasm(false, true));
            Assert.AreEqual("crypt_ledge_1", ThemePieces.Wall(DungeonTheme.Crypt, Nothing, 0, 0, false, true, true, 0));
            // Floor toward -x and -y: the wall fills the far half, facing the camera.
            Assert.AreEqual("b", ThemePieces.Diagonal((x, y) => x < 0 && y == 0 || y < 0 && x == 0, 0, 0));
            Assert.AreEqual("f", ThemePieces.Diagonal((x, y) => x > 0 && y == 0 || y > 0 && x == 0, 0, 0));
            // The drawn wall (2026-10-26): the thirds of the picture by the cell, running on along x and along y.
            Assert.AreEqual("crypt_wall_f00", ThemePieces.Wall(DungeonTheme.Crypt, Nothing, 0, 0, false, false, false, 7));
            Assert.AreEqual("crypt_wall_f01", ThemePieces.Wall(DungeonTheme.Crypt, Nothing, 1, 0, false, false, false, 7));
            Assert.AreEqual("crypt_wall_f02", ThemePieces.Wall(DungeonTheme.Crypt, Nothing, -1, 0, false, false, false, 7));
            Assert.AreEqual("crypt_wall_f20", ThemePieces.Wall(DungeonTheme.Crypt, Nothing, 0, 1, false, false, false, 7));
            Assert.AreEqual("crypt_wall_f10", ThemePieces.Wall(DungeonTheme.Crypt, Nothing, 0, -1, false, false, false, 7));
        }
        [Test]
        public void NoThinWall_ReachesIntoARoom_OutsideTheCaves()
        {
            // 2026-10-13, the owner: stubs of wall joined to a room's side were cut low on the camera side and read as
            // loose slabs. A wall one cell thin with floor on both sides, not standing free, is such a stub.
            var found = new List<string>();
            foreach (var seed in new[] { 7919, 31, 4242 })
                for (var depth = 1; depth <= 36; depth++)
                {
                    var layout = DungeonGenerator.Generate(seed, depth, Library());
                    // Diablo 1's cathedral is built of walls one cell thin, at full height (2026-10-26).
                    if (layout.Cathedral)
                        continue;
                    bool Open(int x, int y) => layout.Get(x, y) == DungeonCell.Floor;
                    var b = layout.Bounds;
                    var count = 0;
                    for (var x = b.xMin; x < b.xMax; x++)
                        for (var y = b.yMin; y < b.yMax; y++)
                            // A dais's rim (2026-10-20) is one cell thin by design, drawn as its own low piece.
                            if (layout.Get(x, y) == DungeonCell.Wall && !layout.FreeStanding.Contains(new UnityEngine.Vector2Int(x, y)) &&
                                !layout.Ledges.Contains(new UnityEngine.Vector2Int(x, y)) && !layout.Chasm.Contains(new UnityEngine.Vector2Int(x, y)) &&
                                ((Open(x - 1, y) && Open(x + 1, y)) || (Open(x, y - 1) && Open(x, y + 1))))
                                count++;
                    if (count > 0)
                        found.Add($"seed {seed} depth {depth}: {count}");
                }
            Assert.IsEmpty(found, string.Join("; ", found));
        }

        [Test]
        public void CryptHalls_SomeGetADais_WithStepsDownTowardTheCamera()
        {
            // The owner, 2026-10-20: "Add verticality, stairs, platforms".
            var daises = 0;
            foreach (var seed in new[] { 7919, 31, 4242 })
                for (var depth = 1; depth <= 12; depth++)
                {
                    var layout = DungeonGenerator.Generate(seed, depth, Library(), new DungeonSettings { Tiered = false, Cathedral = false });
                    if (layout.Raised.Count > 0)
                        daises++;
                    foreach (var pair in layout.TerraceStairs)
                    {
                        Assert.IsTrue(pair.Value.x < 0 || pair.Value.y < 0, "steps come down toward the camera");
                        Assert.IsTrue(layout.IsFloor(pair.Key + pair.Value), "floor at the foot");
                        Assert.IsTrue(layout.Raised.Contains(pair.Key - pair.Value) || layout.Sunken.Contains(pair.Key + pair.Value),
                            "a dais at the head, or a pit at the foot");
                    }
                }
            Assert.Greater(daises, 20, "most crypt levels have one");
        }

        [Test]
        public void SomeCryptHalls_HaveABalcony_RaisedAlongTheFarWall()
        {
            // The owner, 2026-10-20: balconies with a pack's archers shooting down, reached by stairs.
            var balconies = 0;
            foreach (var seed in new[] { 7919, 31, 4242 })
                for (var depth = 1; depth <= 12; depth++)
                {
                    var layout = DungeonGenerator.Generate(seed, depth, Library(), new DungeonSettings { Tiered = false, Cathedral = false });
                    foreach (var pair in layout.Balconies)
                    {
                        balconies++;
                        var open = 0;
                        foreach (var cell in pair.Value)
                        {
                            Assert.IsTrue(layout.Raised.Contains(cell));
                            if (layout.IsFloor(cell))
                                open++;
                        }
                        Assert.Greater(open, 8, "room for the archers");
                    }
                }
            Assert.Greater(balconies, 5);
        }

        [Test]
        public void NoCryptHall_IsSplitByAChasm()
        {
            // The owner, 2026-10-20: "Bridges over chasms". Every level stays one piece (KeepReachable is checked by the
            // generator's own tests); here: chasms exist and are wall to walking.
            var chasms = 0;
            foreach (var seed in new[] { 7919, 31, 4242 })
                for (var depth = 1; depth <= 12; depth++)
                {
                    var layout = DungeonGenerator.Generate(seed, depth, Library(), new DungeonSettings { Tiered = false, Cathedral = false });
                    if (layout.Chasm.Count > 0)
                        chasms++;
                    foreach (var cell in layout.Chasm)
                        Assert.AreEqual(DungeonCell.Wall, layout.Get(cell));
                }
            // Switched off on 2026-10-25 (the owner: "Remove the pits/chasms").
            Assert.AreEqual(0, chasms);
        }

        [Test]
        public void BurialPits_AndCryptHallsWithTheirPurposes()
        {
            // 2026-10-21, the owner: "remove the pits, add the rest".
            var themes = new HashSet<HallTheme>();
            var thrones = 0;
            var pits = 0;
            var rugs = 0;
            foreach (var seed in new[] { 7919, 31, 4242, 77, 1234 })
                for (var depth = 1; depth <= 12; depth++)
                {
                    var layout = DungeonGenerator.Generate(seed, depth, Library(), new DungeonSettings { Cathedral = false });
                    if (layout.Sunken.Count > 0)
                        pits++;
                    foreach (var pair in layout.Halls)
                    {
                        themes.Add(pair.Value);
                        if (pair.Value == HallTheme.Throne)
                        {
                            thrones++;
                            Assert.AreEqual(DungeonGenerator.ThroneDepth, depth);
                            Assert.IsTrue(layout.Props.ContainsValue(PropKind.ThroneX) || layout.Props.ContainsValue(PropKind.ThroneY));
                        }
                    }
                    foreach (var pair in layout.Decals)
                        if (RugRules.IsRug(pair.Value))
                            rugs++;
                }
            Assert.Greater(rugs, 20, "carpets");
            // Pits back since 2026-10-25 (the owner: "re insert the pits").
            Assert.Greater(pits, 5, "burial pits");
            Assert.IsTrue(themes.Contains(HallTheme.Dining) && themes.Contains(HallTheme.Chapel) && themes.Contains(HallTheme.Sarcophagi));
            Assert.LessOrEqual(thrones, 5, "one at most a level, only at the throne depth");
        }

        [Test]
        public void Rugs_HaveTheirBorderOnTheOuterSide()
        {
            Assert.AreEqual(DecalKind.RugXLow, RugRules.Kind(true, false));
            Assert.AreEqual(DecalKind.RugYHigh, RugRules.Kind(false, true));
            var red = RugRules.Colour(DecalKind.RugXLow, 0.1f, 0.3f, 0f);
            var border = RugRules.Colour(DecalKind.RugXLow, 0.1f, -0.35f, 0f);
            Assert.AreNotEqual(red.r, border.r);
            // The high side's border is at +0.5 across.
            Assert.AreEqual(border.r, RugRules.Colour(DecalKind.RugXHigh, 0.1f, 0.35f, 0f).r);
        }

        [Test]
        public void TheCarpetPicture_SpansBothCellsAcross_AndRepeatsAlong()
        {
            // The low cell's outer side is the picture's left edge, the high cell's its right edge; they meet in the middle.
            Assert.AreEqual(0f, RugRules.ImagePoint(DecalKind.RugXLow, new Vector2Int(4, 7), 0f, -0.5f).x, 1e-5f);
            Assert.AreEqual(0.5f, RugRules.ImagePoint(DecalKind.RugXLow, new Vector2Int(4, 7), 0f, 0.5f).x, 1e-5f);
            Assert.AreEqual(0.5f, RugRules.ImagePoint(DecalKind.RugXHigh, new Vector2Int(4, 8), 0f, -0.5f).x, 1e-5f);
            Assert.AreEqual(1f, RugRules.ImagePoint(DecalKind.RugYHigh, new Vector2Int(9, 3), 0.5f, 0f).x, 1e-5f);
            // Along, it runs on from cell to cell and repeats every CellsPerImage cells.
            var a = RugRules.ImagePoint(DecalKind.RugXLow, new Vector2Int(4, 7), 0.5f, 0f).y;
            var b = RugRules.ImagePoint(DecalKind.RugXLow, new Vector2Int(5, 7), -0.5f, 0f).y;
            Assert.AreEqual(a, b, 1e-5f);
            Assert.AreEqual(RugRules.ImagePoint(DecalKind.RugYLow, new Vector2Int(1, 3), 0.2f, 0.1f).y,
                RugRules.ImagePoint(DecalKind.RugYLow, new Vector2Int(1, 3 + (int)RugRules.CellsPerImage), 0.2f, 0.1f).y, 1e-5f);
        }


        [Test]
        public void TheCrypt_Climbs_HallByHall_UpTheScreen_WhenTiered()
        {
            // The owner, 2026-10-21: vertical levels, "you fight upwards. From the bottom up".
            foreach (var seed in new[] { 7919, 31, 4242, 77, 1234 })
                for (var depth = 1; depth <= 12; depth++)
                {
                    var layout = DungeonGenerator.Generate(seed, depth, Library(), new DungeonSettings { Tiered = true, Cathedral = false });
                    var label = $"seed {seed} depth {depth}";
                    Assert.IsTrue(layout.Tiered, label);
                    Assert.That(layout.Rooms.Count, Is.InRange(7, 9), label);
                    // Each hall a step up the screen from the one before (+x or +y on the room grid), never sideways.
                    for (var i = 1; i < layout.Rooms.Count; i++)
                    {
                        var step = layout.Rooms[i].Macro - layout.Rooms[i - 1].Macro;
                        Assert.IsTrue(step == Vector2Int.right || step == Vector2Int.up, label);
                    }
                    // The last hall stands higher than the first, by more than half the halls.
                    var first = layout.TierLevel(layout.ArrivalFromAbove);
                    var lastRoom = layout.Rooms[layout.Rooms.Count - 1].Interior;
                    var top = layout.TierLevel(new Vector2Int(lastRoom.xMin + lastRoom.width / 2, lastRoom.yMin + lastRoom.height / 2));
                    Assert.Greater(top - first, (layout.Rooms.Count - 1) / 2, label);
                    // Every step between levels is a flight toward the camera from the higher floor.
                    foreach (var pair in layout.TerraceStairs)
                        Assert.IsTrue(pair.Value.x < 0 || pair.Value.y < 0, label);
                }
            Assert.IsFalse(DungeonGenerator.Generate(7919, 16, Library(), new DungeonSettings { Tiered = true, Cathedral = false }).Tiered, "the prison does not climb");
            // Off by default since 2026-10-25 (the owner: "remove the vertical dungeon design").
            Assert.IsFalse(DungeonGenerator.Generate(7919, 4, Library()).Tiered);
        }
    }
}
