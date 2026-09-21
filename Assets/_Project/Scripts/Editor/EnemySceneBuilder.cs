using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// Adds the placeholder swarmer enemy and the enemy manager to the sandbox scene. Enemies appear through
    /// <see cref="EnemyPack"/>s, see TestRoomBuilder. Run from Tools > ARPG > Add Enemies To Sandbox. Running it
    /// again rebuilds the prefab and the manager but keeps the tuned values of an existing enemy definition.
    /// </summary>
    public static class EnemySceneBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/Sandbox.unity";
        const string PrefabPath = "Assets/_Project/Prefabs/Enemies/Swarmer.prefab";
        const string DefinitionPath = "Assets/_Project/Data/Enemies/Swarmer.asset";
        const string CharacterArtFolder = "Assets/_Project/Art/Characters";

        const int PixelsPerUnit = 128;

        // Names of the scene objects this builder owns, so a rerun can replace them.
        const string ManagerObjectName = "Enemy Manager";

        // An earlier version added a ring spawner (survivor style). It is gone; a rerun removes its leftover object.
        const string RemovedSpawnerObjectName = "Enemy Spawner";

        [MenuItem("Tools/ARPG/Add Enemies To Sandbox")]
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
            PlayerSceneBuilder.RemoveExisting(scene, ManagerObjectName, RemovedSpawnerObjectName);

            LoadOrCreateDefinition();
            var prefab = BuildPrefab();

            var manager = new GameObject(ManagerObjectName, typeof(EnemyManager)).GetComponent<EnemyManager>();
            SetReference(manager, "prefab", prefab.GetComponent<EnemyController>());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ARPG] Swarmer enemy and enemy manager added to the sandbox scene.");
        }

        internal static EnemyDefinition LoadOrCreateDefinition()
        {
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(DefinitionPath);
            if (definition != null)
                return definition;

            definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            AssetDatabase.CreateAsset(definition, DefinitionPath);
            AssetDatabase.SaveAssets();
            return definition;
        }

        static GameObject BuildPrefab()
        {
            var bodySprite = PlaceholderArt.ImportSprite(
                $"{CharacterArtFolder}/PlaceholderSwarmer.png",
                PlaceholderArt.Capsule(80, 112, new Color32(190, 60, 60, 255)),
                PixelsPerUnit, SpriteAlignment.BottomCenter, FilterMode.Bilinear);

            // The player builder owns the shadow art; reuse it and only make one if it is missing.
            var shadowPath = $"{CharacterArtFolder}/PlaceholderShadow.png";
            var shadowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(shadowPath) ?? PlaceholderArt.ImportSprite(
                shadowPath, PlaceholderArt.ShadowEllipse(96, 48, 0.45f),
                PixelsPerUnit, SpriteAlignment.Center, FilterMode.Bilinear);

            var material = PlayerSceneBuilder.Default2DMaterial();

            // No collider or rigidbody: enemies move by their own steering and are found through the spatial hash.
            var root = new GameObject("Swarmer", typeof(EnemyController));
            PlayerSceneBuilder.SetLayer(root, GameLayers.Enemy);

            var shadow = PlayerSceneBuilder.AddSprite(root.transform, "Shadow", shadowSprite, GameSortingLayers.Decals, material);
            shadow.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            // Sorted by pivot (the feet) so enemies Y-sort against the player and each other.
            var body = PlayerSceneBuilder.AddSprite(root.transform, "Body", bodySprite, GameSortingLayers.Entities, material);
            body.spriteSortPoint = SpriteSortPoint.Pivot;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        internal static void SetReference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
