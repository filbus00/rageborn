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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            launched = false;
            pendingSummary = null;
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
            // The run summary, shown when the town has loaded (2026-10-15).
            pendingSummary = RunSummary.Of(session, died ? RunEnd.Died : won ? RunEnd.Won : RunEnd.Portal, learned, next);
        }

        static RunSummary pendingSummary;
        EnemyManager countedEnemies;

        // Counts the run's kills and bosses for its summary.
        void OnKilled(EnemyController enemy)
        {
            var run = GameSession.Current.Run;
            run.Kills++;
            if (enemy.Definition.Rank == EnemyRank.Boss)
                run.Bosses++;
        }

        void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

        void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var session = GameSession.Current;
            if (countedEnemies != null)
                countedEnemies.Killed -= OnKilled;
            countedEnemies = null;
            if (scene.name != SceneTravel.TownScene)
            {
                launched = true;
                countedEnemies = FindAnyObjectByType<EnemyManager>();
                if (countedEnemies != null)
                    countedEnemies.Killed += OnKilled;
                return;
            }
            if (!launched)
            {
                launched = true;
                // Opened mid-run: back down to the depth she was on.
                if (session.RunStarted && session.DeepestDepth > 0)
                    StartCoroutine(Resume(session.DeepestDepth));
                return;
            }
            if (pendingSummary != null)
            {
                RunSummaryScreen.Open(pendingSummary);
                pendingSummary = null;
            }
        }

        // The travel is set a frame after the town's load: in the load's own frame the town's Start
        // (TownDressing) clears it, and the dungeon then came up at depth 1 (the owner, 2026-10-17: "if phone is locked
        // during a ongoing run and the game is relaunched, the character starts at level one").
        System.Collections.IEnumerator Resume(int depth)
        {
            yield return null;
            GameSession.Current.Travel = new LevelTravel(depth, Arrival.FromAbove);
            LoadTimer.Begin(SceneTravel.DungeonScene);
            SceneManager.LoadScene(SceneTravel.DungeonScene);
        }

        void Update()
        {
            var session = GameSession.Current;
            // The run's time in the dungeon, for its summary.
            if (session.RunStarted && SceneManager.GetActiveScene().name == SceneTravel.DungeonScene)
                session.Run.Seconds += Time.deltaTime;
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
