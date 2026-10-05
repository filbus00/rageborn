using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Dungeon-wide rules: how many levels, where the bosses are, what enemy level each depth holds, and how a level's
    /// seed and id come from the session. Pure.
    /// </summary>
    public static class DungeonRules
    {
        /// <summary>The owner, 2026-10-05: no acts; one town above one dungeon of about 24 levels ("Fixed, about 24
        /// levels"), changing gradually with depth.</summary>
        public const int Depths = 24;

        /// <summary>A boss every 6 levels (the Cinder Warden at 6). The first 6 are what was act 1.</summary>
        public const int BossEvery = 6;

        /// <summary>The first 6 levels (was act 1): the autopilot plays them, the DEV shortcuts reach their boss.</summary>
        public const int LevelsPerAct = BossEvery;

        /// <summary>Depths whose exit room is a boss arena: the Cinder Warden at 6, the Tidewife at 12, Saint Marrow at 18
        /// and the First Watchman at 24 (2026-10-05).</summary>
        static readonly int[] BuiltBossDepths = { 6, 12, 18, 24 };

        public static bool IsBossDepth(int depth) => System.Array.IndexOf(BuiltBossDepths, depth) >= 0;

        /// <summary>Docs/05: the Portal Tome is found around level 3; the Wanderer gives it on this depth (the user's
        /// choice, 2026-09-26).</summary>
        public const int PortalTomeDepth = 3;

        /// <summary>
        /// The enemy level of each depth, 1 to 6 (index 0 unused). Tuning: they follow the level a character reaches by
        /// clearing each level, measured with the balance report. The first version spread Docs/05's act 1 range of player
        /// levels 1 to 12 evenly (1, 3, 5, 8, 10, 12), and the character fell three levels behind; the user chose to
        /// flatten it (2026-09-26). With lower enemy levels a clear also gives less XP, so act 1 now ends near
        /// character level 7, not 12.
        /// </summary>
        static readonly int[] ActOneEnemyLevels = { 0, 1, 2, 3, 4, 5, 7 };

        /// <summary>
        /// The enemy level of a depth: the first 6 as tuned for act 1, then 0.6 a level, rounded up (depth 7 is 8, 12 is
        /// 11, 18 is 15, 24 is 18). Tuning (2026-10-05), chosen with the balance report: at one a level the character
        /// fell eight levels behind by depth 24; at 0.6 it stays about two behind all the way down, as at the Warden.
        /// </summary>
        public static int EnemyLevel(int depth)
        {
            depth = Mathf.Clamp(depth, 1, Depths);
            if (depth <= BossEvery)
                return ActOneEnemyLevels[depth];
            return ActOneEnemyLevels[BossEvery] + Mathf.CeilToInt((depth - BossEvery) * EnemyLevelsPerDepth - 1e-4f);
        }

        /// <summary>Extra Champion chance below the first boss: a point a level, at most 20 points.</summary>
        public static float ExtraChampionChance(int depth) => Mathf.Clamp(depth - BossEvery, 0, 20) * 0.01f;

        /// <summary>How fast enemy levels climb below the first boss.</summary>
        public const float EnemyLevelsPerDepth = 0.6f;

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

        /// <summary>A room's inside is this many cells wide or tall at least and at most (the boss arena's row and column
        /// excepted). Rooms sit wall to wall on a grid of columns and rows, each with its own width, so neighbours
        /// differ in size.</summary>
        public int MinRoomSpan = 18;
        public int MaxRoomSpan = 32;

        /// <summary>The largest hand-authored pillar layout a room may use (a room shape wider than this is refused).</summary>
        public int MaxRoomSize => 36;

        /// <summary>Chance a join along the room tree opens the whole shared wall (bar a pier at each end) into one hall,
        /// rather than an arch.</summary>
        public float HallJoinChance = 0.3f;

        /// <summary>Chance two neighbouring rooms not joined by the tree get an arch anyway, so the level has loops.</summary>
        public float LoopChance = 0.45f;

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
    /// Builds a dungeon level from a seed (Docs/05-world-and-content.md, level generation), as connected halls in the
    /// manner of Diablo 1's cathedral (the owner, 2026-09-30: the rooms-and-corridors levels were "more like corridors
    /// with open rooms"). Rooms sit wall to wall on a grid whose columns and rows each have their own width, one room
    /// per slot, grown as a tree from the start room so every room is reachable; the exit room is the one furthest from
    /// the start along the tree. Tree neighbours are joined through their shared wall by a wide arch or opened into one
    /// hall, other neighbours often by an arch too, so there are loops and no corridors. Rooms are dressed with pillar
    /// rows, pillar grids, wall stubs or a hand-authored pillar layout, and get a floor style, props against the walls
    /// and floor decals. Every empty cell touching floor becomes wall. Same seed, same level. Pure.
    /// </summary>
    public static class DungeonGenerator
    {
        /// <summary>
        /// Which generator built the saved dungeon. Bump it whenever the same seed would build a different level (room
        /// library, sizes, pack rules): a save from another version forgets its dungeon kills and chests, which would
        /// otherwise land on the wrong packs. 1: the first, small rooms. 2: bigger, open rooms, 5 cell doorways.
        /// 3: the last level's exit room is the boss arena. 4: connected halls, props and floor styles (2026-09-30).
        /// 5: buckets, standing torches, ritual sigils and summoning circles (2026-10-04).
        /// </summary>
        public const int Version = 5;

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
                    kinds[i] = DungeonRules.IsBossDepth(depth) ? RoomKind.Boss : RoomKind.Exit;
                else
                {
                    kinds[i] = RollKind(random, settings);
                    middle.Add(i);
                }
            }
            Guarantee(RoomKind.Elite, kinds, middle, random);
            Guarantee(RoomKind.Treasure, kinds, middle, random);

            // 3. The grid: every column and row its own width; the boss arena's column and row fit its disc exactly.
            var minMacro = macros[0];
            var maxMacro = macros[0];
            foreach (var m in macros)
            {
                minMacro = Vector2Int.Min(minMacro, m);
                maxMacro = Vector2Int.Max(maxMacro, m);
            }
            var columns = maxMacro.x - minMacro.x + 1;
            var rows = maxMacro.y - minMacro.y + 1;
            var widths = new int[columns];
            var heights = new int[rows];
            for (var c = 0; c < columns; c++)
                widths[c] = random.Next(settings.MinRoomSpan, settings.MaxRoomSpan + 1);
            for (var r = 0; r < rows; r++)
                heights[r] = random.Next(settings.MinRoomSpan, settings.MaxRoomSpan + 1);
            for (var i = 0; i < macros.Count; i++)
                if (kinds[i] == RoomKind.Boss)
                {
                    widths[macros[i].x - minMacro.x] = BossArenaShape.Size;
                    heights[macros[i].y - minMacro.y] = BossArenaShape.Size;
                }

            // Wall lines: column c's inside runs from lineX[c] + 1 to lineX[c + 1] - 1; neighbours share a line.
            var lineX = new int[columns + 1];
            var lineY = new int[rows + 1];
            for (var c = 0; c < columns; c++)
                lineX[c + 1] = lineX[c] + widths[c] + 1;
            for (var r = 0; r < rows; r++)
                lineY[r + 1] = lineY[r] + heights[r] + 1;

            var layout = new DungeonLayout(new RectInt(-1, -1, lineX[columns] + 3, lineY[rows] + 3))
            {
                Depth = depth,
                EnemyLevel = DungeonRules.EnemyLevel(depth),
            };

            var interiors = new RectInt[macros.Count];
            var slotOf = new Dictionary<Vector2Int, int>();
            for (var i = 0; i < macros.Count; i++)
            {
                var c = macros[i].x - minMacro.x;
                var r = macros[i].y - minMacro.y;
                interiors[i] = new RectInt(lineX[c] + 1, lineY[r] + 1, widths[c], heights[r]);
                slotOf[macros[i]] = i;
            }

            // 4. Carve the rooms, dress them, then open the joins (openings win over dressing).
            var styles = new int[macros.Count];
            var shapesUsed = new RoomShape[macros.Count];
            for (var i = 0; i < macros.Count; i++)
            {
                var interior = interiors[i];
                styles[i] = random.Next(FloorStyles);
                if (kinds[i] == RoomKind.Boss)
                {
                    shapesUsed[i] = BossArenaShape;
                    for (var x = 0; x < interior.width; x++)
                        for (var y = 0; y < interior.height; y++)
                            layout.Set(interior.xMin + x, interior.yMin + y, BossArenaShape.IsBlocked(x, y) ? DungeonCell.Wall : DungeonCell.Floor);
                }
                else
                {
                    for (var x = interior.xMin; x < interior.xMax; x++)
                        for (var y = interior.yMin; y < interior.yMax; y++)
                            layout.Set(x, y, DungeonCell.Floor);
                    // The start and exit rooms stay open, so the stairs and the arrival have room.
                    if (kinds[i] != RoomKind.Start && kinds[i] != RoomKind.Exit)
                        shapesUsed[i] = Dress(layout, interior, shapes, random);
                }
                layout.Rooms.Add(new RoomPlacement(kinds[i], shapesUsed[i], macros[i], interior, styles[i]));
            }

            var doors = new List<Vector2Int>[macros.Count];
            for (var i = 0; i < macros.Count; i++)
                doors[i] = new List<Vector2Int>();
            var joined = new HashSet<(int, int)>();
            for (var i = 1; i < macros.Count; i++)
            {
                var hall = kinds[i] != RoomKind.Boss && kinds[parents[i]] != RoomKind.Boss && random.NextDouble() < settings.HallJoinChance;
                Join(layout, parents[i], i, interiors, macros, kinds, hall, random, doors);
                joined.Add((Mathf.Min(parents[i], i), Mathf.Max(parents[i], i)));
            }
            // Loops: neighbours the tree did not join. Never into the arena, which keeps its one way in.
            for (var i = 0; i < macros.Count; i++)
                foreach (var side in new[] { Vector2Int.right, Vector2Int.up })
                {
                    if (!slotOf.TryGetValue(macros[i] + side, out var j) || joined.Contains((Mathf.Min(i, j), Mathf.Max(i, j))))
                        continue;
                    if (kinds[i] == RoomKind.Boss || kinds[j] == RoomKind.Boss || random.NextDouble() >= settings.LoopChance)
                        continue;
                    Join(layout, i, j, interiors, macros, kinds, false, random, doors);
                }

            KeepReachable(layout, interiors[0]);
            WrapWalls(layout);

            // 5. Stairs and arrival points.
            // The stairs up go in the start room's corner furthest from its doorways, and the player arrives on the room's
            // side of them, so heading off into the level never walks back over the stairs (the first build put them in
            // the middle with the player just below, and walking up the screen went straight back to town).
            var startRoom = layout.Rooms[0];
            layout.StairsUp = NearestFloor(layout, startRoom.Interior, CornerAwayFrom(startRoom.Interior, doors[0]), null, 0);
            layout.ArrivalFromAbove = NearestFloor(layout, startRoom.Interior, Vector2Int.RoundToInt(Vector2.Lerp(layout.StairsUp, Center(startRoom.Interior), 0.35f)), layout.StairsUp, 2);
            // Docs/05: each level has a waypoint. In the start room (the user's choice), further in than the arrival
            // point, so it is passed on the way in but not stood on when arriving by the stairs. No random numbers are
            // drawn, so older layouts are unchanged.
            layout.Waypoint = NearestFloor(layout, startRoom.Interior, Vector2Int.RoundToInt(Vector2.Lerp(layout.StairsUp, Center(startRoom.Interior), 0.75f)), layout.ArrivalFromAbove, 3);

            var exitRoom = layout.Rooms[exit];
            layout.HasStairsDown = depth < DungeonRules.Depths && exitRoom.Kind != RoomKind.Boss;
            if (exitRoom.Kind == RoomKind.Boss)
            {
                layout.HasBossArena = true;
                layout.BossArenaCenter = new Vector2Int(exitRoom.Interior.xMin + BossArenaRadiusCells, exitRoom.Interior.yMin + BossArenaRadiusCells);
                layout.BossArenaRadius = BossArenaRadiusCells;
                // The stairs down appear in the arena once the boss is dead (DungeonLevel.SetUpBoss), near its far edge;
                // coming back up from below lands beside them. No random numbers are drawn, so layouts are unchanged.
                layout.StairsDown = layout.BossArenaCenter + new Vector2Int(0, BossArenaRadiusCells - 4);
                layout.ArrivalFromBelow = layout.BossArenaCenter + new Vector2Int(0, BossArenaRadiusCells - 6);
            }
            if (layout.HasStairsDown)
            {
                // In the corner furthest from the doorway the player walks in by, so arriving from below lands clear of
                // the exit room's own pack, which goes toward the doorway, and the way out does not cross the stairs.
                layout.StairsDown = NearestFloor(layout, exitRoom.Interior, CornerAwayFrom(exitRoom.Interior, doors[exit]), null, 0);
                layout.ArrivalFromBelow = NearestFloor(layout, exitRoom.Interior, Vector2Int.RoundToInt(Vector2.Lerp(layout.StairsDown, Center(exitRoom.Interior), 0.35f)), layout.StairsDown, 2);
            }

            // 6. Chests, then packs.
            for (var i = 0; i < layout.Rooms.Count; i++)
                if (layout.Rooms[i].Kind == RoomKind.Treasure)
                    layout.Chests.Add(NearestFloor(layout, layout.Rooms[i].Interior, Center(layout.Rooms[i].Interior), null, 0));

            // A spot beside the first treasure chest for the Wanderer, who gives the Portal Tome on its depth.
            for (var i = 0; i < layout.Rooms.Count && !layout.HasWandererSpot; i++)
                if (layout.Rooms[i].Kind == RoomKind.Treasure)
                {
                    layout.WandererSpot = NearestFloor(layout, layout.Rooms[i].Interior, layout.Chests[0] + new Vector2Int(3, 0), layout.Chests[0], 2);
                    layout.HasWandererSpot = true;
                }

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

            // 7. Props against the walls and decals on the floor, last, clear of everything placed above.
            DungeonDressing.Place(layout, doors, random);
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

        /// <summary>How many floor styles a room can have (<see cref="RoomPlacement.Style"/>).</summary>
        public const int FloorStyles = 4;

        /// <summary>
        /// Opens the wall two neighbouring rooms share: an arch 5 to 11 cells wide somewhere along it, or, for a hall,
        /// all of it but a 2 cell pier at each end. The arena's way in is 5 wide at the middle of its side, where its disc
        /// meets the wall. Two cells in front of the opening are cleared on both sides, so dressing never blocks it.
        /// </summary>
        static void Join(DungeonLayout layout, int a, int b, RectInt[] interiors, List<Vector2Int> macros, RoomKind[] kinds,
            bool hall, System.Random random, List<Vector2Int>[] doors)
        {
            if (macros[b].x < macros[a].x || macros[b].y < macros[a].y)
                (a, b) = (b, a);
            var horizontal = macros[b].x != macros[a].x;
            var ra = interiors[a];
            // The shared wall line and the span along it (both rooms of a column or row have the same span).
            var line = horizontal ? ra.xMax : ra.yMax;
            var spanMin = horizontal ? ra.yMin : ra.xMin;
            var length = horizontal ? ra.height : ra.width;

            int from, width;
            if (kinds[a] == RoomKind.Boss || kinds[b] == RoomKind.Boss)
            {
                width = RoomShape.DoorWidth;
                from = spanMin + length / 2 - width / 2;
            }
            else if (hall)
            {
                width = length - 4;
                from = spanMin + 2;
            }
            else
            {
                width = Mathf.Min(random.Next(5, 12), length - 6);
                from = spanMin + random.Next(3, length - 3 - width + 1);
            }

            for (var t = from; t < from + width; t++)
                for (var d = -2; d <= 2; d++)
                {
                    var cell = horizontal ? new Vector2Int(line + d, t) : new Vector2Int(t, line + d);
                    // Into the arena nothing is cleared: its rim stays a disc (its shape keeps the middle of each side open).
                    if ((kinds[a] == RoomKind.Boss && d < 0) || (kinds[b] == RoomKind.Boss && d > 0))
                        continue;
                    layout.Set(cell.x, cell.y, DungeonCell.Floor);
                }

            var middle = from + width / 2;
            var door = horizontal ? new Vector2Int(line, middle) : new Vector2Int(middle, line);
            doors[a].Add(door);
            doors[b].Add(door);
        }

        /// <summary>
        /// Dresses a room with standing stone: a hand-authored pillar layout when one fits, else pillar rows along its
        /// length, a grid of pillars, or stubs of wall reaching in from its sides, or nothing. Returns the authored shape
        /// used, or null.
        /// </summary>
        static RoomShape Dress(DungeonLayout layout, RectInt r, IReadOnlyList<RoomShape> shapes, System.Random random)
        {
            var roll = random.Next(100);
            if (roll < 30)
            {
                var fitting = new List<RoomShape>();
                foreach (var shape in shapes)
                    if (shape.Size <= Mathf.Min(r.width, r.height))
                        fitting.Add(shape);
                if (fitting.Count > 0)
                {
                    var shape = fitting[random.Next(fitting.Count)];
                    var ox = r.xMin + (r.width - shape.Size) / 2;
                    var oy = r.yMin + (r.height - shape.Size) / 2;
                    for (var x = 0; x < shape.Size; x++)
                        for (var y = 0; y < shape.Size; y++)
                            if (shape.IsBlocked(x, y))
                                layout.Set(ox + x, oy + y, DungeonCell.Wall);
                    return shape;
                }
                roll = 30 + random.Next(70);
            }

            if (roll < 55)
            {
                // Two rows of pillars along the room's length, as in a nave.
                var alongX = r.width >= r.height;
                var length = alongX ? r.width : r.height;
                var across = alongX ? r.height : r.width;
                var thick = across >= 22 ? 2 : 1;
                foreach (var offset in new[] { 4, across - 4 - thick })
                    for (var t = 4; t + thick <= length - 4; t += 5)
                        Block(layout, alongX ? r.xMin + t : r.xMin + offset, alongX ? r.yMin + offset : r.yMin + t, thick);
            }
            else if (roll < 72)
            {
                // A grid of square pillars.
                for (var x = 5; x + 2 <= r.width - 5; x += 7)
                    for (var y = 5; y + 2 <= r.height - 5; y += 7)
                        Block(layout, r.xMin + x, r.yMin + y, 2);
            }
            else if (roll < 90)
            {
                // Stubs of wall reaching in from the sides, breaking the room into bays.
                for (var side = 0; side < 4; side++)
                {
                    var alongX = side < 2;
                    var length = alongX ? r.width : r.height;
                    var stubs = random.Next(0, 3);
                    for (var k = 0; k < stubs; k++)
                    {
                        var at = random.Next(4, length - 4);
                        var reach = random.Next(2, 5);
                        for (var d = 0; d < reach; d++)
                        {
                            var x = alongX ? r.xMin + at : side == 2 ? r.xMin + d : r.xMax - 1 - d;
                            var y = !alongX ? r.yMin + at : side == 0 ? r.yMin + d : r.yMax - 1 - d;
                            layout.Set(x, y, DungeonCell.Wall);
                        }
                    }
                }
            }
            return null;
        }

        static void Block(DungeonLayout layout, int x, int y, int size)
        {
            for (var dx = 0; dx < size; dx++)
                for (var dy = 0; dy < size; dy++)
                    layout.Set(x + dx, y + dy, DungeonCell.Wall);
        }

        /// <summary>Turns floor that dressing cut off from the start room into wall, so every floor cell can be walked to.</summary>
        static void KeepReachable(DungeonLayout layout, RectInt startRoom)
        {
            var b = layout.Bounds;
            var seen = new bool[b.width, b.height];
            var queue = new Queue<Vector2Int>();
            var start = new Vector2Int(startRoom.xMin + startRoom.width / 2, startRoom.yMin + startRoom.height / 2);
            seen[start.x - b.xMin, start.y - b.yMin] = true;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in RoomShape.Sides)
                {
                    var next = cell + step;
                    if (!layout.IsFloor(next) || seen[next.x - b.xMin, next.y - b.yMin])
                        continue;
                    seen[next.x - b.xMin, next.y - b.yMin] = true;
                    queue.Enqueue(next);
                }
            }
            for (var x = b.xMin; x < b.xMax; x++)
                for (var y = b.yMin; y < b.yMax; y++)
                    if (layout.Get(x, y) == DungeonCell.Floor && !seen[x - b.xMin, y - b.yMin])
                        layout.Set(x, y, DungeonCell.Wall);
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
            // The side of a square of the same floor area, to size rooms of any proportion alike.
            var size = Mathf.RoundToInt(Mathf.Sqrt(room.Interior.width * room.Interior.height));
            switch (room.Kind)
            {
                case RoomKind.Combat:
                {
                    // Bigger rooms hold more packs, so an open hall is a fight, not a walk.
                    var packs = size <= SmallMax ? random.Next(1, 3) : size <= MediumMax ? random.Next(2, 4) : random.Next(3, 5);
                    for (var i = 0; i < packs; i++)
                        TryPlacePack(layout, roomIndex, NormalKind(random, settings, layout.Depth), random.Next(settings.MinPackSize, settings.MaxPackSize + 1), random, settings);
                    break;
                }
                case RoomKind.Elite:
                    TryPlacePack(layout, roomIndex, PackKind.Elite, random.Next(settings.MinElitePackSize, settings.MaxElitePackSize + 1), random, settings);
                    // A large elite room also holds a normal pack, so the elites are not alone in a big empty hall.
                    if (size > MediumMax)
                        TryPlacePack(layout, roomIndex, NormalKind(random, settings, layout.Depth), random.Next(settings.MinPackSize, settings.MaxPackSize + 1), random, settings);
                    break;
                case RoomKind.Treasure:
                    TryPlacePack(layout, roomIndex, PackKind.Normal, random.Next(settings.MinGuardPackSize, settings.MaxGuardPackSize + 1), random, settings);
                    break;
                case RoomKind.Exit:
                    TryPlacePack(layout, roomIndex, NormalKind(random, settings, layout.Depth), random.Next(settings.MinPackSize, settings.MaxPackSize + 1), random, settings);
                    break;
            }
        }

        // Below the first boss more packs have a Champion, a point a level up to 45 percent (2026-10-05: the dungeon changes
        // gradually). The same random draw either way, so the first six levels are as they were.
        static PackKind NormalKind(System.Random random, DungeonSettings settings, int depth) =>
            random.NextDouble() < settings.ChampionChance + DungeonRules.ExtraChampionChance(depth) ? PackKind.WithChampion : PackKind.Normal;

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
