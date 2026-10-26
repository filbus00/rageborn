using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The crypt laid out as Diablo 1's cathedral (2026-10-26, the owner: "Can you make dungeons in the same style as
    /// diablo 1?", with two screenshots of its first level): rectangular rooms only, with straight walls and square
    /// corners, grown off each other as a tree from a chain of three big halls, after Diablo 1's own generator. Neighbours
    /// share a wall two cells thick, opened by a doorway, by an arcade (the wall cut into a row of pillars) or entirely,
    /// so rooms merge into L and T shaped halls. Big halls may hold two rows of columns. Same seed, same level. Pure.
    /// </summary>
    public static partial class DungeonGenerator
    {
        /// <summary>The wall between two neighbouring rooms, in cells (two, so it has a front and a back face, and is never
        /// taken for a thin wall).</summary>
        public const int CathedralWall = 2;

        /// <summary>The level's rooms are grown inside a square this many cells wide.</summary>
        public const int CathedralSpan = 110;

        /// <summary>Rooms on a level, all told.</summary>
        public const int MinCathedralRooms = 18, MaxCathedralRooms = 26;

        /// <summary>The first three halls (the chain the rest grows off) and the rest: inside sizes in cells, even.</summary>
        static readonly int[] HallSizes = { 14, 16, 16, 18, 20 };
        static readonly int[] RoomSizes = { 6, 8, 8, 10, 10, 12, 12, 14 };

        /// <summary>How a room opens into its parent, in percent: a doorway, an arcade, or the shared wall taken away.</summary>
        public const int DoorwayPercent = 50, ArcadePercent = 30;

        /// <summary>The chance two neighbours the tree did not join get a doorway anyway, for loops.</summary>
        public const int CathedralLoopPercent = 35;

        /// <summary>The chance a big hall (12 cells or more across) holds two rows of columns.</summary>
        public const int ColumnsPercent = 50;

        enum Opening { Doorway, Arcade, Open }

        static DungeonLayout Cathedral(int seed, int depth, DungeonSettings settings, System.Random random)
        {
            var outer = settings.OuterRock;
            var area = new RectInt(outer, outer, CathedralSpan, CathedralSpan);

            // 1. The chain of three big halls across the middle, then rooms grown off every room's sides in turn.
            var rooms = new List<RectInt>();
            var parents = new List<int>();
            var alongX = random.Next(2) == 0;
            var hubW = Pick(HallSizes, random);
            var hubH = Pick(HallSizes, random);
            rooms.Add(new RectInt(area.xMin + area.width / 2 - hubW / 2, area.yMin + area.height / 2 - hubH / 2, hubW, hubH));
            parents.Add(-1);
            foreach (var sign in new[] { -1, 1 })
            {
                var side = alongX ? new Vector2Int(sign, 0) : new Vector2Int(0, sign);
                if (TryChild(rooms, 0, side, Pick(HallSizes, random), Pick(HallSizes, random), area, random, out var hall))
                {
                    rooms.Add(hall);
                    parents.Add(0);
                }
            }
            var target = random.Next(MinCathedralRooms, MaxCathedralRooms + 1);
            var sides = new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
            for (var pass = 0; pass < 6 && rooms.Count < target; pass++)
            {
                var grew = false;
                for (var i = 0; i < rooms.Count && rooms.Count < target; i++)
                    foreach (var k in Shuffled(sides.Length, random))
                    {
                        if (rooms.Count >= target || random.Next(100) >= 55)
                            continue;
                        if (TryChild(rooms, i, sides[k], Pick(RoomSizes, random), Pick(RoomSizes, random), area, random, out var room))
                        {
                            rooms.Add(room);
                            parents.Add(i);
                            grew = true;
                        }
                    }
                if (!grew)
                    break;
            }

            // 2. Who is joined to whom: the tree, then loops between neighbours. The start is the room furthest from the
            // middle hall, the exit the room furthest from the start.
            var links = new List<(int a, int b, Opening how)>();
            for (var i = 1; i < rooms.Count; i++)
            {
                var roll = random.Next(100);
                links.Add((parents[i], i, roll < DoorwayPercent ? Opening.Doorway : roll < DoorwayPercent + ArcadePercent ? Opening.Arcade : Opening.Open));
            }
            for (var i = 0; i < rooms.Count; i++)
                for (var j = i + 1; j < rooms.Count; j++)
                    if (parents[j] != i && parents[i] != j && Shared(rooms[i], rooms[j], out _, out _, out _, out _) &&
                        random.Next(100) < CathedralLoopPercent)
                        links.Add((i, j, Opening.Doorway));
            var next = new List<int>[rooms.Count];
            for (var i = 0; i < rooms.Count; i++)
                next[i] = new List<int>();
            foreach (var (a, b, _) in links)
            {
                next[a].Add(b);
                next[b].Add(a);
            }
            var fromHub = Steps(next, 0);
            var start = Furthest(fromHub);
            var fromStart = Steps(next, start);
            var exit = Furthest(fromStart);

            // Room 0 is the start (as Finish expects): the start and room 0 trade places.
            int Index(int i) => i == start ? 0 : i == 0 ? start : i;

            // 3. The cells: every room's floor, then the openings in the walls between them.
            var layout = new DungeonLayout(new RectInt(0, 0, CathedralSpan + 2 * outer, CathedralSpan + 2 * outer))
            {
                Depth = depth,
                EnemyLevel = DungeonRules.EnemyLevel(depth),
                Theme = DungeonRules.ThemeAt(depth),
                Cathedral = true,
            };
            foreach (var r in rooms)
                FillRect(layout, r, DungeonCell.Floor);
            var doors = new List<Vector2Int>[rooms.Count];
            for (var i = 0; i < rooms.Count; i++)
                doors[i] = new List<Vector2Int>();
            foreach (var (a, b, how) in links)
                Open(layout, rooms[a], rooms[b], how, random, doors[Index(a)], doors[Index(b)]);

            // 4. Room kinds, with the docs' guarantees, and columns in some big halls.
            var kinds = new RoomKind[rooms.Count];
            var middle = new List<int>();
            for (var i = 0; i < rooms.Count; i++)
            {
                var at = Index(i);
                kinds[at] = i == start ? RoomKind.Start : i == exit ? RoomKind.Exit : RollKind(random, settings);
                if (i != start && i != exit)
                    middle.Add(at);
            }
            Guarantee(RoomKind.Elite, kinds, middle, random);
            Guarantee(RoomKind.Treasure, kinds, middle, random);
            var ordered = new RectInt[rooms.Count];
            var treeDepth = new List<int>();
            for (var i = 0; i < rooms.Count; i++)
                ordered[Index(i)] = rooms[i];
            for (var i = 0; i < rooms.Count; i++)
                treeDepth.Add(fromStart[Index(i)]);
            for (var i = 0; i < rooms.Count; i++)
            {
                var r = ordered[i];
                var hall = PickHall(layout, kinds[i], r, depth, random);
                if (hall != HallTheme.None)
                    layout.Halls[i] = hall;
                else if (kinds[i] != RoomKind.Start && kinds[i] != RoomKind.Exit && Mathf.Min(r.width, r.height) >= 12 &&
                         random.Next(100) < ColumnsPercent)
                    Columns(layout, r);
                layout.Rooms.Add(new RoomPlacement(kinds[i], null, new Vector2Int(i, 0), r, random.Next(FloorStyles)));
            }

            KeepReachable(layout, ordered[0]);
            FillRock(layout);
            for (var i = 0; i < rooms.Count; i++)
                if (layout.Halls.TryGetValue(i, out var theme))
                    Furnish(layout, i, theme, ordered[i], random);
                else if ((kinds[i] == RoomKind.Combat || kinds[i] == RoomKind.Elite) && Mathf.Max(ordered[i].width, ordered[i].height) >= 12 &&
                         random.Next(100) < CarpetPercent)
                    Runner(layout, ordered[i], ordered[i].width >= ordered[i].height);
            MarkFreeStanding(layout);
            return Finish(layout, doors, Index(exit), treeDepth, random, settings, seed, depth);
        }

        static int Pick(int[] sizes, System.Random random) => sizes[random.Next(sizes.Length)];

        /// <summary>
        /// A room of the size asked on one side of room <paramref name="from"/>, a wall's thickness away, lined up with
        /// it give or take (at least 4 cells of their sides facing each other, for an opening), inside the area and at
        /// least a wall's thickness from every other room. False when it does not fit.
        /// </summary>
        static bool TryChild(List<RectInt> rooms, int from, Vector2Int side, int w, int h, RectInt area, System.Random random, out RectInt room)
        {
            var p = rooms[from];
            room = default;
            const int overlap = 4;
            int x, y;
            if (side.x != 0)
            {
                x = side.x > 0 ? p.xMax + CathedralWall : p.xMin - CathedralWall - w;
                var lo = p.yMin + overlap - h;
                var hi = p.yMax - overlap;
                if (lo > hi)
                    return false;
                var centred = p.yMin + p.height / 2 - h / 2;
                y = Mathf.Clamp(centred + random.Next(-p.height / 3, p.height / 3 + 1), lo, hi);
            }
            else
            {
                y = side.y > 0 ? p.yMax + CathedralWall : p.yMin - CathedralWall - h;
                var lo = p.xMin + overlap - w;
                var hi = p.xMax - overlap;
                if (lo > hi)
                    return false;
                var centred = p.xMin + p.width / 2 - w / 2;
                x = Mathf.Clamp(centred + random.Next(-p.width / 3, p.width / 3 + 1), lo, hi);
            }
            room = new RectInt(x, y, w, h);
            if (room.xMin < area.xMin || room.yMin < area.yMin || room.xMax > area.xMax || room.yMax > area.yMax)
                return false;
            var clear = new RectInt(room.xMin - CathedralWall, room.yMin - CathedralWall, room.width + 2 * CathedralWall, room.height + 2 * CathedralWall);
            foreach (var other in rooms)
                if (clear.Overlaps(other))
                    return false;
            return true;
        }

        /// <summary>
        /// Whether two rooms face each other across one wall's thickness with at least 4 cells in common: the wall's
        /// cells along x from <paramref name="x0"/> (inclusive) to <paramref name="x1"/> (exclusive), and along y the same.
        /// </summary>
        public static bool Shared(RectInt a, RectInt b, out int x0, out int x1, out int y0, out int y1)
        {
            x0 = x1 = y0 = y1 = 0;
            if (a.xMax + CathedralWall == b.xMin || b.xMax + CathedralWall == a.xMin)
            {
                y0 = Mathf.Max(a.yMin, b.yMin);
                y1 = Mathf.Min(a.yMax, b.yMax);
                x0 = Mathf.Min(a.xMax, b.xMax);
                x1 = x0 + CathedralWall;
                return y1 - y0 >= 4;
            }
            if (a.yMax + CathedralWall == b.yMin || b.yMax + CathedralWall == a.yMin)
            {
                x0 = Mathf.Max(a.xMin, b.xMin);
                x1 = Mathf.Min(a.xMax, b.xMax);
                y0 = Mathf.Min(a.yMax, b.yMax);
                y1 = y0 + CathedralWall;
                return x1 - x0 >= 4;
            }
            return false;
        }

        /// <summary>Opens the wall between two neighbouring rooms: a doorway 2 or 3 cells wide, an arcade (the wall cut
        /// into pillars a cell wide, two cells apart), or the whole wall but a pier at each end.</summary>
        static void Open(DungeonLayout layout, RectInt a, RectInt b, Opening how, System.Random random, List<Vector2Int> doorsA, List<Vector2Int> doorsB)
        {
            if (!Shared(a, b, out var x0, out var x1, out var y0, out var y1))
                return;
            var acrossX = x1 - x0 == CathedralWall;
            // Along the wall: the run of cells it shares; across: its two cells.
            var from = acrossX ? y0 : x0;
            var to = acrossX ? y1 : x1;
            var length = to - from;
            if (how == Opening.Arcade && length < 7)
                how = Opening.Doorway;
            for (var t = from; t < to; t++)
            {
                bool open;
                switch (how)
                {
                    case Opening.Arcade:
                        // Pillars at both ends and every third cell between, none right beside an end (it would join it).
                        open = t != from && t != to - 1 && ((t - from) % 3 != 0 || t == to - 2);
                        break;
                    case Opening.Open:
                        open = t != from && t != to - 1;
                        break;
                    default:
                        open = false;
                        break;
                }
                if (open)
                    OpenAcross(layout, acrossX, t, acrossX ? x0 : y0);
                // An arcade's pillars between its ends stand one cell deep (two deep, each drew as a pair).
                else if (how == Opening.Arcade && t != from && t != to - 1)
                    layout.Set(acrossX ? x0 + 1 : t, acrossX ? t : y0 + 1, DungeonCell.Floor);
            }
            var middle = from + length / 2;
            if (how == Opening.Doorway)
            {
                var width = length >= 8 && random.Next(2) == 0 ? 3 : 2;
                var at = from + 1 + random.Next(Mathf.Max(1, length - width - 1));
                for (var t = at; t < at + width && t < to - 1; t++)
                    OpenAcross(layout, acrossX, t, acrossX ? x0 : y0);
                middle = at + width / 2;
            }
            var door = acrossX ? new Vector2Int(x0, middle) : new Vector2Int(middle, y0);
            doorsA.Add(door);
            doorsB.Add(door);
        }

        static void OpenAcross(DungeonLayout layout, bool acrossX, int along, int start)
        {
            for (var k = 0; k < CathedralWall; k++)
                layout.Set(acrossX ? start + k : along, acrossX ? along : start + k, DungeonCell.Floor);
        }

        /// <summary>Two rows of columns down a big hall's length, a third of the way in from each long wall, every three or
        /// four cells, clear of its ends: the cathedral's nave.</summary>
        static void Columns(DungeonLayout layout, RectInt r)
        {
            var alongX = r.width >= r.height;
            var across = alongX ? r.height : r.width;
            var length = alongX ? r.width : r.height;
            // Single columns: a 2 x 2 block drew as a clump of four pillars (2026-10-26).
            const int size = 1;
            var inset = Mathf.Max(2, across / 3 - size / 2);
            const int gap = 3;
            for (var t = 3; t + size <= length - 3; t += gap)
                foreach (var row in new[] { inset, across - inset - size })
                {
                    var x = alongX ? r.xMin + t : r.xMin + row;
                    var y = alongX ? r.yMin + row : r.yMin + t;
                    Block(layout, x, y, size);
                }
        }

        /// <summary>Steps along the links from one room to every other (-1 where none reach).</summary>
        static int[] Steps(List<int>[] next, int from)
        {
            var steps = new int[next.Length];
            for (var i = 0; i < steps.Length; i++)
                steps[i] = -1;
            steps[from] = 0;
            var queue = new Queue<int>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var i = queue.Dequeue();
                foreach (var j in next[i])
                    if (steps[j] < 0)
                    {
                        steps[j] = steps[i] + 1;
                        queue.Enqueue(j);
                    }
            }
            return steps;
        }

        static int Furthest(int[] steps)
        {
            var best = 0;
            for (var i = 1; i < steps.Length; i++)
                if (steps[i] > steps[best])
                    best = i;
            return best;
        }
    }
}
