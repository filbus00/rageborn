using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// `Tools > ARPG > Add Deep Roster`: the deep levels' enemies and bosses (2026-10-05, from Docs/05's later acts):
    /// Drowned, Harpooner, Drowned Watchman, Skeleton Knight, Grave Priest, Hollowed, Void Wraith, Rift Caller, and the
    /// Tidewife, Saint Marrow, the First Watchman, the Ash Archivist, the Rift Mother, the Gaoler and the Hanged Magistrate. Creates their definitions if missing, copied from the existing
    /// enemies with Docs/05's numbers (left alone after, so tuning survives a rerun), and lists them on the dungeon
    /// scene's level (<c>deepRoster</c>). Their art: ArtSource/tools/props/undead.py, then Bake Enemies.
    /// </summary>
    public static class DeepRosterBuilder
    {
        const string Folder = "Assets/_Project/Data/Enemies/";

        public static readonly string[] Names = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(new[]
        {
            "Drowned", "Harpooner", "DrownedWatchman", "SkeletonKnight", "GravePriest", "Hollowed", "VoidWraith", "RiftCaller",
            "Tidewife", "SaintMarrow", "FirstWatchman",
            // The burnt library's boss and the caves' (2026-10-11).
            "AshArchivist", "RiftMother",
            // The prison's bosses (2026-10-12).
            "Gaoler", "HangedMagistrate",
            // Quest targets to break (2026-10-08): standing, no attack, drawn with a world sprite.
            "RiftHeart", "BonePyre",
        }, ConceptNames));

        /// <summary>The concept roster (2026-10-13): one definition per <see cref="PackMember"/>, by its name.</summary>
        static string[] ConceptNames => System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(
            System.Enum.GetNames(typeof(PackMember)), n => n != "None"));

        /// <summary>The concept roster's definitions (2026-10-13, the owner's enemies concept art): each copied from the old
        /// kind that fights the way its card says, then tuned (life, speed, damage). Claude's numbers, to review.</summary>
        static void CreateConceptDefinitions()
        {
            void Concept(string to, string from, float life, float speed, float damage = 1f, System.Action<SerializedObject> more = null) =>
                Copy(from, to, so =>
                {
                    F(so, "lifeMultiplier", life);
                    if (speed > 0f)
                        F(so, "moveSpeed", speed);
                    F(so, "damageMultiplier", damage);
                    more?.Invoke(so);
                });
            // The crypt.
            Concept("Zombie", "Swarmer", 1.2f, 2.8f);
            Concept("SkeletonWarrior", "Skeleton", 1.2f, 0f, 1.1f);
            Concept("Fallen", "Swarmer", 0.7f, 3.8f, 0.9f);
            Concept("QuillRat", "Swarmer", 0.6f, 4f, 0.8f, so => F(so, "bodyRadius", 0.25f));
            Concept("Ghoul", "Cutthroat", 1.1f, 3.8f, 1.1f);
            Concept("DarkCultist", "EmberAcolyte", 1f, 0f);
            Concept("BileFiend", "CarrionBloat", 2f, 0f, 1.1f);
            Concept("Wight", "GravePriest", 1.4f, 0f, 1.1f);
            // The prison.
            Concept("Mummy", "Drowned", 1.6f, 2.4f, 1.2f);
            Concept("BoneMage", "RiftCaller", 0.8f, 0f);
            Concept("FleshGolem", "Ghoul", 2.4f, 2.4f, 1.2f);
            Concept("Wraith", "BanditArcher", 1f, 0f, 1f, so => so.FindProperty("projectileColor").colorValue = new Color(0.45f, 0.7f, 1f, 1f));
            Concept("Ogre", "DrownedWatchman", 2.2f, 0f, 1.3f);
            Concept("Gargoyle", "AshWolf", 1.4f, 0f, 1.1f, so => F(so, "armor", 40f));
            Concept("Lich", "GravePriest", 2.2f, 0f, 1.2f);
            // The burnt library.
            Concept("Imp", "Swarmer", 0.6f, 4.2f, 0.9f, so => F(so, "bodyRadius", 0.25f));
            Concept("Succubus", "Harpooner", 1.1f, 0f, 1.1f, so => so.FindProperty("projectileColor").colorValue = new Color(0.8f, 0.35f, 0.9f, 1f));
            Concept("Harpy", "BanditArcher", 0.8f, 0f, 0.9f);
            Concept("StygianDemon", "Ghoul", 1.8f, 0f, 1.3f);
            Concept("HellHound", "AshWolf", 1.1f, 0f, 1.1f);
            // The caves.
            Concept("Arachnid", "Harpooner", 1.2f, 0f, 1.1f, so => so.FindProperty("projectileColor").colorValue = new Color(0.55f, 0.85f, 0.25f, 1f));
            Concept("SandWorm", "AshWolf", 2f, 3.2f, 1.3f);
            Concept("Giant", "Ghoul", 3f, 2.2f, 1.5f, so => F(so, "slamRadius", 2.5f));
            Concept("Nightmare", "AshWolf", 1.6f, 4.5f, 1.2f);
            Concept("BalrogLite", "PyreKeeper", 2.6f, 0f, 1.3f);
        }

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
            CreateConceptDefinitions();
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
            Copy("CinderWarden", "AshArchivist", so => F(so, "lifeMultiplier", 62f));
            Copy("CinderWarden", "RiftMother", so => F(so, "lifeMultiplier", 66f));
            Copy("CinderWarden", "Gaoler", so => F(so, "lifeMultiplier", 60f));
            Copy("CinderWarden", "HangedMagistrate", so => F(so, "lifeMultiplier", 60f));
            // Quest targets (2026-10-08): they stand and call others (BreakTargets); a pyre takes about eight normal
            // enemies' arrows, the rift heart fifteen.
            Copy("Swarmer", "BonePyre", so => Standing(so, 8f));
            Copy("Swarmer", "RiftHeart", so => Standing(so, 15f));
            SetBodySprite("BonePyre", "quest_bone_pyre");
            SetBodySprite("RiftHeart", "quest_rift_heart");
            AssetDatabase.SaveAssets();
        }

        static void F(SerializedObject so, string property, float value) => so.FindProperty(property).floatValue = value;

        static void Standing(SerializedObject so, float life)
        {
            F(so, "lifeMultiplier", life);
            F(so, "moveSpeed", 0f);
            F(so, "attackRange", 0f);
            F(so, "aggroRange", 0f);
            F(so, "damageMultiplier", 0f);
        }

        // The quest targets are drawn with their world sprite (Import World Art), set each run so new art shows.
        static void SetBodySprite(string definitionName, string worldArt)
        {
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(Folder + definitionName + ".asset");
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Resources/Art/World/" + worldArt + ".png");
            if (definition == null || sprite == null)
                return;
            var so = new SerializedObject(definition);
            so.FindProperty("bodySprite").objectReferenceValue = sprite;
            so.FindProperty("spriteCharacter").stringValue = "";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }

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
