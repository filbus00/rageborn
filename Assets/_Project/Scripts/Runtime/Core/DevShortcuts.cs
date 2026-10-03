using UnityEngine;
using UnityEngine.SceneManagement;

namespace ARPG
{
    /// <summary>
    /// Development builds only (and the editor): extra stairways in the town, labeled DEV. One goes straight into the
    /// boss arena on the act's last level, so the boss can be tested without clearing five levels first; one goes to
    /// depth 3, the first level deep enough for every enemy type and pack mix. They never exist in a release build.
    /// Created from code, like the perf overlay, so no scene carries them.
    /// </summary>
    public static class DevShortcuts
    {
        const string TownScene = "Town";
        const string DungeonScene = "Dungeon";

        // To the side of the town's own stairway (TownSceneBuilder puts it at cell 5, 5).
        static readonly Vector2Int BossCell = new Vector2Int(9, 5);
        // Moved from (9, 1) on 2026-10-03: its label ran into the Pet Vendor's (cell 5, 1).
        static readonly Vector2Int MixedCell = new Vector2Int(12, 0);
        const int MixedDepth = 3;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Debug.isDebugBuild)
                SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != TownScene)
                return;

            var town = GameObject.Find("Stairs Down");
            if (town == null)
                return;

            AddStairs(town, "DEV Stairs To Boss", BossCell, DungeonRules.LevelsPerAct, Arrival.AtBoss, "DEV: boss");
            AddStairs(town, "DEV Stairs To Depth 3", MixedCell, MixedDepth, Arrival.FromAbove, "DEV: depth 3");
        }

        static void AddStairs(GameObject template, string name, Vector2Int cell, int depth, Arrival arrival, string caption)
        {
            var stairs = Object.Instantiate(template);
            stairs.name = name;
            var ground = IsoMath.CellToGround(cell);
            stairs.transform.position = IsoMath.GroundToWorld(ground);
            stairs.GetComponent<SceneExit>().Configure(DungeonScene, depth, arrival);
            if (stairs.TryGetComponent<SpriteRenderer>(out var sprite))
                sprite.color = new Color(1f, 0.55f, 0.35f);

            var label = new GameObject("DEV Label", typeof(TextMesh));
            label.transform.SetParent(stairs.transform, false);
            label.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            var text = label.GetComponent<TextMesh>();
            text.text = caption;
            text.anchor = TextAnchor.LowerCenter;
            text.characterSize = 0.08f;
            text.fontSize = 48;
            text.color = new Color(1f, 0.65f, 0.4f);
            label.GetComponent<MeshRenderer>().sortingLayerName = GameSortingLayers.WorldUI;
        }
    }
}
