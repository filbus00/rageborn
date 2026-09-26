using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ARPG
{
    /// <summary>
    /// A stairway. Walking onto it fades out and loads another scene, with no button (Docs/05-world-and-content.md).
    /// A hand-built scene puts the player wherever it placed them, next to its own stairway. A stairway into the
    /// dungeon also names the depth and which way the player arrives (<see cref="LevelTravel"/>), and the dungeon
    /// scene builds that level and puts the player by the matching stairs.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SceneExit : MonoBehaviour
    {
        [Tooltip("The scene to load, by name. It must be in the build settings.")]
        [SerializeField] string targetScene;

        [Tooltip("Seconds after the scene starts before the stairway works, so arriving next to it does not bounce the player back.")]
        [SerializeField, Min(0f)] float armSeconds = 0.75f;

        [SerializeField, Min(0f)] float fadeSeconds = 0.35f;

        [Tooltip("For a stairway into the dungeon scene: the depth to build, 1 for its first level. 0 for any other scene.")]
        [SerializeField, Min(0)] int targetDepth;

        [Tooltip("Which stairs of the target level the player arrives by.")]
        [SerializeField] Arrival arrival;

        /// <summary>Sets a stairway up from code, as a generated dungeon level does, before it can be walked onto.</summary>
        public void Configure(string scene, int depth, Arrival arriveBy)
        {
            targetScene = scene;
            targetDepth = depth;
            arrival = arriveBy;
        }

        static bool loading;
        float armedAt;

        void Awake()
        {
            loading = false;
            // Counted from whichever is later, the scene start or this stairway appearing (a boss's stairs appear mid-level).
            armedAt = Time.timeSinceLevelLoad + armSeconds;
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (loading || Time.timeSinceLevelLoad < armedAt || other.GetComponentInParent<PlayerController>() == null)
                return;

            loading = true;
            StartCoroutine(Load());
        }

        IEnumerator Load()
        {
            var fade = ScreenFade.Current;
            if (fade != null)
                yield return fade.FadeOut(fadeSeconds);

            if (targetDepth > 0)
                GameSession.Current.Travel = new LevelTravel(targetDepth, arrival);
            SceneManager.LoadScene(targetScene);
        }
    }
}
