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

        /// <summary>The first cave level (the owner, 2026-10-10: a crypt and cathedral to the Tidewife at 12, then caves
        /// going down).</summary>
        public const int CaveFrom = 13;

        public static DungeonTheme ThemeAt(int depth) => depth >= CaveFrom ? DungeonTheme.Cave : DungeonTheme.Crypt;

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
        /// <summary>Rooms between the start room and the exit room. Docs/05 said 5 to 8; the owner doubled the levels
        /// (2026-10-08: "make the levels bigger, double their size").</summary>
        public int MinMiddleRooms = 10;
        public int MaxMiddleRooms = 15;

        /// <summary>Solid rock between neighbouring rooms, in cells: each column and row boundary its own thickness, so
        /// rooms are joined by short rough passages through the rock rather than sharing one thin wall (2026-10-08: "no
        /// real feel of traveling underground").</summary>
        public int MinRock = 3;
        public int MaxRock = 7;

        /// <summary>Solid rock around the whole level, in cells, so its edge is rock, not black.</summary>
        public int OuterRock = 8;

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
        /// 6: twice the rooms, rock between rooms with rough passages, broken walls and collapses (2026-10-08).
        /// 7: crypt rooms of many shapes to depth 12, caves with terraces below (2026-10-10).
        /// </summary>
        public const int Version = 7;

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

            // Rooms with rock between them: column c's inside starts at startX[c]; rockX[c] is the rock after it.
            var startX = new int[columns];
            var startY = new int[rows];
            var rockX = new int[columns];
            var rockY = new int[rows];
            var x0 = settings.OuterRock;
            for (var c = 0; c < columns; c++)
            {
                startX[c] = x0;
                rockX[c] = random.Next(settings.MinRock, settings.MaxRock + 1);
                x0 += widths[c] + rockX[c];
            }
            var y0 = settings.OuterRock;
            for (var r = 0; r < rows; r++)
            {
                startY[r] = y0;
                rockY[r] = random.Next(settings.MinRock, settings.MaxRock + 1);
                y0 += heights[r] + rockY[r];
            }

            var layout = new DungeonLayout(new RectInt(0, 0, x0 - rockX[columns - 1] + settings.OuterRock, y0 - rockY[rows - 1] + settings.OuterRock))
            {
                Depth = depth,
                EnemyLevel = DungeonRules.EnemyLevel(depth),
                Theme = DungeonRules.ThemeAt(depth),
            };
            var rugged = Ruggedness(depth);
            var cave = layout.Theme == DungeonTheme.Cave;

            // Each room fills only part of its slot, at its own offset, and often joins a second rectangle (an L, a T, a
            // cross), so rooms neither line up in rows nor all read as boxes (2026-10-08: "it also feels way too straight").
            var slots = new RectInt[macros.Count];
            var mains = new RectInt[macros.Count];
            var interiors = new RectInt[macros.Count];
            var slotOf = new Dictionary<Vector2Int, int>();
            for (var i = 0; i < macros.Count; i++)
            {
                var c = macros[i].x - minMacro.x;
                var r = macros[i].y - minMacro.y;
                var slot = new RectInt(startX[c], startY[r], widths[c], heights[r]);
                slots[i] = slot;
                slotOf[macros[i]] = i;
                if (kinds[i] == RoomKind.Boss)
                {
                    mains[i] = interiors[i] = slot;
                    continue;
                }
                var mw = Mathf.Max(MinMainSpan, Mathf.RoundToInt(slot.width * Mathf.Lerp(cave ? 0.8f : 0.7f, 1f, (float)random.NextDouble())));
                var mh = Mathf.Max(MinMainSpan, Mathf.RoundToInt(slot.height * Mathf.Lerp(cave ? 0.8f : 0.7f, 1f, (float)random.NextDouble())));
                mw = Mathf.Min(mw, slot.width);
                mh = Mathf.Min(mh, slot.height);
                var main = new RectInt(slot.xMin + random.Next(0, slot.width - mw + 1), slot.yMin + random.Next(0, slot.height - mh + 1), mw, mh);
                mains[i] = main;
                interiors[i] = main;
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
                else if (cave)
                {
                    // A cave: a lump of open ground worn into the rock, its edge rough all round.
                    CarveCave(layout, mains[i], random);
                    // The start and exit rooms stay open, so the stairs and the arrival have room.
                    if (kinds[i] != RoomKind.Start && kinds[i] != RoomKind.Exit)
                    {
                        Stalagmites(layout, mains[i], rugged, random);
                        Collapse(layout, mains[i], rugged, random);
                    }
                }
                else
                {
                    // A crypt: a built hall, octagonal, round, a cross, with an apse, or with its corners cut; clean edges.
                    CarveCrypt(layout, mains[i], random);
                    if (kinds[i] != RoomKind.Start && kinds[i] != RoomKind.Exit)
                        shapesUsed[i] = Dress(layout, mains[i], shapes, random);
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
                Join(layout, parents[i], i, slots, mains, macros, kinds, hall, cave, random, doors);
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
                    Join(layout, i, j, slots, mains, macros, kinds, false, cave, random, doors);
                }

            // Caves: raised terraces in the bigger rooms, ringed by a ledge with steps cut through it.
            if (cave)
                for (var i = 0; i < macros.Count; i++)
                    if (kinds[i] == RoomKind.Combat || kinds[i] == RoomKind.Elite || kinds[i] == RoomKind.Treasure)
                        Terrace(layout, mains[i], doors[i], random);

            KeepReachable(layout, mains[0]);
            FillRock(layout);
            MarkFreeStanding(layout);

            // 5. Stairs and arrival points.
            // The stairs up go in the start room's corner furthest from its doorways, and the player arrives on the room's
            // side of them, so heading off into the level never walks back over the stairs (the first build put them in
            // the middle with the player just below, and walking up the screen went straight back to town).
            var startRoom = layout.Rooms[0];
            layout.StairsUp = NearestFloor(layout, startRoom.Interior, CornerAwayFrom(startRoom.Interior, doors[0]), null, 0);
            layout.ArrivalFromAbove = ArrivalBeside(layout, layout.StairsUp, Center(startRoom.Interior));
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
                layout.ArrivalFromBelow = ArrivalBeside(layout, layout.StairsDown, Center(exitRoom.Interior));
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
            DungeonDressing.Place(layout, doors, random, seed);
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

        /// <summary>The smallest a room's main rectangle is across, so it holds a pack and its dressing.</summary>
        const int MinMainSpan = 14;

        static void FillRect(DungeonLayout layout, RectInt r, DungeonCell cell)
        {
            for (var x = r.xMin; x < r.xMax; x++)
                for (var y = r.yMin; y < r.yMax; y++)
                    layout.Set(x, y, cell);
        }

        /// <summary>
        /// Opens a way through the rock between two neighbouring rooms: a rough passage 3 to 6 cells wide, or for a hall
        /// a wide breach 7 to 13 across, from inside one room's main rectangle to inside the other's. Where the rooms do
        /// not face each other it turns in the rock between them, so it runs straight, or doglegs. Its sides fray cell
        /// by cell. The arena is entered straight, 5 wide, at the middle of its side.
        /// </summary>
        static void Join(DungeonLayout layout, int a, int b, RectInt[] slots, RectInt[] mains, List<Vector2Int> macros, RoomKind[] kinds,
            bool hall, bool cave, System.Random random, List<Vector2Int>[] doors)
        {
            if (macros[b].x < macros[a].x || macros[b].y < macros[a].y)
                (a, b) = (b, a);
            var horizontal = macros[b].x != macros[a].x;
            var boss = kinds[a] == RoomKind.Boss || kinds[b] == RoomKind.Boss;
            // A crypt's corridors are built, straight and clean; a cave's tunnels wander and fray.
            var width = boss ? RoomShape.DoorWidth : hall ? random.Next(7, 14) : cave ? random.Next(3, 7) : random.Next(3, 6);
            var fray = !boss && cave;

            int Across(RectInt r, bool high) => horizontal ? (high ? r.xMax : r.xMin) : (high ? r.yMax : r.yMin);
            int AlongMin(RectInt r) => horizontal ? r.yMin : r.xMin;
            int AlongLength(RectInt r) => horizontal ? r.height : r.width;

            // Where the passage starts in each room: its along position, and how far into the room it reaches.
            int Along(int room)
            {
                var r = mains[room];
                if (kinds[room] == RoomKind.Boss)
                    return AlongMin(r) + AlongLength(r) / 2 - width / 2;
                var w = Mathf.Min(width, AlongLength(r) - 6);
                return AlongMin(r) + random.Next(3, AlongLength(r) - 3 - w + 1);
            }
            var alongA = Along(a);
            var alongB = Along(b);
            width = Mathf.Min(width, AlongLength(mains[a]) - 6, AlongLength(mains[b]) - 6);
            // Into each room as far as its middle, which every room shape keeps open, so the join always meets floor.
            var startA = Across(mains[a], true) - (horizontal ? mains[a].width : mains[a].height) / 2;
            var endB = Across(mains[b], false) + (horizontal ? mains[b].width : mains[b].height) / 2;
            if (kinds[a] == RoomKind.Boss)
                startA = Across(mains[a], true) - 3;
            if (kinds[b] == RoomKind.Boss)
                endB = Across(mains[b], false) + 3;
            // The turn, in the rock between the two slots.
            var turn = (Across(slots[a], true) + Across(slots[b], false)) / 2;

            Carve(layout, horizontal, startA, turn, alongA, width, fray, random);
            if (alongA != alongB)
            {
                var lo = Mathf.Min(alongA, alongB);
                var hi = Mathf.Max(alongA, alongB) + width;
                Carve(layout, !horizontal, lo, hi, turn - width / 2, width, fray, random);
            }
            Carve(layout, horizontal, turn, endB, alongB, width, fray, random);

            Vector2Int At(int across, int along) => horizontal ? new Vector2Int(across, along) : new Vector2Int(along, across);
            doors[a].Add(At(turn, (alongA + alongB) / 2 + width / 2));
            doors[b].Add(At(turn, (alongA + alongB) / 2 + width / 2));
            doors[a].Add(At(Across(mains[a], true) - 1, alongA + width / 2));
            doors[b].Add(At(Across(mains[b], false), alongB + width / 2));
        }

        // A straight run of passage along the across axis (x when acrossX) from one across value to another, starting at
        // along and width wide; with fray, its sides wander by a cell and it drifts now and then.
        static void Carve(DungeonLayout layout, bool acrossX, int from, int to, int along, int width, bool fray, System.Random random)
        {
            if (from > to)
                (from, to) = (to, from);
            var drift = 0;
            for (var t = from; t <= to; t++)
            {
                if (fray && random.NextDouble() < 0.15)
                    drift = Mathf.Clamp(drift + (random.Next(2) == 0 ? -1 : 1), -1, 1);
                var lo = along + drift - (fray ? random.Next(0, 2) : 0);
                var hi = along + width + drift + (fray ? random.Next(0, 2) : 0);
                for (var s = lo; s < hi; s++)
                {
                    var x = acrossX ? t : s;
                    var y = acrossX ? s : t;
                    if (layout.Bounds.Contains(new Vector2Int(x, y)))
                        layout.Set(x, y, DungeonCell.Floor);
                }
            }
        }

        /// <summary>How rough the walls are at a depth, 0.5 at the top to 1 at the bottom (2026-10-08: "more rugged and
        /// broken down ... the further down you descend").</summary>
        public static float Ruggedness(int depth) => Mathf.Lerp(0.5f, 1f, (Mathf.Clamp(depth, 1, DungeonRules.Depths) - 1) / (float)(DungeonRules.Depths - 1));

        /// <summary>How deep a room's broken edge reaches into it at most, in cells.</summary>
        public const int RoughDepth = 5;

        /// <summary>
        /// A crypt room in a rectangle (the owner, 2026-10-10: walls "not only right shapes"): an octagon with its corners
        /// cut, a round or oval hall, a cross, a hall with a round apse at one end, or a plain hall with its corners cut
        /// a little. Its middle is always floor. Clean edges: it was built.
        /// </summary>
        static void CarveCrypt(DungeonLayout layout, RectInt r, System.Random random)
        {
            var w = r.width;
            var h = r.height;
            var roll = random.Next(100);
            var chamfer = roll < 30 ? random.Next(3, Mathf.Max(4, Mathf.Min(w, h) / 3) + 1) : 2;
            var band = new Vector2(w * Mathf.Lerp(0.4f, 0.6f, (float)random.NextDouble()), h * Mathf.Lerp(0.4f, 0.6f, (float)random.NextDouble()));
            var apseSide = random.Next(4);
            var apse = Mathf.Max(4, Mathf.Min(w, h) / 3);
            for (var x = 0; x < w; x++)
                for (var y = 0; y < h; y++)
                {
                    var cx = x + 0.5f - w / 2f;
                    var cy = y + 0.5f - h / 2f;
                    bool open;
                    if (roll < 30 || roll >= 80)
                        open = Corners(x, y, w, h) >= chamfer;
                    else if (roll < 50)
                        open = cx * cx / (w * w / 4f) + cy * cy / (h * h / 4f) <= 1f;
                    else if (roll < 65)
                        open = Mathf.Abs(cx) <= band.x / 2f || Mathf.Abs(cy) <= band.y / 2f;
                    else
                    {
                        // The apse: the hall stops short of one side, and a half oval fills the rest.
                        var along = apseSide < 2 ? cx : cy;
                        var across = apseSide < 2 ? cy : cx;
                        var span = apseSide < 2 ? w : h;
                        var other = apseSide < 2 ? h : w;
                        if (apseSide % 2 == 1)
                            along = -along;
                        var edge = span / 2f - apse;
                        open = along <= edge ? Corners(x, y, w, h) >= 2
                            : (along - edge) * (along - edge) / (apse * apse) + across * across / (other * other * 0.16f) <= 1f;
                    }
                    if (open)
                        layout.Set(r.xMin + x, r.yMin + y, DungeonCell.Floor);
                }
        }

        // How far a cell of a w x h rectangle is from its nearest corner, counted along both edges (x + y from that
        // corner): cells under a value are cut off the corner.
        static int Corners(int x, int y, int w, int h) =>
            Mathf.Min(Mathf.Min(x + y, w - 1 - x + y), Mathf.Min(x + h - 1 - y, w - 1 - x + h - 1 - y));

        /// <summary>
        /// A cave in a rectangle: an oval worn into the rock with a lumpy edge from two scales of noise, smoothed like
        /// a cellular automaton so it reads as rock, not noise. Its middle is always floor.
        /// </summary>
        static void CarveCave(DungeonLayout layout, RectInt r, System.Random random)
        {
            var w = r.width;
            var h = r.height;
            var noise = random.Next();
            var open = new bool[w, h];
            var core = new bool[w, h];
            for (var x = 0; x < w; x++)
                for (var y = 0; y < h; y++)
                {
                    var nx = (x + 0.5f - w / 2f) / (w / 2f);
                    var ny = (y + 0.5f - h / 2f) / (h / 2f);
                    var d = Mathf.Sqrt(nx * nx + ny * ny);
                    var n = ValueNoise(noise, x / 5f, y / 5f) * 0.7f + ValueNoise(noise + 3, x / 2.5f, y / 2.5f) * 0.3f;
                    core[x, y] = d < 0.45f;
                    open[x, y] = core[x, y] || d < 0.6f + 0.55f * n;
                }
            for (var pass = 0; pass < 2; pass++)
            {
                var next = new bool[w, h];
                for (var x = 0; x < w; x++)
                    for (var y = 0; y < h; y++)
                    {
                        var count = 0;
                        for (var dx = -1; dx <= 1; dx++)
                            for (var dy = -1; dy <= 1; dy++)
                                if ((dx != 0 || dy != 0) && x + dx >= 0 && y + dy >= 0 && x + dx < w && y + dy < h && open[x + dx, y + dy])
                                    count++;
                        next[x, y] = core[x, y] || count >= 5 || open[x, y] && count >= 4;
                    }
                open = next;
            }
            for (var x = 0; x < w; x++)
                for (var y = 0; y < h; y++)
                    if (open[x, y])
                        layout.Set(r.xMin + x, r.yMin + y, DungeonCell.Floor);
        }

        /// <summary>Stalagmites in a cave: a few clumps of one to four cells standing on the floor, away from its edge,
        /// more the deeper and the bigger the cave.</summary>
        static void Stalagmites(DungeonLayout layout, RectInt r, float rugged, System.Random random)
        {
            const int margin = 4;
            if (r.width <= 2 * margin + 2 || r.height <= 2 * margin + 2)
                return;
            var count = random.Next(2, 5) + Mathf.RoundToInt(rugged * r.width * r.height / 300f);
            for (var k = 0; k < count; k++)
            {
                var cell = new Vector2Int(random.Next(r.xMin + margin, r.xMax - margin), random.Next(r.yMin + margin, r.yMax - margin));
                var size = random.Next(1, 5);
                for (var n = 0; n < size; n++)
                {
                    if (layout.IsFloor(cell))
                        layout.Set(cell.x, cell.y, DungeonCell.Wall);
                    cell += RoomShape.Sides[random.Next(RoomShape.Sides.Length)];
                }
            }
        }

        /// <summary>
        /// A raised terrace in a cave (the owner's concept art, 2026-10-10): a lump of the floor ringed by a rock ledge
        /// that blocks the way, with steps cut through it, one or two flights three cells wide. Only where low floor runs
        /// all round it, so it never cuts the cave in two, and well clear of the cave's ways in. Nothing changes when no
        /// such terrace fits.
        /// </summary>
        static void Terrace(DungeonLayout layout, RectInt r, List<Vector2Int> doors, System.Random random)
        {
            if (r.width < 16 || r.height < 16 || random.NextDouble() > 0.75)
                return;
            var noise = random.Next();
            for (var attempt = 0; attempt < 30; attempt++)
            {
                // Smaller as the tries go on, so a cramped cave still gets a low step up.
                var radius = Mathf.Max(4, Mathf.RoundToInt(Mathf.Min(r.width, r.height) / 2.6f * (1f - attempt / 40f)));
                if (r.width <= 2 * radius + 4 || r.height <= 2 * radius + 4)
                    continue;
                var center = new Vector2Int(random.Next(r.xMin + radius + 2, r.xMax - radius - 2), random.Next(r.yMin + radius + 2, r.yMax - radius - 2));
                var clear = true;
                foreach (var door in doors)
                    if (Vector2Int.Distance(door, center) < radius + 4)
                        clear = false;
                if (!clear || !layout.IsFloor(center))
                    continue;

                // The terrace's area, and its floor; rock or stalagmites inside it stand on it.
                var area = new HashSet<Vector2Int>();
                var plateau = new HashSet<Vector2Int>();
                for (var x = center.x - radius - 2; x <= center.x + radius + 2; x++)
                    for (var y = center.y - radius - 2; y <= center.y + radius + 2; y++)
                    {
                        var cell = new Vector2Int(x, y);
                        var reach = radius * (0.8f + 0.35f * ValueNoise(noise, x / 6f, y / 6f));
                        if (Vector2Int.Distance(cell, center) > reach)
                            continue;
                        area.Add(cell);
                        if (layout.IsFloor(cell))
                            plateau.Add(cell);
                    }
                // The rim: terrace floor beside lower floor.
                var ring = new HashSet<Vector2Int>();
                foreach (var cell in plateau)
                    for (var dx = -1; dx <= 1; dx++)
                        for (var dy = -1; dy <= 1; dy++)
                        {
                            var next = new Vector2Int(cell.x + dx, cell.y + dy);
                            if (!area.Contains(next) && layout.IsFloor(next))
                                ring.Add(cell);
                        }
                var inner = plateau.Count - ring.Count;
                if (inner < 12)
                    continue;

                // Steps: a straight piece of the rim three cells wide, lower floor straight out from it and the terrace
                // straight in.
                var flights = new List<(Vector2Int cell, Vector2Int down)>();
                foreach (var cell in ring)
                    foreach (var down in RoomShape.Sides)
                    {
                        var side = new Vector2Int(down.y, down.x);
                        var fits = true;
                        for (var k = -1; k <= 1 && fits; k++)
                        {
                            var c = cell + side * k;
                            fits = ring.Contains(c) && layout.IsFloor(c + down) && !plateau.Contains(c + down) &&
                                   plateau.Contains(c - down) && !ring.Contains(c - down);
                        }
                        if (fits)
                            flights.Add((cell, down));
                    }
                if (flights.Count == 0)
                    continue;
                var chosen = new List<(Vector2Int cell, Vector2Int down)> { flights[random.Next(flights.Count)] };
                if (random.Next(2) == 0)
                    foreach (var flight in flights)
                        if (Vector2Int.Distance(flight.cell, chosen[0].cell) >= radius + 2)
                        {
                            chosen.Add(flight);
                            break;
                        }

                var steps = new Dictionary<Vector2Int, Vector2Int>();
                foreach (var (cell, down) in chosen)
                    for (var k = -1; k <= 1; k++)
                        steps[cell + new Vector2Int(down.y, down.x) * k] = down;
                // The ledge must not cut the cave: every floor cell around that was reached before is reached after.
                var walls = new HashSet<Vector2Int>(ring);
                walls.ExceptWith(steps.Keys);
                var region = new RectInt(r.xMin - 4, r.yMin - 4, r.width + 8, r.height + 8);
                var before = Reach(layout, region, chosen[0].cell + chosen[0].down, null);
                var after = Reach(layout, region, chosen[0].cell + chosen[0].down, walls);
                before.ExceptWith(walls);
                if (!after.IsSupersetOf(before))
                    continue;
                foreach (var cell in ring)
                {
                    if (steps.ContainsKey(cell))
                        continue;
                    layout.Set(cell.x, cell.y, DungeonCell.Wall);
                    layout.Ledges.Add(cell);
                }
                foreach (var pair in steps)
                    layout.TerraceStairs[pair.Key] = pair.Value;
                foreach (var cell in plateau)
                    if (!ring.Contains(cell))
                        layout.Raised.Add(cell);
                return;
            }
        }

        // The floor cells of a region reached from a start by edge steps, treating the given cells as wall.
        static HashSet<Vector2Int> Reach(DungeonLayout layout, RectInt region, Vector2Int start, HashSet<Vector2Int> blocked)
        {
            var seen = new HashSet<Vector2Int>();
            if (!layout.IsFloor(start))
                return seen;
            var queue = new Queue<Vector2Int>();
            seen.Add(start);
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in RoomShape.Sides)
                {
                    var next = cell + step;
                    if (!region.Contains(next) || !layout.IsFloor(next) || blocked != null && blocked.Contains(next) || !seen.Add(next))
                        continue;
                    queue.Enqueue(next);
                }
            }
            return seen;
        }

        /// <summary>Smooth noise in [0, 1] from a seed: random values on a lattice, blended between.</summary>
        public static float ValueNoise(int seed, float x, float y)
        {
            var x0 = Mathf.FloorToInt(x);
            var y0 = Mathf.FloorToInt(y);
            var fx = x - x0;
            var fy = y - y0;
            fx = fx * fx * (3 - 2 * fx);
            fy = fy * fy * (3 - 2 * fy);
            float L(int i, int j)
            {
                unchecked
                {
                    var h = (uint)seed * 374761393u + (uint)i * 668265263u + (uint)j * 2246822519u;
                    h = (h ^ (h >> 13)) * 1274126177u;
                    return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
                }
            }
            var top = Mathf.Lerp(L(x0, y0), L(x0 + 1, y0), fx);
            var bottom = Mathf.Lerp(L(x0, y0 + 1), L(x0 + 1, y0 + 1), fx);
            return Mathf.Lerp(top, bottom, fy);
        }

        /// <summary>
        /// Heaps of fallen ceiling standing in a room: rough blobs of rubble and broken masonry, one to a few, more the
        /// deeper and the bigger the room. Kept away from the room's edge, so walking around them is always possible.
        /// </summary>
        static void Collapse(DungeonLayout layout, RectInt r, float rugged, System.Random random)
        {
            var area = r.width * r.height;
            var count = random.Next(0, 2) + Mathf.RoundToInt(rugged * area / 500f * (float)random.NextDouble());
            for (var k = 0; k < count; k++)
            {
                var radius = 1f + (float)random.NextDouble() * (0.8f + 1f * rugged);
                var reach = Mathf.CeilToInt(radius);
                var margin = RoughDepth + reach + 2;
                if (r.width <= 2 * margin || r.height <= 2 * margin)
                    return;
                var cx = random.Next(r.xMin + margin, r.xMax - margin);
                var cy = random.Next(r.yMin + margin, r.yMax - margin);
                var stretch = 0.6f + (float)random.NextDouble() * 0.8f;
                for (var dx = -reach - 1; dx <= reach + 1; dx++)
                    for (var dy = -reach - 1; dy <= reach + 1; dy++)
                    {
                        var d = Mathf.Sqrt(dx * dx * stretch + dy * dy / stretch);
                        if (d <= radius + (float)random.NextDouble() * 0.9f - 0.45f)
                            layout.Set(cx + dx, cy + dy, DungeonCell.Wall);
                    }
            }
        }

        /// <summary>Every cell of the level that is not floor is solid rock (wall), so between and around the rooms there
        /// is rock, not black void.</summary>
        static void FillRock(DungeonLayout layout)
        {
            var b = layout.Bounds;
            for (var x = b.xMin; x < b.xMax; x++)
                for (var y = b.yMin; y < b.yMax; y++)
                    if (layout.Get(x, y) == DungeonCell.Void)
                        layout.Set(x, y, DungeonCell.Wall);
        }

        /// <summary>
        /// Marks the wall that stands free inside a room (pillars, heaps of fallen stone): wall not joined, corner to
        /// corner, to the rock at the level's edge. Free-standing wall keeps its full height on the camera side; the rest
        /// is cut low there (<see cref="WallRules"/>).
        /// </summary>
        static void MarkFreeStanding(DungeonLayout layout)
        {
            var b = layout.Bounds;
            var seen = new bool[b.width, b.height];
            var queue = new Queue<Vector2Int>();
            var start = new Vector2Int(b.xMin, b.yMin);
            seen[0, 0] = true;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                for (var dx = -1; dx <= 1; dx++)
                    for (var dy = -1; dy <= 1; dy++)
                    {
                        var next = new Vector2Int(cell.x + dx, cell.y + dy);
                        if (!b.Contains(next) || seen[next.x - b.xMin, next.y - b.yMin] || layout.Get(next) != DungeonCell.Wall)
                            continue;
                        seen[next.x - b.xMin, next.y - b.yMin] = true;
                        queue.Enqueue(next);
                    }
            }
            // Only clumps of a few cells (pillars, stalagmites, heaps, a terrace's ledge): rock walled in by a loop of
            // rooms and passages is still rock.
            for (var x = b.xMin; x < b.xMax; x++)
                for (var y = b.yMin; y < b.yMax; y++)
                {
                    if (layout.Get(x, y) != DungeonCell.Wall || seen[x - b.xMin, y - b.yMin])
                        continue;
                    var clump = new List<Vector2Int>();
                    seen[x - b.xMin, y - b.yMin] = true;
                    queue.Enqueue(new Vector2Int(x, y));
                    while (queue.Count > 0)
                    {
                        var cell = queue.Dequeue();
                        clump.Add(cell);
                        for (var dx = -1; dx <= 1; dx++)
                            for (var dy = -1; dy <= 1; dy++)
                            {
                                var next = new Vector2Int(cell.x + dx, cell.y + dy);
                                if (!b.Contains(next) || seen[next.x - b.xMin, next.y - b.yMin] || layout.Get(next) != DungeonCell.Wall)
                                    continue;
                                seen[next.x - b.xMin, next.y - b.yMin] = true;
                                queue.Enqueue(next);
                            }
                    }
                    if (clump.Count <= MaxFreeStanding || clump.TrueForAll(c => layout.Ledges.Contains(c)))
                        layout.FreeStanding.UnionWith(clump);
                }
        }

        /// <summary>The largest clump of wall that stands free; bigger ones are rock.</summary>
        const int MaxFreeStanding = 24;

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

        /// <summary>
        /// Where the player stands on arriving by a stairway: a floor cell two to four cells from it, reached by walking
        /// from it (so never behind a heap of rubble, 2026-10-08), as far toward the room's middle as can be.
        /// </summary>
        static Vector2Int ArrivalBeside(DungeonLayout layout, Vector2Int stairs, Vector2Int toward)
        {
            var seen = new HashSet<Vector2Int> { stairs };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(stairs);
            var best = stairs;
            var bestScore = float.MaxValue;
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                var away = Mathf.Max(Mathf.Abs(cell.x - stairs.x), Mathf.Abs(cell.y - stairs.y));
                if (away >= 2 && away <= 4)
                {
                    var score = (cell - toward).sqrMagnitude + (away == 3 ? 0 : 40);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = cell;
                    }
                }
                if (away >= 4)
                    continue;
                foreach (var step in RoomShape.Sides)
                {
                    var next = cell + step;
                    if (layout.IsFloor(next) && seen.Add(next))
                        queue.Enqueue(next);
                }
            }
            return best;
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
