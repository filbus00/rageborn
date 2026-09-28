using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Holds the player's <see cref="GameSettings"/> for every system to read (<see cref="Current"/>, never null) and
    /// keeps them in the account file, <c>account.json</c> in the save folder, with the save's own rules (atomic
    /// write, 3 backups, fallback on a bad read; Docs/07-technical.md). Created from code before the first scene, kept
    /// across scenes. A change is written 0.5 s after the last one (unscaled time: the Bag pauses the game) and at once
    /// when the app goes to the background. Saving follows <see cref="SaveDirector.Enabled"/>, so in the editor the
    /// settings last one play unless Save In Editor is ticked.
    /// </summary>
    public class SettingsDirector : MonoBehaviour
    {
        public const string FileName = "account.json";
        const float WriteDelaySeconds = 0.5f;

        static SettingsDirector instance;
        static GameSettings current = new GameSettings();

        SaveStore store;
        float writeAt = -1f;

        /// <summary>The settings in effect. Read them at the point of use; do not keep a copy.</summary>
        public static GameSettings Current => current;

        /// <summary>Raised after any setting changes.</summary>
        public static event Action Changed;

        // Domain reload is off: start every play from defaults, then from the file. This runs before any object's
        // OnEnable, so it cannot wipe a subscription made at BeforeSceneLoad (TravelDirector's).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            current = new GameSettings();
            Changed = null;
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            var go = new GameObject("Settings Director");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SettingsDirector>();
        }

        /// <summary>Replaces the settings (a changed copy of <see cref="Current"/>), tells every listener and
        /// writes them shortly after.</summary>
        public static void Apply(GameSettings settings)
        {
            if (settings == null)
                return;
            settings.Clamp();
            current = settings;
            Changed?.Invoke();
            if (instance != null)
                instance.writeAt = Time.unscaledTime + WriteDelaySeconds;
        }

        void Awake()
        {
            if (!SaveDirector.Enabled)
                return;

            store = new SaveStore(SaveDirector.SaveDirectory, FileName);
            var failures = new List<string>();
            if (store.TryLoad<GameSettings>(GameSettings.TryParse, out var loaded, out var path, out var backup, failures))
            {
                current = loaded;
                if (backup > 0)
                    Debug.LogWarning($"SettingsDirector: settings loaded from backup {backup} ({path}): {string.Join("; ", failures)}");
            }
            else if (failures.Count > 0)
            {
                // Settings are cheap to lose; start from defaults and let the next write replace the bad file.
                Debug.LogWarning($"SettingsDirector: no readable settings, using defaults: {string.Join("; ", failures)}");
            }
        }

        void Update()
        {
            if (writeAt >= 0f && Time.unscaledTime >= writeAt)
                Write();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
                Write();
        }

        void OnApplicationQuit() => Write();

        void Write()
        {
            if (writeAt < 0f)
                return;
            writeAt = -1f;
            if (store == null)
                return;
            try
            {
                store.Write(GameSettings.ToJson(current));
            }
            catch (Exception e)
            {
                Debug.LogError($"SettingsDirector: could not write the settings: {e.Message}");
            }
        }
    }
}
