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
        const string CharacterArtFolder = "Assets/_Project/Art/Characters";
        public const string BossDefinitionPath = "Assets/_Project/Data/Enemies/CinderWarden.asset";
        public const string GhoulDefinitionPath = "Assets/_Project/Data/Enemies/Ghoul.asset";
        public const string ArcherDefinitionPath = "Assets/_Project/Data/Enemies/BanditArcher.asset";
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
            LoadOrCreateBoss();
            LoadOrCreateGhoul();
            LoadOrCreateArcher();

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
            serialized.FindProperty("lowWallTile").objectReferenceValue = CreateLowWallTile();
            SetArray(serialized.FindProperty("rooms"), rooms.ToArray());
            serialized.FindProperty("normalEnemy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Swarmer.asset");
            serialized.FindProperty("championEnemy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/SwarmerChampion.asset");
            serialized.FindProperty("eliteEnemy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/SwarmerElite.asset");
            // Loaded again by path, not kept from creation: an import in between can leave a fresh asset reference dead.
            serialized.FindProperty("bossEnemy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(BossDefinitionPath);
            serialized.FindProperty("ghoulEnemy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(GhoulDefinitionPath);
            serialized.FindProperty("archerEnemy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(ArcherDefinitionPath);
            UndeadEnemiesBuilder.CreateDefinitions();
            serialized.FindProperty("skeletonEnemy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(UndeadEnemiesBuilder.SkeletonPath);
            serialized.FindProperty("cultistEnemy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(UndeadEnemiesBuilder.CultistPath);
            Act1RosterBuilder.CreateDefinitions();
            Act1RosterBuilder.Assign(serialized);
            DeepRosterBuilder.CreateDefinitions();
            DeepRosterBuilder.Assign(serialized);
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

        /// <summary>
        /// The Cinder Warden's definition, created with starting tuning if missing and left alone afterwards, like the
        /// swarmer variants. 30 times a normal enemy's life: about 9,700 at level 7, which the balance report's character
        /// arriving at level 6 with typical gear (about 140 damage per second on one target) kills in about 70 seconds,
        /// inside the docs' 60 to 120 second target for a zone boss (Docs/04-progression-and-economy.md). It was 15 until
        /// act 1's enemy levels were flattened (2026-09-26), which put the fight at 35 seconds. Its attacks live in
        /// <see cref="CinderWardenFight"/>, not here.
        /// </summary>
        static EnemyDefinition LoadOrCreateBoss()
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(BossDefinitionPath);
            if (existing != null)
                return existing;

            var sprite = PlaceholderArt.ImportSprite(
                $"{CharacterArtFolder}/PlaceholderCinderWarden.png",
                PlaceholderArt.Capsule(120, 170, new Color32(222, 104, 38, 255)),
                PixelsPerUnit, SpriteAlignment.BottomCenter, FilterMode.Bilinear);
            return EnemySceneBuilder.LoadOrCreateVariant(
                BossDefinitionPath, EnemyRank.Boss, lifeMultiplier: 60f, damageMultiplier: 1f, bodyRadius: 0.9f,
                aggroRange: 12f, visualScale: 1.6f, bodySprite: sprite);
        }

        /// <summary>Creates the Ghoul and Bandit Archer definitions if missing, without rebuilding the dungeon scene.</summary>
        [MenuItem("Tools/ARPG/Create Act 1 Enemies")]
        public static void CreateActOneEnemies()
        {
            LoadOrCreateGhoul();
            LoadOrCreateArcher();
        }

        /// <summary>
        /// Act 1's brute (Docs/05-world-and-content.md: Ghoul, slow slam, 2 unit radius), created with starting tuning if
        /// missing and left alone afterwards. Three times a husk's life and a little over twice its hit, slow enough to
        /// walk away from; the slam fills for 0.9 s, inside the docs' 0.6 to 1.2. Sickly green (Docs/05: cold blue and
        /// sickly green for enemies).
        /// </summary>
        static EnemyDefinition LoadOrCreateGhoul()
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(GhoulDefinitionPath);
            if (existing != null)
                return existing;

            var sprite = PlaceholderArt.ImportSprite(
                $"{CharacterArtFolder}/PlaceholderGhoul.png",
                PlaceholderArt.Capsule(104, 120, new Color32(112, 150, 70, 255)),
                PixelsPerUnit, SpriteAlignment.BottomCenter, FilterMode.Bilinear);
            return EnemySceneBuilder.LoadOrCreateVariant(
                GhoulDefinitionPath, EnemyRank.Normal, lifeMultiplier: 3f, damageMultiplier: 2.2f, bodyRadius: 0.45f,
                aggroRange: 7f, visualScale: 1.2f, bodySprite: sprite, configure: so =>
                {
                    so.FindProperty("archetype").enumValueIndex = (int)EnemyArchetype.Brute;
                    so.FindProperty("slamRadius").floatValue = 2f;
                    so.FindProperty("attackRange").floatValue = 1.6f;
                    so.FindProperty("attackWindupSeconds").floatValue = 0.9f;
                    so.FindProperty("attackRecoverSeconds").floatValue = 1.2f;
                    so.FindProperty("moveSpeed").floatValue = 2.2f;
                    so.FindProperty("stopDistance").floatValue = 1.1f;
                    so.FindProperty("separationRadius").floatValue = 1.2f;
                });
        }

        /// <summary>
        /// Act 1's archer (Docs/05: Bandit Archer, line telegraph; Docs/01: keeps distance). Frailer than a husk and a
        /// bit harder hitting; aims for 0.4 s (the docs' projectile line), then looses an arrow at 9 units per second from
        /// up to 7.5 away, and backs off to 5 when the player closes in. Cold blue.
        /// </summary>
        static EnemyDefinition LoadOrCreateArcher()
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(ArcherDefinitionPath);
            if (existing != null)
                return existing;

            var sprite = PlaceholderArt.ImportSprite(
                $"{CharacterArtFolder}/PlaceholderBanditArcher.png",
                PlaceholderArt.Capsule(64, 116, new Color32(84, 120, 190, 255)),
                PixelsPerUnit, SpriteAlignment.BottomCenter, FilterMode.Bilinear);
            return EnemySceneBuilder.LoadOrCreateVariant(
                ArcherDefinitionPath, EnemyRank.Normal, lifeMultiplier: 0.7f, damageMultiplier: 1.2f, bodyRadius: 0.3f,
                aggroRange: 7f, visualScale: 1f, bodySprite: sprite, configure: so =>
                {
                    so.FindProperty("archetype").enumValueIndex = (int)EnemyArchetype.Archer;
                    so.FindProperty("attackRange").floatValue = 7.5f;
                    so.FindProperty("attackWindupSeconds").floatValue = 0.4f;
                    so.FindProperty("attackRecoverSeconds").floatValue = 1.3f;
                    so.FindProperty("preferredRange").floatValue = 5f;
                    so.FindProperty("projectileSpeed").floatValue = 9f;
                    so.FindProperty("moveSpeed").floatValue = 3f;
                });
        }

        const string LowWallTilePath = "Assets/_Project/Art/Tilesets/PlaceholderWallLow.asset";

        /// <summary>
        /// Adds the cut-down wall to the existing dungeon scene without rebuilding it: creates the tile and sets
        /// <c>DungeonLevel.lowWallTile</c>. Idempotent.
        /// </summary>
        [MenuItem("Tools/ARPG/Add Low Walls To Dungeon")]
        public static void AddLowWalls()
        {
            CreateLowWallTile();
            var scene = EditorSceneManager.OpenScene(DungeonPath, OpenSceneMode.Single);
            // Loaded again by path: opening a scene unloads assets nothing references yet, and the fresh tile came back
            // null the first time.
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(LowWallTilePath);
            var level = Object.FindAnyObjectByType<DungeonLevel>();
            if (level == null)
            {
                Debug.LogError("[ARPG] The dungeon scene has no Dungeon Level.");
                return;
            }
            var serialized = new SerializedObject(level);
            serialized.FindProperty("lowWallTile").objectReferenceValue = tile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ARPG] Low walls added to the dungeon scene.");
        }

        // The placeholder wall block (128 x 128, 64 px of wall above its footprint) cut down to a 16 px stub, same colors,
        // same footprint and pivot, so it sits in the wall line and still reads as wall.
        static Tile CreateLowWallTile()
        {
            const int width = 128;
            const int height = 80;
            var sprite = PlaceholderArt.ImportSprite(
                $"{Path.GetDirectoryName(LowWallTilePath)}/PlaceholderWallLow.png",
                PlaceholderArt.WallBlock(width, height, new Color32(112, 112, 122, 255), new Color32(84, 84, 96, 255), new Color32(58, 58, 70, 255)),
                PixelsPerUnit, SpriteAlignment.Custom, FilterMode.Point, new Vector2(0.5f, width * 0.25f / height));
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(LowWallTilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, LowWallTilePath);
            }
            tile.sprite = sprite;
            // The collider is the whole cell, as for the full wall: a low wall blocks just the same.
            tile.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile);
            AssetDatabase.SaveAssets();
            return tile;
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
