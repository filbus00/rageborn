using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ARPG
{
    /// <summary>The music for a moment of play (2026-10-04): the town's guitar, the dungeon's ambience, the boss fight.</summary>
    public enum MusicTrack
    {
        Town,
        Dungeon,
        Boss,
    }

    /// <summary>Which track plays where. Pure.</summary>
    public static class MusicRules
    {
        /// <summary>The town's track in town, the boss's while the Cinder Warden fights, the dungeon's anywhere else.</summary>
        public static MusicTrack For(string scene, bool bossFighting) =>
            scene == SceneTravel.TownScene ? MusicTrack.Town : bossFighting ? MusicTrack.Boss : MusicTrack.Dungeon;

        /// <summary>The file under Resources (Audio/Music/&lt;name&gt;), made by ArtSource/tools/audio/make_music.py.</summary>
        public static string ResourcePath(MusicTrack track) => "Audio/Music/" + track.ToString().ToLowerInvariant();

        /// <summary>How long a change of track crossfades: quick into the fight, slow everywhere else.</summary>
        public static float FadeSeconds(MusicTrack to) => to == MusicTrack.Boss ? 1f : 3f;
    }

    /// <summary>
    /// Plays the background music in every scene, created from code before the first scene and kept across scenes. Each
    /// place has its own looping track (<see cref="MusicRules"/>, files in Resources/Audio/Music, streamed), crossfaded
    /// on two sources when the place changes; checked twice a second. When the files are missing it falls back to the
    /// one synthesized loop (<see cref="MusicSynth"/>), rendered on a worker thread. The music volume setting scales it
    /// (<see cref="GameSettings.musicVolume"/>; its default 80 is the level the music was mixed at).
    /// </summary>
    public class MusicDirector : MonoBehaviour
    {
        /// <summary>Under the sound effects, which carry the fight: 0.3 at the default setting of 80.</summary>
        public const float FullVolume = 0.375f;
        const float CheckSeconds = 0.5f;

        readonly AudioSource[] sources = new AudioSource[2];
        readonly float[] levels = new float[2];
        int front;
        float fadeSeconds = 4f;
        float nextCheck;
        bool started;
        MusicTrack playing;
        bool useSynth;
        Task<float[]> rendering;
        CinderWardenFight fight;
        Scene fightScene;

        public static MusicDirector Instance { get; private set; }

        public bool Playing => sources[front] != null && sources[front].isPlaying;

        /// <summary>The track now playing (or fading in).</summary>
        public MusicTrack Track => playing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            var go = new GameObject("Music");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<MusicDirector>();
        }

        void Awake()
        {
            for (var i = 0; i < sources.Length; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.loop = true;
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.priority = 0; // Never stolen by a burst of sound effects.
                source.volume = 0f;
                sources[i] = source;
            }
            useSynth = Resources.Load<AudioClip>(MusicRules.ResourcePath(MusicTrack.Town)) == null;
            if (useSynth)
                rendering = Task.Run(MusicSynth.Render);
        }

        void Update()
        {
            if (useSynth)
                StartSynth();
            else if (Time.unscaledTime >= nextCheck)
            {
                nextCheck = Time.unscaledTime + CheckSeconds;
                var wanted = MusicRules.For(SceneManager.GetActiveScene().name, BossFighting());
                if (!started || wanted != playing)
                    Switch(wanted);
            }

            // Unscaled, so the inventory pause does not hold the fade. Kept running after it, so a volume change in
            // Settings is heard at once.
            var step = Time.unscaledDeltaTime / fadeSeconds;
            for (var i = 0; i < sources.Length; i++)
            {
                levels[i] = Mathf.MoveTowards(levels[i], i == front && started ? 1f : 0f, step);
                sources[i].volume = FullVolume * levels[i] * SettingsDirector.Current.MusicLevel;
                if (levels[i] <= 0f && i != front && sources[i].isPlaying)
                    sources[i].Stop();
            }
        }

        void Switch(MusicTrack track)
        {
            var clip = Resources.Load<AudioClip>(MusicRules.ResourcePath(track));
            if (clip == null)
                return;
            playing = track;
            fadeSeconds = started ? MusicRules.FadeSeconds(track) : 4f;
            started = true;
            front = 1 - front;
            sources[front].clip = clip;
            sources[front].Play();
        }

        bool BossFighting()
        {
            var scene = SceneManager.GetActiveScene();
            if (fight == null || fightScene != scene)
            {
                fight = FindAnyObjectByType<CinderWardenFight>();
                fightScene = scene;
            }
            return fight != null && fight.Engaged && fight.Boss != null && fight.Boss.IsAlive;
        }

        void StartSynth()
        {
            if (started || rendering == null || !rendering.IsCompleted)
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
            sources[front].clip = clip;
            sources[front].Play();
            started = true;
        }
    }
}
