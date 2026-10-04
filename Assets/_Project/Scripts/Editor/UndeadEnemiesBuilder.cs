using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// `Tools > ARPG > Add Undead Enemies`: act 1's skeleton and cultist (2026-10-04, from depth 4 by
    /// <see cref="PackComposition"/>). Creates their definitions if missing, copied from the husk and the bandit archer
    /// with their own tuning (numbers left alone after, so tuning survives a rerun), and puts them on the dungeon scene's
    /// level. Their art: ArtSource/tools/props/undead.py, then Bake Enemies (which wires the looks).
    /// </summary>
    public static class UndeadEnemiesBuilder
    {
        public const string SkeletonPath = "Assets/_Project/Data/Enemies/Skeleton.asset";
        public const string CultistPath = "Assets/_Project/Data/Enemies/Cultist.asset";

        [MenuItem("Tools/ARPG/Add Undead Enemies")]
        public static void Build()
        {
            CreateDefinitions();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            AssignToDungeon();
        }

        public static void CreateDefinitions()
        {
            // The skeleton: a husk that is slower, tougher and armoured, and hits a little harder with its blade.
            Copy("Assets/_Project/Data/Enemies/Swarmer.asset", SkeletonPath, so =>
            {
                so.FindProperty("lifeMultiplier").floatValue = 1.6f;
                so.FindProperty("armor").floatValue = 15f;
                so.FindProperty("moveSpeed").floatValue = 2.9f;
                so.FindProperty("damageMultiplier").floatValue = 1.25f;
                so.FindProperty("spriteCharacter").stringValue = "";
            });
            // The cultist: an archer whose fire bolt is slower and heavier, and who stands further back.
            Copy("Assets/_Project/Data/Enemies/BanditArcher.asset", CultistPath, so =>
            {
                so.FindProperty("lifeMultiplier").floatValue = 0.85f;
                so.FindProperty("damageMultiplier").floatValue = 1.6f;
                so.FindProperty("projectileSpeed").floatValue = 6.5f;
                so.FindProperty("preferredRange").floatValue = 6f;
                so.FindProperty("projectileColor").colorValue = new Color(1f, 0.45f, 0.15f, 1f);
                so.FindProperty("spriteCharacter").stringValue = "";
            });
            AssetDatabase.SaveAssets();
        }

        public static void AssignToDungeon()
        {
            var scene = EditorSceneManager.OpenScene(DungeonSceneBuilder.DungeonPath, OpenSceneMode.Single);
            var level = Object.FindAnyObjectByType<DungeonLevel>();
            if (level == null)
                return;
            var serialized = new SerializedObject(level);
            serialized.FindProperty("skeletonEnemy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(SkeletonPath);
            serialized.FindProperty("cultistEnemy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(CultistPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ARPG] Skeleton and cultist on the dungeon level.");
        }

        static void Copy(string from, string to, System.Action<SerializedObject> tune)
        {
            if (AssetDatabase.LoadAssetAtPath<EnemyDefinition>(to) != null)
                return;
            AssetDatabase.CopyAsset(from, to);
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(to);
            var so = new SerializedObject(definition);
            tune(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }
    }
}
