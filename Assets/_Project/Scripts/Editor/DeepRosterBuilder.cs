using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// `Tools > ARPG > Add Deep Roster`: the deep levels' enemies and bosses (2026-10-05, from Docs/05's later acts):
    /// Drowned, Harpooner, Drowned Watchman, Skeleton Knight, Grave Priest, Hollowed, Void Wraith, Rift Caller, and the
    /// Tidewife, Saint Marrow and the First Watchman. Creates their definitions if missing, copied from the existing
    /// enemies with Docs/05's numbers (left alone after, so tuning survives a rerun), and lists them on the dungeon
    /// scene's level (<c>deepRoster</c>). Their art: ArtSource/tools/props/undead.py, then Bake Enemies.
    /// </summary>
    public static class DeepRosterBuilder
    {
        const string Folder = "Assets/_Project/Data/Enemies/";

        public static readonly string[] Names =
        {
            "Drowned", "Harpooner", "DrownedWatchman", "SkeletonKnight", "GravePriest", "Hollowed", "VoidWraith", "RiftCaller",
            "Tidewife", "SaintMarrow", "FirstWatchman",
        };

        [MenuItem("Tools/ARPG/Add Deep Roster")]
        public static void Build()
        {
            CreateDefinitions();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene(DungeonSceneBuilder.DungeonPath, OpenSceneMode.Single);
            var level = Object.FindAnyObjectByType<DungeonLevel>();
            if (level == null)
                return;
            var serialized = new SerializedObject(level);
            Assign(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ARPG] The deep roster is on the dungeon level.");
        }

        public static void Assign(SerializedObject level)
        {
            var list = level.FindProperty("deepRoster");
            var found = new List<EnemyDefinition>();
            foreach (var name in Names)
            {
                var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(Folder + name + ".asset");
                if (definition != null)
                    found.Add(definition);
            }
            list.arraySize = found.Count;
            for (var i = 0; i < found.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
        }

        public static void CreateDefinitions()
        {
            // Docs/05 act 2: husk-like, 1.1 life, hits slow 10 percent for 1 s.
            Copy("Swarmer", "Drowned", so =>
            {
                F(so, "lifeMultiplier", 1.1f);
                F(so, "damageMultiplier", 1f);
                F(so, "moveSpeed", 3.2f);
                F(so, "hitSlowFraction", 0.1f);
                F(so, "hitSlowSeconds", 1f);
            });
            // Act 2: a harpoon that slows 50 percent for 1.5 s; 0.9 life, 1.3 hit.
            Copy("BanditArcher", "Harpooner", so =>
            {
                F(so, "lifeMultiplier", 0.9f);
                F(so, "damageMultiplier", 1.3f);
                F(so, "projectileSpeed", 10f);
                F(so, "hitSlowFraction", 0.5f);
                F(so, "hitSlowSeconds", 1.5f);
            });
            // Act 2: 3.5 life, 2.4 hit, a slam forward (Docs/05's 3-unit cone, as a circle ahead of it).
            Copy("Ghoul", "DrownedWatchman", so =>
            {
                F(so, "lifeMultiplier", 3.5f);
                F(so, "damageMultiplier", 2.4f);
                F(so, "slamForward", 1.6f);
                F(so, "slamRadius", 1.8f);
                F(so, "attackWindupSeconds", 1f);
            });
            // Act 3: 3 life, 2 hit, a 2-unit cleave ahead of it; armoured.
            Copy("Ghoul", "SkeletonKnight", so =>
            {
                F(so, "lifeMultiplier", 3f);
                F(so, "damageMultiplier", 2f);
                F(so, "slamForward", 1.4f);
                F(so, "slamRadius", 1.6f);
                F(so, "attackWindupSeconds", 0.8f);
                F(so, "armor", 30f);
            });
            // Act 3: heals the most hurt ally within 6 for 25 percent every 6 s; 1.5 life, 0.8 hit.
            Copy("BanditArcher", "GravePriest", so =>
            {
                so.FindProperty("archetype").enumValueIndex = (int)EnemyArchetype.Support;
                F(so, "lifeMultiplier", 1.5f);
                F(so, "damageMultiplier", 0.8f);
                F(so, "preferredRange", 5f);
                F(so, "attackRange", 1f);
                F(so, "attackWindupSeconds", 0.45f);
                F(so, "attackRecoverSeconds", 0.9f);
                F(so, "moveSpeed", 2.8f);
                F(so, "auraDamageBonus", 0f);
                F(so, "healPercent", 0.25f);
                F(so, "healEverySeconds", 6f);
                F(so, "healRadius", 6f);
            });
            // Act 5: the Hollow's husk, faster (3.8); 1.1 life, 1.2 hit.
            Copy("Swarmer", "Hollowed", so =>
            {
                F(so, "lifeMultiplier", 1.1f);
                F(so, "damageMultiplier", 1.2f);
                F(so, "moveSpeed", 3.8f);
                F(so, "attackWindupSeconds", 0.3f);
            });
            // Act 5: a charger, 1.4 life, 1.8 hit (its blink left out: a lunge of 5 at 14 a second).
            Copy("AshWolf", "VoidWraith", so =>
            {
                F(so, "lifeMultiplier", 1.4f);
                F(so, "damageMultiplier", 1.8f);
                F(so, "moveSpeed", 4f);
                F(so, "lungeDistance", 5f);
                F(so, "lungeSpeed", 14f);
            });
            // Act 5: a void circle (radius 2) that pulses 3 times, 1 s apart, 0.9 s fill; 1.2 life, 1.4 hit.
            Copy("EmberAcolyte", "RiftCaller", so =>
            {
                F(so, "lifeMultiplier", 1.2f);
                F(so, "damageMultiplier", 1.4f);
                F(so, "slamRadius", 2f);
                F(so, "attackWindupSeconds", 0.9f);
                so.FindProperty("castPulses").intValue = 3;
                F(so, "castPulseGap", 1f);
            });
            // The bosses: the Warden's numbers (60 times a normal enemy's life), the last a little tougher.
            Copy("CinderWarden", "Tidewife", so => { });
            Copy("CinderWarden", "SaintMarrow", so => { });
            Copy("CinderWarden", "FirstWatchman", so => F(so, "lifeMultiplier", 70f));
            AssetDatabase.SaveAssets();
        }

        static void F(SerializedObject so, string property, float value) => so.FindProperty(property).floatValue = value;

        static void Copy(string from, string to, System.Action<SerializedObject> tune)
        {
            var path = Folder + to + ".asset";
            if (AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path) != null)
                return;
            AssetDatabase.CopyAsset(Folder + from + ".asset", path);
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            var so = new SerializedObject(definition);
            tune(so);
            so.FindProperty("spriteCharacter").stringValue = "";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }
    }
}
