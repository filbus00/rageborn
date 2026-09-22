using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// Builds a minimal town by copying the sandbox and stripping it of everything hostile: no enemies, no combat,
    /// no walls. What is left is the ground, the player, the camera, the life bar and a stairway down to the dungeon
    /// (Docs/05-world-and-content.md: the town is a small safe scene). The NPCs come later.
    /// Run from Tools > ARPG > Create Town Scene, after Add Survival To Sandbox. Running it again rebuilds the town
    /// from the current sandbox.
    /// </summary>
    public static class TownSceneBuilder
    {
        const string SandboxPath = "Assets/_Project/Scenes/Sandbox.unity";
        const string TownPath = "Assets/_Project/Scenes/Town.unity";

        // The town stairway is five cells up the map from the player.
        static readonly Vector2Int StairsCell = new Vector2Int(5, 5);

        static readonly string[] RemovedObjects =
        {
            "Enemy Manager", "Test Room", "Player Combat", "Hit Stop", "Damage Numbers Canvas", "Corpse Spawner",
            "Stairs Up", "Loot System",
        };

        [MenuItem("Tools/ARPG/Create Town Scene")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SandboxPath) == null)
            {
                Debug.LogError($"[ARPG] {SandboxPath} not found. Run Create Sandbox Scene first.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // Copy the saved sandbox, so make sure the scene on disk is the one the user last built.
            var sandbox = EditorSceneManager.OpenScene(SandboxPath, OpenSceneMode.Single);
            if (Object.FindAnyObjectByType<PlayerHealth>() == null)
            {
                Debug.LogError("[ARPG] The sandbox has no player life. Run Add Survival To Sandbox first.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TownPath) != null)
            {
                // Overwrite the file in place. Deleting and copying the asset would give the town a new GUID every
                // time, which churns its .meta file and leaves the build settings pointing at the old one.
                File.Copy(SandboxPath, TownPath, true);
                AssetDatabase.ImportAsset(TownPath, ImportAssetOptions.ForceUpdate);
            }
            else if (!AssetDatabase.CopyAsset(SandboxPath, TownPath))
            {
                Debug.LogError($"[ARPG] Could not copy {SandboxPath} to {TownPath}.");
                return;
            }

            var town = EditorSceneManager.OpenScene(TownPath, OpenSceneMode.Single);
            PlayerSceneBuilder.RemoveExisting(town, RemovedObjects);

            var grid = Object.FindAnyObjectByType<Grid>();
            var walls = grid.transform.Find("Walls");
            if (walls != null)
                Object.DestroyImmediate(walls.gameObject);

            // The player starts in the middle of the town, the stairway down a short walk away.
            var player = Object.FindAnyObjectByType<PlayerController>();
            var start = IsoMath.GroundToWorld(IsoMath.CellToGround(Vector2Int.zero));
            player.transform.position = new Vector3(start.x, start.y, 0f);

            SurvivalSceneBuilder.AddStairs("Stairs Down", StairsCell, "Sandbox");

            EditorSceneManager.MarkSceneDirty(town);
            EditorSceneManager.SaveScene(town);

            // The town is where a new character starts, so it comes first.
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(TownPath, true),
                new EditorBuildSettingsScene(SandboxPath, true),
            };

            // Unity keeps the build settings in memory until the project is saved; write them so a clone has the Town.
            AssetDatabase.SaveAssets();
            Debug.Log($"[ARPG] Town scene created at {TownPath}. Build settings: Town, then Sandbox.");
        }
    }
}
