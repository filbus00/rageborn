using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Which voice a new sound takes (Docs/07-technical.md: a priority system that drops low priority sounds when more
    /// than 16 voices play): a free voice if there is one, else the oldest voice playing something of no higher
    /// priority, else none, and the new sound is dropped. Pure.
    /// </summary>
    public static class VoicePicker
    {
        public static int Pick(bool[] busy, int[] priorities, float[] startedAt, int priority)
        {
            var oldest = -1;
            for (var i = 0; i < busy.Length; i++)
            {
                if (!busy[i])
                    return i;
                if (priorities[i] <= priority && (oldest < 0 || startedAt[i] < startedAt[oldest]))
                    oldest = i;
            }
            return oldest;
        }
    }

    /// <summary>
    /// Plays the placeholder sounds (<see cref="SoundSynth"/>): <c>Sfx.Play(SoundId.Hit)</c> from anywhere. Created from
    /// code before the first scene and kept across scenes; it makes every clip once at startup. 16 voices, 2D (the camera
    /// follows the character, so everything that matters is near it), a little random pitch so repeats do not sound
    /// mechanical, and a minimum gap per sound so a sweep that hits eight enemies plays one hit, not eight. Uses the
    /// default iOS audio session, which follows the ring switch (Docs/01: drop sounds audible when the phone is on ring).
    /// The effects volume setting scales every sound (<see cref="GameSettings.effectsVolume"/>, 100 by default).
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        const int Voices = 16;

        /// <summary>Where the designed sound files lie, under Resources: one per <see cref="SoundId"/>, named after it.</summary>
        public const string DesignedFolder = "Audio/Sfx/";

        static Sfx instance;

        AudioClip[] clips;
        AudioSource[] sources;
        readonly bool[] busy = new bool[Voices];
        readonly int[] priorities = new int[Voices];
        readonly float[] startedAt = new float[Voices];
        float[] lastPlayed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            var go = new GameObject("Sfx");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<Sfx>();
        }

        void Awake()
        {
            var ids = (SoundId[])System.Enum.GetValues(typeof(SoundId));
            clips = new AudioClip[ids.Length];
            lastPlayed = new float[ids.Length];
            foreach (var id in ids)
            {
                // The designed sounds (ArtSource/tools/audio/make_sfx.py, 2026-10-04) where they exist, else the synth.
                var designed = Resources.Load<AudioClip>(DesignedFolder + id);
                if (designed != null)
                {
                    clips[(int)id] = designed;
                    lastPlayed[(int)id] = -10f;
                    continue;
                }
                var samples = SoundSynth.Make(id);
                var clip = AudioClip.Create(id.ToString(), samples.Length, 1, SoundSynth.SampleRate, false);
                clip.SetData(samples, 0);
                clips[(int)id] = clip;
                lastPlayed[(int)id] = -10f;
            }

            sources = new AudioSource[Voices];
            for (var i = 0; i < Voices; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                sources[i] = source;
            }
        }

        /// <summary>Plays a sound, unless the same one played too recently or every voice is busy with more important
        /// sounds. <paramref name="volume"/> scales the sound's own level.</summary>
        public static void Play(SoundId id, float volume = 1f)
        {
            if (instance != null)
                instance.PlayInternal(id, volume);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Played?.Invoke(id, volume);
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Every request to play a sound, played or not (for <see cref="AudioCapture"/>). Development only.</summary>
        public static event System.Action<SoundId, float> Played;
#endif

        void PlayInternal(SoundId id, float volume)
        {
            var index = (int)id;
            var now = Time.unscaledTime;
            if (now - lastPlayed[index] < MinGap(id))
                return;

            for (var i = 0; i < Voices; i++)
                busy[i] = sources[i].isPlaying;
            var voice = VoicePicker.Pick(busy, priorities, startedAt, Priority(id));
            if (voice < 0)
                return;

            lastPlayed[index] = now;
            priorities[voice] = Priority(id);
            startedAt[voice] = now;
            var source = sources[voice];
            source.clip = clips[index];
            source.volume = Mathf.Clamp01(volume * SettingsDirector.Current.EffectsLevel);
            source.pitch = 1f + Random.Range(-0.06f, 0.06f);
            source.Play();
        }

        // Loot and progression cues matter most (Docs/01 makes rarity sounds a point of the loot), then skills and big
        // hits, then the steady stream of swings and hits.
        static int Priority(SoundId id)
        {
            switch (id)
            {
                case SoundId.DropLegendary:
                case SoundId.LevelUp:
                case SoundId.BossPhase:
                case SoundId.Death:
                    return 4;
                case SoundId.DropRare:
                case SoundId.DropMagic:
                case SoundId.Forge:
                case SoundId.Hint:
                case SoundId.Potion:
                    return 3;
                case SoundId.GroundBreaker:
                case SoundId.Explosion:
                case SoundId.BullRush:
                case SoundId.Hew:
                case SoundId.AxeThrow:
                case SoundId.EnemySlam:
                case SoundId.Hurt:
                case SoundId.Kill:
                case SoundId.Crit:
                    return 2;
                default:
                    return 1;
            }
        }

        static float MinGap(SoundId id)
        {
            switch (id)
            {
                case SoundId.Hit:
                case SoundId.Crit:
                case SoundId.Swing:
                case SoundId.Gold:
                case SoundId.Pickup:
                case SoundId.DropCommon:
                    return 0.05f;
                case SoundId.Kill:
                case SoundId.Hurt:
                case SoundId.ArrowShot:
                    return 0.08f;
                default:
                    return 0.02f;
            }
        }
    }
}
