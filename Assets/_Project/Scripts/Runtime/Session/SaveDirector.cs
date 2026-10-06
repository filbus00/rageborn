using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ARPG
{
    /// <summary>
    /// Loads the save before the first scene and writes it when Docs/07-technical.md says to: 2 seconds after the
    /// first unsaved change (pickups, gold, equipping, kills; the docs' "within 2 seconds of pickup"), and at once on a scene
    /// change, a death (<see cref="SaveNow"/> from <see cref="DeathFlow"/>) and the app going to the background.
    /// Created from code, kept across scenes. A loaded game starts wherever the first scene in the build puts the
    /// player, which is the town: the player's position in a level is not saved.
    /// In the editor it is off unless Tools > ARPG > Save > Save In Editor is ticked, so play-testing a scene does not
    /// find its packs already dead from an earlier play.
    /// </summary>
    public class SaveDirector : MonoBehaviour
    {
        public const float DebounceSeconds = 2f;
        public const string EditorPrefKey = "ARPG.SaveInEditor";

        static SaveDirector instance;

        SaveStore store;
        GameSession session;
        float dirtySince = -1f;

        public static string SaveDirectory => Path.Combine(Application.persistentDataPath, "Save");

        public static bool Enabled
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetBool(EditorPrefKey, false);
#else
                // A device benchmark plays a fresh character and must never touch the save.
                return !DeviceBenchmark.Active;
#endif
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            instance = null;
            if (!Enabled)
                return;

            var go = new GameObject("Save Director");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SaveDirector>();
        }

        /// <summary>Writes the save now, if saving is on. For moments that must not wait for the debounce.</summary>
        public static void SaveNow()
        {
            if (instance != null)
                instance.Flush();
        }

        /// <summary>Starts over with a new character (the owner, 2026-09-29: a reset button in Settings). The save files
        /// are set aside as <c>*.reset-&lt;time&gt;</c> rather than deleted, so a reset by mistake can still be recovered by
        /// hand; then a fresh session replaces the live one and the town loads, where every scene object reads it.</summary>
        public static void StartOver()
        {
            if (instance != null)
            {
                instance.store.SetAsideAll("reset-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
                instance.session.Modified -= instance.MarkDirty;
            }
            GameSession.Install(new GameSession(Environment.TickCount));
            if (instance != null)
            {
                instance.session = GameSession.Current;
                instance.session.Modified += instance.MarkDirty;
                instance.dirtySince = -1f;
            }
            Time.timeScale = 1f;
            SceneManager.LoadScene(TownScene);
        }

        const string TownScene = "Town";

        void Awake()
        {
            store = new SaveStore(SaveDirectory);
            Load();

            session = GameSession.Current;
            session.Modified += MarkDirty;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        void OnDestroy()
        {
            if (session != null)
                session.Modified -= MarkDirty;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        void Load()
        {
            LoadAndInstall(store, Environment.TickCount);
        }

        /// <summary>Reads the save into <see cref="GameSession.Current"/>. Public and static so it can be tested without
        /// a scene. Returns what it loaded, for logging.</summary>
        public static SaveStore.LoadResult LoadAndInstall(SaveStore store, int lootSeed)
        {
            var result = store.Load();
            foreach (var failure in result.Failures)
                Debug.LogWarning($"[Save] Could not read {failure}");

            if (!result.Loaded)
            {
                if (result.Failures.Count > 0)
                {
                    // Nothing readable. Keep the files for recovery rather than saving a new game over them.
                    var suffix = "unreadable-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                    store.SetAsideAll(suffix);
                    Debug.LogError($"[Save] No save could be read; set them aside as *.{suffix} and started a new game.");
                }
                return result;
            }

            // Bows only (2026-09-30): a Wrathborn save is set aside, not deleted, and the game starts clean.
            if (SaveCodec.IsRetired(result.Data))
            {
                var suffix = "wrathborn-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                store.SetAsideAll(suffix);
                Debug.Log($"[Save] A save from before bows only; set it aside as *.{suffix} and started a new game.");
                return new SaveStore.LoadResult(null, null, 0, result.Failures);
            }

            var warnings = new List<string>();
            GameSession.Install(SaveCodec.Restore(result.Data, lootSeed, warnings));
            foreach (var warning in warnings)
                Debug.LogWarning($"[Save] {warning}");

            if (result.BackupNumber > 0)
                Debug.LogWarning($"[Save] The save was unreadable; loaded backup {result.BackupNumber} instead.");
            else
                Debug.Log($"[Save] Loaded {result.Path}");
            return result;
        }

        void MarkDirty()
        {
            if (dirtySince < 0f)
                dirtySince = Time.unscaledTime;
        }

        void Update()
        {
            // Measured from the first unsaved change, not the last, so steady looting cannot hold a save off forever.
            if (dirtySince >= 0f && Time.unscaledTime - dirtySince >= DebounceSeconds)
                Flush();
        }

        void OnActiveSceneChanged(Scene from, Scene to) => Flush();

        void OnApplicationPause(bool paused)
        {
            if (paused)
                Flush();
        }

        void OnApplicationQuit() => Flush();

        void Flush()
        {
            dirtySince = -1f;
            try
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                store.Write(SaveCodec.ToJson(SaveCodec.Capture(session, now)));
            }
            catch (Exception e)
            {
                // A failed save must never take the game down with it. The next trigger tries again.
                Debug.LogError($"[Save] Could not write the save: {e}");
            }
        }
    }
}
