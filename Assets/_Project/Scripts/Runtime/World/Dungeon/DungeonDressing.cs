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

        /// <summary>Cells (Chebyshev) kept clear around a doorway's middle and around the stairs and similar spots.</summary>
        const int DoorClearance = 4;
        const int SpotClearance = 2;

        static readonly (PropKind kind, int weight)[] PropWeights =
        {
            (PropKind.Barrel, 20), (PropKind.Crate, 16), (PropKind.Urn, 12), (PropKind.BonePile, 12), (PropKind.Rubble, 14),
            (PropKind.BrokenColumn, 8), (PropKind.Sarcophagus, 6), (PropKind.Brazier, 7), (PropKind.Candles, 5),
            (PropKind.Bucket, 8), (PropKind.Torch, 7),
        };

        static readonly (DecalKind kind, int weight)[] DecalWeights =
        {
            (DecalKind.Cracks, 30), (DecalKind.Bones, 12), (DecalKind.Blood, 12), (DecalKind.Rubble, 18), (DecalKind.Moss, 14),
            (DecalKind.Skull, 6), (DecalKind.Puddle, 8), (DecalKind.Ritual, 4),
        };

        /// <summary>Whether a prop gives off light (placed at most once a room, so a level has few lights).</summary>
        public static bool IsLit(PropKind kind) => kind == PropKind.Brazier || kind == PropKind.Candles || kind == PropKind.Torch;

        public static void Place(DungeonLayout layout, List<Vector2Int>[] doors, System.Random random)
        {
            var spots = new List<Vector2Int> { layout.StairsUp, layout.ArrivalFromAbove, layout.Waypoint };
            if (layout.HasStairsDown)
            {
                spots.Add(layout.StairsDown);
                spots.Add(layout.ArrivalFromBelow);
            }
            spots.AddRange(layout.Chests);
            if (layout.HasWandererSpot)
                spots.Add(layout.WandererSpot);
            var allDoors = new List<Vector2Int>();
            foreach (var list in doors)
                allDoors.AddRange(list);

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
                    PlaceProps(layout, floor, spots, allDoors, random);
                PlaceDecals(layout, floor, room.Style, random);
                if ((room.Kind == RoomKind.Combat || room.Kind == RoomKind.Elite) && random.Next(RitualOneIn) == 0)
                    PlaceRitual(layout, room.Interior);
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

        static void PlaceProps(DungeonLayout layout, List<Vector2Int> floor, List<Vector2Int> spots, List<Vector2Int> doors, System.Random random)
        {
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

                var kind = Pick(PropWeights, random);
                if (IsLit(kind))
                {
                    if (lit)
                        kind = PropKind.Barrel;
                    lit = true;
                }
                layout.Set(cell.x, cell.y, DungeonCell.Prop);
                layout.Props[cell] = kind;
                placed++;

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
            var wanted = floor.Count / CellsPerDecal;
            for (var k = 0; k < wanted; k++)
            {
                var cell = floor[random.Next(floor.Count)];
                if (!layout.IsFloor(cell) || layout.Decals.ContainsKey(cell))
                    continue;
                var kind = Pick(DecalWeights, random);
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
