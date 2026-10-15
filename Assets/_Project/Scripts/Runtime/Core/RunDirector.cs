using UnityEngine;
using UnityEngine.SceneManagement;

namespace ARPG
{
    /// <summary>
    /// The roguelite's runs (the owner, 2026-10-13). A run begins when she first stands on a depth and ends when she comes
    /// back to town (dead, or up the stairs after the last boss): <see cref="BeforeTownLoad"/>, called just before the town
    /// loads, swaps in the next run's session (coins, upgrades, pets and stash kept) and the town shows what the run came
    /// to. A game opened mid-run goes straight back to the depth it was on. In the dungeon, a boon owed for a new depth
    /// opens the choice (<see cref="BoonScreen"/>) once the level has settled.
    /// </summary>
    public class RunDirector : MonoBehaviour
    {
        static bool launched;
        static string endNotice;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            launched = false;
            endNotice = null;
            if (!Features.Roguelite)
                return;
            var go = new GameObject("Run Director");
            DontDestroyOnLoad(go);
            go.AddComponent<RunDirector>();
        }

        /// <summary>The portal home (the owner, 2026-10-15: "Add a portal function that ends the run and lets you keep all you
        /// have found"; anytime, from the Bag): ends the run, keeping what she carries, and goes to town.</summary>
        public static void TakePortal()
        {
            if (SceneTravel.IsTravelling)
                return;
            BeforeTownLoad(false);
            SceneTravel.ToTown();
        }

        /// <summary>Ends the run if one is under way, before the town loads (the death fade, the portal, the stairs after
        /// the last boss).</summary>
        public static void BeforeTownLoad(bool died)
        {
            var session = GameSession.Current;
            if (!Features.Roguelite || !session.RunStarted)
                return;
            var depth = session.DeepestDepth;
            var won = !died && depth >= DungeonRules.Depths;
            // Home alive (the portal, or the last boss): what she carries comes with her and Thorin learns the
            // blueprints; dead, all of it is lost (the owner, 2026-10-15).
            var learned = died ? 0 : session.LearnCarriedBlueprints();
            var next = session.NextRun(System.Environment.TickCount, !died);
            SaveDirector.BeginRun(next);
            var taught = learned == 1 ? "  Thorin learns a new blueprint." : learned > 1 ? $"  Thorin learns {learned} new blueprints." : "";
            endNotice = won
                ? $"The First Watchman has fallen. The run is won.  {next.Gold:N0} coins kept.{taught}"
                : died
                    ? $"The run ended at depth {depth}.  All you carried is lost; {next.Gold:N0} coins kept."
                    : $"Home through the portal from depth {depth}, with everything you carried.{taught}";
        }

        void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

        void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var session = GameSession.Current;
            if (scene.name != SceneTravel.TownScene)
            {
                launched = true;
                return;
            }
            if (!launched)
            {
                launched = true;
                // Opened mid-run: back down to the depth she was on.
                if (session.RunStarted && session.DeepestDepth > 0)
                {
                    session.Travel = new LevelTravel(session.DeepestDepth, Arrival.FromAbove);
                    LoadTimer.Begin(SceneTravel.DungeonScene);
                    SceneManager.LoadScene(SceneTravel.DungeonScene);
                }
                return;
            }
            if (endNotice != null)
            {
                var notice = endNotice;
                endNotice = null;
                var banner = HintBanner.Current != null ? HintBanner.Current : HintBanner.Create();
                banner.Show(notice);
            }
        }

        void Update()
        {
            var session = GameSession.Current;
            if (session.PendingBoons <= 0 || BoonScreen.IsOpen || TownSheets.AnyOpen || SceneTravel.IsTravelling)
                return;
            if (SceneManager.GetActiveScene().name != SceneTravel.DungeonScene || Time.timeSinceLevelLoad < 0.6f)
                return;
            var bag = InventoryScreen.Current;
            if (bag != null && bag.IsOpen)
                return;
            var health = FindAnyObjectByType<PlayerHealth>();
            if (health == null || !health.IsAlive)
                return;
            BoonScreen.Open();
        }
    }
}
