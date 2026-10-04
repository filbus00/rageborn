using System;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Placeholder sound effects made in code, so the game is not silent before real audio is commissioned or licensed
    /// (Docs/08-production.md). Docs/05: heavy, tactile, low frequency emphasis, and a pickup sound per rarity. Each
    /// sound is a few layers of the basic shapes here (a pitch-swept sine, low-passed noise, a bell partial) under an
    /// exponential decay. Every function returns mono samples in -1..1 at <see cref="SampleRate"/>. Pure.
    /// </summary>
    public static class SoundSynth
    {
        public const int SampleRate = 22050;

        public static int Samples(float seconds) => Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));

        /// <summary>A sine whose pitch glides from one frequency to another, decaying with the given time constant.</summary>
        public static void AddSweep(float[] into, float from, float to, float amplitude, float decay, float startSeconds = 0f)
        {
            var start = Samples(startSeconds);
            var phase = 0.0;
            for (var i = start; i < into.Length; i++)
            {
                var t = (i - start) / (float)SampleRate;
                var progress = (i - start) / (float)Mathf.Max(1, into.Length - start);
                var frequency = Mathf.Lerp(from, to, progress);
                phase += 2.0 * Math.PI * frequency / SampleRate;
                into[i] += (float)Math.Sin(phase) * amplitude * Mathf.Exp(-t / decay);
            }
        }

        /// <summary>White noise through a one-pole low-pass whose cutoff glides, decaying, optionally swelling in first
        /// (a whoosh) instead of starting at full level (a thud).</summary>
        public static void AddNoise(float[] into, float cutoffFrom, float cutoffTo, float amplitude, float decay, bool swell, int seed)
        {
            var random = new System.Random(seed);
            var smoothed = 0f;
            for (var i = 0; i < into.Length; i++)
            {
                var t = i / (float)SampleRate;
                var progress = i / (float)into.Length;
                var cutoff = Mathf.Lerp(cutoffFrom, cutoffTo, progress);
                var alpha = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / SampleRate);
                smoothed += alpha * ((float)random.NextDouble() * 2f - 1f - smoothed);
                var envelope = swell ? Mathf.Sin(progress * Mathf.PI) : Mathf.Exp(-t / decay);
                into[i] += smoothed * amplitude * envelope;
            }
        }

        /// <summary>A struck bell or chime: the note and a quieter inharmonic partial, decaying.</summary>
        public static void AddBell(float[] into, float frequency, float amplitude, float decay, float startSeconds)
        {
            var start = Samples(startSeconds);
            for (var i = start; i < into.Length; i++)
            {
                var t = (i - start) / (float)SampleRate;
                var envelope = Mathf.Exp(-t / decay) * Mathf.Min(1f, t * 400f);
                into[i] += amplitude * envelope * (Mathf.Sin(2f * Mathf.PI * frequency * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * frequency * 2.76f * t));
            }
        }

        /// <summary>Scales the samples so the loudest is at <paramref name="peak"/>, and fades the last few milliseconds so
        /// a sound never ends on a click.</summary>
        public static float[] Finish(float[] samples, float peak = 0.9f)
        {
            var max = 0f;
            for (var i = 0; i < samples.Length; i++)
                max = Mathf.Max(max, Mathf.Abs(samples[i]));
            var scale = max > 0f ? peak / max : 0f;
            var fade = Mathf.Min(samples.Length, Samples(0.005f));
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] *= scale;
                var fromEnd = samples.Length - 1 - i;
                if (fromEnd < fade)
                    samples[i] *= fromEnd / (float)fade;
            }
            return samples;
        }

        /// <summary>The samples of one placeholder sound.</summary>
        public static float[] Make(SoundId id)
        {
            float[] s;
            switch (id)
            {
                case SoundId.Hit:
                    s = new float[Samples(0.09f)];
                    AddSweep(s, 150f, 60f, 1f, 0.03f);
                    AddNoise(s, 1500f, 500f, 0.8f, 0.025f, false, 1);
                    return Finish(s, 0.8f);
                case SoundId.Crit:
                    s = new float[Samples(0.14f)];
                    AddSweep(s, 180f, 60f, 1f, 0.04f);
                    AddSweep(s, 1100f, 500f, 0.35f, 0.02f);
                    AddNoise(s, 4000f, 800f, 0.8f, 0.03f, false, 2);
                    return Finish(s);
                case SoundId.Kill:
                    s = new float[Samples(0.2f)];
                    AddSweep(s, 120f, 40f, 1f, 0.07f);
                    AddNoise(s, 900f, 200f, 0.9f, 0.05f, false, 3);
                    return Finish(s);
                case SoundId.Swing:
                    s = new float[Samples(0.14f)];
                    AddNoise(s, 700f, 2600f, 1f, 1f, true, 4);
                    return Finish(s, 0.5f);
                case SoundId.Hew:
                    s = new float[Samples(0.24f)];
                    AddNoise(s, 500f, 3000f, 1f, 1f, true, 5);
                    AddSweep(s, 90f, 50f, 0.5f, 0.12f, 0.1f);
                    return Finish(s, 0.75f);
                case SoundId.AxeThrow:
                    s = new float[Samples(0.22f)];
                    AddNoise(s, 1200f, 2400f, 1f, 1f, true, 6);
                    for (var i = 0; i < s.Length; i++)
                        s[i] *= 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 28f * i / SampleRate);
                    return Finish(s, 0.6f);
                case SoundId.BullRush:
                    s = new float[Samples(0.4f)];
                    AddNoise(s, 300f, 1400f, 1f, 1f, true, 7);
                    AddSweep(s, 100f, 45f, 0.9f, 0.1f, 0.25f);
                    return Finish(s, 0.8f);
                case SoundId.GroundBreaker:
                    s = new float[Samples(0.7f)];
                    AddSweep(s, 75f, 28f, 1f, 0.25f);
                    AddNoise(s, 400f, 120f, 0.9f, 0.15f, false, 8);
                    return Finish(s);
                case SoundId.EnemySlam:
                    s = new float[Samples(0.45f)];
                    AddSweep(s, 90f, 35f, 1f, 0.15f);
                    AddNoise(s, 600f, 150f, 0.7f, 0.08f, false, 9);
                    return Finish(s, 0.75f);
                case SoundId.ArrowShot:
                    s = new float[Samples(0.12f)];
                    AddSweep(s, 1300f, 500f, 0.6f, 0.04f);
                    AddNoise(s, 5000f, 2000f, 0.5f, 0.03f, false, 10);
                    return Finish(s, 0.45f);
                case SoundId.Hurt:
                    s = new float[Samples(0.16f)];
                    AddSweep(s, 230f, 110f, 1f, 0.06f);
                    AddNoise(s, 700f, 300f, 0.5f, 0.05f, false, 11);
                    return Finish(s, 0.7f);
                case SoundId.DropCommon:
                    s = new float[Samples(0.1f)];
                    AddBell(s, 700f, 1f, 0.03f, 0f);
                    return Finish(s, 0.35f);
                case SoundId.DropMagic:
                    s = new float[Samples(0.35f)];
                    AddBell(s, 880f, 1f, 0.12f, 0f);
                    return Finish(s, 0.5f);
                case SoundId.DropRare:
                    s = new float[Samples(0.55f)];
                    AddBell(s, 660f, 1f, 0.15f, 0f);
                    AddBell(s, 990f, 1f, 0.2f, 0.09f);
                    return Finish(s, 0.65f);
                case SoundId.DropLegendary:
                    s = new float[Samples(1.3f)];
                    AddSweep(s, 55f, 50f, 0.8f, 0.5f);
                    AddBell(s, 523f, 1f, 0.35f, 0f);
                    AddBell(s, 659f, 1f, 0.35f, 0.1f);
                    AddBell(s, 784f, 1f, 0.35f, 0.2f);
                    AddBell(s, 1047f, 1f, 0.5f, 0.3f);
                    return Finish(s);
                case SoundId.Gold:
                    s = new float[Samples(0.18f)];
                    AddBell(s, 2500f, 1f, 0.03f, 0f);
                    AddBell(s, 3300f, 0.8f, 0.03f, 0.06f);
                    return Finish(s, 0.35f);
                case SoundId.Pickup:
                    s = new float[Samples(0.07f)];
                    AddSweep(s, 600f, 950f, 1f, 0.03f);
                    return Finish(s, 0.35f);
                case SoundId.LevelUp:
                    s = new float[Samples(1f)];
                    AddBell(s, 392f, 1f, 0.3f, 0f);
                    AddBell(s, 494f, 1f, 0.3f, 0.1f);
                    AddBell(s, 587f, 1f, 0.3f, 0.2f);
                    AddBell(s, 784f, 1f, 0.45f, 0.3f);
                    return Finish(s, 0.8f);
                case SoundId.Potion:
                    s = new float[Samples(0.4f)];
                    for (var i = 0; i < 5; i++)
                        AddBell(s, 320f + 70f * i, 0.8f, 0.03f, i * 0.07f);
                    return Finish(s, 0.5f);
                case SoundId.Forge:
                    s = new float[Samples(0.8f)];
                    AddBell(s, 523f, 0.8f, 0.3f, 0f);
                    AddBell(s, 1318f, 0.6f, 0.2f, 0f);
                    AddBell(s, 2093f, 0.4f, 0.15f, 0f);
                    AddNoise(s, 6000f, 3000f, 0.6f, 0.01f, false, 12);
                    return Finish(s, 0.7f);
                case SoundId.BossPhase:
                    s = new float[Samples(1.4f)];
                    for (var harmonic = 1; harmonic <= 5; harmonic++)
                        AddSweep(s, 55f * harmonic, 52f * harmonic, 1f / harmonic, 0.7f);
                    for (var i = 0; i < s.Length; i++)
                        s[i] *= Mathf.Min(1f, i / (float)Samples(0.25f));
                    return Finish(s, 0.85f);
                case SoundId.Death:
                    s = new float[Samples(1.4f)];
                    AddSweep(s, 110f, 35f, 1f, 0.6f);
                    AddNoise(s, 500f, 100f, 0.5f, 0.4f, false, 13);
                    return Finish(s, 0.8f);
                case SoundId.Hint:
                    s = new float[Samples(0.5f)];
                    AddBell(s, 784f, 1f, 0.15f, 0f);
                    AddBell(s, 1047f, 1f, 0.2f, 0.12f);
                    return Finish(s, 0.4f);
                case SoundId.Explosion:
                    s = new float[Samples(0.6f)];
                    AddSweep(s, 90f, 30f, 1f, 0.2f);
                    AddNoise(s, 1200f, 200f, 0.9f, 0.18f, false, 11);
                    return Finish(s, 0.9f);
                default:
                    return new float[1];
            }
        }
    }

    /// <summary>Every placeholder sound. Adding one means a recipe in <see cref="SoundSynth.Make"/> and a priority in
    /// <see cref="Sfx"/>.</summary>
    public enum SoundId
    {
        Hit,
        Crit,
        Kill,
        Swing,
        Hew,
        AxeThrow,
        BullRush,
        GroundBreaker,
        EnemySlam,
        ArrowShot,
        Hurt,
        DropCommon,
        DropMagic,
        DropRare,
        DropLegendary,
        Gold,
        Pickup,
        LevelUp,
        Potion,
        Forge,
        BossPhase,
        Death,
        Hint,

        /// <summary>Explosive Arrow's burst (2026-10-04; a file only, the synth makes it a short boom).</summary>
        Explosion,
    }
}
