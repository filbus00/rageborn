using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// The Wild Arrow's four skills (Docs/02-classes-and-skills.md, the owner's list of 2026-09-30): Split Arrow, Pierce
    /// Arrow, Homing Arrow and Explosive Arrow, unlocking at 1, 2, 4 and 6, as assets in Data/Skills. Like the retired
    /// Wrathborn's builder, a skill is created with the docs' numbers if missing and its numbers are left alone after,
    /// so tuning survives a rerun; its trigger words, animation and alternative triggers are refreshed. Tools > ARPG >
    /// Create Wild Arrow Skills puts them on the Player Combat of the sandbox and dungeon scenes and in Resources
    /// (<see cref="ClassSkills"/>), in the class order an unchosen loadout fills from, which is cast priority:
    /// Explosive, Homing, Pierce, Split (Docs/02's proposal: the area skill first on a crowd).
    /// </summary>
    public static class WildArrowSkillsBuilder
    {
        const string SkillsFolder = "Assets/_Project/Data/Skills";

        static readonly string[] Scenes =
        {
            "Assets/_Project/Scenes/Sandbox.unity",
            DungeonSceneBuilder.DungeonPath,
        };

        [MenuItem("Tools/ARPG/Create Wild Arrow Skills")]
        public static void BuildAndAssign()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            AssignToScenes();
        }

        /// <summary>
        /// Everything the bows-only change needs from the editor after its code is pulled, in one go: the four skills on
        /// every combat scene and in Resources, then the Wild Arrow's stand-in bake (a few minutes; see
        /// <see cref="WildArrowBakeSetup"/>). The Pet Vendor and pets need nothing: they are placed from code.
        /// </summary>
        [MenuItem("Tools/ARPG/Apply Bows Change (skills and stand-in bake)")]
        public static void ApplyBowsChange()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            AssignToScenes();
            WildArrowBakeSetup.Bake();
        }

        /// <summary>Creates the skills and puts them on every combat scene and in Resources, without asking to save the
        /// open scene first (the caller has).</summary>
        public static void AssignToScenes()
        {
            // Kept as paths: opening a scene in single mode unloads assets nothing references yet.
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
                WrathbornSkillsBuilder.Assign(combat, skills);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("[ARPG] Wild Arrow skills created and assigned: Explosive, Homing, Pierce and Split Arrow.");
        }

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

        /// <summary>The four skills in class order (cast priority): Explosive, Homing, Pierce, Split.</summary>
        public static SkillDefinition[] LoadOrCreateAll()
        {
            // Docs/02's numbers; all tuning.
            var split = LoadOrCreate("SplitArrow", so =>
            {
                Set(so, SkillKind.Volley, "Split Arrow", unlock: 1, cost: 12f, cooldown: 3f, multiplier: 0.9f, range: 7.5f, speed: 16f);
                so.FindProperty("minEnemies").intValue = 2;
                so.FindProperty("projectileCount").intValue = 5;
                so.FindProperty("spreadDegrees").floatValue = 40f;
                so.FindProperty("effectColor").colorValue = new Color(0.7f, 0.88f, 1f, 0.9f);
            });
            var pierce = LoadOrCreate("PierceArrow", so =>
            {
                Set(so, SkillKind.PierceShot, "Pierce Arrow", unlock: 2, cost: 15f, cooldown: 4f, multiplier: 2f, range: 9f, speed: 20f);
                so.FindProperty("minRange").floatValue = 4f;
                so.FindProperty("effectColor").colorValue = new Color(1f, 0.22f, 0.18f, 0.95f);
            });
            var homing = LoadOrCreate("HomingArrow", so =>
            {
                Set(so, SkillKind.HomingShot, "Homing Arrow", unlock: 4, cost: 15f, cooldown: 5f, multiplier: 1.2f, range: 7.5f, speed: 12f);
                so.FindProperty("projectileCount").intValue = 3;
                so.FindProperty("spreadDegrees").floatValue = 70f;
                so.FindProperty("turnDegreesPerSecond").floatValue = 420f;
                so.FindProperty("effectColor").colorValue = new Color(0.72f, 0.45f, 1f, 0.9f);
            });
            var explosive = LoadOrCreate("ExplosiveArrow", so =>
            {
                Set(so, SkillKind.ExplosiveShot, "Explosive Arrow", unlock: 6, cost: 30f, cooldown: 7f, multiplier: 2.2f, range: 7.5f, speed: 15f);
                so.FindProperty("minEnemies").intValue = 3;
                so.FindProperty("burstRadius").floatValue = 2.5f;
                so.FindProperty("effectColor").colorValue = new Color(1f, 0.55f, 0.15f, 0.95f);
            });

            // The held skills (the owner, 2026-10-04: Knockback Shot, Barrage, Kill Shot, Hunter's Breath and Wild Frenzy).
            var knockback = LoadOrCreate("KnockbackShot", so =>
            {
                Set(so, SkillKind.KnockbackShot, "Knockback Shot", unlock: 1, cost: 0f, cooldown: 6f, multiplier: 2f, range: 2.5f, speed: 22f);
                so.FindProperty("rageGain").floatValue = 15f;
                so.FindProperty("effectStrength").floatValue = 3f;
                so.FindProperty("burstRadius").floatValue = 1.5f;
                so.FindProperty("effectColor").colorValue = new Color(0.85f, 0.85f, 0.75f, 0.95f);
            });
            var barrage = LoadOrCreate("Barrage", so =>
            {
                Set(so, SkillKind.Barrage, "Barrage", unlock: 1, cost: 20f, cooldown: 7f, multiplier: 0.8f, range: 7f, speed: 17f);
                so.FindProperty("minEnemies").intValue = 2;
                so.FindProperty("projectileCount").intValue = 8;
                so.FindProperty("durationSeconds").floatValue = 1.2f;
                so.FindProperty("spreadDegrees").floatValue = 12f;
                so.FindProperty("effectColor").colorValue = new Color(0.55f, 1f, 0.6f, 0.9f);
            });
            var killShot = LoadOrCreate("KillShot", so =>
            {
                Set(so, SkillKind.KillShot, "Kill Shot", unlock: 1, cost: 15f, cooldown: 4f, multiplier: 2f, range: 8f, speed: 26f);
                so.FindProperty("executeThreshold").floatValue = 0.3f;
                so.FindProperty("executeMultiplier").floatValue = 6f;
                so.FindProperty("effectColor").colorValue = new Color(1f, 0.85f, 0.3f, 0.95f);
            });
            var breath = LoadOrCreate("HuntersBreath", so =>
            {
                Set(so, SkillKind.Buff, "Hunter's Breath", unlock: 1, cost: 0f, cooldown: 15f, multiplier: 0f, range: 6f, speed: 0f);
                so.FindProperty("rageGain").floatValue = 60f;
                so.FindProperty("maxRage").floatValue = 30f;
                so.FindProperty("effectColor").colorValue = new Color(0.45f, 0.75f, 1f, 0.9f);
            });
            var frenzy = LoadOrCreate("WildFrenzy", so =>
            {
                Set(so, SkillKind.Buff, "Wild Frenzy", unlock: 1, cost: 10f, cooldown: 12f, multiplier: 0f, range: 6f, speed: 0f);
                so.FindProperty("minMomentum").intValue = 3;
                so.FindProperty("durationSeconds").floatValue = 8f;
                so.FindProperty("buffAttackSpeed").floatValue = 0.25f;
                so.FindProperty("buffAttackSpeedPerMomentum").floatValue = 0.05f;
                so.FindProperty("effectColor").colorValue = new Color(1f, 0.6f, 0.25f, 0.9f);
            });

            // Every run: the trigger words, the animation and the two alternative triggers (Docs/02, proposed). Her own
            // bake (2026-10-03) has the longbow pack's overdraw for Pierce Arrow and its recoil for Explosive Arrow; the
            // rest use the draw. A sheet the bake lacks (the stand-in's) falls back to the draw (PlayerSpriteAnimator).
            Describe(split, "2+ enemies ahead", "attack", SkillTrigger.Always, SkillTrigger.ElitePresent);
            Describe(pierce, "An enemy 4 to 9 away", "overdraw", SkillTrigger.ElitePresent, SkillTrigger.Standing);
            Describe(homing, "An enemy in reach, in sight", "attack", SkillTrigger.ElitePresent, SkillTrigger.EnemiesThreePlus);
            Describe(explosive, "3+ enemies around a target", "recoil", SkillTrigger.Always, SkillTrigger.ElitePresent);
            Describe(knockback, "An enemy within 2.5", "recoil", SkillTrigger.LifeBelowHalf, SkillTrigger.EnemiesThreePlus);
            Describe(barrage, "2+ enemies in reach, moving", "attack", SkillTrigger.Always, SkillTrigger.EnemiesThreePlus);
            Describe(killShot, "An enemy below 30% life", "overdraw", SkillTrigger.ElitePresent, SkillTrigger.Always);
            Describe(breath, "Focus below 30 in a fight", "", SkillTrigger.LifeBelowHalf, SkillTrigger.ElitePresent);
            Describe(frenzy, "3+ Momentum in a fight", "", SkillTrigger.Moving, SkillTrigger.ElitePresent);

            // Every skill unlocks at level 1 (the owner, 2026-10-04: "All at once"), the first four too.
            foreach (var path in new[] { split, pierce, homing, explosive, knockback, barrage, killShot, breath, frenzy })
            {
                var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<SkillDefinition>(path));
                so.FindProperty("unlockLevel").intValue = 1;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.SaveAssets();
            // The first four keep the loadout an unchosen character starts with; the new five follow.
            return new[]
            {
                Reload(explosive), Reload(homing), Reload(pierce), Reload(split),
                Reload(killShot), Reload(knockback), Reload(barrage), Reload(frenzy), Reload(breath),
            };
        }

        static void Set(SerializedObject so, SkillKind kind, string displayName, int unlock, float cost, float cooldown, float multiplier, float range, float speed)
        {
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.FindProperty("unlockLevel").intValue = unlock;
            so.FindProperty("rageCost").floatValue = cost;
            so.FindProperty("cooldownSeconds").floatValue = cooldown;
            so.FindProperty("damageMultiplier").floatValue = multiplier;
            so.FindProperty("range").floatValue = range;
            so.FindProperty("speed").floatValue = speed;
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

        static string LoadOrCreate(string name, Action<SerializedObject> firstTime)
        {
            var path = $"{SkillsFolder}/{name}.asset";
            var skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
            if (skill == null)
            {
                skill = ScriptableObject.CreateInstance<SkillDefinition>();
                AssetDatabase.CreateAsset(skill, path);
                var so = new SerializedObject(skill);
                firstTime(so);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(skill);
            }
            return path;
        }

        static SkillDefinition Reload(string path) => AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
    }
}
