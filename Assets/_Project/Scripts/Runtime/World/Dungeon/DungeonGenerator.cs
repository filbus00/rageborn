using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Act-wide dungeon rules: how many levels, what enemy level each depth holds, and how a level's seed and id come
    /// from the session. Pure.
    /// </summary>
    public static class DungeonRules
    {
        /// <summary>Docs/05-world-and-content.md: about 6 levels per act.</summary>
        public const int LevelsPerAct = 6;

        /// <summary>
        /// The enemy level of each depth, 1 to 6 (index 0 unused). Tuning: they follow the level a character reaches by
        /// clearing each level, measured with the balance report. The first version spread Docs/05's act 1 range of player
        /// levels 1 to 12 evenly (1, 3, 5, 8, 10, 12), and the character fell three levels behind; the user chose to
        /// flatten it (2026-09-26). With lower enemy levels a clear also gives less XP, so act 1 now ends near
        /// character level 7, not 12.
        /// </summary>
        static readonly int[] ActOneEnemyLevels = { 0, 1, 2, 3, 4, 5, 7 };

        public static int EnemyLevel(int depth) => ActOneEnemyLevels[Mathf.Clamp(depth, 1, LevelsPerAct)];

        /// <summary>A level's seed, mixed from the session's dungeon seed and the depth, so the whole dungeon of a game
        /// session is one saved number and each level still differs.</summary>
        public static int LevelSeed(int dungeonSeed, int depth)
        {
            unchecked
            {
                var h = (uint)dungeonSeed * 2654435761u ^ (uint)depth * 2246822519u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                h *= 3266489917u;
                h ^= h >> 16;
                return (int)h;
            }
        }

        const string LevelIdPrefix = "Dungeon ";

        /// <summary>The id a dungeon level uses for kills, chests and corpses, in place of a scene name.</summary>
        public static string LevelId(int depth) => LevelIdPrefix + depth;

        /// <summary>Whether a level id, or a key that starts with one ("Dungeon 2/Pack 3"), belongs to a generated level.</summary>
        public static bool IsDungeonKey(string key) => key != null && key.StartsWith(LevelIdPrefix, StringComparison.Ordinal);
    }

    /// <summary>The generator's knobs. The defaults are the game's; tests can change them. Every value not quoted from
    /// Docs/05-world-and-content.md is tuning.</summary>
    public sealed class DungeonSettings
    {
        /// <summary>Docs: 5 to 8 rooms between the start room and the exit room.</summary>
        public int MinMiddleRooms = 5;
        public int MaxMiddleRooms = 8;

        /// <summary>Cells between neighbouring room centers. Room insides are at most <see cref="MaxRoomSize"/> (36,
        /// about 25 ground units), so every corridor is at least 6 cells long.</summary>
        public int Pitch = 44;

        public int MaxRoomSize => Pitch - 8;

        // Docs: combat 60, elite 15, treasure 10, shrine 10, ambush 5. Shrine and ambush rooms are not built, so their
        // 15 goes to combat.
        public int CombatWeight = 75;
        public int EliteWeight = 15;
        public int TreasureWeight = 10;

        /// <summary>Room size mix for middle rooms: small (up to 22 cells), medium (up to 30), large.</summary>
        public int SmallWeight = 30;
        public int MediumWeight = 50;
        public int LargeWeight = 20;

        /// <summary>Docs/01-core-gameplay.md: 7 for normal enemies, 10 for elites. Packs are kept at least this far
        /// (plus their radius and <see cref="ArrivalMargin"/>) from where the player arrives.</summary>
        public float NormalAggroRange = 7f;
        public float EliteAggroRange = 10f;
        public float ArrivalMargin = 1.5f;

        /// <summary>Docs: normal packs have 3 to 12 members.</summary>
        public int MinPackSize = 6;
        public int MaxPackSize = 12;
        public int MinElitePackSize = 3;
        public int MaxElitePackSize = 4;
        public int MinGuardPackSize = 3;
        public int MaxGuardPackSize = 5;

        /// <summary>Chance that a normal pack has a Champion leader.</summary>
        public float ChampionChance = 0.25f;

        public float ElitePackRadius = 1.1f;

        /// <summary>A pack of 12 at 2.2 keeps its members clear of each other, as the test room found.</summary>
        public float PackRadius(int count) => 1.2f + 0.08f * count;
    }

    /// <summary>
    /// Builds a dungeon level from hand-authored rooms and a seed (Docs/05-world-and-content.md, level generation).
    /// Rooms sit on a coarse grid, one per slot, grown as a tree from the start room so every room is reachable; the
    /// exit room is the one furthest from the start along the tree. Neighbours in the tree are joined by straight
    /// corridors as wide as a doorway, and every empty cell touching floor becomes wall. Same seed, same level. Pure.
    /// </summary>
    public static class DungeonGenerator
    {
        /// <summary>
        /// Which generator built the saved dungeon. Bump it whenever the same seed would build a different level (room
        /// library, sizes, pack rules): a save from another version forgets its dungeon kills and chests, which would
        /// otherwise land on the wrong packs. 1: the first, small rooms. 2: bigger, open rooms, 5 cell doorways.
        /// 3: the last level's exit room is the boss arena.
        /// </summary>
        public const int Version = 3;

        /// <summary>Docs/05-world-and-content.md: an act boss fights in a circular arena of radius 12 units. 18 cells is
        /// 12.7 units; the arena is the largest room that fits, so the doorways stay on its rim.</summary>
        public const int BossArenaRadiusCells = 18;

        static RoomShape bossArena;

        /// <summary>The arena as a room shape: a disc of floor in a 36 cell square, blocked outside it.</summary>
        public static RoomShape BossArenaShape
        {
            get
            {
                if (bossArena != null)
                    return bossArena;
                var size = BossArenaRadiusCells * 2;
                var blocked = new bool[size, size];
                var center = BossArenaRadiusCells;
                for (var x = 0; x < size; x++)
                    for (var y = 0; y < size; y++)
                    {
                        var dx = x + 0.5f - center;
                        var dy = y + 0.5f - center;
                        blocked[x, y] = dx * dx + dy * dy > BossArenaRadiusCells * BossArenaRadiusCells;
                    }
                bossArena = new RoomShape("Boss Arena", blocked);
                return bossArena;
            }
        }

        const int SmallMax = 22;
        const int MediumMax = 30;

        // The chance a new room grows off the most recent one rather than any earlier one: higher makes a longer main
        // path, lower a bushier level.
        const double GrowFromNewestChance = 0.6;

        public static DungeonLayout Generate(int seed, int depth, IReadOnlyList<RoomShape> shapes, DungeonSettings settings = null)
        {
            settings ??= new DungeonSettings();
            if (shapes == null || shapes.Count == 0)
                throw new ArgumentException("The dungeon needs at least one room shape.", nameof(shapes));
            foreach (var shape in shapes)
                if (shape.Size > settings.MaxRoomSize)
                    throw new ArgumentException($"Room {shape.Name} is {shape.Size} cells; the largest that fits is {settings.MaxRoomSize}.", nameof(shapes));

            var random = new System.Random(seed);

            // 1. Grow the room tree on the coarse grid.
            var roomCount = random.Next(settings.MinMiddleRooms, settings.MaxMiddleRooms + 1) + 2;
            var macros = new List<Vector2Int> { Vector2Int.zero };
            var parents = new List<int> { -1 };
            var treeDepth = new List<int> { 0 };
            var taken = new HashSet<Vector2Int> { Vector2Int.zero };
            while (macros.Count < roomCount)
            {
                var from = random.NextDouble() < GrowFromNewestChance ? macros.Count - 1 : random.Next(macros.Count);
                if (!TryGrow(from, macros, taken, random, out var next))
                {
                    // The chosen room is boxed in; grow off any room that is not.
                    var grown = false;
                    foreach (var index in Shuffled(macros.Count, random))
                        if (TryGrow(index, macros, taken, random, out next))
                        {
                            from = index;
                            grown = true;
                            break;
                        }
                    if (!grown)
                        break;
                }

                macros.Add(next);
                parents.Add(from);
                treeDepth.Add(treeDepth[from] + 1);
                taken.Add(next);
            }

            var exit = 0;
            for (var i = 1; i < macros.Count; i++)
                if (treeDepth[i] >= treeDepth[exit])
                    exit = i;

            // 2. Room kinds, with the docs' guarantees: at least one elite room and one treasure room.
            var kinds = new RoomKind[macros.Count];
            var middle = new List<int>();
            for (var i = 0; i < macros.Count; i++)
            {
                if (i == 0)
                    kinds[i] = RoomKind.Start;
                else if (i == exit)
                    kinds[i] = depth >= DungeonRules.LevelsPerAct ? RoomKind.Boss : RoomKind.Exit;
                else
                {
                    kinds[i] = RollKind(random, settings);
                    middle.Add(i);
                }
            }
            Guarantee(RoomKind.Elite, kinds, middle, random);
            Guarantee(RoomKind.Treasure, kinds, middle, random);

            // 3. Shapes.
            var small = new List<RoomShape>();
            var medium = new List<RoomShape>();
            var large = new List<RoomShape>();
            foreach (var shape in shapes)
                (shape.Size <= SmallMax ? small : shape.Size <= MediumMax ? medium : large).Add(shape);

            var chosen = new RoomShape[macros.Count];
            for (var i = 0; i < macros.Count; i++)
            {
                if (kinds[i] == RoomKind.Boss)
                    chosen[i] = BossArenaShape;
                else if (kinds[i] == RoomKind.Start)
                    chosen[i] = Pick(random, small, medium, large);
                else if (kinds[i] == RoomKind.Exit)
                    chosen[i] = Pick(random, medium, large, small);
                else
                {
                    var roll = random.Next(settings.SmallWeight + settings.MediumWeight + settings.LargeWeight);
                    chosen[i] = roll < settings.SmallWeight ? Pick(random, small, medium, large)
                        : roll < settings.SmallWeight + settings.MediumWeight ? Pick(random, medium, large, small)
                        : Pick(random, large, medium, small);
                }
            }

            // 4. Bounds and carving.
            var pitch = settings.Pitch;
            var minMacro = macros[0];
            var maxMacro = macros[0];
            foreach (var m in macros)
            {
                minMacro = Vector2Int.Min(minMacro, m);
                maxMacro = Vector2Int.Max(maxMacro, m);
            }
            var bounds = new RectInt(
                minMacro.x * pitch - pitch / 2, minMacro.y * pitch - pitch / 2,
                (maxMacro.x - minMacro.x + 1) * pitch, (maxMacro.y - minMacro.y + 1) * pitch);

            var layout = new DungeonLayout(bounds)
            {
                Depth = depth,
                EnemyLevel = DungeonRules.EnemyLevel(depth),
            };

            for (var i = 0; i < macros.Count; i++)
            {
                var shape = chosen[i];
                var center = macros[i] * pitch;
                var interior = new RectInt(center.x - shape.Size / 2, center.y - shape.Size / 2, shape.Size, shape.Size);
                for (var x = 0; x < shape.Size; x++)
                    for (var y = 0; y < shape.Size; y++)
                        layout.Set(interior.xMin + x, interior.yMin + y, shape.IsBlocked(x, y) ? DungeonCell.Wall : DungeonCell.Floor);
                layout.Rooms.Add(new RoomPlacement(kinds[i], shape, macros[i], interior));
            }

            for (var i = 1; i < macros.Count; i++)
                CarveCorridor(layout, layout.Rooms[parents[i]], layout.Rooms[i], pitch);

            WrapWalls(layout);

            // 5. Stairs and arrival points.
            // The stairs up go in the start room's corner furthest from its doorways, and the player arrives on the room's
            // side of them, so heading off into the level never walks back over the stairs (the first build put them in
            // the middle with the player just below, and walking up the screen went straight back to town).
            var startRoom = layout.Rooms[0];
            var startDoors = new List<Vector2Int>();
            for (var i = 1; i < macros.Count; i++)
                if (parents[i] == 0)
                    startDoors.Add(DoorMidpoint(startRoom, macros[i] - macros[0]));
            layout.StairsUp = NearestFloor(layout, startRoom.Interior, CornerAwayFrom(startRoom.Interior, startDoors), null, 0);
            layout.ArrivalFromAbove = NearestFloor(layout, startRoom.Interior, Vector2Int.RoundToInt(Vector2.Lerp(layout.StairsUp, Center(startRoom.Interior), 0.35f)), layout.StairsUp, 2);

            var exitRoom = layout.Rooms[exit];
            layout.HasStairsDown = depth < DungeonRules.LevelsPerAct;
            if (exitRoom.Kind == RoomKind.Boss)
            {
                layout.HasBossArena = true;
                layout.BossArenaCenter = new Vector2Int(exitRoom.Interior.xMin + BossArenaRadiusCells, exitRoom.Interior.yMin + BossArenaRadiusCells);
                layout.BossArenaRadius = BossArenaRadiusCells;
            }
            if (layout.HasStairsDown)
            {
                // In the corner furthest from the doorway the player walks in by, so arriving from below lands clear of
                // the exit room's own pack, which goes toward the doorway, and the way out does not cross the stairs.
                var exitDoor = new List<Vector2Int> { DoorMidpoint(exitRoom, macros[parents[exit]] - macros[exit]) };
                layout.StairsDown = NearestFloor(layout, exitRoom.Interior, CornerAwayFrom(exitRoom.Interior, exitDoor), null, 0);
                layout.ArrivalFromBelow = NearestFloor(layout, exitRoom.Interior, Vector2Int.RoundToInt(Vector2.Lerp(layout.StairsDown, Center(exitRoom.Interior), 0.35f)), layout.StairsDown, 2);
            }

            // 6. Chests, then packs.
            for (var i = 0; i < layout.Rooms.Count; i++)
                if (layout.Rooms[i].Kind == RoomKind.Treasure)
                    layout.Chests.Add(NearestFloor(layout, layout.Rooms[i].Interior, Center(layout.Rooms[i].Interior), null, 0));

            for (var i = 0; i < layout.Rooms.Count; i++)
                PlacePacks(layout, i, random, settings);

            // The docs guarantee an elite pack. When its room could not hold it (pillars, or too close to an arrival
            // point), put it in another room, furthest from the start first.
            if (!layout.Packs.Exists(p => p.Kind == PackKind.Elite))
            {
                var order = new List<int>();
                for (var i = 1; i < layout.Rooms.Count; i++)
                    if (layout.Rooms[i].Kind != RoomKind.Boss)
                        order.Add(i);
                order.Sort((a, b) => treeDepth[b].CompareTo(treeDepth[a]));
                foreach (var i in order)
                    if (TryPlacePack(layout, i, PackKind.Elite, random.Next(settings.MinElitePackSize, settings.MaxElitePackSize + 1), random, settings))
                        break;
            }

            return layout;
        }

        static bool TryGrow(int from, List<Vector2Int> macros, HashSet<Vector2Int> taken, System.Random random, out Vector2Int next)
        {
            foreach (var d in Shuffled(RoomShape.Sides.Length, random))
            {
                var candidate = macros[from] + RoomShape.Sides[d];
                if (!taken.Contains(candidate))
                {
                    next = candidate;
                    return true;
                }
            }
            next = default;
            return false;
        }

        static IEnumerable<int> Shuffled(int count, System.Random random)
        {
            var order = new int[count];
            for (var i = 0; i < count; i++)
                order[i] = i;
            for (var i = count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            return order;
        }

        static RoomKind RollKind(System.Random random, DungeonSettings settings)
        {
            var roll = random.Next(settings.CombatWeight + settings.EliteWeight + settings.TreasureWeight);
            if (roll < settings.CombatWeight)
                return RoomKind.Combat;
            return roll < settings.CombatWeight + settings.EliteWeight ? RoomKind.Elite : RoomKind.Treasure;
        }

        static void Guarantee(RoomKind kind, RoomKind[] kinds, List<int> middle, System.Random random)
        {
            foreach (var i in middle)
                if (kinds[i] == kind)
                    return;

            var combat = middle.FindAll(i => kinds[i] == RoomKind.Combat);
            var pool = combat.Count > 0 ? combat : middle;
            if (pool.Count > 0)
                kinds[pool[random.Next(pool.Count)]] = kind;
        }

        static RoomShape Pick(System.Random random, List<RoomShape> first, List<RoomShape> second, List<RoomShape> third)
        {
            var list = first.Count > 0 ? first : second.Count > 0 ? second : third;
            return list[random.Next(list.Count)];
        }

        static Vector2Int Center(RectInt r) => new Vector2Int(r.xMin + r.width / 2, r.yMin + r.height / 2);

        /// <summary>The middle of a room's doorway on the side facing <paramref name="direction"/>.</summary>
        static Vector2Int DoorMidpoint(RoomPlacement room, Vector2Int direction) =>
            Center(room.Interior) + direction * (room.Shape.Size / 2);

        /// <summary>Of the four spots three cells in from the room's corners, the one whose nearest doorway is furthest.</summary>
        static Vector2Int CornerAwayFrom(RectInt interior, List<Vector2Int> doors)
        {
            const int inset = 3;
            var corners = new[]
            {
                new Vector2Int(interior.xMin + inset, interior.yMin + inset),
                new Vector2Int(interior.xMax - 1 - inset, interior.yMin + inset),
                new Vector2Int(interior.xMin + inset, interior.yMax - 1 - inset),
                new Vector2Int(interior.xMax - 1 - inset, interior.yMax - 1 - inset),
            };

            var best = corners[0];
            var bestDistance = -1f;
            foreach (var corner in corners)
            {
                var nearest = float.MaxValue;
                foreach (var door in doors)
                    nearest = Mathf.Min(nearest, Vector2Int.Distance(corner, door));
                if (nearest > bestDistance)
                {
                    bestDistance = nearest;
                    best = corner;
                }
            }
            return best;
        }

        static void CarveCorridor(DungeonLayout layout, RoomPlacement a, RoomPlacement b, int pitch)
        {
            var from = a.Macro * pitch;
            var step = b.Macro - a.Macro;
            var half = RoomShape.DoorWidth / 2;
            for (var t = 0; t <= pitch; t++)
            {
                var along = from + step * t;
                for (var w = -half; w <= half; w++)
                {
                    var cell = step.x != 0 ? new Vector2Int(along.x, along.y + w) : new Vector2Int(along.x + w, along.y);
                    if (!a.Interior.Contains(cell) && !b.Interior.Contains(cell))
                        layout.Set(cell.x, cell.y, DungeonCell.Floor);
                }
            }
        }

        static void WrapWalls(DungeonLayout layout)
        {
            var b = layout.Bounds;
            for (var x = b.xMin; x < b.xMax; x++)
                for (var y = b.yMin; y < b.yMax; y++)
                {
                    if (layout.Get(x, y) != DungeonCell.Void)
                        continue;
                    for (var dx = -1; dx <= 1; dx++)
                        for (var dy = -1; dy <= 1; dy++)
                            if (layout.Get(x + dx, y + dy) == DungeonCell.Floor)
                            {
                                layout.Set(x, y, DungeonCell.Wall);
                                dx = dy = 2;
                            }
                }
        }

        /// <summary>The floor cell in the rectangle closest to the target, at least minDistance cells (Chebyshev) from
        /// avoid when given.</summary>
        static Vector2Int NearestFloor(DungeonLayout layout, RectInt area, Vector2Int target, Vector2Int? avoid, int minDistance)
        {
            var best = target;
            var bestDistance = int.MaxValue;
            for (var x = area.xMin; x < area.xMax; x++)
                for (var y = area.yMin; y < area.yMax; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!layout.IsFloor(cell))
                        continue;
                    if (avoid.HasValue && Mathf.Max(Mathf.Abs(x - avoid.Value.x), Mathf.Abs(y - avoid.Value.y)) < minDistance)
                        continue;
                    var d = (cell - target).sqrMagnitude;
                    if (d < bestDistance)
                    {
                        bestDistance = d;
                        best = cell;
                    }
                }
            return best;
        }

        static float GroundDistance(Vector2Int a, Vector2Int b) => Vector2.Distance(IsoMath.CellToGround(a), IsoMath.CellToGround(b));

        static void PlacePacks(DungeonLayout layout, int roomIndex, System.Random random, DungeonSettings settings)
        {
            var room = layout.Rooms[roomIndex];
            var size = room.Shape.Size;
            switch (room.Kind)
            {
                case RoomKind.Combat:
                {
                    // Bigger rooms hold more packs, so an open hall is a fight, not a walk.
                    var packs = size <= SmallMax ? random.Next(1, 3) : size <= MediumMax ? random.Next(2, 4) : random.Next(3, 5);
                    for (var i = 0; i < packs; i++)
                        TryPlacePack(layout, roomIndex, NormalKind(random, settings), random.Next(settings.MinPackSize, settings.MaxPackSize + 1), random, settings);
                    break;
                }
                case RoomKind.Elite:
                    TryPlacePack(layout, roomIndex, PackKind.Elite, random.Next(settings.MinElitePackSize, settings.MaxElitePackSize + 1), random, settings);
                    // A large elite room also holds a normal pack, so the elites are not alone in a big empty hall.
                    if (size > MediumMax)
                        TryPlacePack(layout, roomIndex, NormalKind(random, settings), random.Next(settings.MinPackSize, settings.MaxPackSize + 1), random, settings);
                    break;
                case RoomKind.Treasure:
                    TryPlacePack(layout, roomIndex, PackKind.Normal, random.Next(settings.MinGuardPackSize, settings.MaxGuardPackSize + 1), random, settings);
                    break;
                case RoomKind.Exit:
                    TryPlacePack(layout, roomIndex, NormalKind(random, settings), random.Next(settings.MinPackSize, settings.MaxPackSize + 1), random, settings);
                    break;
            }
        }

        static PackKind NormalKind(System.Random random, DungeonSettings settings) =>
            random.NextDouble() < settings.ChampionChance ? PackKind.WithChampion : PackKind.Normal;

        // A pack's disc may overlap a pillar: members whose spot is not walkable are left out by EnemyPack. Most of it
        // must be floor so the pack keeps its size.
        const float MinDiscFloorShare = 0.75f;

        /// <summary>
        /// Puts a pack on a random floor spot in the room where most of its disc is floor, it is clear of the other
        /// packs, and it cannot see the player arrive. Returns false, leaving it out, when the room has no such spot.
        /// </summary>
        static bool TryPlacePack(DungeonLayout layout, int roomIndex, PackKind kind, int count, System.Random random, DungeonSettings settings)
        {
            var room = layout.Rooms[roomIndex];
            var radius = kind == PackKind.Elite ? settings.ElitePackRadius : settings.PackRadius(count);
            var aggro = kind == PackKind.Elite ? settings.EliteAggroRange : settings.NormalAggroRange;
            var safeFromArrival = aggro + radius + settings.ArrivalMargin;

            // An edge neighbour is 0.707 ground units away, so the disc spans this many cells each way.
            var reach = Mathf.CeilToInt(radius / 0.7071f);

            var candidates = new List<Vector2Int>();
            var area = room.Interior;
            for (var x = area.xMin + reach; x < area.xMax - reach; x++)
                for (var y = area.yMin + reach; y < area.yMax - reach; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!layout.IsFloor(cell) || DiscFloorShare(layout, cell, reach) < MinDiscFloorShare)
                        continue;
                    if (GroundDistance(cell, layout.ArrivalFromAbove) < safeFromArrival)
                        continue;
                    if (layout.HasStairsDown && GroundDistance(cell, layout.ArrivalFromBelow) < safeFromArrival)
                        continue;
                    if (TooCloseToPacks(layout, cell, radius))
                        continue;
                    candidates.Add(cell);
                }

            if (candidates.Count == 0)
                return false;

            layout.Packs.Add(new PackPlacement(candidates[random.Next(candidates.Count)], count, radius, kind, roomIndex));
            return true;
        }

        static float DiscFloorShare(DungeonLayout layout, Vector2Int center, int reach)
        {
            var floor = 0;
            var total = 0;
            for (var dx = -reach; dx <= reach; dx++)
                for (var dy = -reach; dy <= reach; dy++)
                {
                    total++;
                    if (layout.IsFloor(new Vector2Int(center.x + dx, center.y + dy)))
                        floor++;
                }
            return (float)floor / total;
        }

        static bool TooCloseToPacks(DungeonLayout layout, Vector2Int cell, float radius)
        {
            foreach (var other in layout.Packs)
                if (GroundDistance(cell, other.Cell) < radius + other.Radius + 2f)
                    return true;
            return false;
        }
    }
}
