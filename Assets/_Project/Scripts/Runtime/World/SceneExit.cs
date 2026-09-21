using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ARPG
{
    /// <summary>
    /// A stairway. Walking onto it fades out and loads another scene, with no button (Docs/05-world-and-content.md).
    /// The player arrives wherever that scene placed it, which is next to its own stairway.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SceneExit : MonoBehaviour
    {
        [Tooltip("The scene to load, by name. It must be in the build settings.")]
        [SerializeField] string targetScene;

        [Tooltip("Seconds after the scene starts before the stairway works, so arriving next to it does not bounce the player back.")]
        [SerializeField, Min(0f)] float armSeconds = 0.75f;

        [SerializeField, Min(0f)] float fadeSeconds = 0.35f;

        static bool loading;

        void Awake() => loading = false;

        void OnTriggerStay2D(Collider2D other)
        {
            if (loading || Time.timeSinceLevelLoad < armSeconds || other.GetComponentInParent<PlayerController>() == null)
                return;

            loading = true;
            StartCoroutine(Load());
        }

        IEnumerator Load()
        {
            var fade = ScreenFade.Current;
            if (fade != null)
                yield return fade.FadeOut(fadeSeconds);

            SceneManager.LoadScene(targetScene);
        }
    }
}
