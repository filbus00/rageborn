using UnityEngine;
using UnityEngine.SceneManagement;

namespace ARPG
{
    /// <summary>
    /// The portal home (the owner, 2026-10-15: "Add a portal function that ends the run and lets you keep all you have
    /// found", anytime from the Bag): opening it takes <see cref="Seconds"/>, and a hit before then breaks it. When it
    /// opens, the run ends with everything she carries kept (<see cref="RunDirector.TakePortal"/>).
    /// </summary>
    public class PortalChannel : MonoBehaviour
    {
        public const float Seconds = 2.5f;

        static PortalChannel current;
        PlayerHealth health;
        float left;
        int shownSeconds = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => current = null;

        /// <summary>A portal can be opened: in the dungeon, in a run, alive.</summary>
        public static bool CanOpen
        {
            get
            {
                if (!Features.Roguelite || SceneManager.GetActiveScene().name != SceneTravel.DungeonScene || SceneTravel.IsTravelling)
                    return false;
                var health = FindAnyObjectByType<PlayerHealth>();
                return health != null && health.IsAlive;
            }
        }

        public static bool IsOpening => current != null;

        public static void Begin()
        {
            if (current != null || !CanOpen)
                return;
            var go = new GameObject("Portal Channel");
            current = go.AddComponent<PortalChannel>();
            current.left = Seconds;
            current.health = FindAnyObjectByType<PlayerHealth>();
            current.health.HitTaken += current.Broken;
            Sfx.Play(SoundId.LevelUp, 0.5f);
        }

        void Broken(float damage)
        {
            Banner("The portal collapses: you were struck.");
            Destroy(gameObject);
        }

        void Update()
        {
            if (health == null || !health.IsAlive)
            {
                Destroy(gameObject);
                return;
            }
            left -= Time.deltaTime;
            var seconds = Mathf.CeilToInt(left);
            if (seconds != shownSeconds && left > 0f)
            {
                shownSeconds = seconds;
                Banner($"Opening a portal home... {seconds}");
            }
            if (left > 0f)
                return;
            Destroy(gameObject);
            RunDirector.TakePortal();
        }

        static void Banner(string text) => (HintBanner.Current != null ? HintBanner.Current : HintBanner.Create()).Show(text);

        void OnDestroy()
        {
            if (health != null)
                health.HitTaken -= Broken;
            if (current == this)
                current = null;
        }
    }
}
