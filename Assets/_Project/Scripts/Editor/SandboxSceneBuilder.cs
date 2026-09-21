using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace ARPG.Editor
{
    /// <summary>
    /// Builds the isometric sandbox scene: the template's SampleScene (camera + Global Light 2D) moved into
    /// _Project/Scenes, plus an isometric Grid with a Tilemap of generated placeholder tiles.
    /// Run from Tools > ARPG > Create Sandbox Scene. Running it again rebuilds the Grid and resets the camera framing.
    /// </summary>
    public static class SandboxSceneBuilder
    {
        const string TemplateScenePath = "Assets/Scenes/SampleScene.unity";
        const string ScenePath = "Assets/_Project/Scenes/Sandbox.unity";
        const string TilesetFolder = "Assets/_Project/Art/Tilesets";

        // 128x64 px diamond at 128 pixels per unit = 1 x 0.5 world units, matching the isometric grid cell size
        // and the 128 px tile sets in Docs/07-technical.md.
        const int TileWidthPx = 128;
        const int TileHeightPx = 64;
        const int PixelsPerUnit = 128;
        static readonly Vector3 CellSize = new Vector3(1f, 0.5f, 1f);

        // The 9 x 16 portrait view (camera size 8) must sit inside the diamond: |x|/HalfExtent + |y|/(HalfExtent/2) < 1
        // at the screen corner (4.5, 8), which HalfExtent 32 satisfies with room to move.
        const int HalfExtent = 32;
        const float PortraitCameraSize = 8f;

        [MenuItem("Tools/ARPG/Create Sandbox Scene")]
        public static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (!EnsureSceneAsset())
                return;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var existingGrid = Object.FindAnyObjectByType<Grid>();
            if (existingGrid != null)
                Object.DestroyImmediate(existingGrid.gameObject);

            ConfigureCamera();
            ConfigureLights();

            var tiles = new[]
            {
                CreateTile("PlaceholderTile_A", new Color32(74, 84, 64, 255)),
                CreateTile("PlaceholderTile_B", new Color32(66, 76, 58, 255)),
            };

            BuildGrid(tiles);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log($"[ARPG] Sandbox scene created at {ScenePath}.");
        }

        static void ConfigureCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogWarning("[ARPG] No Main Camera found; set the orthographic size to 8 manually.");
                return;
            }

            camera.orthographic = true;
            camera.orthographicSize = PortraitCameraSize;
        }

        // The template's Global Light 2D only targets the sorting layers that existed when it was created, so
        // sprites on Ground, Decals and Entities render black under the lit 2D materials until it targets them all.
        static void ConfigureLights()
        {
            var layerIds = System.Array.ConvertAll(SortingLayer.layers, layer => layer.id);
            foreach (var light in Object.FindObjectsByType<Light2D>())
            {
                light.targetSortingLayers = layerIds;
                EditorUtility.SetDirty(light);
            }
        }

        static bool EnsureSceneAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                return true;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TemplateScenePath) == null)
            {
                Debug.LogError($"[ARPG] Neither {ScenePath} nor {TemplateScenePath} exists.");
                return false;
            }

            var error = AssetDatabase.MoveAsset(TemplateScenePath, ScenePath);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"[ARPG] Could not move the template scene: {error}");
                return false;
            }

            if (AssetDatabase.FindAssets("", new[] { "Assets/Scenes" }).Length == 0)
                AssetDatabase.DeleteAsset("Assets/Scenes");

            return true;
        }

        static void BuildGrid(TileBase[] tiles)
        {
            var gridObject = new GameObject("Grid", typeof(Grid));
            var grid = gridObject.GetComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Isometric;
            grid.cellSize = CellSize;

            var groundObject = new GameObject("Ground", typeof(Tilemap), typeof(TilemapRenderer));
            groundObject.transform.SetParent(gridObject.transform, false);

            var tilemap = groundObject.GetComponent<Tilemap>();
            var tilemapRenderer = groundObject.GetComponent<TilemapRenderer>();
            tilemapRenderer.sortingLayerName = GameSortingLayers.Ground;

            var default2DMaterial = GraphicsSettings.defaultRenderPipeline != null
                ? GraphicsSettings.defaultRenderPipeline.default2DMaterial
                : null;
            if (default2DMaterial != null)
                tilemapRenderer.sharedMaterial = default2DMaterial;

            var size = HalfExtent * 2;
            var bounds = new BoundsInt(-HalfExtent, -HalfExtent, 0, size, size, 1);
            var block = new TileBase[size * size];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                    block[x + y * size] = tiles[(x + y) % 2];

            tilemap.SetTilesBlock(bounds, block);
        }

        static Tile CreateTile(string name, Color32 fill)
        {
            Directory.CreateDirectory(TilesetFolder);

            var spritePath = $"{TilesetFolder}/{name}.png";
            File.WriteAllBytes(spritePath, GenerateDiamondPng(fill));
            AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(spritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            var tilePath = $"{TilesetFolder}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }

            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            EditorUtility.SetDirty(tile);
            return tile;
        }

        static byte[] GenerateDiamondPng(Color32 fill)
        {
            var edge = new Color32(
                (byte)(fill.r * 0.7f), (byte)(fill.g * 0.7f), (byte)(fill.b * 0.7f), 255);
            var pixels = new Color32[TileWidthPx * TileHeightPx];

            for (var y = 0; y < TileHeightPx; y++)
            {
                for (var x = 0; x < TileWidthPx; x++)
                {
                    // Normalised diamond distance from the tile centre: 1.0 is the outline.
                    var dx = Mathf.Abs(x + 0.5f - TileWidthPx * 0.5f) / (TileWidthPx * 0.5f);
                    var dy = Mathf.Abs(y + 0.5f - TileHeightPx * 0.5f) / (TileHeightPx * 0.5f);
                    var d = dx + dy;

                    pixels[x + y * TileWidthPx] = d > 1f ? new Color32(0, 0, 0, 0)
                        : d > 0.93f ? edge
                        : fill;
                }
            }

            var texture = new Texture2D(TileWidthPx, TileHeightPx, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            var png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            return png;
        }
    }
}
