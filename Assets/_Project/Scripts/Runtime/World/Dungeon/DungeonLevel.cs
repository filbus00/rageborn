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
        [SerializeField] TileBase[] floorTiles;
        [SerializeField] TileBase wallTile;

        [Tooltip("The room library the generator draws from.")]
        [SerializeField] RoomTemplate[] rooms;

        [SerializeField] EnemyDefinition normalEnemy;
        [SerializeField] EnemyDefinition championEnemy;
        [SerializeField] EnemyDefinition eliteEnemy;

        [SerializeField] Sprite stairsSprite;
        [SerializeField] Sprite chestClosedSprite;
        [SerializeField] Sprite chestOpenSprite;

        [Tooltip("This scene's own name, which the stairs between dungeon levels load.")]
        [SerializeField] string dungeonScene = "Dungeon";

        [SerializeField] string townScene = "Town";

        [Tooltip("The depth built when the scene is opened directly (in the editor), not reached by stairs.")]
        [SerializeField, Min(1)] int editorDepth = 1;

        string levelId;

        public DungeonLayout Layout { get; private set; }

        void Awake()
        {
            var session = GameSession.Current;
            var travel = session.Travel;
            var depth = travel.Depth > 0 ? travel.Depth : editorDepth;
            var arrival = travel.Depth > 0 ? travel.Arrival : Arrival.FromAbove;
            session.Travel = default;

            levelId = DungeonRules.LevelId(depth);
            LevelContext.Set(levelId);

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

            Layout = DungeonGenerator.Generate(DungeonRules.LevelSeed(session.DungeonSeed, depth), depth, shapes, settings);

            Paint();
            RescueCorpses(session);
            PlacePlayer(arrival == Arrival.FromBelow && Layout.HasStairsDown ? Layout.ArrivalFromBelow : Layout.ArrivalFromAbove);

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

            for (var y = 0; y < bounds.height; y++)
                for (var x = 0; x < bounds.width; x++)
                {
                    var cell = Layout.Get(bounds.xMin + x, bounds.yMin + y);
                    if (cell == DungeonCell.Void)
                        continue;
                    var index = x + y * bounds.width;
                    // A checkerboard of the placeholder tiles, as the sandbox ground uses; under walls too, so a wall never
                    // shows the void behind it.
                    groundTiles[index] = floorTiles[((x + y) & 1) % floorTiles.Length];
                    if (cell == DungeonCell.Wall)
                        wallTiles[index] = wallTile;
                }

            ground.ClearAllTiles();
            walls.ClearAllTiles();
            ground.SetTilesBlock(area, groundTiles);
            walls.SetTilesBlock(area, wallTiles);
        }

        static Vector3 CellWorld(Vector2Int cell)
        {
            var world = IsoMath.GroundToWorld(IsoMath.CellToGround(cell));
            return new Vector3(world.x, world.y, 0f);
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
            spriteRenderer.sprite = stairsSprite;
            spriteRenderer.sortingLayerName = GameSortingLayers.Decals;

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

            go.AddComponent<Chest>().Configure(levelId + "/Chest " + index, Layout.EnemyLevel, spriteRenderer, chestClosedSprite, chestOpenSprite);
        }

        void AddPack(Transform parent, int index, PackPlacement placement)
        {
            // Pack names key the killed members, so they must be unique within the level and stable for its seed.
            var go = new GameObject("Pack " + index);
            go.transform.SetParent(parent, false);
            go.transform.position = CellWorld(placement.Cell);

            var member = placement.Kind == PackKind.Elite ? eliteEnemy : normalEnemy;
            var leader = placement.Kind == PackKind.WithChampion ? championEnemy : null;
            go.AddComponent<EnemyPack>().Configure(member, leader, placement.Count, placement.Radius, Layout.EnemyLevel);
        }

        static void SetLayer(GameObject go, string layerName)
        {
            var layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
                go.layer = layer;
        }
    }
}
