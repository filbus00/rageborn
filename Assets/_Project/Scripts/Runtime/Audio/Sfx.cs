// Unity 6.6 marks DEVELOPMENT_BUILD as deprecated (UAC0009), but its replacement, UNITY_INCLUDE_INSTRUMENTATION, is not
// set in this project's development builds (2026-10-18: the dev tools dropped out of the phone build), so the old symbol
// stays and its warning is silenced here.
#pragma warning disable UAC0009
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
    /// Which of a sound's variants plays next (2026-11-18, the owner: the bow and the arrow's hit "get a bit repetitive,
    /// make them more dynamic and varied"): a random one that is not the last, and with four or more not the one before
    /// it either, so the same two never alternate. <paramref name="roll"/> is a number in [0, 1). Pure.
    /// </summary>
    public static class SoundVariants
    {
        public static int Next(int count, int last, int beforeLast, float roll)
        {
            if (count <= 1)
                return 0;
            var avoidTwo = count >= 4;
            var allowed = count - (last >= 0 ? 1 : 0) - (avoidTwo && beforeLast >= 0 && beforeLast != last ? 1 : 0);
            var pick = Mathf.Clamp(Mathf.FloorToInt(roll * allowed), 0, allowed - 1);
            for (var i = 0; i < count; i++)
            {
                if (i == last || (avoidTwo && i == beforeLast))
                    continue;
                if (pick-- == 0)
                    return i;
            }
            return 0;
        }
    }

    /// <summary>
    /// How hard an arrow lands decides how it sounds (2026-11-18): the share of the target's life one hit takes sets the
    /// pitch (a light prick high and quiet, a heavy blow low and full) and an elite's or a boss's body sounds deeper.
    /// Pure.
    /// </summary>
    public static class HitSoundRules
    {
        /// <summary>A hit that takes a fifth of the target's life or more is a full blow.</summary>
        const float FullBlowShare = 0.2f;

        public static float Pitch(float damage, float maxLife, EnemyRank rank)
        {
            var heft = Mathf.Clamp01(damage / Mathf.Max(1f, maxLife) / FullBlowShare);
            var pitch = Mathf.Lerp(1.12f, 0.9f, heft);
            return rank == EnemyRank.Boss ? pitch * 0.88f : rank == EnemyRank.Elite ? pitch * 0.94f : pitch;
        }

        public static float Volume(float damage, float maxLife)
        {
            var heft = Mathf.Clamp01(damage / Mathf.Max(1f, maxLife) / FullBlowShare);
            return Mathf.Lerp(0.7f, 1f, heft);
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
        AudioClip[][] banks;
        int[] lastVariant, beforeLastVariant;
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
            banks = new AudioClip[ids.Length][];
            lastVariant = new int[ids.Length];
            beforeLastVariant = new int[ids.Length];
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
            // A sound that plays often has variants beside its file: Name_1, Name_2 ... (make_sfx.py banks, 2026-11-18).
            foreach (var id in ids)
            {
                var bank = new System.Collections.Generic.List<AudioClip> { clips[(int)id] };
                for (var k = 1; ; k++)
                {
                    var variant = Resources.Load<AudioClip>(DesignedFolder + id + "_" + k);
                    if (variant == null)
                        break;
                    bank.Add(variant);
                }
                banks[(int)id] = bank.ToArray();
                lastVariant[(int)id] = beforeLastVariant[(int)id] = -1;
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
        public static void Play(SoundId id, float volume = 1f, float pitch = 1f)
        {
            if (instance != null)
                instance.PlayInternal(id, volume, pitch);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Played?.Invoke(id, volume);
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Every request to play a sound, played or not (for <see cref="AudioCapture"/>). Development only.</summary>
        public static event System.Action<SoundId, float> Played;
#endif

        void PlayInternal(SoundId id, float volume, float pitchScale)
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
            var bank = banks[index];
            var variant = SoundVariants.Next(bank.Length, lastVariant[index], beforeLastVariant[index], Random.value);
            beforeLastVariant[index] = lastVariant[index];
            lastVariant[index] = variant;
            source.clip = bank[variant];
            // A sound with variants is varied again on every play: a little pitch and a little quieter or not.
            var varied = bank.Length > 1;
            var pitch = (1f + Random.Range(varied ? -0.08f : -0.06f, varied ? 0.08f : 0.06f)) * pitchScale;
            if (varied)
                volume *= Random.Range(0.86f, 1f);
            if (id == SoundId.Gold)
            {
                // Coins picked up one after another climb and soften rather than repeat (2026-10-12, CoinStreak).
                goldStreak = CoinStreak.Next(goldStreak, now - lastGold);
                lastGold = now;
                pitch = CoinStreak.Pitch(goldStreak) + Random.Range(-0.02f, 0.02f);
                volume *= CoinStreak.Volume(goldStreak);
            }
            source.volume = Mathf.Clamp01(volume * SettingsDirector.Current.EffectsLevel);
            source.pitch = pitch;
            source.Play();
        }

        int goldStreak;
        float lastGold = -10f;

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
                case SoundId.FrostNova:
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
                case SoundId.Pickup:
                case SoundId.DropCommon:
                    return 0.05f;
                case SoundId.Kill:
                case SoundId.Hurt:
                case SoundId.ArrowShot:
                    return 0.08f;
                // The elemental arrows (2026-10-18) hit many enemies at once: one sound a moment.
                case SoundId.FireHit:
                case SoundId.FrostHit:
                case SoundId.Zap:
                    return 0.12f;
                case SoundId.Freeze:
                    return 0.18f;
                case SoundId.Thunder:
                    return 0.3f;
                // Gold piles are picked up in bursts; the owner found every pile's sound annoying (2026-10-05).
                case SoundId.Gold:
                    return 0.3f;
                default:
                    return 0.02f;
            }
        }
    }

    /// <summary>
    /// How a run of coin pickups sounds (2026-10-12, the owner: the coin "sounds very repetitive, maybe make it sound
    /// different when repeated"): pickups less than <see cref="StreakGap"/> apart are a streak; each step up it plays a
    /// little higher (a rising five-note run, then round again) and a little softer, and a pause starts it over. Pure.
    /// </summary>
    public static class CoinStreak
    {
        public const float StreakGap = 1.2f;

        static readonly float[] Steps = { 1f, 1.06f, 1.12f, 1.19f, 1.26f };

        /// <summary>The streak after a pickup <paramref name="sinceLast"/> seconds after the last: one more, or 0 again.</summary>
        public static int Next(int streak, float sinceLast) => sinceLast < StreakGap ? streak + 1 : 0;

        public static float Pitch(int streak) => Steps[streak % Steps.Length];

        /// <summary>Full for the first coin, down to 60 percent by the fifth and on.</summary>
        public static float Volume(int streak) => Mathf.Max(0.6f, 1f - 0.1f * streak);
    }
}
