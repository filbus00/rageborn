using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// Builds a minimal town by copying the sandbox and stripping it of everything hostile: no enemies, no combat,
    /// no walls. What is left is the ground, the player, the camera, the life bar and a stairway down to the first
    /// dungeon level (Docs/05-world-and-content.md: the town is a small safe scene), plus the smith who opens the Forge.
    /// The Stash, Trainer and Waystone NPCs come later.
    /// Run from Tools > ARPG > Create Town Scene, after Add Inventory Screen To Sandbox, then run Create Dungeon Scene.
    /// Running it again rebuilds the town from the current sandbox. Add Forge To Town adds or rebuilds just the smith.
    /// </summary>
    public static class TownSceneBuilder
    {
        const string SandboxPath = "Assets/_Project/Scenes/Sandbox.unity";
        const string TownPath = "Assets/_Project/Scenes/Town.unity";

        // The town stairway is five cells up the map from the player.
        static readonly Vector2Int StairsCell = new Vector2Int(5, 5);

        // The smith stands up and to the left of the start, the other way from the stairs.
        static readonly Vector2Int ForgeCell = new Vector2Int(-4, 4);
        const string ForgeObjectName = "Forge Smith";
        const string CharacterArtFolder = "Assets/_Project/Art/Characters";

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

            // Down into the first level of the dungeon (Docs/05-world-and-content.md: the dungeon entrance is a
            // stairway in the town).
            var stairs = SurvivalSceneBuilder.AddStairs("Stairs Down", StairsCell, "Dungeon");
            stairs.GetComponent<SceneExit>().Configure("Dungeon", 1, Arrival.FromAbove);
            EditorUtility.SetDirty(stairs.GetComponent<SceneExit>());

            PlaceForge();

            EditorSceneManager.MarkSceneDirty(town);
            EditorSceneManager.SaveScene(town);

            // The town is where a new character starts, so it comes first.
            DungeonSceneBuilder.WriteBuildSettings();
            Debug.Log($"[ARPG] Town scene created at {TownPath}.");
        }

        /// <summary>Adds the smith to the existing town without rebuilding it, or rebuilds just the smith.</summary>
        [MenuItem("Tools/ARPG/Add Forge To Town")]
        public static void AddForge()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TownPath) == null)
            {
                Debug.LogError($"[ARPG] {TownPath} not found. Run Create Town Scene first.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var town = EditorSceneManager.OpenScene(TownPath, OpenSceneMode.Single);
            PlaceForge();
            EditorSceneManager.MarkSceneDirty(town);
            EditorSceneManager.SaveScene(town);
            Debug.Log("[ARPG] Forge smith added to the town.");
        }

        static void PlaceForge()
        {
            var existing = GameObject.Find(ForgeObjectName);
            if (existing != null)
                Object.DestroyImmediate(existing);

            // A dark iron figure: a placeholder until there is character art.
            var sprite = PlaceholderArt.ImportSprite(
                $"{CharacterArtFolder}/PlaceholderSmith.png",
                PlaceholderArt.Capsule(96, 150, new Color32(92, 84, 96, 255)),
                128, SpriteAlignment.BottomCenter, FilterMode.Bilinear);

            var go = new GameObject(ForgeObjectName, typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(ForgeNpc));
            var world = IsoMath.GroundToWorld(IsoMath.CellToGround(ForgeCell));
            go.transform.position = new Vector3(world.x, world.y, 0f);
            PlayerSceneBuilder.SetLayer(go, GameLayers.Interactable);

            var spriteRenderer = go.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingLayerName = GameSortingLayers.Entities;
            spriteRenderer.spriteSortPoint = SpriteSortPoint.Pivot;
            var material = PlayerSceneBuilder.Default2DMaterial();
            if (material != null)
                spriteRenderer.sharedMaterial = material;

            // Walking up to the smith, not onto it, opens the Forge: the trigger reaches a little past the figure.
            var collider = go.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.8f;

            var label = new GameObject("Label", typeof(TextMesh));
            label.transform.SetParent(go.transform, false);
            label.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            var text = label.GetComponent<TextMesh>();
            text.text = "Forge";
            text.anchor = TextAnchor.LowerCenter;
            text.characterSize = 0.08f;
            text.fontSize = 48;
            text.color = new Color(1f, 0.6f, 0.3f);
            label.GetComponent<MeshRenderer>().sortingLayerName = GameSortingLayers.WorldUI;
        }
    }
}
