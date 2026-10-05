using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// `Tools > ARPG > Add Act 1 Roster`: the rest of act 1's enemies (2026-10-05, Docs/05): the Ash Wolf, the Bandit
    /// Cutthroat, the Ember Acolyte, the Pyre Keeper and the Carrion Bloat. Creates their definitions if missing, copied
    /// from the husk, the archer and the ghoul with Docs/05's numbers (left alone after, so tuning survives a rerun), and
    /// puts them on the dungeon scene's level. Their art: ArtSource/tools/props/undead.py and wolf.py, then Bake Enemies.
    /// </summary>
    public static class Act1RosterBuilder
    {
        const string Folder = "Assets/_Project/Data/Enemies/";
        public const string AshWolfPath = Folder + "AshWolf.asset";
        public const string CutthroatPath = Folder + "Cutthroat.asset";
        public const string EmberAcolytePath = Folder + "EmberAcolyte.asset";
        public const string PyreKeeperPath = Folder + "PyreKeeper.asset";
        public const string CarrionBloatPath = Folder + "CarrionBloat.asset";

        [MenuItem("Tools/ARPG/Add Act 1 Roster")]
        public static void Build()
        {
            CreateDefinitions();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            AssignToDungeon();
        }

        public static void CreateDefinitions()
        {
            // Docs/05: a charger, 0.8 life and 1.0 hit of a husk's; lunges 4 units at a target 3 to 5 away (a 0.5 s
            // line), then bites normally.
            Copy(Folder + "Swarmer.asset", AshWolfPath, so =>
            {
                Archetype(so, EnemyArchetype.Charger);
                so.FindProperty("lifeMultiplier").floatValue = 0.8f;
                so.FindProperty("damageMultiplier").floatValue = 1f;
                so.FindProperty("moveSpeed").floatValue = 4.4f;
                so.FindProperty("aggroRange").floatValue = 8f;
                so.FindProperty("lungeDistance").floatValue = 4f;
                so.FindProperty("lungeMinRange").floatValue = 3f;
                so.FindProperty("lungeSpeed").floatValue = 12f;
                so.FindProperty("lungeWindupSeconds").floatValue = 0.5f;
                so.FindProperty("lungeCooldownSeconds").floatValue = 4f;
            });
            // Docs/05: fast (4.2), 0.9 life and 1.1 hit; strikes twice quickly, then backs off 2 units for 1 s.
            Copy(Folder + "Swarmer.asset", CutthroatPath, so =>
            {
                so.FindProperty("lifeMultiplier").floatValue = 0.9f;
                so.FindProperty("damageMultiplier").floatValue = 1.1f;
                so.FindProperty("moveSpeed").floatValue = 4.2f;
                so.FindProperty("attackWindupSeconds").floatValue = 0.3f;
                so.FindProperty("attackRecoverSeconds").floatValue = 1.2f;
                so.FindProperty("strikes").intValue = 2;
                so.FindProperty("strikeGapSeconds").floatValue = 0.25f;
                so.FindProperty("retreatDistance").floatValue = 2f;
                so.FindProperty("retreatSeconds").floatValue = 1f;
            });
            // Docs/05: keeps 6 away; every 5 s a fire circle (radius 1.5, a 1.0 s fill) on the player's spot that burns
            // 3 s after landing at 0.5 hit a second; 0.8 life, 1.3 hit.
            Copy(Folder + "BanditArcher.asset", EmberAcolytePath, so =>
            {
                Archetype(so, EnemyArchetype.Caster);
                so.FindProperty("lifeMultiplier").floatValue = 0.8f;
                so.FindProperty("damageMultiplier").floatValue = 1.3f;
                so.FindProperty("preferredRange").floatValue = 6f;
                so.FindProperty("attackRange").floatValue = 8f;
                so.FindProperty("attackWindupSeconds").floatValue = 1f;
                so.FindProperty("attackRecoverSeconds").floatValue = 4f;
                so.FindProperty("slamRadius").floatValue = 1.5f;
                so.FindProperty("burnSeconds").floatValue = 3f;
                so.FindProperty("burnHitsPerSecond").floatValue = 0.5f;
            });
            // Docs/05: stays behind its pack; allies within 5 deal 20 percent more damage (a visible ring); backs off
            // when the player comes within 3; 1.5 life, 0.8 hit.
            Copy(Folder + "BanditArcher.asset", PyreKeeperPath, so =>
            {
                Archetype(so, EnemyArchetype.Support);
                so.FindProperty("lifeMultiplier").floatValue = 1.5f;
                so.FindProperty("damageMultiplier").floatValue = 0.8f;
                so.FindProperty("preferredRange").floatValue = 5f;
                so.FindProperty("attackRange").floatValue = 1f;
                so.FindProperty("attackWindupSeconds").floatValue = 0.45f;
                so.FindProperty("attackRecoverSeconds").floatValue = 0.9f;
                so.FindProperty("moveSpeed").floatValue = 2.8f;
                so.FindProperty("auraRadius").floatValue = 5f;
                so.FindProperty("auraDamageBonus").floatValue = 0.2f;
            });
            // Docs/05: slow (1.6), 2 life, 1.8 hit; walks at the player and bursts when it reaches her or is killed: a
            // 2-unit circle filling for 0.8 s.
            Copy(Folder + "Ghoul.asset", CarrionBloatPath, so =>
            {
                so.FindProperty("lifeMultiplier").floatValue = 2f;
                so.FindProperty("damageMultiplier").floatValue = 1.8f;
                so.FindProperty("moveSpeed").floatValue = 1.6f;
                so.FindProperty("slamRadius").floatValue = 2f;
                so.FindProperty("attackWindupSeconds").floatValue = 0.8f;
                so.FindProperty("attackRange").floatValue = 1.2f;
                so.FindProperty("burstOnDeath").boolValue = true;
                so.FindProperty("diesOnAttack").boolValue = true;
                so.FindProperty("burstFillSeconds").floatValue = 0.8f;
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
            Assign(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ARPG] Ash Wolf, Cutthroat, Ember Acolyte, Pyre Keeper and Carrion Bloat on the dungeon level.");
        }

        /// <summary>Puts the five on a DungeonLevel's serialized fields (also used by Create Dungeon Scene).</summary>
        public static void Assign(SerializedObject level)
        {
            level.FindProperty("ashWolfEnemy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AshWolfPath);
            level.FindProperty("cutthroatEnemy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(CutthroatPath);
            level.FindProperty("emberAcolyteEnemy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(EmberAcolytePath);
            level.FindProperty("pyreKeeperEnemy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(PyreKeeperPath);
            level.FindProperty("carrionBloatEnemy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(CarrionBloatPath);
        }

        static void Archetype(SerializedObject so, EnemyArchetype archetype) =>
            so.FindProperty("archetype").enumValueIndex = (int)archetype;

        static void Copy(string from, string to, System.Action<SerializedObject> tune)
        {
            if (AssetDatabase.LoadAssetAtPath<EnemyDefinition>(to) != null)
                return;
            AssetDatabase.CopyAsset(from, to);
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(to);
            var so = new SerializedObject(definition);
            tune(so);
            so.FindProperty("spriteCharacter").stringValue = "";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }
    }
}
