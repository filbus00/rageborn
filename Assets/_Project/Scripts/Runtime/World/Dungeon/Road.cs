using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The numbers of the Vigil's road (Docs/05, "The Vigil's road", the owner's design of 2026-10-06): one long hall up
    /// the screen in stretches, each with a rift at its top streaming demon packs until she kills its guardian. What
    /// makes the stream harder is how close she is to the rift (no timers). Pure. A stretch's number is the old depth.
    /// </summary>
    public static class RoadRules
    {
        /// <summary>The rifts that close; the stretch after the last holds the rift that never does.</summary>
        public const int Rifts = DungeonRules.Depths;

        /// <summary>How far a lit beacon's light reaches, in ground units: demons will not come within it.</summary>
        public const float LightRadius = 8f;

        /// <summary>How near the rift she comes (ground units) before its guardian comes through.</summary>
        public const float GuardianReach = 6f;

        /// <summary>The stream stops adding packs while this many of its demons are alive: 8 at the beacon, 18 at the
        /// rift (tuned 2026-10-06 after a scripted push swamped a starter character at three quarters up rift 1).</summary>
        public static int MaxAwake(float closeness) => 8 + Mathf.RoundToInt(10f * Mathf.Clamp01(closeness));

        /// <summary>The share of carried gold lost on dying (the owner: "lose gold"; the amount is Claude's).</summary>
        public const float DeathGoldLoss = 0.1f;

        /// <summary>How far up the hall ahead of her a pack joins the stream (path units): about a screen, so packs come
        /// down at her from out of sight.</summary>
        public const float SpawnAhead = 34f;

        /// <summary>A pack never joins nearer than this to her in a straight line (ground units): off the screen's top.</summary>
        public const float MinSpawnDistance = 24f;

        /// <summary>On the endless stretch, one more enemy level per this many path units up the hall.</summary>
        public const float EndlessLevelEvery = 12f;

        public static bool IsEndless(int stretch) => stretch > Rifts;

        /// <summary>Whether a stretch's rift is closed: every stretch below the one whose rift is open. The game keeps the
        /// open rift's stretch as <see cref="GameSession.DeepestDepth"/> (beacons lit, the first one counted).</summary>
        public static bool IsClosed(int stretch, int openStretch) => stretch < openStretch && !IsEndless(stretch);

        /// <summary>Which stretch the town's stairs lead to: the open rift's, at its lit beacon.</summary>
        public static int StretchFromTown(int openStretch) => Mathf.Clamp(openStretch, 1, Rifts + 1);

        /// <summary>0 at the bottom beacon, 1 at the rift, by how far up the hall she is.</summary>
        public static float Closeness(float progress, float length) => length <= 0f ? 1f : Mathf.Clamp01(progress / length);

        /// <summary>The level of a pack joining the stream: the stretch's level at the beacon, 3 more at the rift; on the
        /// endless stretch it keeps rising up the hall.</summary>
        public static int PackLevel(int stretch, float closeness, float progress)
        {
            if (IsEndless(stretch))
                return DungeonRules.EnemyLevel(Rifts) + 3 + Mathf.FloorToInt(Mathf.Max(0f, progress) / EndlessLevelEvery);
            return DungeonRules.EnemyLevel(stretch) + Mathf.RoundToInt(3f * Mathf.Clamp01(closeness));
        }

        /// <summary>The guardian's level: the rift's own.</summary>
        public static int GuardianLevel(int stretch) => DungeonRules.EnemyLevel(Mathf.Min(stretch, Rifts)) + 3;

        /// <summary>3 demons a pack at the beacon, 5 at the rift.</summary>
        public static int PackSize(float closeness) => 3 + Mathf.RoundToInt(2f * Mathf.Clamp01(closeness));

        /// <summary>Seconds between packs: 8 at the beacon, 4 at the rift.</summary>
        public static float SurgeSeconds(float closeness) => Mathf.Lerp(8f, 4f, Mathf.Clamp01(closeness));

        public static float ChampionChance(int stretch, float closeness) =>
            Mathf.Lerp(0.05f, 0.3f, Mathf.Clamp01(closeness)) + DungeonRules.ExtraChampionChance(Mathf.Min(stretch, Rifts));

        /// <summary>Elite pairs: none on the first stretch, growing in over the next four, more toward the rift.</summary>
        public static float EliteChance(int stretch, float closeness) =>
            Mathf.Lerp(0f, 0.12f, Mathf.Clamp01(closeness)) * Mathf.Clamp01((stretch - 1) / 4f);

        /// <summary>What kind of pack joins, from a roll in 0..1: an elite pack, one led by a champion, or a normal one.</summary>
        public static PackKind RollKind(int stretch, float closeness, float roll)
        {
            var elite = EliteChance(stretch, closeness);
            if (roll < elite)
                return PackKind.Elite;
            return roll < elite + ChampionChance(stretch, closeness) ? PackKind.WithChampion : PackKind.Normal;
        }

        public static int GoldLostOnDeath(int gold) => Mathf.FloorToInt(Mathf.Max(0, gold) * DeathGoldLoss);
    }

    /// <summary>The hall's middle line from the bottom beacon to the rift, in ground space: how far up she is.</summary>
    public sealed class RoadPath
    {
        readonly List<Vector2> points;
        readonly float[] along;

        public RoadPath(List<Vector2> points)
        {
            this.points = points;
            along = new float[points.Count];
            for (var i = 1; i < points.Count; i++)
                along[i] = along[i - 1] + Vector2.Distance(points[i - 1], points[i]);
        }

        public IReadOnlyList<Vector2> Points => points;

        public float Length => along.Length > 0 ? along[along.Length - 1] : 0f;

        /// <summary>How far along the line the nearest point to a position lies.</summary>
        public float Progress(Vector2 position)
        {
            if (points.Count < 2)
                return 0f;
            var best = float.MaxValue;
            var result = 0f;
            for (var i = 1; i < points.Count; i++)
            {
                Vector2 a = points[i - 1], b = points[i];
                var segment = b - a;
                var lengthSquared = segment.sqrMagnitude;
                var t = lengthSquared > 1e-6f ? Mathf.Clamp01(Vector2.Dot(position - a, segment) / lengthSquared) : 0f;
                var distance = (a + segment * t - position).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    result = along[i - 1] + t * Mathf.Sqrt(lengthSquared);
                }
            }
            return result;
        }

        /// <summary>The point a distance along the line (clamped to its ends).</summary>
        public Vector2 PointAt(float distance)
        {
            if (points.Count == 0)
                return Vector2.zero;
            if (distance <= 0f)
                return points[0];
            for (var i = 1; i < points.Count; i++)
                if (distance <= along[i])
                {
                    var span = along[i] - along[i - 1];
                    return Vector2.Lerp(points[i - 1], points[i], span > 1e-6f ? (distance - along[i - 1]) / span : 1f);
                }
            return points[points.Count - 1];
        }

        /// <summary>The line's direction a distance along it (unit, ground space).</summary>
        public Vector2 DirectionAt(float distance)
        {
            for (var i = 1; i < points.Count; i++)
                if (distance <= along[i] || i == points.Count - 1)
                    return (points[i] - points[i - 1]).normalized;
            return Vector2.up;
        }
    }

    /// <summary>One stretch of the road: the level's cells and places, the hall's line, the rift and both beacons.</summary>
    public sealed class RoadLayout
    {
        public DungeonLayout Layout;
        public RoadPath Path;

        /// <summary>Where the rift has broken through, at the top of the stretch (the far end on the endless one).</summary>
        public Vector2Int Rift;

        /// <summary>The lit beacon at the bottom, where she arrives; its waystone stands beside it (Layout.Waypoint).</summary>
        public Vector2Int BottomBeacon;

        /// <summary>The beacon beside the rift, lit when the rift closes; the next stretch starts at it.</summary>
        public Vector2Int TopBeacon;

        /// <summary>The hall's width across, in cells.</summary>
        public int Width;
    }

    /// <summary>
    /// Builds a stretch of the Vigil's road (Docs/05, 2026-10-06), seeded: a wide hall winding up the screen in legs that
    /// run along the tile grid (up-right, then up-left, in turn), so its walls stay straight; pillars, props and
    /// decals in the legs; small side rooms with a chest off the outer side of some legs; the bottom beacon's landing and
    /// the way back down; at the top the rift's chamber (a boss's round arena on every sixth stretch) and a short way on.
    /// The endless stretch is a much longer hall with its rift at the far end. Pure.
    /// </summary>
    public static class RoadGenerator
    {
        /// <summary>Bump when the same seed would build a different stretch.</summary>
        public const int Version = 1;

        public const int HallWidth = 10;
        const int ChamberSize = 16;
        const int Legs = 7;
        const int EndlessLegs = 40;

        static readonly PropKind[] EdgeProps = { PropKind.Barrel, PropKind.Crate, PropKind.Urn, PropKind.BonePile, PropKind.Rubble, PropKind.BrokenColumn };
        static readonly DecalKind[] Decals = { DecalKind.Cracks, DecalKind.Bones, DecalKind.Blood, DecalKind.Rubble, DecalKind.Moss, DecalKind.Skull, DecalKind.Puddle };

        public static RoadLayout Generate(int seed, int stretch)
        {
            var random = new System.Random(seed);
            var endless = RoadRules.IsEndless(stretch);
            var boss = !endless && DungeonRules.IsBossDepth(stretch);
            const int w = HallWidth;
            const float half = (w - 1) * 0.5f;

            var floor = new HashSet<Vector2Int>();
            var walls = new HashSet<Vector2Int>();
            var rooms = new List<RoomPlacement>();
            var line = new List<Vector2>();
            var legRects = new List<(RectInt rect, int axis)>();

            // The legs. Each starts on the last W x W cells of the one before, turned a quarter.
            int x = 0, y = 0, axis = 0;
            var legCount = endless ? EndlessLegs : Legs;
            for (var i = 0; i < legCount; i++)
            {
                var length = i == 0 ? 24 : random.Next(18, 25);
                var rect = axis == 0 ? new RectInt(x, y, length, w) : new RectInt(x, y, w, length);
                Fill(floor, rect);
                legRects.Add((rect, axis));
                rooms.Add(new RoomPlacement(i == 0 ? RoomKind.Start : RoomKind.Combat, null, new Vector2Int(i, 0), rect));
                if (i == 0)
                    line.Add(new Vector2(x + 5, y + half));
                if (axis == 0)
                {
                    line.Add(new Vector2(x + length - 1 - half, y + half));
                    x += length - w;
                }
                else
                {
                    line.Add(new Vector2(x + half, y + length - 1 - half));
                    y += length - w;
                }
                axis = 1 - axis;
            }

            // The top: the rift's chamber continuing the way the next leg would go, or the boss's round arena, then a short
            // way on. (x, y) is the last leg's end square; axis the next direction.
            var along = axis == 0 ? new Vector2Int(1, 0) : new Vector2Int(0, 1);
            var across = axis == 0 ? new Vector2Int(0, 1) : new Vector2Int(1, 0);
            var corner = new Vector2(x + half, y + half);
            Vector2Int rift, topBeacon, exitEnd;
            var layoutBossCenter = Vector2Int.zero;
            const int arenaRadius = DungeonGenerator.BossArenaRadiusCells;
            if (endless)
            {
                rift = Vector2Int.RoundToInt(line[line.Count - 1]);
                topBeacon = rift;
                exitEnd = rift;
            }
            else if (boss)
            {
                var center = Vector2Int.RoundToInt(corner) + along * (arenaRadius - 1);
                for (var dx = -arenaRadius; dx <= arenaRadius; dx++)
                    for (var dy = -arenaRadius; dy <= arenaRadius; dy++)
                        if (dx * dx + dy * dy <= arenaRadius * arenaRadius)
                            floor.Add(center + new Vector2Int(dx, dy));
                rooms.Add(new RoomPlacement(RoomKind.Boss, null, new Vector2Int(legCount, 0),
                    new RectInt(center.x - arenaRadius, center.y - arenaRadius, arenaRadius * 2 + 1, arenaRadius * 2 + 1)));
                layoutBossCenter = center;
                rift = center + along * 4;
                topBeacon = center + along * (arenaRadius - 3) + across * 3;
                exitEnd = Stub(floor, center + along * arenaRadius, along, across);
                line.Add(new Vector2(rift.x, rift.y));
            }
            else
            {
                var start = new Vector2Int(x, y) - across * ((ChamberSize - w) / 2);
                var chamber = axis == 0 ? new RectInt(start.x, start.y, ChamberSize, ChamberSize) : new RectInt(start.x, start.y, ChamberSize, ChamberSize);
                Fill(floor, chamber);
                rooms.Add(new RoomPlacement(RoomKind.Exit, null, new Vector2Int(legCount, 0), chamber));
                var middle = new Vector2(chamber.x + (ChamberSize - 1) * 0.5f, chamber.y + (ChamberSize - 1) * 0.5f);
                rift = Vector2Int.RoundToInt(middle) + along * 2;
                topBeacon = rift + across * 4 - along * 2;
                var edge = axis == 0 ? new Vector2Int(chamber.xMax, Mathf.RoundToInt(middle.y)) : new Vector2Int(Mathf.RoundToInt(middle.x), chamber.yMax);
                exitEnd = Stub(floor, edge, along, across);
                line.Add(new Vector2(rift.x, rift.y));
            }

            // Side rooms with a chest, off the outer side of some turns: the legs are too short for one off their middle,
            // but beyond a turn's far wall nothing else is built (the next leg goes the other way).
            var chests = new List<Vector2Int>();
            for (var i = 1; i < legRects.Count - 1; i++)
            {
                if (random.NextDouble() > 0.6)
                    continue;
                var (rect, legAxis) = legRects[i];
                var size = random.Next(7, 10);
                RectInt room;
                var doors = new List<Vector2Int>();
                if (legAxis == 0)
                {
                    // The turn at this +x leg's end; the room beyond its far wall (+x), a little down the screen.
                    var mid = rect.y + w / 2;
                    room = new RectInt(rect.xMax + 1, mid - size / 2 - 4, size, size);
                    for (var d = -1; d <= 1; d++)
                        doors.Add(new Vector2Int(rect.xMax, mid - 1 + d));
                }
                else
                {
                    var mid = rect.x + w / 2;
                    room = new RectInt(mid - size / 2 - 4, rect.yMax + 1, size, size);
                    for (var d = -1; d <= 1; d++)
                        doors.Add(new Vector2Int(mid - 1 + d, rect.yMax));
                }
                if (Overlaps(floor, room, 2))
                    continue;
                Fill(floor, room);
                foreach (var door in doors)
                    floor.Add(door);
                rooms.Add(new RoomPlacement(RoomKind.Treasure, null, new Vector2Int(i, 1), room));
                chests.Add(new Vector2Int(room.x + room.width / 2, room.y + room.height / 2));
            }

            // Walls: everything beside floor.
            foreach (var cell in floor)
                for (var dx = -1; dx <= 1; dx++)
                    for (var dy = -1; dy <= 1; dy++)
                    {
                        var next = cell + new Vector2Int(dx, dy);
                        if (!floor.Contains(next))
                            walls.Add(next);
                    }

            var min = new Vector2Int(int.MaxValue, int.MaxValue);
            var max = new Vector2Int(int.MinValue, int.MinValue);
            foreach (var cell in walls)
            {
                min = Vector2Int.Min(min, cell);
                max = Vector2Int.Max(max, cell);
            }
            var layout = new DungeonLayout(new RectInt(min.x - 1, min.y - 1, max.x - min.x + 3, max.y - min.y + 3));
            foreach (var cell in floor)
                layout.Set(cell.x, cell.y, DungeonCell.Floor);
            foreach (var cell in walls)
                layout.Set(cell.x, cell.y, DungeonCell.Wall);
            layout.Rooms.AddRange(rooms);
            layout.Depth = stretch;
            layout.EnemyLevel = DungeonRules.EnemyLevel(Mathf.Min(stretch, RoadRules.Rifts));

            // The bottom: the beacon on the first leg's middle line, the waystone across from it, the way back down at the
            // very end.
            var bottomBeacon = new Vector2Int(5, Mathf.RoundToInt(half) + 2);
            layout.Waypoint = new Vector2Int(5, Mathf.RoundToInt(half) - 2);
            layout.ArrivalFromAbove = new Vector2Int(8, Mathf.RoundToInt(half));
            layout.StairsUp = new Vector2Int(1, Mathf.RoundToInt(half));
            layout.WandererSpot = new Vector2Int(9, Mathf.RoundToInt(half) + 3);
            layout.HasWandererSpot = true;
            layout.HasStairsDown = !endless;
            layout.StairsDown = exitEnd;
            layout.ArrivalFromBelow = exitEnd - along * 4;
            if (boss)
            {
                layout.HasBossArena = true;
                layout.BossArenaCenter = layoutBossCenter;
                layout.BossArenaRadius = arenaRadius;
            }

            // Pillars across the legs (pairs of 2 x 2 blocks, the middle kept clear), props on the edges, decals.
            var keep = new HashSet<Vector2Int> { bottomBeacon, layout.Waypoint, layout.ArrivalFromAbove, layout.StairsUp, layout.WandererSpot, rift, topBeacon, exitEnd, layout.ArrivalFromBelow };
            foreach (var chest in chests)
                keep.Add(chest);
            for (var i = 0; i < legRects.Count; i++)
            {
                var (rect, legAxis) = legRects[i];
                var length = legAxis == 0 ? rect.width : rect.height;
                // Past the turning square shared with the leg before, and short of the next one.
                for (var s = i == 0 ? 12 : w + 1; s < length - w; s += 7)
                {
                    if (random.NextDouble() > 0.55)
                        continue;
                    foreach (var offset in new[] { 2, w - 4 })
                    {
                        var at = legAxis == 0 ? new Vector2Int(rect.x + s, rect.y + offset) : new Vector2Int(rect.x + offset, rect.y + s);
                        Pillar(layout, at, keep);
                    }
                }
            }
            foreach (var room in rooms)
            {
                if (room.Kind == RoomKind.Boss)
                    continue;
                var r = room.Interior;
                for (var cx = r.xMin; cx < r.xMax; cx++)
                    for (var cy = r.yMin; cy < r.yMax; cy++)
                    {
                        var at = new Vector2Int(cx, cy);
                        if (layout.Get(at) != DungeonCell.Floor || keep.Contains(at) || NearAny(at, keep, 2))
                            continue;
                        var edge = layout.Get(cx - 1, cy) == DungeonCell.Wall || layout.Get(cx + 1, cy) == DungeonCell.Wall ||
                                   layout.Get(cx, cy - 1) == DungeonCell.Wall || layout.Get(cx, cy + 1) == DungeonCell.Wall;
                        var roll = random.NextDouble();
                        if (edge && roll < 0.05 && room.Kind != RoomKind.Start)
                        {
                            layout.Set(cx, cy, DungeonCell.Prop);
                            layout.Props[at] = EdgeProps[random.Next(EdgeProps.Length)];
                        }
                        else if (roll > 0.965)
                            layout.Decals[at] = Decals[random.Next(Decals.Length)];
                    }
            }
            layout.Chests.AddRange(chests);

            // The hall's line in ground space.
            var ground = new List<Vector2>(line.Count);
            foreach (var point in line)
                ground.Add(CellToGround(point));

            return new RoadLayout
            {
                Layout = layout, Path = new RoadPath(ground), Rift = rift, BottomBeacon = bottomBeacon, TopBeacon = topBeacon, Width = w,
            };
        }

        /// <summary><see cref="IsoMath.CellToGround"/> for a point between cells.</summary>
        public static Vector2 CellToGround(Vector2 cell) => new Vector2((cell.x - cell.y) * 0.5f, (cell.x + cell.y) * 0.5f + 0.5f);

        // A short way on, 4 cells wide and 6 long, out from an edge cell; returns its far end, where the way on opens.
        static Vector2Int Stub(HashSet<Vector2Int> floor, Vector2Int edge, Vector2Int along, Vector2Int across)
        {
            for (var a = 0; a < 6; a++)
                for (var b = -2; b <= 1; b++)
                    floor.Add(edge + along * a + across * b);
            return edge + along * 4;
        }

        static void Fill(HashSet<Vector2Int> floor, RectInt rect)
        {
            for (var cx = rect.xMin; cx < rect.xMax; cx++)
                for (var cy = rect.yMin; cy < rect.yMax; cy++)
                    floor.Add(new Vector2Int(cx, cy));
        }

        static bool Overlaps(HashSet<Vector2Int> floor, RectInt rect, int margin)
        {
            for (var cx = rect.xMin - margin; cx < rect.xMax + margin; cx++)
                for (var cy = rect.yMin - margin; cy < rect.yMax + margin; cy++)
                    if (floor.Contains(new Vector2Int(cx, cy)))
                        return true;
            return false;
        }

        static void Pillar(DungeonLayout layout, Vector2Int at, HashSet<Vector2Int> keep)
        {
            for (var dx = -1; dx <= 2; dx++)
                for (var dy = -1; dy <= 2; dy++)
                    if (layout.Get(at.x + dx, at.y + dy) != DungeonCell.Floor || keep.Contains(at + new Vector2Int(dx, dy)))
                        return;
            for (var dx = 0; dx <= 1; dx++)
                for (var dy = 0; dy <= 1; dy++)
                    layout.Set(at.x + dx, at.y + dy, DungeonCell.Wall);
        }

        static bool NearAny(Vector2Int cell, HashSet<Vector2Int> points, int reach)
        {
            foreach (var point in points)
                if (Mathf.Abs(point.x - cell.x) <= reach && Mathf.Abs(point.y - cell.y) <= reach)
                    return true;
            return false;
        }
    }
}
