using UnityEngine;
using UnityEngine.SceneManagement;

namespace ARPG
{
    /// <summary>
    /// Development builds only (and the editor): a second stairway in the town, labeled DEV, that goes straight into the
    /// boss arena on the act's last level, so the boss can be tested without clearing five levels first. It never exists in a release
    /// build. Created from code, like the perf overlay, so no scene carries it.
    /// </summary>
    public static class DevShortcuts
    {
        const string TownScene = "Town";
        const string DungeonScene = "Dungeon";

        // Four cells to the side of the town's own stairway (TownSceneBuilder puts it at cell 5, 5).
        static readonly Vector2Int Cell = new Vector2Int(9, 5);

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

            var stairs = Object.Instantiate(town);
            stairs.name = "DEV Stairs To Boss";
            var ground = IsoMath.CellToGround(Cell);
            stairs.transform.position = IsoMath.GroundToWorld(ground);
            stairs.GetComponent<SceneExit>().Configure(DungeonScene, DungeonRules.LevelsPerAct, Arrival.AtBoss);
            if (stairs.TryGetComponent<SpriteRenderer>(out var sprite))
                sprite.color = new Color(1f, 0.55f, 0.35f);

            var label = new GameObject("DEV Label", typeof(TextMesh));
            label.transform.SetParent(stairs.transform, false);
            label.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            var text = label.GetComponent<TextMesh>();
            text.text = "DEV: boss";
            text.anchor = TextAnchor.LowerCenter;
            text.characterSize = 0.08f;
            text.fontSize = 48;
            text.color = new Color(1f, 0.65f, 0.4f);
            label.GetComponent<MeshRenderer>().sortingLayerName = GameSortingLayers.WorldUI;
        }
    }
}
