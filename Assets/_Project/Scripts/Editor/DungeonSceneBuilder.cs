using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ARPG.Editor
{
    /// <summary>
    /// Builds the dungeon scene: a copy of the sandbox with the test room, its walls, its ground and its stairway
    /// removed, and a <see cref="DungeonLevel"/> that generates each level at load instead. Also creates the room
    /// library (Data/Rooms, from <see cref="PlaceholderRooms"/>; existing templates are kept, so edits survive a rerun)
    /// and the placeholder chest art, and writes the build settings: Town, then Dungeon.
    /// Run from Tools > ARPG > Create Dungeon Scene, after Create Town Scene.
    /// </summary>
    public static class DungeonSceneBuilder
    {
        const string SandboxPath = "Assets/_Project/Scenes/Sandbox.unity";
        public const string DungeonPath = "Assets/_Project/Scenes/Dungeon.unity";
        public const string TownPath = "Assets/_Project/Scenes/Town.unity";
        const string RoomsFolder = "Assets/_Project/Data/Rooms";
        const string EnvironmentArtFolder = "Assets/_Project/Art/Environment";
        const int PixelsPerUnit = 128;

        // Every level spawns all its packs at load, typically 100 to 180 enemies in the bigger levels; the pool should
        // not have to grow.
        const int EnemyPoolSize = 200;

        static readonly string[] RemovedObjects = { "Test Room", "Stairs Up" };

        [MenuItem("Tools/ARPG/Create Dungeon Scene")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SandboxPath) == null)
            {
                Debug.LogError($"[ARPG] {SandboxPath} not found. Run the sandbox build steps first.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // Art first: importing it refreshes the asset database, which would leave room templates created just before
            // it as dead references (they were all null in the scene the first time).
            var (chestClosed, chestOpen) = CreateChestArt();

            EditorSceneManager.OpenScene(SandboxPath, OpenSceneMode.Single);
            if (Object.FindAnyObjectByType<EnemyManager>() == null || Object.FindAnyObjectByType<LootDirector>() == null)
            {
                Debug.LogError("[ARPG] The sandbox needs its enemies and loot. Run the sandbox build steps first.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DungeonPath) != null)
            {
                // Overwrite in place so the scene keeps its GUID, as the town builder does.
                File.Copy(SandboxPath, DungeonPath, true);
                AssetDatabase.ImportAsset(DungeonPath, ImportAssetOptions.ForceUpdate);
            }
            else if (!AssetDatabase.CopyAsset(SandboxPath, DungeonPath))
            {
                Debug.LogError($"[ARPG] Could not copy {SandboxPath} to {DungeonPath}.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(DungeonPath, OpenSceneMode.Single);
            PlayerSceneBuilder.RemoveExisting(scene, RemovedObjects);

            var grid = Object.FindAnyObjectByType<Grid>();
            var ground = grid.transform.Find("Ground").GetComponent<Tilemap>();
            var walls = grid.transform.Find("Walls").GetComponent<Tilemap>();
            ground.ClearAllTiles();
            walls.ClearAllTiles();

            // Beyond the walls is nothing, as in Diablo: black, not the sandbox's sky color.
            var camera = Camera.main;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;

            var manager = Object.FindAnyObjectByType<EnemyManager>();
            var managerObject = new SerializedObject(manager);
            managerObject.FindProperty("poolSize").intValue = EnemyPoolSize;
            managerObject.ApplyModifiedPropertiesWithoutUndo();

            var rooms = EnsureRoomTemplates();
            if (rooms.Count == 0 || rooms.Exists(r => r == null))
            {
                Debug.LogError("[ARPG] The room templates could not be loaded; the dungeon scene was not finished.");
                return;
            }

            var level = new GameObject("Dungeon Level").AddComponent<DungeonLevel>();
            var serialized = new SerializedObject(level);
            serialized.FindProperty("ground").objectReferenceValue = ground;
            serialized.FindProperty("walls").objectReferenceValue = walls;
            SetArray(serialized.FindProperty("floorTiles"), new Object[]
            {
                AssetDatabase.LoadAssetAtPath<TileBase>("Assets/_Project/Art/Tilesets/PlaceholderTile_A.asset"),
                AssetDatabase.LoadAssetAtPath<TileBase>("Assets/_Project/Art/Tilesets/PlaceholderTile_B.asset"),
            });
            serialized.FindProperty("wallTile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/_Project/Art/Tilesets/PlaceholderWall.asset");
            SetArray(serialized.FindProperty("rooms"), rooms.ToArray());
            serialized.FindProperty("normalEnemy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Swarmer.asset");
            serialized.FindProperty("championEnemy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/SwarmerChampion.asset");
            serialized.FindProperty("eliteEnemy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/SwarmerElite.asset");
            serialized.FindProperty("stairsSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{EnvironmentArtFolder}/PlaceholderStairs.png");
            serialized.FindProperty("chestClosedSprite").objectReferenceValue = chestClosed;
            serialized.FindProperty("chestOpenSprite").objectReferenceValue = chestOpen;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            WriteBuildSettings();
            Debug.Log($"[ARPG] Dungeon scene created at {DungeonPath} with {rooms.Count} room templates.");
        }

        /// <summary>The scenes a build contains: the town, where a new character starts, then the dungeon. The sandbox
        /// is a development scene only.</summary>
        public static void WriteBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(TownPath, true) };
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DungeonPath) != null)
                scenes.Add(new EditorBuildSettingsScene(DungeonPath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            // Unity keeps the build settings in memory until the project is saved.
            AssetDatabase.SaveAssets();
        }

        static List<RoomTemplate> EnsureRoomTemplates()
        {
            Directory.CreateDirectory(RoomsFolder);
            foreach (var (name, layout) in PlaceholderRooms.All)
            {
                var path = $"{RoomsFolder}/{name}.asset";
                if (AssetDatabase.LoadAssetAtPath<RoomTemplate>(path) != null)
                    continue;
                var template = ScriptableObject.CreateInstance<RoomTemplate>();
                template.SetLayout(layout);
                AssetDatabase.CreateAsset(template, path);
            }
            AssetDatabase.SaveAssets();

            var templates = new List<RoomTemplate>();
            foreach (var guid in AssetDatabase.FindAssets("t:RoomTemplate", new[] { RoomsFolder }))
                templates.Add(AssetDatabase.LoadAssetAtPath<RoomTemplate>(AssetDatabase.GUIDToAssetPath(guid)));
            templates.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return templates;
        }

        static (Sprite closed, Sprite open) CreateChestArt()
        {
            // A small block like the walls: a wooden box, and the same box with a dark top once opened. The pivot sits on
            // the middle of its footprint, a quarter of the width up, so it stands on its cell.
            const int width = 64;
            const int height = 56;
            var pivot = new Vector2(0.5f, width * 0.25f / height);
            var closed = PlaceholderArt.ImportSprite(
                $"{EnvironmentArtFolder}/PlaceholderChestClosed.png",
                PlaceholderArt.WallBlock(width, height, new Color32(196, 150, 70, 255), new Color32(140, 96, 44, 255), new Color32(104, 70, 32, 255)),
                PixelsPerUnit, SpriteAlignment.Custom, FilterMode.Point, pivot);
            var open = PlaceholderArt.ImportSprite(
                $"{EnvironmentArtFolder}/PlaceholderChestOpen.png",
                PlaceholderArt.WallBlock(width, height, new Color32(40, 30, 22, 255), new Color32(140, 96, 44, 255), new Color32(104, 70, 32, 255)),
                PixelsPerUnit, SpriteAlignment.Custom, FilterMode.Point, pivot);
            return (closed, open);
        }

        static void SetArray(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
