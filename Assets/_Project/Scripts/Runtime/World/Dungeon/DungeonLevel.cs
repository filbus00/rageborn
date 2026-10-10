using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ARPG
{
    /// <summary>
    /// Builds the dungeon level the player is heading to when the dungeon scene loads: generates its layout from the
    /// session's dungeon seed and the depth (<see cref="DungeonGenerator"/>), paints the tilemaps, and places the
    /// player, the stairs, the chests and the packs. It runs before the <see cref="EnemyManager"/> bakes its
    /// navigation grid from those tilemaps, and before the packs spawn their members.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class DungeonLevel : MonoBehaviour
    {
        [SerializeField] Tilemap ground;
        [SerializeField] Tilemap walls;

        // The props (barrels, braziers, bones) on a tilemap of their own beside the walls, with no collider: she walks
        // through them (the owner, 2026-10-10: "make it so that they have no collision, it is annoying").
        Tilemap props;
        [Tooltip("No longer painted: the floors come from DungeonArt (2026-09-30). Kept so the scene's reference stays valid.")]
        [SerializeField] TileBase[] floorTiles;
        [SerializeField] TileBase wallTile;

        [Tooltip("The cut-down wall for walls between the camera and a room (WallRules). Empty draws every wall full height.")]
        [SerializeField] TileBase lowWallTile;

        [Tooltip("The room library the generator draws from.")]
        [SerializeField] RoomTemplate[] rooms;

        [SerializeField] EnemyDefinition normalEnemy;
        [SerializeField] EnemyDefinition championEnemy;
        [SerializeField] EnemyDefinition eliteEnemy;

        [Tooltip("Act 1's brute (Docs/05): mixed into packs from depth 2 (PackComposition).")]
        [SerializeField] EnemyDefinition ghoulEnemy;

        [Tooltip("Act 1's archer (Docs/05): mixed into packs from depth 2 (PackComposition).")]
        [SerializeField] EnemyDefinition archerEnemy;

        [Tooltip("From depth 4 (PackComposition): the skeleton in a husk's place, the cultist in an archer's.")]
        [SerializeField] EnemyDefinition skeletonEnemy;
        [SerializeField] EnemyDefinition cultistEnemy;
        [Tooltip("The rest of act 1's roster (PackComposition, 2026-10-05).")]
        [SerializeField] EnemyDefinition ashWolfEnemy;
        [SerializeField] EnemyDefinition cutthroatEnemy;
        [SerializeField] EnemyDefinition emberAcolyteEnemy;
        [SerializeField] EnemyDefinition pyreKeeperEnemy;
        [SerializeField] EnemyDefinition carrionBloatEnemy;
        [Tooltip("The deep levels' enemies and bosses (2026-10-05), found by name: a pack member's (Drowned, Harpooner...) " +
                 "or a boss's (Tidewife, SaintMarrow, FirstWatchman).")]
        [SerializeField] EnemyDefinition[] deepRoster;

        [Tooltip("The act boss, fought in the arena on the act's last level.")]
        [SerializeField] EnemyDefinition bossEnemy;

        [SerializeField] Sprite stairsSprite;
        [SerializeField] Sprite chestClosedSprite;
        [SerializeField] Sprite chestOpenSprite;

        [Tooltip("This scene's own name, which the stairs between dungeon levels load.")]
        [SerializeField] string dungeonScene = "Dungeon";

        [SerializeField] string townScene = "Town";

        [Tooltip("The depth built when the scene is opened directly (in the editor), not reached by stairs.")]
        [SerializeField, Min(1)] int editorDepth = 1;

        string levelId;
        int depth;
        int levelSeed;

        public DungeonLayout Layout { get; private set; }

        /// <summary>This level's depth, 1 for the first.</summary>
        public int Depth => depth;

        // Shown once the level is up (Start), when reaching this depth brought a newcomer to town.
        string arrivalNotice;

        void Start()
        {
            if (arrivalNotice != null)
                HintBanner.Current?.Show(arrivalNotice);
        }

        void Awake()
        {
            var session = GameSession.Current;
            var travel = session.Travel;
            depth = travel.Depth > 0 ? travel.Depth : editorDepth;
            var arrival = travel.Depth > 0 ? travel.Arrival : Arrival.FromAbove;
            session.Travel = default;

            levelId = DungeonRules.LevelId(depth);
            LevelContext.Set(levelId);
            // Elite packs roll their affixes by depth (2026-10-08).
            var enemies = FindAnyObjectByType<EnemyManager>();
            if (enemies != null)
                enemies.Depth = depth;
            // The deepest depth reached brings newcomers to town (2026-10-05); the first time, a banner says who.
            if (session.ReachDepth(depth) && Newcomers.TryArrivingAt(depth, out var newcomer))
                arrivalNotice = $"Word from town: a {Newcomers.Name(newcomer).ToLowerInvariant()} has arrived.";

            var shapes = new List<RoomShape>();
            foreach (var template in rooms)
            {
                var shape = template != null ? template.TryGetShape() : null;
                if (shape != null)
                    shapes.Add(shape);
            }
            if (shapes.Count == 0)
            {
                Debug.LogError("[ARPG] The dungeon has no usable room templates.", this);
                return;
            }

            var settings = new DungeonSettings();
            if (normalEnemy != null)
                settings.NormalAggroRange = normalEnemy.AggroRange;
            if (eliteEnemy != null)
                settings.EliteAggroRange = eliteEnemy.AggroRange;

            levelSeed = DungeonRules.LevelSeed(session.DungeonSeed, depth);
            Layout = DungeonGenerator.Generate(levelSeed, depth, shapes, settings);

            Paint();
            RescueCorpses(session);
            if (arrival == Arrival.AtPortal && session.PortalDepth == depth)
            {
                // Back through the portal: it closes behind the player (as in Diablo 1).
                PlacePlayerAt(session.PortalPosition);
                session.ClosePortal();
            }
            else
            {
                PlacePlayer(ArrivalCell(arrival));
            }

            var root = new GameObject("Level " + depth).transform;
            if (depth > 1)
                AddStairs(root, "Stairs Up", Layout.StairsUp, dungeonScene, depth - 1, Arrival.FromBelow);
            else
                AddStairs(root, "Stairs Up", Layout.StairsUp, townScene, 0, Arrival.FromAbove);
            if (Layout.HasStairsDown)
                AddStairs(root, "Stairs Down", Layout.StairsDown, dungeonScene, depth + 1, Arrival.FromAbove);

            for (var i = 0; i < Layout.Chests.Count; i++)
                AddChest(root, i, Layout.Chests[i]);

            for (var i = 0; i < Layout.Packs.Count; i++)
                AddPack(root, i, Layout.Packs[i]);

            if (Layout.HasBossArena)
                SetUpBoss(session, root);

            // The quests' things for this depth (2026-10-08).
            QuestObjectives.Create(this, Layout, root, depth, levelSeed);

            // Docs/05: a waypoint on every level; the Wanderer with the Portal Tome on its depth; an open portal.
            Waypoint.Create(depth, IsoMath.CellToGround(Layout.Waypoint), root);
            if (depth == DungeonRules.PortalTomeDepth && Layout.HasWandererSpot)
                Wanderer.Create(IsoMath.CellToGround(Layout.WandererSpot), root);
            if (session.PortalDepth == depth)
                TownPortal.Create(false, depth, session.PortalPosition, root);

            var playerController = FindAnyObjectByType<PlayerController>();
            if (playerController != null)
                gameObject.AddComponent<Minimap>().Init(Layout, levelId, playerController.transform);
        }

        Vector2Int ArrivalCell(Arrival arrival)
        {
            if (arrival == Arrival.FromBelow && (Layout.HasStairsDown || Layout.HasBossArena))
                return Layout.ArrivalFromBelow;
            if (arrival == Arrival.AtBoss && Layout.HasBossArena)
                return Layout.BossArenaEntry;
            if (arrival == Arrival.AtWaypoint)
                return Layout.Waypoint;
            return Layout.ArrivalFromAbove;
        }

        /// <summary>
        /// The boss waits in the arena until it is killed; after that, and for good in this game session, the arena
        /// holds the stairs down to the next depth (2026-10-05: one dungeon of 24 levels), or back to town from the last.
        /// The stairs stand near the arena's far edge, not in its middle, so walking over to the boss's loot does not end
        /// the level.
        /// </summary>
        void SetUpBoss(GameSession session, Transform root)
        {
            var key = levelId + "/Boss";
            var stairsCell = Layout.StairsDown;
            void AddWayOn()
            {
                if (depth < DungeonRules.Depths)
                    AddStairs(root, "Stairs Down", stairsCell, dungeonScene, depth + 1, Arrival.FromAbove);
                else
                    AddStairs(root, "Stairs To Town", stairsCell, townScene, 0, Arrival.FromAbove);
            }
            if (session.IsKilled(key, 0))
            {
                AddWayOn();
                return;
            }
            if (bossEnemy == null)
            {
                Debug.LogError("[ARPG] The boss arena has no boss definition.", this);
                return;
            }

            // The boss of this depth (2026-10-05: one every 6 levels) and the adds it calls; a missing deep boss falls
            // back to the Warden so the arena is never empty.
            BossFight fight;
            EnemyDefinition definition = bossEnemy, adds = normalEnemy;
            switch (depth)
            {
                case 12 when Deep("Tidewife") != null:
                    fight = new GameObject("Tidewife Fight").AddComponent<TidewifeFight>();
                    definition = Deep("Tidewife");
                    adds = Deep("Drowned") ?? normalEnemy;
                    break;
                case 18 when Deep("SaintMarrow") != null:
                    fight = new GameObject("Saint Marrow Fight").AddComponent<SaintMarrowFight>();
                    definition = Deep("SaintMarrow");
                    adds = skeletonEnemy != null ? skeletonEnemy : normalEnemy;
                    break;
                case 24 when Deep("FirstWatchman") != null:
                    fight = new GameObject("First Watchman Fight").AddComponent<FirstWatchmanFight>();
                    definition = Deep("FirstWatchman");
                    adds = Deep("Hollowed") ?? normalEnemy;
                    break;
                default:
                    fight = new GameObject("Cinder Warden Fight").AddComponent<CinderWardenFight>();
                    break;
            }
            fight.Configure(definition, adds, Layout.EnemyLevel, IsoMath.CellToGround(Layout.BossArenaCenter),
                Layout.BossArenaRadius * 0.7071f, key,
                () =>
                {
                    AddWayOn();
                    // A quest waiting on this boss moves on (the kill is recorded by now).
                    QuestSync.Bosses();
                });
        }

        /// <summary>A corpse on this level whose spot is no longer floor (the generator changed since it fell) moves to
        /// the arrival point, so its gear can always be reached. Runs before the corpse spawner places the markers.</summary>
        void RescueCorpses(GameSession session)
        {
            var corpses = session.Corpses;
            for (var i = 0; i < corpses.Count; i++)
            {
                var corpse = corpses[i];
                if (corpse.LevelId != levelId || Layout.IsFloor(IsoMath.GroundToCell(corpse.GroundPosition)))
                    continue;
                session.MoveCorpse(corpse, IsoMath.CellToGround(Layout.ArrivalFromAbove));
            }
        }

        void OnDestroy()
        {
            // The next level may already have named itself; only clear our own name.
            if (LevelContext.CurrentId == levelId)
                LevelContext.Clear();
        }

        void Paint()
        {
            var bounds = Layout.Bounds;
            var area = new BoundsInt(bounds.xMin, bounds.yMin, 0, bounds.width, bounds.height, 1);
            var groundTiles = new TileBase[bounds.width * bounds.height];
            var wallTiles = new TileBase[groundTiles.Length];
            var propTiles = new TileBase[groundTiles.Length];
            MeasureRock();

            for (var y = 0; y < bounds.height; y++)
                for (var x = 0; x < bounds.width; x++)
                {
                    var cell = Layout.Get(bounds.xMin + x, bounds.yMin + y);
                    if (cell == DungeonCell.Void)
                        continue;
                    var index = x + y * bounds.width;
                    var at = new Vector2Int(bounds.xMin + x, bounds.yMin + y);
                    // Rock further from the open floor than this is left out: darkness, the earth around the rooms.
                    var rock = RockDistance(at);
                    if (rock > RockShown)
                        continue;
                    if (rock > 1)
                    {
                        wallTiles[index] = DungeonArt.Wall(false, WallVariant(at.x, at.y), false);
                        continue;
                    }
                    var freeStanding = Layout.FreeStanding.Contains(at);
                    var cameraSide = !freeStanding && WallRules.IsCameraSide(IsOpenCell, at.x, at.y);
                    // The room's floor style in one of its variants, with any decal drawn on; under walls too, so a wall
                    // never shows the void behind it.
                    var style = Layout.StyleAt(at);
                    var variant = DungeonArt.VariantAt(style, at, (int)(LightingRules.FloorShade(at * 7).r * 1000f));
                    var decal = Layout.Decals.TryGetValue(at, out var kind) ? (int)kind : -1;
                    groundTiles[index] = DungeonArt.Floor(style, variant, decal);
                    // Only the level's outer and dividing walls are cut low on the camera side: a pillar or stub standing
                    // inside a room keeps its height (cut low, a 2 x 2 pillar read as a cross of stubs).
                    // Rock with no floor beside it is drawn the same but blocks nothing (2026-10-08: the rock between and
                    // around the rooms is many thousand cells; colliders only where she can reach).
                    if (cell == DungeonCell.Wall)
                        wallTiles[index] = DungeonArt.Wall(cameraSide, cameraSide ? LowWallVariant(at.x, at.y) : freeStanding ? WallVariant(at.x, at.y) : TallWallVariant(at.x, at.y),
                            TouchesOpen(at));
                    else if (cell == DungeonCell.Prop && Layout.Props.TryGetValue(at, out var prop))
                        propTiles[index] = DungeonArt.Prop(prop);
                }

            ground.ClearAllTiles();
            walls.ClearAllTiles();
            // One ground through every room, painted from the ground textures (GroundPainter); the old per-room floors
            // only when those are missing.
            var floorCells = new List<Vector2Int>();
            for (var y = 0; y < bounds.height; y++)
                for (var x = 0; x < bounds.width; x++)
                    if (groundTiles[x + y * bounds.width] != null)
                        floorCells.Add(new Vector2Int(bounds.xMin + x, bounds.yMin + y));
            // The painter shades its cells as it paints them (2026-10-06); the fallback floors are tinted tile by tile.
            if (!GroundPainter.PaintDungeon(ground, floorCells, Layout.Decals))
            {
                ground.SetTilesBlock(area, groundTiles);
                WorldLights.ShadeGround(ground);
            }
            walls.SetTilesBlock(area, wallTiles);
            var propMap = PropTilemap();
            propMap.ClearAllTiles();
            propMap.SetTilesBlock(area, propTiles);
            ShadeWalls();

            // Braziers and candles light their corner of a dark level.
            var lights = new GameObject("Prop Lights").transform;
            lights.SetParent(transform, false);
            // At most MaxPropLights lit (2026-10-08: lairs bring fires, braziers and candles; the phone draws every 2D
            // light): fires first, then braziers and torches, then candles.
            var lit = new List<KeyValuePair<Vector2Int, PropKind>>();
            foreach (var pair in Layout.Props)
                if (DungeonDressing.IsLit(pair.Value))
                    lit.Add(pair);
            lit.Sort((a, b) => LightRank(a.Value).CompareTo(LightRank(b.Value)));
            if (lit.Count > MaxPropLights)
                lit.RemoveRange(MaxPropLights, lit.Count - MaxPropLights);
            foreach (var pair in lit)
            {
                var go = new GameObject(pair.Value.ToString());
                go.transform.SetParent(lights, false);
                go.transform.position = CellWorld(pair.Key);
                var brazier = pair.Value == PropKind.Brazier;
                var torch = pair.Value == PropKind.Torch;
                var campfire = pair.Value == PropKind.Campfire;
                var hellfire = pair.Value == PropKind.Hellfire;
                var light = campfire
                    ? WorldLights.Add(go.transform, new Color(1f, 0.6f, 0.3f), 1.4f, 0.3f, 5.5f, 0.3f)
                    : hellfire
                        ? WorldLights.Add(go.transform, new Color(1f, 0.25f, 0.15f), 1.5f, 0.3f, 5f, 0.3f)
                        : WorldLights.Add(go.transform, new Color(1f, 0.55f, 0.25f), brazier ? 1.3f : torch ? 1.1f : 0.9f, 0.3f,
                            brazier ? 4.5f : torch ? 4f : 3f, torch ? 1.2f : 0.3f);
                if (light != null)
                    go.AddComponent<FlickerLight>().Init(light, pair.Key.x * 1.7f + pair.Key.y);
            }

            DressWalls(lights);

            // Summoning circles on some rooms' floors, their candles lighting them a dim red.
            foreach (var cell in Layout.Rituals)
            {
                var circle = WorldArt.Place("ritual_circle", lights, CellWorld(cell), true);
                if (circle == null)
                    break;
                var glow = WorldLights.Add(circle.transform, new Color(1f, 0.35f, 0.25f), 0.9f, 0.4f, 3.5f, 0.3f);
                if (glow != null)
                    circle.gameObject.AddComponent<FlickerLight>().Init(glow, cell.x * 0.9f + cell.y);
            }
        }

        const int MaxPropLights = 48;

        static int LightRank(PropKind kind) =>
            kind == PropKind.Campfire || kind == PropKind.Hellfire ? 0 : kind == PropKind.Brazier || kind == PropKind.Torch ? 1 : 2;

        // Wall details (WorldArt, the owner's "torches"; 2026-10-04): about one full-height wall block in nine that
        // faces into the room carries a torch, chains or a banner on its face, or candles, skulls or a cobweb on its top.
        // Picked by a hash of the cell, so a level dresses the same every time without touching the generator. Torches
        // light their stretch of wall, at most MaxWallTorches a level.
        const float WallDetailChance = 0.11f;
        const int MaxWallTorches = 14;
        static readonly string[] FaceDetails = { "wall_torch", "wall_torch", "wall_torch", "wall_chains", "wall_banner" };
        static readonly string[] TopDetails = { "wall_candles", "wall_skulls", "wall_web" };

        void DressWalls(Transform parent)
        {
            var bounds = Layout.Bounds;
            var torches = 0;
            for (var x = bounds.xMin; x < bounds.xMax; x++)
                for (var y = bounds.yMin; y < bounds.yMax; y++)
                {
                    var at = new Vector2Int(x, y);
                    if (Layout.Get(at) != DungeonCell.Wall || WallHash(x, y, 1) > WallDetailChance)
                        continue;
                    if (!Layout.FreeStanding.Contains(at) && WallRules.IsCameraSide(IsOpenCell, x, y))
                        continue;
                    // The faces the camera sees: toward -x (left front) and -y (right front).
                    bool faceX = IsOpenCell(x - 1, y), faceY = IsOpenCell(x, y - 1);
                    if (!faceX && !faceY)
                        continue;
                    // Candles, skulls and webs sit on a short wall's top; a tall wall face (2026-10-08) only carries
                    // things hung on it.
                    var onFace = !Layout.FreeStanding.Contains(at) || WallHash(x, y, 2) < 0.7f;
                    var name = onFace
                        ? FaceDetails[(int)(WallHash(x, y, 3) * FaceDetails.Length) % FaceDetails.Length]
                        : TopDetails[(int)(WallHash(x, y, 3) * TopDetails.Length) % TopDetails.Length];
                    var torch = name == "wall_torch";
                    if (torch && torches >= MaxWallTorches)
                        name = "wall_chains";
                    if (onFace)
                        name += faceX && (!faceY || WallHash(x, y, 4) < 0.5f) ? "_x" : "_y";
                    // A hair in front of its own block, so it draws over the wall and under anyone standing before it.
                    var detail = WorldArt.Place(name, parent, CellWorld(at) + new Vector3(0f, -0.01f, 0f));
                    if (detail == null || !torch || name == "wall_chains_x" || name == "wall_chains_y")
                        continue;
                    torches++;
                    var glow = WorldLights.Add(detail.transform, new Color(1f, 0.55f, 0.25f), 1.0f, 0.3f, 3.5f, 0.5f);
                    if (glow != null)
                        detail.gameObject.AddComponent<FlickerLight>().Init(glow, x * 1.3f + y * 0.7f);
                }
        }

        static float WallHash(int x, int y, int salt)
        {
            unchecked
            {
                var h = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(salt * 83492791);
                h ^= h >> 13;
                h *= 0x5bd1e995;
                h ^= h >> 15;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        /// <summary>Which wall variant a block shows: the plain one (wall_1) about half the time, else the timber,
        /// broken or mossy one, by a hash of the cell.</summary>
        static int WallVariant(int x, int y)
        {
            var h = WallHash(x, y, 5);
            return h < 0.55f ? 0 : 1 + Mathf.Min(2, (int)((h - 0.55f) / 0.45f * 3f));
        }

        // The tall wall faces (2026-10-08): wall_5 to wall_16, three themes of four, crypt at the top, ruin in the middle,
        // demonic at the bottom (the owner: "more rugged and broken down and more demonic the further down"). Each block
        // takes its theme from the depth with a little spread, so the change comes on gradually, and its variant by a
        // hash. Without them (an older import), the short walls.
        const int FirstTallWall = 4;
        const int TallThemes = 3;
        const int TallPerTheme = 4;

        int TallWallVariant(int x, int y)
        {
            if (DungeonArt.WallVariants(false) < FirstTallWall + TallThemes * TallPerTheme)
                return WallVariant(x, y);
            var theme = Mathf.Clamp(Mathf.FloorToInt(ThemeAt(x, y)), 0, TallThemes - 1);
            var h = WallHash(x, y, 6);
            // The plain variant half the time, the others shared out.
            var variant = h < 0.45f ? 0 : 1 + Mathf.Min(2, (int)((h - 0.45f) / 0.55f * 3f));
            return FirstTallWall + theme * TallPerTheme + variant;
        }

        // The theme a depth leans to, 0 (crypt) to 3: depth 1 is 0, 24 is 3, each block nudged by up to half a theme.
        float ThemeAt(int x, int y) => (depth - 1) / (float)(DungeonRules.Depths - 1) * TallThemes + (WallHash(x, y, 7) - 0.5f);

        // Camera-side walls: the demonic low walls (wall_low_5, 6) where the demonic theme has come.
        int LowWallVariant(int x, int y)
        {
            if (DungeonArt.WallVariants(true) >= 6 && ThemeAt(x, y) >= 2f)
                return 4 + (WallHash(x, y, 8) < 0.7f ? 0 : 1);
            return WallVariant(x, y);
        }

        // How many cells of rock around the open floor are drawn, darkening into black (2026-10-08: the rock between the
        // rooms, so the level reads as dug out of the earth rather than laid on a black page).
        const int RockShown = 4;
        static readonly float[] RockShade = { 1f, 1f, 0.5f, 0.26f, 0.1f };
        int[,] rockDistance;

        // Chebyshev distance from each wall cell to the nearest open cell, up to RockShown + 1.
        void MeasureRock()
        {
            var b = Layout.Bounds;
            rockDistance = new int[b.width, b.height];
            var queue = new Queue<Vector2Int>();
            for (var x = b.xMin; x < b.xMax; x++)
                for (var y = b.yMin; y < b.yMax; y++)
                {
                    var open = IsOpenCell(x, y);
                    rockDistance[x - b.xMin, y - b.yMin] = open ? 0 : RockShown + 1;
                    if (open)
                        queue.Enqueue(new Vector2Int(x, y));
                }
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                var d = rockDistance[cell.x - b.xMin, cell.y - b.yMin] + 1;
                if (d > RockShown)
                    continue;
                for (var dx = -1; dx <= 1; dx++)
                    for (var dy = -1; dy <= 1; dy++)
                    {
                        var next = new Vector2Int(cell.x + dx, cell.y + dy);
                        if (!b.Contains(next) || rockDistance[next.x - b.xMin, next.y - b.yMin] <= d)
                            continue;
                        rockDistance[next.x - b.xMin, next.y - b.yMin] = d;
                        queue.Enqueue(next);
                    }
            }
        }

        int RockDistance(Vector2Int cell)
        {
            var b = Layout.Bounds;
            return b.Contains(cell) ? rockDistance[cell.x - b.xMin, cell.y - b.yMin] : RockShown + 1;
        }

        // Each wall block takes its own shade, so a run of wall does not read as one repeated box; one stone through every
        // room (the owner, 2026-10-04: no per-room looks). Props keep their own colours. Rock away from the floor darkens.
        void ShadeWalls()
        {
            var bounds = Layout.Bounds;
            for (var x = bounds.xMin; x < bounds.xMax; x++)
                for (var y = bounds.yMin; y < bounds.yMax; y++)
                {
                    var at = new Vector2Int(x, y);
                    if (Layout.Get(at) != DungeonCell.Wall || RockDistance(at) > RockShown)
                        continue;
                    var cell = new Vector3Int(x, y, 0);
                    var shade = (0.9f + 0.1f * LightingRules.FloorShade(at * 3).g) * RockShade[RockDistance(at)];
                    walls.SetTileFlags(cell, TileFlags.None);
                    walls.SetColor(cell, new Color(shade, shade, shade, 1f));
                }
        }

        // Made once beside the walls, drawn the same way (sorted with the characters), but blocking nothing: the enemy
        // manager bakes its nav grid from the walls alone.
        Tilemap PropTilemap()
        {
            if (props != null)
                return props;
            var go = new GameObject("Props", typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(walls.transform.parent, false);
            go.transform.localPosition = walls.transform.localPosition;
            props = go.GetComponent<Tilemap>();
            props.tileAnchor = walls.tileAnchor;
            props.orientation = walls.orientation;
            var from = walls.GetComponent<TilemapRenderer>();
            var to = go.GetComponent<TilemapRenderer>();
            to.sortingLayerID = from.sortingLayerID;
            to.sortingOrder = from.sortingOrder;
            to.mode = from.mode;
            to.sortOrder = from.sortOrder;
            to.sharedMaterial = from.sharedMaterial;
            return props;
        }

        bool TouchesOpen(Vector2Int cell)
        {
            for (var dx = -1; dx <= 1; dx++)
                for (var dy = -1; dy <= 1; dy++)
                    if (IsOpenCell(cell.x + dx, cell.y + dy))
                        return true;
            return false;
        }

        // Floor for the low-wall rule: a prop stands on floor, so a wall in front of it is still a camera-side wall.
        bool IsOpenCell(int x, int y) => Layout.Get(x, y) == DungeonCell.Floor || Layout.Get(x, y) == DungeonCell.Prop;

        static Vector3 CellWorld(Vector2Int cell)
        {
            var world = IsoMath.GroundToWorld(IsoMath.CellToGround(cell));
            return new Vector3(world.x, world.y, 0f);
        }

        void PlacePlayerAt(Vector2 ground)
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player == null)
                return;
            var world = IsoMath.GroundToWorld(ground);
            var position = new Vector3(world.x, world.y, 0f);
            player.transform.position = position;
            if (player.TryGetComponent<Rigidbody2D>(out var body))
                body.position = position;
        }

        void PlacePlayer(Vector2Int cell)
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player == null)
                return;

            var position = CellWorld(cell);
            player.transform.position = position;
            if (player.TryGetComponent<Rigidbody2D>(out var body))
                body.position = position;
        }

        void AddStairs(Transform parent, string objectName, Vector2Int cell, string scene, int depth, Arrival arriveBy)
        {
            var go = new GameObject(objectName, typeof(SpriteRenderer), typeof(CircleCollider2D));
            go.transform.SetParent(parent, false);
            go.transform.position = CellWorld(cell);
            SetLayer(go, GameLayers.Interactable);

            var spriteRenderer = go.GetComponent<SpriteRenderer>();
            // The modelled stairs (WorldArt) when imported: a shaft down, or a flight of steps up.
            var modelled = WorldArt.Get(objectName == "Stairs Down" ? "stairs_down" : "stairs_up");
            spriteRenderer.sprite = modelled != null ? modelled : stairsSprite;
            spriteRenderer.sortingLayerName = GameSortingLayers.Decals;
            if (modelled != null)
                WorldArt.Lit(spriteRenderer);

            var collider = go.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.4f;

            go.AddComponent<SceneExit>().Configure(scene, depth, arriveBy);
        }

        void AddChest(Transform parent, int index, Vector2Int cell)
        {
            var go = new GameObject("Chest " + index, typeof(SpriteRenderer), typeof(CircleCollider2D));
            go.transform.SetParent(parent, false);
            go.transform.position = CellWorld(cell);
            SetLayer(go, GameLayers.Interactable);

            var spriteRenderer = go.GetComponent<SpriteRenderer>();
            spriteRenderer.sortingLayerName = GameSortingLayers.Entities;
            spriteRenderer.spriteSortPoint = SpriteSortPoint.Pivot;

            var collider = go.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.5f;

            var closed = WorldArt.Get("chest_closed");
            var opened = WorldArt.Get("chest_open");
            if (closed != null && opened != null)
                WorldArt.Lit(spriteRenderer);
            else
            {
                closed = chestClosedSprite;
                opened = chestOpenSprite;
            }
            go.AddComponent<Chest>().Configure(levelId + "/Chest " + index, Layout.EnemyLevel, spriteRenderer, closed, opened);
        }

        void AddPack(Transform parent, int index, PackPlacement placement)
        {
            // Pack names key the killed members, so they must be unique within the level and stable for its seed.
            var go = new GameObject("Pack " + index);
            go.transform.SetParent(parent, false);
            go.transform.position = CellWorld(placement.Cell);

            var pack = go.AddComponent<EnemyPack>();
            if (placement.Kind == PackKind.Elite)
            {
                pack.Configure(eliteEnemy, null, placement.Count, placement.Radius, Layout.EnemyLevel);
                return;
            }

            var members = PackComposition.Roll(depth, placement.Kind, placement.Count, levelSeed, index);
            var perSlot = new EnemyDefinition[members.Length];
            for (var i = 0; i < members.Length; i++)
                perSlot[i] = Definition(members[i]);
            if (placement.Kind == PackKind.WithChampion && perSlot.Length > 0)
                perSlot[0] = championEnemy;
            pack.Configure(perSlot, placement.Radius, Layout.EnemyLevel);
        }

        /// <summary>The definition a pack member uses here (for quest foes and the waves at a watch fire).</summary>
        public EnemyDefinition DefinitionOf(PackMember member) => Definition(member);

        /// <summary>A deep or quest definition by its asset's name (RiftHeart, BonePyre), or null.</summary>
        public EnemyDefinition DeepDefinition(string name) => Deep(name);

        // A missing ghoul or archer definition falls back to a husk, so an older scene still fills its packs.
        EnemyDefinition Definition(PackMember member)
        {
            switch (member)
            {
                case PackMember.None: return null;
                case PackMember.Ghoul: return ghoulEnemy != null ? ghoulEnemy : normalEnemy;
                case PackMember.Archer: return archerEnemy != null ? archerEnemy : normalEnemy;
                case PackMember.Skeleton: return skeletonEnemy != null ? skeletonEnemy : normalEnemy;
                case PackMember.Cultist: return cultistEnemy != null ? cultistEnemy : archerEnemy != null ? archerEnemy : normalEnemy;
                case PackMember.AshWolf: return ashWolfEnemy != null ? ashWolfEnemy : normalEnemy;
                case PackMember.Cutthroat: return cutthroatEnemy != null ? cutthroatEnemy : normalEnemy;
                case PackMember.EmberAcolyte: return emberAcolyteEnemy != null ? emberAcolyteEnemy : archerEnemy != null ? archerEnemy : normalEnemy;
                case PackMember.PyreKeeper: return pyreKeeperEnemy != null ? pyreKeeperEnemy : normalEnemy;
                case PackMember.CarrionBloat: return carrionBloatEnemy != null ? carrionBloatEnemy : ghoulEnemy != null ? ghoulEnemy : normalEnemy;
                case PackMember.Husk: return normalEnemy;
                default: return Deep(member.ToString()) ?? normalEnemy;
            }
        }

        /// <summary>A deep enemy or boss definition by its asset's name, or null.</summary>
        EnemyDefinition Deep(string name)
        {
            if (deepRoster == null)
                return null;
            foreach (var definition in deepRoster)
                if (definition != null && definition.name == name)
                    return definition;
            return null;
        }

        static void SetLayer(GameObject go, string layerName)
        {
            var layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
                go.layer = layer;
        }
    }
}
