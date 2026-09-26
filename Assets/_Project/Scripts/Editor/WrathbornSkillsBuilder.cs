using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// The Wrathborn's first four skills (Docs/02-classes-and-skills.md, M1 slice): Hew, Hurl Axe, Bull Rush and Ground
    /// Breaker, as assets in Data/Skills, with their placeholder art. A skill is created with the docs' numbers if missing
    /// and its numbers are left alone afterwards, so tuning survives a rerun; the art references are refreshed.
    /// Tools > ARPG > Create Wrathborn Skills also puts them on the Player Combat of the sandbox and dungeon scenes, in
    /// slot order: Ground Breaker, Hurl Axe, Bull Rush, Hew. Add Combat To Sandbox uses them too, so rebuilding the scene chain keeps them.
    /// </summary>
    public static class WrathbornSkillsBuilder
    {
        const string SkillsFolder = "Assets/_Project/Data/Skills";
        const string EffectsArtFolder = "Assets/_Project/Art/Effects";
        const int PixelsPerUnit = 128;

        static readonly string[] Scenes =
        {
            "Assets/_Project/Scenes/Sandbox.unity",
            DungeonSceneBuilder.DungeonPath,
        };

        [MenuItem("Tools/ARPG/Create Wrathborn Skills")]
        public static void BuildAndAssign()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // Kept as paths: opening a scene in single mode unloads assets nothing references yet, which left every
            // skill loaded before it a dead reference (all four slots came out empty the first time).
            var paths = Array.ConvertAll(LoadOrCreateAll(), AssetDatabase.GetAssetPath);
            var skills = new SkillDefinition[paths.Length];
            foreach (var path in Scenes)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                    continue;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var combat = UnityEngine.Object.FindAnyObjectByType<PlayerCombat>();
                if (combat == null)
                    continue;
                for (var i = 0; i < paths.Length; i++)
                    skills[i] = AssetDatabase.LoadAssetAtPath<SkillDefinition>(paths[i]);
                Assign(combat, skills);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log($"[ARPG] Wrathborn skills created and assigned: {string.Join(", ", Array.ConvertAll(skills, s => s.DisplayName))}.");
        }

        /// <summary>Puts the skills on a Player Combat, slot 1 first.</summary>
        public static void Assign(PlayerCombat combat, SkillDefinition[] skills)
        {
            var serialized = new SerializedObject(combat);
            var property = serialized.FindProperty("skills");
            property.arraySize = skills.Length;
            for (var i = 0; i < skills.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = skills[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>The four skills in slot order. Art first: an import in between left fresh asset references dead
        /// before (see the dungeon builder), so each skill is loaded again by path at the end.</summary>
        public static SkillDefinition[] LoadOrCreateAll()
        {
            var hewArt = ImportWedge("SweepHew", 180f);
            var rushArt = ImportWedge("SweepRush", 70f);
            var axeArt = PlaceholderArt.ImportSprite(
                $"{EffectsArtFolder}/ThrownAxe.png", PlaceholderArt.Diamond(56, 56),
                PixelsPerUnit, SpriteAlignment.Center, FilterMode.Bilinear);

            var hew = LoadOrCreate("Hew", hewArt, so =>
            {
                Set(so, SkillKind.Sweep, unlock: 1, cost: 20f, cooldown: 3f, multiplier: 1.7f, range: 2f);
                so.FindProperty("arcDegrees").floatValue = 180f;
                so.FindProperty("minEnemies").intValue = 2;
                so.FindProperty("effectColor").colorValue = new Color(1f, 0.45f, 0.25f, 0.6f);
            });
            var axe = LoadOrCreate("HurlAxe", axeArt, so =>
            {
                Set(so, SkillKind.Projectile, unlock: 2, cost: 10f, cooldown: 4f, multiplier: 2f, range: 9f);
                so.FindProperty("displayName").stringValue = "Hurl Axe";
                so.FindProperty("minRange").floatValue = 4f;
                so.FindProperty("speed").floatValue = 14f;
                so.FindProperty("effectColor").colorValue = new Color(0.8f, 0.82f, 0.86f, 1f);
            });
            var rush = LoadOrCreate("BullRush", rushArt, so =>
            {
                Set(so, SkillKind.Charge, unlock: 4, cost: 0f, cooldown: 6f, multiplier: 1.5f, range: 6f);
                so.FindProperty("rageGain").floatValue = 15f; // the opener: it builds Rage (the user's choice, 2026-09-26)
                so.FindProperty("displayName").stringValue = "Bull Rush";
                so.FindProperty("minRange").floatValue = 3f;
                so.FindProperty("speed").floatValue = 16f;
                so.FindProperty("effectStrength").floatValue = 1.5f; // knockback distance
                so.FindProperty("effectColor").colorValue = new Color(1f, 0.85f, 0.5f, 0.5f);
            });
            var breaker = LoadOrCreate("GroundBreaker", null, so =>
            {
                Set(so, SkillKind.Slam, unlock: 6, cost: 35f, cooldown: 8f, multiplier: 2.8f, range: 3.5f);
                so.FindProperty("displayName").stringValue = "Ground Breaker";
                so.FindProperty("minEnemies").intValue = 4;
                so.FindProperty("effectStrength").floatValue = 0.7f; // slowed to 70 percent speed
                so.FindProperty("effectSeconds").floatValue = 2f;
                so.FindProperty("effectColor").colorValue = new Color(0.9f, 0.55f, 0.2f, 0.7f);
            });

            AssetDatabase.SaveAssets();
            // Slot order, which is cast priority: the slam first, so a big crowd gets it before Hew spends the Rage (the
            // user's choice, 2026-09-26).
            return new[] { Reload(breaker), Reload(axe), Reload(rush), Reload(hew) };
        }

        static void Set(SerializedObject so, SkillKind kind, int unlock, float cost, float cooldown, float multiplier, float range)
        {
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.FindProperty("unlockLevel").intValue = unlock;
            so.FindProperty("rageCost").floatValue = cost;
            so.FindProperty("cooldownSeconds").floatValue = cooldown;
            so.FindProperty("damageMultiplier").floatValue = multiplier;
            so.FindProperty("range").floatValue = range;
        }

        static string PathOf(string name) => $"{SkillsFolder}/{name}.asset";

        static string LoadOrCreate(string name, Sprite art, Action<SerializedObject> firstTime)
        {
            var path = PathOf(name);
            var skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
            var created = skill == null;
            if (created)
            {
                skill = ScriptableObject.CreateInstance<SkillDefinition>();
                AssetDatabase.CreateAsset(skill, path);
            }

            var so = new SerializedObject(skill);
            if (created)
            {
                so.FindProperty("displayName").stringValue = name;
                firstTime(so);
            }
            so.FindProperty("effectSprite").objectReferenceValue = art;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skill);
            return path;
        }

        static SkillDefinition Reload(string path) => AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);

        static Sprite ImportWedge(string name, float arcDegrees) =>
            PlaceholderArt.ImportSprite(
                $"{EffectsArtFolder}/{name}.png", PlaceholderArt.Wedge(128, arcDegrees),
                PixelsPerUnit, SpriteAlignment.Center, FilterMode.Bilinear);
    }
}
