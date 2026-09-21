using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// Adds the placeholder swarmer enemy, the enemy manager and the spawner to the sandbox scene.
    /// Run from Tools > ARPG > Add Enemies To Sandbox. Running it again rebuilds the prefab and the scene objects
    /// but keeps the tuned values of an existing enemy definition.
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
        const string SpawnerObjectName = "Enemy Spawner";

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
            PlayerSceneBuilder.RemoveExisting(scene, ManagerObjectName, SpawnerObjectName);

            var definition = LoadOrCreateDefinition();
            var prefab = BuildPrefab();

            var manager = new GameObject(ManagerObjectName, typeof(EnemyManager)).GetComponent<EnemyManager>();
            SetReference(manager, "prefab", prefab.GetComponent<EnemyController>());

            var spawner = new GameObject(SpawnerObjectName, typeof(EnemySpawner)).GetComponent<EnemySpawner>();
            SetReference(spawner, "manager", manager);
            SetReference(spawner, "definition", definition);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ARPG] Swarmer enemy, enemy manager and spawner added to the sandbox scene.");
        }

        static EnemyDefinition LoadOrCreateDefinition()
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

        static void SetReference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
