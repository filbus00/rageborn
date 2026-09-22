using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ARPG.Editor
{
    /// <summary>
    /// Adds a walled test room to the sandbox: a perimeter, a divider with a two cell gap so enemies have to route
    /// around it, four packs of 10 (the M0 target of 40 enemies, one with a Champion leader) and one smaller Elite
    /// pack. It exists to exercise pathfinding, aggro and leash behaviour. Run from Tools > ARPG > Add Test Room To
    /// Sandbox, after Add Enemies To Sandbox. Running Create Sandbox Scene again rebuilds the Grid, which removes
    /// the walls; run this again afterwards.
    /// </summary>
    public static class TestRoomBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/Sandbox.unity";
        const string WallSpritePath = "Assets/_Project/Art/Tilesets/PlaceholderWall.png";
        const string WallTilePath = "Assets/_Project/Art/Tilesets/PlaceholderWall.asset";
        const string ChampionDefinitionPath = "Assets/_Project/Data/Enemies/SwarmerChampion.asset";
        const string EliteDefinitionPath = "Assets/_Project/Data/Enemies/SwarmerElite.asset";
        const string CharacterArtFolder = "Assets/_Project/Art/Characters";

        const int PixelsPerUnit = 128;

        // Names of the scene objects this builder owns, so a rerun can replace them.
        const string WallsObjectName = "Walls";
        const string RoomObjectName = "Test Room";

        // The room is a square in cell space, which shows as a diamond on screen. Interior cells run from -8 to 7 on
        // both axes and the walls sit one cell outside that.
        const int InteriorMin = -8;
        const int InteriorMax = 7;

        // A divider across the middle with a gap two cells wide, so packs behind it must path through the gap.
        const int DividerY = 0;
        static readonly int[] DividerGap = { -1, 0 };

        static readonly Vector2Int PlayerStartCell = new Vector2Int(0, -7);

        // Pack centers in cell coordinates. The two near packs are within aggro range of the start, the two far
        // ones are behind the divider and idle until the player approaches.
        static readonly (string name, Vector2Int cell)[] Packs =
        {
            ("Pack West (near)", new Vector2Int(-4, -5)),
            ("Pack East (near)", new Vector2Int(4, -5)),
            ("Pack West (far)", new Vector2Int(-4, 4)),
            ("Pack East (far)", new Vector2Int(4, 4)),
        };

        const int PackSize = 10;

        // Small enough that a pack of 10 fits between the walls and the divider.
        const float PackRadius = 1.8f;

        // Docs/03-itemization.md: Champion is a single pack leader. This is the one pack that gets one, so it is
        // visible early without crossing the divider.
        const string ChampionPackName = "Pack West (near)";

        // Docs/05-world-and-content.md: every level has one guaranteed elite pack. Smaller than a normal pack (an
        // elite pack is a rarer, tougher encounter, not another crowd), centered between the two far packs.
        const string ElitePackName = "Pack Elite (far)";
        static readonly Vector2Int EliteCell = new Vector2Int(0, 4);
        const int EliteCount = 4;
        const float EliteRadius = 1.1f;

        [MenuItem("Tools/ARPG/Add Test Room To Sandbox")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogError($"[ARPG] {ScenePath} not found. Run Tools > ARPG > Create Sandbox Scene first.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var grid = Object.FindAnyObjectByType<Grid>();
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (grid == null || player == null || Object.FindAnyObjectByType<EnemyManager>() == null)
            {
                Debug.LogError("[ARPG] The scene needs a Grid, a Player and an Enemy Manager. Run Add Player And Camera To Sandbox and Add Enemies To Sandbox first.");
                return;
            }

            PlayerSceneBuilder.RemoveExisting(scene, RoomObjectName);
            var oldWalls = grid.transform.Find(WallsObjectName);
            if (oldWalls != null)
                Object.DestroyImmediate(oldWalls.gameObject);

            BuildWalls(grid, CreateWallTile());

            var normal = EnemySceneBuilder.LoadOrCreateDefinition();
            // Each rank gets its own sprite, not a runtime tint: SpriteRenderer.color multiplies the sprite's own
            // (already red) pixels, so a tint can only darken toward brown, never reach a clean gold or crimson.
            var championSprite = ImportBodySprite("SwarmerChampion", new Color32(224, 168, 50, 255));
            var eliteSprite = ImportBodySprite("SwarmerElite", new Color32(176, 40, 90, 255));

            // Tuning values: no target time to kill is fixed yet, so these are starting points, not a fit to the
            // docs' 5-9 second elite kill time at recommended gear.
            var champion = EnemySceneBuilder.LoadOrCreateVariant(
                ChampionDefinitionPath, EnemyRank.Champion,
                lifeMultiplier: 3f, damageMultiplier: 1.6f, bodyRadius: 0.4f, aggroRange: 7f,
                visualScale: 1.4f, bodySprite: championSprite);
            var elite = EnemySceneBuilder.LoadOrCreateVariant(
                EliteDefinitionPath, EnemyRank.Elite,
                lifeMultiplier: 6f, damageMultiplier: 2.5f, bodyRadius: 0.45f, aggroRange: 10f,
                visualScale: 1.6f, bodySprite: eliteSprite);

            BuildPacks(normal, champion, elite);

            var start = IsoMath.GroundToWorld(IsoMath.CellToGround(PlayerStartCell));
            player.transform.position = new Vector3(start.x, start.y, 0f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ARPG] Test room added to the sandbox scene.");
        }

        static Sprite ImportBodySprite(string name, Color32 color) =>
            PlaceholderArt.ImportSprite(
                $"{CharacterArtFolder}/Placeholder{name}.png",
                PlaceholderArt.Capsule(80, 112, color),
                PixelsPerUnit, SpriteAlignment.BottomCenter, FilterMode.Bilinear);

        static Tile CreateWallTile()
        {
            // The footprint center of the block sits a quarter of the way up the sprite, so that is the pivot.
            var sprite = PlaceholderArt.ImportSprite(
                WallSpritePath,
                PlaceholderArt.WallBlock(128, 128, new Color32(112, 112, 122, 255), new Color32(84, 84, 96, 255), new Color32(58, 58, 70, 255)),
                PixelsPerUnit, SpriteAlignment.Custom, FilterMode.Point, new Vector2(0.5f, 0.25f));

            var tile = AssetDatabase.LoadAssetAtPath<Tile>(WallTilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, WallTilePath);
            }

            tile.sprite = sprite;

            // The collider is the whole grid cell, so it matches the footprint the pathfinder blocks.
            tile.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile);
            AssetDatabase.SaveAssets();
            return tile;
        }

        static void BuildWalls(Grid grid, TileBase wallTile)
        {
            var walls = new GameObject(WallsObjectName,
                typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D), typeof(Rigidbody2D), typeof(CompositeCollider2D));
            walls.transform.SetParent(grid.transform, false);
            PlayerSceneBuilder.SetLayer(walls, GameLayers.Obstacle);

            // Individual mode sorts each block against characters by its feet, so the player can walk behind a wall.
            var tilemapRenderer = walls.GetComponent<TilemapRenderer>();
            tilemapRenderer.sortingLayerName = GameSortingLayers.Entities;
            tilemapRenderer.mode = TilemapRenderer.Mode.Individual;
            var material = PlayerSceneBuilder.Default2DMaterial();
            if (material != null)
                tilemapRenderer.sharedMaterial = material;

            // Merging the cell colliders into one static shape stops the player catching on seams between blocks.
            walls.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            walls.GetComponent<TilemapCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
            walls.GetComponent<CompositeCollider2D>().geometryType = CompositeCollider2D.GeometryType.Polygons;

            var tilemap = walls.GetComponent<Tilemap>();
            foreach (var cell in WallCells())
                tilemap.SetTile(new Vector3Int(cell.x, cell.y, 0), wallTile);
        }

        static IEnumerable<Vector2Int> WallCells()
        {
            for (var x = InteriorMin - 1; x <= InteriorMax + 1; x++)
            {
                yield return new Vector2Int(x, InteriorMin - 1);
                yield return new Vector2Int(x, InteriorMax + 1);
            }

            for (var y = InteriorMin; y <= InteriorMax; y++)
            {
                yield return new Vector2Int(InteriorMin - 1, y);
                yield return new Vector2Int(InteriorMax + 1, y);
            }

            for (var x = InteriorMin; x <= InteriorMax; x++)
                if (System.Array.IndexOf(DividerGap, x) < 0)
                    yield return new Vector2Int(x, DividerY);
        }

        static void BuildPacks(EnemyDefinition definition, EnemyDefinition championDefinition, EnemyDefinition eliteDefinition)
        {
            var room = new GameObject(RoomObjectName);

            foreach (var (name, cell) in Packs)
            {
                var pack = CreatePack(room.transform, name, cell, definition, PackSize, PackRadius);
                if (name == ChampionPackName)
                    EnemySceneBuilder.SetReference(pack, "championDefinition", championDefinition);
            }

            CreatePack(room.transform, ElitePackName, EliteCell, eliteDefinition, EliteCount, EliteRadius);
        }

        static EnemyPack CreatePack(Transform parent, string name, Vector2Int cell, EnemyDefinition definition, int count, float radius)
        {
            var pack = new GameObject(name, typeof(EnemyPack));
            pack.transform.SetParent(parent, false);

            var world = IsoMath.GroundToWorld(IsoMath.CellToGround(cell));
            pack.transform.position = new Vector3(world.x, world.y, 0f);

            var component = pack.GetComponent<EnemyPack>();
            EnemySceneBuilder.SetReference(component, "definition", definition);

            var serialized = new SerializedObject(component);
            serialized.FindProperty("count").intValue = count;
            serialized.FindProperty("radius").floatValue = radius;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return component;
        }
    }
}
