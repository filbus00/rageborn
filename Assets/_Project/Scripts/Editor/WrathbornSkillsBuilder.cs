using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// The Wrathborn's eight skills (Docs/02-classes-and-skills.md): Hew, Hurl Axe, Bull Rush and Ground Breaker (the M1
    /// slice), and Battle Roar, Rending Spin, Blood Frenzy and Skullsplitter (unlocking at 9, 12, 15 and 18, the owner's
    /// decision of 2026-09-27, Q4), as assets in Data/Skills with their placeholder art. A skill is created with the docs'
    /// numbers if missing and its numbers are left alone afterwards, so tuning survives a rerun; the art references, the
    /// trigger wording, the animation name and the two alternative triggers (Q15) are refreshed. Tools > ARPG > Create
    /// Wrathborn Skills also puts them on the Player Combat of the sandbox and dungeon scenes, in the class order an
    /// unchosen loadout fills from: Ground Breaker, Hurl Axe, Bull Rush, Hew, then the later four. Add Combat To Sandbox
    /// uses them too, so rebuilding the scene chain keeps them.
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
            WriteClassSkills(paths);
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

        // The same list in Resources, for scenes without combat (the town's loadout page).
        static void WriteClassSkills(string[] paths)
        {
            const string path = "Assets/_Project/Resources/" + ClassSkills.ResourcePath + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<ClassSkills>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ClassSkills>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.Set(Array.ConvertAll(paths, p => AssetDatabase.LoadAssetAtPath<SkillDefinition>(p)));
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
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
            var skullArt = ImportWedge("SweepSkullsplitter", 40f);

            // The later four (Docs/02's table; the "filled in" details are Docs/02's proposals, tuning).
            var roar = LoadOrCreate("BattleRoar", null, so =>
            {
                Set(so, SkillKind.Buff, unlock: 9, cost: 0f, cooldown: 15f, multiplier: 0f, range: 5f);
                so.FindProperty("displayName").stringValue = "Battle Roar";
                so.FindProperty("rageGain").floatValue = 40f;
                so.FindProperty("minEnemies").intValue = 3;
                so.FindProperty("maxRage").floatValue = 30f;
                so.FindProperty("durationSeconds").floatValue = 6f;
                so.FindProperty("buffDamage").floatValue = 0.2f;
                so.FindProperty("effectColor").colorValue = new Color(0.9f, 0.2f, 0.15f, 0.6f);
            });
            var spin = LoadOrCreate("RendingSpin", hewArt, so =>
            {
                Set(so, SkillKind.Channel, unlock: 12, cost: 40f, cooldown: 12f, multiplier: 0.9f, range: 4f);
                so.FindProperty("displayName").stringValue = "Rending Spin";
                so.FindProperty("minEnemies").intValue = 3;
                so.FindProperty("durationSeconds").floatValue = 2.5f;
                so.FindProperty("tickSeconds").floatValue = 0.3f;
                so.FindProperty("bleedMultiplier").floatValue = 0.4f;
                so.FindProperty("bleedSeconds").floatValue = 4f;
                so.FindProperty("effectColor").colorValue = new Color(0.75f, 0.15f, 0.15f, 0.55f);
            });
            var frenzy = LoadOrCreate("BloodFrenzy", null, so =>
            {
                Set(so, SkillKind.Buff, unlock: 15, cost: 20f, cooldown: 14f, multiplier: 0f, range: 6f);
                so.FindProperty("displayName").stringValue = "Blood Frenzy";
                so.FindProperty("minEnemies").intValue = 1;
                so.FindProperty("minMomentum").intValue = 3;
                so.FindProperty("durationSeconds").floatValue = 6f;
                so.FindProperty("buffAttackSpeed").floatValue = 0.3f;
                so.FindProperty("buffAttackSpeedPerMomentum").floatValue = 0.05f;
                so.FindProperty("effectColor").colorValue = new Color(0.65f, 0.05f, 0.1f, 0.6f);
            });
            var skull = LoadOrCreate("Skullsplitter", skullArt, so =>
            {
                Set(so, SkillKind.Execute, unlock: 18, cost: 25f, cooldown: 5f, multiplier: 3.5f, range: 2.5f);
                so.FindProperty("executeThreshold").floatValue = 0.25f;
                so.FindProperty("executeMultiplier").floatValue = 8f;
                so.FindProperty("windupSeconds").floatValue = 0.3f;
                so.FindProperty("effectColor").colorValue = new Color(1f, 0.3f, 0.2f, 0.7f);
            });

            // Every run: the trigger words, the animation and the two alternative triggers (Q15; which two is Claude's
            // choice of 2026-09-28, to review).
            Describe(hew, "2+ enemies in reach", "hew", SkillTrigger.Always, SkillTrigger.ElitePresent);
            Describe(axe, "An enemy 4 to 9 away", "hurl_axe", SkillTrigger.ElitePresent, SkillTrigger.Standing);
            Describe(rush, "Moving, an enemy 3 to 6 ahead", "bull_rush", SkillTrigger.ElitePresent, SkillTrigger.EnemiesThreePlus);
            Describe(breaker, "4+ enemies within 3.5", "ground_breaker", SkillTrigger.EnemiesThreePlus, SkillTrigger.ElitePresent);
            Describe(roar, "3+ enemies near, Rage below 30", "battle_roar", SkillTrigger.Always, SkillTrigger.LifeBelowHalf);
            Describe(spin, "Moving, 3+ enemies near", "rending_spin", SkillTrigger.EnemiesThreePlus, SkillTrigger.Standing);
            Describe(frenzy, "In combat, 3+ Momentum", "blood_frenzy", SkillTrigger.Always, SkillTrigger.ElitePresent);
            Describe(skull, "A target below 25%, or an elite", "skullsplitter", SkillTrigger.Always, SkillTrigger.ElitePresent);

            AssetDatabase.SaveAssets();
            // Class order, which fills an unchosen loadout: the slam first, so a big crowd gets it before Hew spends the
            // Rage (the user's choice, 2026-09-26); the later four only matter once the player chooses them.
            return new[] { Reload(breaker), Reload(axe), Reload(rush), Reload(hew), Reload(roar), Reload(spin), Reload(frenzy), Reload(skull) };
        }

        static void Describe(string path, string trigger, string animation, SkillTrigger first, SkillTrigger second)
        {
            var skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
            var so = new SerializedObject(skill);
            so.FindProperty("triggerText").stringValue = trigger;
            so.FindProperty("animationName").stringValue = animation;
            var alternatives = so.FindProperty("alternativeTriggers");
            alternatives.arraySize = 2;
            alternatives.GetArrayElementAtIndex(0).enumValueIndex = (int)first;
            alternatives.GetArrayElementAtIndex(1).enumValueIndex = (int)second;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skill);
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
