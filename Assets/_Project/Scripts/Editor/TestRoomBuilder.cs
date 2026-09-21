using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ARPG.Editor
{
    /// <summary>
    /// Adds a walled test room to the sandbox: a perimeter, a divider with a two cell gap so enemies have to route
    /// around it, and four packs of 10 (the M0 target of 40 enemies). It exists to exercise pathfinding, aggro and
    /// leash behaviour. Run from Tools > ARPG > Add Test Room To Sandbox, after Add Enemies To Sandbox.
    /// Running Create Sandbox Scene again rebuilds the Grid, which removes the walls; run this again afterwards.
    /// </summary>
    public static class TestRoomBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/Sandbox.unity";
        const string WallSpritePath = "Assets/_Project/Art/Tilesets/PlaceholderWall.png";
        const string WallTilePath = "Assets/_Project/Art/Tilesets/PlaceholderWall.asset";

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
            BuildPacks(EnemySceneBuilder.LoadOrCreateDefinition());

            var start = IsoMath.GroundToWorld(IsoMath.CellToGround(PlayerStartCell));
            player.transform.position = new Vector3(start.x, start.y, 0f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ARPG] Test room added to the sandbox scene.");
        }

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

        static void BuildPacks(EnemyDefinition definition)
        {
            var room = new GameObject(RoomObjectName);

            foreach (var (name, cell) in Packs)
            {
                var pack = new GameObject(name, typeof(EnemyPack));
                pack.transform.SetParent(room.transform, false);

                var world = IsoMath.GroundToWorld(IsoMath.CellToGround(cell));
                pack.transform.position = new Vector3(world.x, world.y, 0f);

                var component = pack.GetComponent<EnemyPack>();
                EnemySceneBuilder.SetReference(component, "definition", definition);

                var serialized = new SerializedObject(component);
                serialized.FindProperty("count").intValue = PackSize;
                serialized.FindProperty("radius").floatValue = PackRadius;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
