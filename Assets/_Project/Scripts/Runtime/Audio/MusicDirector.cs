using System.Threading.Tasks;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Plays the background music (<see cref="MusicSynth"/>) on a loop in every scene. Created from code before the first
    /// scene and kept across scenes. The loop is rendered on a worker thread, since it takes a moment and the game should
    /// not wait for it, then made into a clip on the main thread and faded in. The music volume setting scales it
    /// (<see cref="GameSettings.musicVolume"/>; its default 80 is the level the music was mixed at).
    /// </summary>
    public class MusicDirector : MonoBehaviour
    {
        /// <summary>Under the sound effects, which carry the fight: 0.3 at the default setting of 80.</summary>
        public const float FullVolume = 0.375f;
        const float FadeSeconds = 4f;

        float fade;

        Task<float[]> rendering;
        AudioSource source;

        public static MusicDirector Instance { get; private set; }

        public bool Playing => source != null && source.isPlaying;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            var go = new GameObject("Music");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<MusicDirector>();
        }

        void Awake() => rendering = Task.Run(MusicSynth.Render);

        void Update()
        {
            if (source == null)
            {
                if (rendering == null || !rendering.IsCompleted)
                    return;
                if (rendering.IsFaulted)
                {
                    Debug.LogError($"Music: rendering failed: {rendering.Exception}");
                    enabled = false;
                    return;
                }
                var samples = rendering.Result;
                rendering = null;
                var clip = AudioClip.Create("Music", samples.Length / 2, 2, MusicSynth.SampleRate, false);
                clip.SetData(samples, 0);
                source = gameObject.AddComponent<AudioSource>();
                source.clip = clip;
                source.loop = true;
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.priority = 0; // Never stolen by a burst of sound effects.
                source.volume = 0f;
                source.Play();
            }

            // Unscaled, so the inventory pause does not hold the fade. Kept running after it, so a volume change in
            // Settings is heard at once.
            fade = Mathf.Min(1f, fade + Time.unscaledDeltaTime / FadeSeconds);
            source.volume = FullVolume * fade * SettingsDirector.Current.MusicLevel;
        }
    }
}
