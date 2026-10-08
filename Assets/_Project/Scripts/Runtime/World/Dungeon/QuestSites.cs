using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Where a quest step's thing goes on a generated level (2026-10-08, Docs/05 Quests): in the lair that fits it (Tobin
    /// at a feeding ground, Bram in the drowned's hold, Vessa at a bandit camp) furthest from where she arrives, else
    /// the furthest lair of any kind; the watch fire in the middle of the furthest room with open floor around it. The
    /// same level always puts it in the same place. Pure.
    /// </summary>
    public static class QuestSites
    {
        /// <summary>The lair a target is found in, or None for one that wants open floor (the watch fire).</summary>
        public static LairTheme LairFor(string target) => target switch
        {
            "tobin" or "gorrak" => LairTheme.Feeding,
            "bram" => LairTheme.Drowned,
            "pellam" => LairTheme.Shrine,
            "vessa" => LairTheme.Camp,
            "ivy" or "aldric" or "reliquary_keeper" or "watch_log" or "cartographer_notes" or "bone_pyres" => LairTheme.Tomb,
            "oath_stone" or "great_lamp" or "rift_heart" => LairTheme.Nest,
            _ => LairTheme.None,
        };

        /// <summary>How many things a step places (the bone pyres are three).</summary>
        public static int Count(string target) => target == "bone_pyres" ? 3 : 1;

        /// <summary>
        /// The cells for a step's things: for a foe or a thing to break, the lair's middle (beside its fire or altar);
        /// for a captive or an object, just outside the pack, on open floor. Several things go to several lairs, the
        /// furthest first.
        /// </summary>
        public static List<Vector2Int> Cells(DungeonLayout layout, QuestStep step)
        {
            var cells = new List<Vector2Int>();
            var count = Count(step.Target);
            if (step.Kind == QuestStepKind.HoldFire)
            {
                cells.Add(OpenMiddle(layout));
                return cells;
            }
            var lairs = Ranked(layout, LairFor(step.Target));
            var atMiddle = step.Kind == QuestStepKind.NamedFoe || step.Kind == QuestStepKind.Break;
            foreach (var index in lairs)
            {
                var pack = layout.Packs[index];
                var cell = atMiddle ? NearestOpen(layout, pack.Cell, 0f, 2f) : NearestOpen(layout, pack.Cell, pack.Radius + 0.8f, pack.Radius + 3f);
                if (cell.HasValue && !cells.Contains(cell.Value))
                    cells.Add(cell.Value);
                if (cells.Count == count)
                    break;
            }
            // Too few lairs for every pyre: the rest around the first, spread out.
            for (var k = 0; cells.Count > 0 && cells.Count < count && k < 8; k++)
            {
                var around = NearestOpen(layout, cells[0] + new Vector2Int((k % 2 == 0 ? 6 : -6), (k / 2 % 2 == 0 ? 6 : -6)), 0f, 4f);
                if (around.HasValue && !cells.Contains(around.Value))
                    cells.Add(around.Value);
            }
            if (cells.Count == 0)
                cells.Add(OpenMiddle(layout));
            return cells;
        }

        // The packs of a lair theme, furthest from the arrival first, then every other pack the same way.
        static List<int> Ranked(DungeonLayout layout, LairTheme theme)
        {
            var arrival = IsoMath.CellToGround(layout.ArrivalFromAbove);
            var matching = new List<int>();
            var others = new List<int>();
            for (var i = 0; i < layout.Packs.Count && i < layout.Lairs.Count; i++)
                (layout.Lairs[i].theme == theme ? matching : others).Add(i);
            float Distance(int i) => Vector2.Distance(arrival, IsoMath.CellToGround(layout.Packs[i].Cell));
            matching.Sort((a, b) => Distance(b).CompareTo(Distance(a)));
            others.Sort((a, b) => Distance(b).CompareTo(Distance(a)));
            matching.AddRange(others);
            return matching;
        }

        /// <summary>The open floor cell nearest a point at a ground distance between the bounds from it, or none.</summary>
        static Vector2Int? NearestOpen(DungeonLayout layout, Vector2Int center, float near, float far)
        {
            var origin = IsoMath.CellToGround(center);
            var reach = Mathf.CeilToInt(far / 0.7071f) + 1;
            Vector2Int? best = null;
            var bestScore = float.MaxValue;
            for (var dx = -reach; dx <= reach; dx++)
                for (var dy = -reach; dy <= reach; dy++)
                {
                    var cell = center + new Vector2Int(dx, dy);
                    if (!layout.IsFloor(cell) || !OpenAround(layout, cell))
                        continue;
                    var d = Vector2.Distance(origin, IsoMath.CellToGround(cell));
                    if (d < near || d > far)
                        continue;
                    // Nearest the inner bound, ties by cell order, so the choice is fixed.
                    var score = d - near + (dx * 0.001f + dy * 0.0001f);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = cell;
                    }
                }
            return best;
        }

        // A cell with floor on all four sides, so what stands there can be walked up to.
        static bool OpenAround(DungeonLayout layout, Vector2Int cell)
        {
            foreach (var step in RoomShape.Sides)
                if (!layout.IsFloor(cell + step))
                    return false;
            return true;
        }

        /// <summary>The middle of the room furthest from the arrival that has a 7 x 7 square of open floor, for the watch
        /// fire and its fight; else the furthest room's nearest open cell to its middle.</summary>
        public static Vector2Int OpenMiddle(DungeonLayout layout)
        {
            var arrival = IsoMath.CellToGround(layout.ArrivalFromAbove);
            Vector2Int? best = null;
            var bestDistance = -1f;
            foreach (var room in layout.Rooms)
            {
                if (room.Kind == RoomKind.Start || room.Kind == RoomKind.Boss)
                    continue;
                var r = room.Interior;
                var middle = new Vector2Int(r.xMin + r.width / 2, r.yMin + r.height / 2);
                var cell = SquareNear(layout, middle, 3, 6);
                if (!cell.HasValue)
                    continue;
                var d = Vector2.Distance(arrival, IsoMath.CellToGround(cell.Value));
                if (d > bestDistance)
                {
                    bestDistance = d;
                    best = cell;
                }
            }
            return best ?? layout.Waypoint;
        }

        // A cell within reach of a point whose square of the given half size is all floor.
        static Vector2Int? SquareNear(DungeonLayout layout, Vector2Int center, int half, int reach)
        {
            for (var ring = 0; ring <= reach; ring++)
                for (var dx = -ring; dx <= ring; dx++)
                    for (var dy = -ring; dy <= ring; dy++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != ring)
                            continue;
                        var cell = center + new Vector2Int(dx, dy);
                        var open = true;
                        for (var x = -half; x <= half && open; x++)
                            for (var y = -half; y <= half && open; y++)
                                open = layout.IsFloor(cell + new Vector2Int(x, y));
                        if (open)
                            return cell;
                    }
            return null;
        }
    }
}
