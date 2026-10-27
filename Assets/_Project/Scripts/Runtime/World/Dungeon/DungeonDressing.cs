using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Places a generated level's props and floor decals (the owner, 2026-09-30: "different floor textures, brushes,
    /// and whatever fits inside a dungeon"). Props stand against walls, clear of the doorways, the stairs, the arrival
    /// points, the waypoint, the chests and the packs, and never where they would cut the floor in two; decals are marks
    /// on the floor anywhere a player can walk. All numbers are tuning. Pure.
    /// </summary>
    public static class DungeonDressing
    {
        /// <summary>One prop per this many floor cells of a room.</summary>
        const int CellsPerProp = 70;

        /// <summary>One decal per this many floor cells.</summary>
        const int CellsPerDecal = 30;

        /// <summary>The crypt's floors carry about twice the marks (2026-10-20, okayed: "carpets, rubble, broken flagstones,
        /// old blood trails and scattered bones, so open floor is not bare cobbles").</summary>
        const int CryptCellsPerDecal = 16;

        /// <summary>Old blood trails dragged across a crypt level, and their length in cells.</summary>
        const int BloodTrails = 7, MinTrail = 7, MaxTrail = 15;

        /// <summary>Cells (Chebyshev) kept clear around a doorway's middle and around the stairs and similar spots.</summary>
        const int DoorClearance = 4;
        const int SpotClearance = 2;

        // The two looks' own clutter (2026-10-10): the crypt's urns, tombs, candles and fallen columns; the caves' rubble,
        // bones, torches and a few miners' stores.
        static readonly (PropKind kind, int weight)[] CryptPropWeights =
        {
            (PropKind.Urn, 18), (PropKind.BonePile, 12), (PropKind.Rubble, 8), (PropKind.BrokenColumn, 14), (PropKind.Sarcophagus, 12),
            (PropKind.Brazier, 8), (PropKind.Candles, 12), (PropKind.Barrel, 6), (PropKind.Crate, 4), (PropKind.Torch, 6),
            // The dead (the owner, 2026-10-20, okayed: "fallen adventurers with their packs, a burnt-out camp, hanging cages
            // with skeletons").
            (PropKind.DeadAdventurer, 8), (PropKind.ColdCamp, 4), (PropKind.HangingCage, 5), (PropKind.Coffin, 4),
        };

        static readonly (PropKind kind, int weight)[] CavePropWeights =
        {
            (PropKind.Rubble, 30), (PropKind.BonePile, 18), (PropKind.Torch, 10), (PropKind.Bucket, 8), (PropKind.Crate, 8),
            (PropKind.Barrel, 6), (PropKind.Brazier, 4),
        };

        // The forest (2026-10-26): logs, stumps, mushrooms, ferns, a standing stone now and then.
        static readonly (PropKind kind, int weight)[] ForestPropWeights =
        {
            (PropKind.Ferns, 26), (PropKind.Mushrooms, 18), (PropKind.Stump, 16), (PropKind.FallenLog, 16), (PropKind.StandingStone, 6),
            (PropKind.AxeStump, 4), (PropKind.BonePile, 4),
        };

        // The props drawn anew for the forest (2026-10-27, the owner's Gemini pictures); the forest places no others (the
        // owner: "remove the old props now").
        static readonly HashSet<PropKind> ForestDrawn = new HashSet<PropKind>
        {
            PropKind.Ferns, PropKind.Mushrooms, PropKind.Stump, PropKind.FallenLog, PropKind.StandingStone, PropKind.AxeStump,
            PropKind.BonePile, PropKind.BanditTent, PropKind.WebSac, PropKind.Campfire, PropKind.Sack, PropKind.Crate,
            PropKind.Barrel, PropKind.WeaponRack, PropKind.Cage, PropKind.Carcass,
        };

        /// <summary>Whether a prop may stand on this level: in the forest only those drawn for it.</summary>
        public static bool Allowed(DungeonLayout layout, PropKind kind) =>
            !(layout.Theme == DungeonTheme.Cave && DungeonRules.Forest) || ForestDrawn.Contains(kind);

        static readonly (DecalKind kind, int weight)[] ForestDecalWeights =
        {
            (DecalKind.Leaves, 36), (DecalKind.Flowers, 22), (DecalKind.Pebbles, 20), (DecalKind.Puddle, 10), (DecalKind.Bones, 6),
            (DecalKind.Blood, 6),
        };

        // The burnt library (2026-10-11): books heaped and burnt, ash, lecterns and desks, smouldering piles for light.
        static readonly (PropKind kind, int weight)[] LibraryPropWeights =
        {
            (PropKind.BookPile, 22), (PropKind.AshHeap, 16), (PropKind.Lectern, 10), (PropKind.ReadingDesk, 10),
            (PropKind.BurningBooks, 10), (PropKind.FallenShelf, 10), (PropKind.Candles, 8), (PropKind.Urn, 4),
        };

        // The prison (2026-10-12, the concept's props): hanging cages, torture benches, stocks, floor grates, straw, bones,
        // barrels and crates, braziers and torches; blood on the floor.
        static readonly (PropKind kind, int weight)[] PrisonPropWeights =
        {
            (PropKind.HangingCage, 14), (PropKind.TortureBench, 10), (PropKind.Stocks, 8), (PropKind.FloorGrate, 10),
            (PropKind.StrawPile, 12), (PropKind.BonePile, 12), (PropKind.Barrel, 8), (PropKind.Crate, 6), (PropKind.Bucket, 6),
            (PropKind.Brazier, 8), (PropKind.Torch, 6),
        };

        static readonly (DecalKind kind, int weight)[] PrisonDecalWeights =
        {
            (DecalKind.Blood, 30), (DecalKind.Cracks, 20), (DecalKind.Bones, 16), (DecalKind.Rubble, 12), (DecalKind.Puddle, 10),
            (DecalKind.Skull, 6), (DecalKind.Moss, 6),
        };

        static readonly (DecalKind kind, int weight)[] LibraryDecalWeights =
        {
            (DecalKind.Ash, 34), (DecalKind.Cracks, 20), (DecalKind.Rubble, 14), (DecalKind.Blood, 10), (DecalKind.Bones, 10),
            (DecalKind.Skull, 4),
        };

        static readonly (DecalKind kind, int weight)[] CaveDecalWeights =
        {
            (DecalKind.Rubble, 30), (DecalKind.Puddle, 22), (DecalKind.Bones, 14), (DecalKind.Blood, 10), (DecalKind.Cracks, 12),
            (DecalKind.Skull, 6), (DecalKind.Ritual, 3),
        };

        static readonly (DecalKind kind, int weight)[] DecalWeights =
        {
            (DecalKind.Cracks, 30), (DecalKind.Bones, 12), (DecalKind.Blood, 12), (DecalKind.Rubble, 18), (DecalKind.Moss, 14),
            (DecalKind.Skull, 6), (DecalKind.Puddle, 8), (DecalKind.Ritual, 4),
        };

        /// <summary>Whether a prop gives off light (placed at most once a room, so a level has few lights).</summary>
        public static bool IsLit(PropKind kind) => kind == PropKind.Brazier || kind == PropKind.Candles || kind == PropKind.Torch ||
                                                   kind == PropKind.Campfire || kind == PropKind.Hellfire || kind == PropKind.BurningBooks;

        public static void Place(DungeonLayout layout, List<Vector2Int>[] doors, System.Random random, int levelSeed = 0)
        {
            var spots = new List<Vector2Int> { layout.StairsUp, layout.ArrivalFromAbove, layout.Waypoint };
            if (layout.HasStairsDown)
            {
                spots.Add(layout.StairsDown);
                spots.Add(layout.ArrivalFromBelow);
            }
            spots.AddRange(layout.Chests);
            // A terrace's steps and the floor at their foot and head stay clear (2026-10-10).
            foreach (var pair in layout.TerraceStairs)
            {
                spots.Add(pair.Key + pair.Value);
                spots.Add(pair.Key - pair.Value);
            }
            if (layout.HasWandererSpot)
                spots.Add(layout.WandererSpot);
            var allDoors = new List<Vector2Int>();
            foreach (var list in doors)
                allDoors.AddRange(list);

            PlaceLairs(layout, spots, allDoors, random, levelSeed);

            for (var i = 0; i < layout.Rooms.Count; i++)
            {
                var room = layout.Rooms[i];
                var r = room.Interior;
                var floor = new List<Vector2Int>();
                for (var x = r.xMin; x < r.xMax; x++)
                    for (var y = r.yMin; y < r.yMax; y++)
                        if (layout.IsFloor(new Vector2Int(x, y)))
                            floor.Add(new Vector2Int(x, y));

                // The arena is left bare for the fight; its floor still gets marks.
                if (room.Kind != RoomKind.Boss)
                    PlaceProps(layout, floor, spots, allDoors, random, layout.Depth);
                PlaceDecals(layout, floor, room.Style, random);
                if ((room.Kind == RoomKind.Combat || room.Kind == RoomKind.Elite) && random.Next(RitualOneIn) == 0)
                    PlaceRitual(layout, room.Interior);
            }
            if (layout.Theme == DungeonTheme.Crypt)
                PlaceBloodTrails(layout, random);
        }

        // Trails of old blood (2026-10-20): from a cell of a hall, a few steps at a time, turning now and then, over open
        // floor that has no mark yet.
        static void PlaceBloodTrails(DungeonLayout layout, System.Random random)
        {
            for (var t = 0; t < BloodTrails; t++)
            {
                var room = layout.Rooms[random.Next(layout.Rooms.Count)];
                if (room.Kind == RoomKind.Boss)
                    continue;
                var r = room.Interior;
                var cell = new Vector2Int(random.Next(r.xMin, r.xMax), random.Next(r.yMin, r.yMax));
                var step = RoomShape.Sides[random.Next(RoomShape.Sides.Length)];
                var length = random.Next(MinTrail, MaxTrail + 1);
                for (var k = 0; k < length; k++)
                {
                    if (layout.IsFloor(cell) && !layout.Decals.ContainsKey(cell))
                        layout.Decals[cell] = DecalKind.Blood;
                    if (random.Next(4) == 0)
                        step = new Vector2Int(step.y, step.x) * (random.Next(2) == 0 ? 1 : -1);
                    if (!layout.IsFloor(cell + step))
                        step = -step;
                    cell += step;
                }
            }
        }

        /// <summary>One combat or elite room in this many gets a summoning circle on its floor.</summary>
        const int RitualOneIn = 4;

        // A summoning circle in the room's middle, where the 3 x 3 cells under it are all open floor.
        static void PlaceRitual(DungeonLayout layout, RectInt interior)
        {
            var middle = new Vector2Int(interior.xMin + interior.width / 2, interior.yMin + interior.height / 2);
            for (var x = -1; x <= 1; x++)
                for (var y = -1; y <= 1; y++)
                    if (!layout.IsFloor(middle + new Vector2Int(x, y)))
                        return;
            layout.Rituals.Add(middle);
        }

        /// <summary>
        /// Each pack's lair (2026-10-08): its theme from who lives there (<see cref="Lairs"/>), a fire or altar in its
        /// middle where the pack gathers, the theme's things standing around it just outside the pack, and marks on the
        /// floor under and around it. Elite packs take a theme by depth.
        /// </summary>
        static void PlaceLairs(DungeonLayout layout, List<Vector2Int> spots, List<Vector2Int> doors, System.Random random, int levelSeed)
        {
            for (var p = 0; p < layout.Packs.Count; p++)
            {
                var pack = layout.Packs[p];
                // A roaming duo or trio keeps no lair (2026-10-26).
                var theme = pack.Roams ? LairTheme.None : pack.Kind == PackKind.Elite
                    ? Lairs.EliteTheme(layout.Depth)
                    : Lairs.Theme(PackComposition.Roll(layout.Depth, pack.Kind, pack.Count, levelSeed, p));
                // One entry a pack, in the packs' order, even for a pack with no lair.
                layout.Lairs.Add((pack.Cell, theme));
                if (theme == LairTheme.None)
                    continue;
                var center = IsoMath.CellToGround(pack.Cell);
                var heart = Lairs.Heart(theme);
                if (heart.HasValue && !Allowed(layout, heart.Value))
                    heart = null;
                if (heart.HasValue && layout.IsFloor(pack.Cell) && Clear(pack.Cell, doors, DoorClearance) && Clear(pack.Cell, spots, SpotClearance) && SafeToBlock(layout, pack.Cell))
                {
                    layout.Set(pack.Cell.x, pack.Cell.y, DungeonCell.Prop);
                    layout.Props[pack.Cell] = heart.Value;
                }

                // Cells in a ring just outside the pack, and the whole lair for marks.
                var reach = Mathf.CeilToInt((pack.Radius + 4f) / 0.7071f);
                var ring = new List<Vector2Int>();
                var marks = new List<Vector2Int>();
                for (var dx = -reach; dx <= reach; dx++)
                    for (var dy = -reach; dy <= reach; dy++)
                    {
                        var cell = pack.Cell + new Vector2Int(dx, dy);
                        if (!layout.IsFloor(cell))
                            continue;
                        var d = Vector2.Distance(center, IsoMath.CellToGround(cell));
                        if (d <= pack.Radius + 3f)
                            marks.Add(cell);
                        if (d >= pack.Radius + 0.6f && d <= pack.Radius + 3.5f && Clear(cell, doors, DoorClearance) && Clear(cell, spots, SpotClearance))
                            ring.Add(cell);
                    }

                foreach (var (kind, min, max) in Lairs.Props(theme))
                {
                    if (!Allowed(layout, kind))
                        continue;
                    var count = random.Next(min, max + 1);
                    for (var k = 0; k < count && ring.Count > 0; k++)
                    {
                        // A few tries for a cell that keeps the floor in one piece.
                        for (var attempt = 0; attempt < 6 && ring.Count > 0; attempt++)
                        {
                            var index = random.Next(ring.Count);
                            var cell = ring[index];
                            ring.RemoveAt(index);
                            if (!layout.IsFloor(cell) || !SafeToBlock(layout, cell))
                                continue;
                            layout.Set(cell.x, cell.y, DungeonCell.Prop);
                            layout.Props[cell] = kind;
                            break;
                        }
                    }
                }

                foreach (var (kind, min, max) in Lairs.Decals(theme))
                {
                    var count = random.Next(min, max + 1);
                    for (var k = 0; k < count && marks.Count > 0; k++)
                    {
                        var cell = marks[random.Next(marks.Count)];
                        if (layout.IsFloor(cell))
                            layout.Decals[cell] = kind;
                    }
                }
            }
        }

        // Deeper, the rooms' own clutter turns demonic (2026-10-08: "more demonic the further down"): flesh, spikes,
        // stakes and cages take a growing share, up to half the weight at the bottom.
        static (PropKind kind, int weight)[] PropWeightsAt(int depth)
        {
            var demonic = Mathf.Clamp01((depth - 8) / 16f);
            var theme = DungeonRules.ThemeAt(depth);
            var list = new List<(PropKind, int)>(theme == DungeonTheme.Cave && DungeonRules.Forest ? ForestPropWeights : theme == DungeonTheme.Cave ? CavePropWeights : theme == DungeonTheme.Library ? LibraryPropWeights :
                theme == DungeonTheme.Prison ? PrisonPropWeights : CryptPropWeights);
            var extra = Mathf.RoundToInt(demonic * 120);
            // The forest stays a forest at every depth (2026-10-27).
            if (extra > 0 && !(theme == DungeonTheme.Cave && DungeonRules.Forest))
            {
                list.Add((PropKind.FleshPod, extra * 3 / 10));
                list.Add((PropKind.Spikes, extra * 3 / 10));
                list.Add((PropKind.Stake, extra * 2 / 10));
                list.Add((PropKind.Cage, extra * 2 / 10));
            }
            return list.ToArray();
        }

        static void PlaceProps(DungeonLayout layout, List<Vector2Int> floor, List<Vector2Int> spots, List<Vector2Int> doors, System.Random random, int depth = 1)
        {
            var weights = PropWeightsAt(depth);
            var candidates = new List<Vector2Int>();
            foreach (var cell in floor)
                if (AgainstWall(layout, cell) && Clear(cell, doors, DoorClearance) && Clear(cell, spots, SpotClearance) && !InPack(layout, cell))
                    candidates.Add(cell);

            var wanted = floor.Count / CellsPerProp + 1;
            var lit = false;
            for (var placed = 0; placed < wanted && candidates.Count > 0;)
            {
                var index = random.Next(candidates.Count);
                var cell = candidates[index];
                candidates.RemoveAt(index);
                if (!layout.IsFloor(cell) || !SafeToBlock(layout, cell))
                    continue;

                var kind = Pick(weights, random);
                if (IsLit(kind))
                {
                    if (lit)
                        kind = layout.Theme == DungeonTheme.Cave ? PropKind.Rubble : PropKind.Urn;
                    lit = true;
                }
                layout.Set(cell.x, cell.y, DungeonCell.Prop);
                layout.Props[cell] = kind;
                placed++;
                // A cold camp keeps the bedroll someone slept on.
                if (kind == PropKind.ColdCamp)
                    foreach (var side in RoomShape.Sides)
                        if (layout.IsFloor(cell + side) && !layout.Decals.ContainsKey(cell + side))
                        {
                            layout.Decals[cell + side] = DecalKind.Bedroll;
                            break;
                        }

                // Barrels and crates come in twos and threes, as stores do.
                if (kind != PropKind.Barrel && kind != PropKind.Crate)
                    continue;
                var extra = random.Next(0, 3);
                foreach (var step in RoomShape.Sides)
                {
                    if (extra == 0)
                        break;
                    var next = cell + step;
                    if (!candidates.Contains(next) || !SafeToBlock(layout, next))
                        continue;
                    candidates.Remove(next);
                    layout.Set(next.x, next.y, DungeonCell.Prop);
                    layout.Props[next] = random.Next(2) == 0 ? PropKind.Barrel : PropKind.Crate;
                    extra--;
                }
            }
        }

        static void PlaceDecals(DungeonLayout layout, List<Vector2Int> floor, int style, System.Random random)
        {
            var wanted = floor.Count / (layout.Theme == DungeonTheme.Crypt ? CryptCellsPerDecal : CellsPerDecal);
            for (var k = 0; k < wanted; k++)
            {
                var cell = floor[random.Next(floor.Count)];
                if (!layout.IsFloor(cell) || layout.Decals.ContainsKey(cell))
                    continue;
                var kind = Pick(layout.Theme == DungeonTheme.Cave && DungeonRules.Forest ? ForestDecalWeights : layout.Theme == DungeonTheme.Cave ? CaveDecalWeights : layout.Theme == DungeonTheme.Library ? LibraryDecalWeights :
                    layout.Theme == DungeonTheme.Prison ? PrisonDecalWeights : DecalWeights, random);
                // Moss grows in the damp rooms (style 3) and hardly elsewhere.
                if (kind == DecalKind.Moss && style != 3 && random.Next(3) > 0)
                    kind = DecalKind.Cracks;
                layout.Decals[cell] = kind;
            }
        }

        static bool AgainstWall(DungeonLayout layout, Vector2Int cell)
        {
            foreach (var step in RoomShape.Sides)
                if (layout.Get(cell + step) == DungeonCell.Wall)
                    return true;
            return false;
        }

        static bool Clear(Vector2Int cell, List<Vector2Int> points, int distance)
        {
            foreach (var point in points)
                if (Mathf.Max(Mathf.Abs(cell.x - point.x), Mathf.Abs(cell.y - point.y)) <= distance)
                    return false;
            return true;
        }

        static bool InPack(DungeonLayout layout, Vector2Int cell)
        {
            var ground = IsoMath.CellToGround(cell);
            foreach (var pack in layout.Packs)
                if (Vector2.Distance(ground, IsoMath.CellToGround(pack.Cell)) < pack.Radius + 1f)
                    return true;
            return false;
        }

        // The ring around a cell, in order, each step moving one cell along one axis.
        static readonly Vector2Int[] Ring =
        {
            new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1), new Vector2Int(1, 0),
            new Vector2Int(1, 1), new Vector2Int(0, 1), new Vector2Int(-1, 1), new Vector2Int(-1, 0),
        };

        /// <summary>
        /// Whether blocking a floor cell keeps the floor around it in one piece: its walkable edge neighbours must all lie
        /// on one unbroken run of floor around the ring of its eight neighbours, so any way through the cell can go around
        /// it instead. Local, so it is cheap, and enough to keep the whole level connected.
        /// </summary>
        public static bool SafeToBlock(DungeonLayout layout, Vector2Int cell)
        {
            var open = new bool[8];
            var any = false;
            for (var i = 0; i < 8; i++)
            {
                open[i] = layout.IsFloor(cell + Ring[i]);
                any |= open[i];
            }
            if (!any)
                return false;

            // Start just after a closed cell, so a run that wraps around is counted once.
            var start = 0;
            while (start < 8 && open[start])
                start++;
            if (start == 8)
                return true;

            var runsWithEdge = 0;
            var inRun = false;
            var runHasEdge = false;
            for (var k = 1; k <= 8; k++)
            {
                var i = (start + k) % 8;
                if (open[i])
                {
                    inRun = true;
                    runHasEdge |= i % 2 == 1;
                }
                else if (inRun)
                {
                    if (runHasEdge)
                        runsWithEdge++;
                    inRun = false;
                    runHasEdge = false;
                }
            }
            return runsWithEdge <= 1;
        }

        static T Pick<T>((T kind, int weight)[] table, System.Random random)
        {
            var total = 0;
            foreach (var entry in table)
                total += entry.weight;
            var roll = random.Next(total);
            foreach (var entry in table)
            {
                if (roll < entry.weight)
                    return entry.kind;
                roll -= entry.weight;
            }
            return table[0].kind;
        }
    }
}
