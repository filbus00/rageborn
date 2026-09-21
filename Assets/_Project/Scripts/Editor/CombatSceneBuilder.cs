using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// Adds automatic combat to the sandbox scene: the placeholder Cleave skill, the slash art and a Player Combat
    /// object. It is a separate object that finds the player at runtime, so rebuilding the player does not remove it.
    /// Run from Tools > ARPG > Add Combat To Sandbox, after Add Enemies To Sandbox. Running it again rebuilds the
    /// object and the art but keeps the tuned values of an existing skill.
    /// </summary>
    public static class CombatSceneBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/Sandbox.unity";
        const string EffectsArtFolder = "Assets/_Project/Art/Effects";
        const string CleavePath = "Assets/_Project/Data/Skills/Cleave.asset";

        const int PixelsPerUnit = 128;
        const int WedgeSize = 128;

        // Names of the scene objects this builder owns, so a rerun can replace them.
        const string CombatObjectName = "Player Combat";

        [MenuItem("Tools/ARPG/Add Combat To Sandbox")]
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
            if (Object.FindAnyObjectByType<EnemyManager>() == null || Object.FindAnyObjectByType<PlayerController>() == null)
            {
                Debug.LogError("[ARPG] The scene needs a Player and an Enemy Manager. Run Add Player And Camera To Sandbox and Add Enemies To Sandbox first.");
                return;
            }

            PlayerSceneBuilder.RemoveExisting(scene, CombatObjectName);

            var basicSprite = ImportWedge("SweepBasic", 120f);
            var cleaveSprite = ImportWedge("SweepCleave", 200f);
            var cleave = LoadOrCreateCleave(cleaveSprite);

            var combat = new GameObject(CombatObjectName, typeof(PlayerCombat)).GetComponent<PlayerCombat>();
            EnemySceneBuilder.SetReference(combat, "basicEffectSprite", basicSprite);

            var serialized = new SerializedObject(combat);
            var skills = serialized.FindProperty("skills");
            skills.arraySize = 1;
            skills.GetArrayElementAtIndex(0).objectReferenceValue = cleave;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ARPG] Automatic combat added to the sandbox scene.");
        }

        static Sprite ImportWedge(string name, float arcDegrees) =>
            PlaceholderArt.ImportSprite(
                $"{EffectsArtFolder}/{name}.png", PlaceholderArt.Wedge(WedgeSize, arcDegrees),
                PixelsPerUnit, SpriteAlignment.Center, FilterMode.Bilinear);

        static SkillDefinition LoadOrCreateCleave(Sprite effectSprite)
        {
            var skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(CleavePath);
            if (skill == null)
            {
                skill = ScriptableObject.CreateInstance<SkillDefinition>();
                AssetDatabase.CreateAsset(skill, CleavePath);
            }

            // The sprite reference is refreshed on every run; the numbers stay as tuned.
            EnemySceneBuilder.SetReference(skill, "effectSprite", effectSprite);
            EditorUtility.SetDirty(skill);
            AssetDatabase.SaveAssets();
            return skill;
        }
    }
}
