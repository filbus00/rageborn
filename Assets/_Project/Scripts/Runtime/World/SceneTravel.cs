using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ARPG
{
    /// <summary>
    /// Fades out and loads a scene, for travel that is not a stairway: a waypoint or a town portal. A dungeon trip also
    /// names the depth and how the player arrives (<see cref="LevelTravel"/>). One trip at a time; a second request while
    /// one is under way is ignored.
    /// </summary>
    public class SceneTravel : MonoBehaviour
    {
        public const string TownScene = "Town";
        public const string DungeonScene = "Dungeon";
        const float FadeSeconds = 0.35f;

        static SceneTravel runner;
        static bool travelling;

        public static bool IsTravelling => travelling;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            var go = new GameObject("Scene Travel");
            DontDestroyOnLoad(go);
            runner = go.AddComponent<SceneTravel>();
            travelling = false;
            SceneManager.sceneLoaded += (scene, mode) => travelling = false;
        }

        public static void ToTown() => Go(TownScene, 0, Arrival.FromAbove);

        public static void ToDepth(int depth, Arrival arrival) => Go(DungeonScene, depth, arrival);

        static void Go(string scene, int depth, Arrival arrival)
        {
            if (travelling || runner == null)
                return;
            travelling = true;
            runner.StartCoroutine(Load(scene, depth, arrival));
        }

        static IEnumerator Load(string scene, int depth, Arrival arrival)
        {
            var fade = ScreenFade.Current;
            if (fade != null)
                yield return fade.FadeOut(FadeSeconds);
            if (depth > 0)
                GameSession.Current.Travel = new LevelTravel(depth, arrival);
            SceneManager.LoadScene(scene);
        }
    }

    /// <summary>
    /// Something the player walks onto to use, without a button (Docs/05: nothing in the world needs a tap): a waypoint,
    /// a portal, an NPC. It only fires on walking in, not on appearing under the player: arriving by a waypoint onto it,
    /// or opening a portal at your own feet, needs a step out and back in first.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public abstract class WalkOnTrigger : MonoBehaviour
    {
        bool armed;
        bool playerInside;

        protected virtual void Start()
        {
            // Armed at once unless the player already stands in it (a distance check: a collider added this frame is
            // not in the physics scene yet). The margin covers the player's own body.
            var player = FindAnyObjectByType<PlayerController>();
            var circle = GetComponent<CircleCollider2D>();
            var reach = circle.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y) + 0.4f;
            armed = player == null || Vector2.Distance(player.transform.position, transform.position) > reach;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null)
                return;
            playerInside = true;
            if (armed && !SceneTravel.IsTravelling)
                OnWalkedOn();
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null)
                return;
            playerInside = false;
            armed = true;
        }

        public bool PlayerInside => playerInside;

        protected abstract void OnWalkedOn();
    }
}
