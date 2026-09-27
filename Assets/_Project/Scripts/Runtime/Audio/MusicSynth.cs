using System;

namespace ARPG
{
    /// <summary>
    /// Placeholder background music made in code, like the sound effects (<see cref="SoundSynth"/>), until real stems
    /// exist (Docs/05: sparse low strings, choir and drums in layered stems). The user's brief of 2026-09-27: "dark and
    /// gothic, just some basic synth sounds on a simple loop".
    ///
    /// A 32 second loop in D minor at 60 beats a minute, 8 bars of 4 seconds: a low D drone under everything; pad chords
    /// of detuned, low-passed saw waves, Dm, Bb, Gm, A twice (the A major chord of harmonic minor pulls back to Dm across
    /// the loop point); a sparse bell melody; a low tolling bell at bars 1 and 5; and a reverb, so it sounds like a crypt.
    /// Seamless: every enveloped note wraps around the loop, every endless oscillator has a whole number of cycles in the
    /// loop, and the filters and reverb run over the loop repeated and keep only the last pass, their steady state.
    /// Stereo, interleaved, in -1..1 at <see cref="SampleRate"/>. Pure, and safe to run off the main thread (no Unity
    /// objects), since rendering takes a moment.
    /// </summary>
    public static class MusicSynth
    {
        public const int SampleRate = 22050;
        public const float BarSeconds = 4f;
        public const int Bars = 8;
        public const float LoopSeconds = BarSeconds * Bars;
        public static int LoopSamples => (int)(LoopSeconds * SampleRate);

        // MIDI notes. D minor with the harmonic minor's C#: i, VI, iv, V, twice.
        static readonly int[][] Chords =
        {
            new[] { 50, 53, 57, 62 }, // Dm
            new[] { 46, 50, 53, 58 }, // Bb
            new[] { 43, 50, 55, 58 }, // Gm
            new[] { 45, 49, 52, 57 }, // A
            new[] { 50, 53, 57, 62 },
            new[] { 46, 50, 53, 58 },
            new[] { 43, 50, 55, 58 },
            new[] { 45, 49, 52, 57 },
        };

        // The bell melody: (bar, beat, MIDI note). Sparse, falling, ending on the leading tone C# so the loop resolves.
        static readonly (int bar, float beat, int note)[] Melody =
        {
            (0, 0f, 69), (0, 2.5f, 65),
            (1, 0f, 70), (1, 2f, 69),
            (2, 0f, 67), (2, 3f, 70),
            (3, 0f, 73), (3, 2f, 69),
            (4, 0f, 74), (4, 2.5f, 69),
            (5, 0f, 70), (5, 2f, 65),
            (6, 0f, 67), (6, 2f, 62),
            (7, 0f, 64), (7, 2f, 61),
        };

        public static float Frequency(int midi) => 440f * (float)Math.Pow(2.0, (midi - 69) / 12.0);

        /// <summary>A frequency moved (by a fraction of a hertz) to a whole number of cycles in the loop, so an endless
        /// oscillator meets itself at the loop point.</summary>
        public static double LoopFrequency(double frequency) => Math.Max(1.0, Math.Round(frequency * LoopSeconds)) / LoopSeconds;

        /// <summary>Renders the loop: stereo interleaved (left, right, left...), <see cref="LoopSamples"/> frames.</summary>
        public static float[] Render()
        {
            var n = LoopSamples;
            var left = new float[n];
            var right = new float[n];

            AddDrone(left, right);
            for (var bar = 0; bar < Bars; bar++)
                for (var voice = 0; voice < Chords[bar].Length; voice++)
                    AddPadNote(left, right, Frequency(Chords[bar][voice]), bar * BarSeconds, BarSeconds,
                        // Spread the chord across the stereo field, the bass in the middle.
                        voice == 0 ? 0f : (voice % 2 == 1 ? -0.5f : 0.5f), 0.07f, voice * 97 + bar * 13);
            foreach (var (bar, beat, note) in Melody)
                AddBell(left, right, Frequency(note), bar * BarSeconds + beat, 0.09f, 2.4f, (note % 3 - 1) * 0.3f);
            AddBell(left, right, Frequency(38), 0f, 0.16f, 6f, 0f);
            AddBell(left, right, Frequency(38), 4 * BarSeconds, 0.14f, 6f, 0f);

            left = Reverb(left, new[] { 1557, 1617, 1491, 1422 }, 0.84f, 0.32f);
            right = Reverb(right, new[] { 1580, 1640, 1514, 1445 }, 0.84f, 0.32f);

            var peak = 0f;
            for (var i = 0; i < n; i++)
                peak = Math.Max(peak, Math.Max(Math.Abs(left[i]), Math.Abs(right[i])));
            var gain = peak > 0f ? 0.85f / peak : 1f;
            var output = new float[n * 2];
            for (var i = 0; i < n; i++)
            {
                output[i * 2] = (float)Math.Tanh(left[i] * gain * 1.1f);
                output[i * 2 + 1] = (float)Math.Tanh(right[i] * gain * 1.1f);
            }
            return output;
        }

        // A low D: a sine and a darker, filtered saw on the same note, breathing slowly. Whole cycles in the loop, and the
        // filter runs twice round so its state at the loop point is its steady state.
        static void AddDrone(float[] left, float[] right)
        {
            var n = left.Length;
            var sine = LoopFrequency(Frequency(38));
            var saw = LoopFrequency(Frequency(38) * 1.002);
            var breath = LoopFrequency(1.0 / 16.0);
            var alpha = OnePole(260f);
            var filtered = 0.0;
            for (var pass = 0; pass < 2; pass++)
                for (var i = 0; i < n; i++)
                {
                    var t = i / (double)SampleRate;
                    var s = 2.0 * ((t * saw) % 1.0) - 1.0;
                    filtered += alpha * (s - filtered);
                    if (pass == 0)
                        continue;
                    var swell = 0.75 + 0.25 * Math.Sin(2.0 * Math.PI * breath * t);
                    var sample = (float)((Math.Sin(2.0 * Math.PI * sine * t) * 0.22 + filtered * 0.12) * swell);
                    left[i] += sample;
                    right[i] += sample;
                }
        }

        // A chord note: two saws a little apart in pitch, low-passed (the cutoff opening and closing across the note),
        // swelling in over 1.2 s and dying away 1.8 s after its bar. Written around the loop.
        static void AddPadNote(float[] left, float[] right, float frequency, float start, float length, float pan, float amplitude, int seed)
        {
            var n = left.Length;
            const float attack = 1.2f, release = 1.8f;
            var total = (int)((length + release) * SampleRate);
            var begin = (int)(start * SampleRate);
            var detune = Math.Pow(2.0, 7.0 / 1200.0);
            var phaseA = (seed % 7) / 7.0;
            var phaseB = (seed % 5) / 5.0;
            var filtered = 0.0;
            var filtered2 = 0.0;
            var gainLeft = (float)Math.Sqrt(0.5 * (1.0 - pan));
            var gainRight = (float)Math.Sqrt(0.5 * (1.0 + pan));
            for (var k = 0; k < total; k++)
            {
                var t = k / (float)SampleRate;
                phaseA = (phaseA + frequency * detune / SampleRate) % 1.0;
                phaseB = (phaseB + frequency / detune / SampleRate) % 1.0;
                var s = (2.0 * phaseA - 1.0) + (2.0 * phaseB - 1.0);
                // The cutoff swells with the note, 350 to 1100 Hz: soft, never bright.
                var cutoff = 350.0 + 750.0 * Math.Sin(Math.PI * Math.Min(1.0, t / (length + release)));
                var alpha = OnePole((float)cutoff);
                filtered += alpha * (s - filtered);
                filtered2 += alpha * (filtered - filtered2);
                var envelope = t < attack ? t / attack : t < length ? 1f : Math.Max(0f, 1f - (t - length) / release);
                envelope *= envelope;
                var sample = (float)(filtered2 * amplitude * envelope);
                var index = (begin + k) % n;
                left[index] += sample * gainLeft;
                right[index] += sample * gainRight;
            }
        }

        // A struck bell: the note and inharmonic partials, a soft strike and a long decay. Written around the loop.
        static void AddBell(float[] left, float[] right, float frequency, float start, float amplitude, float decay, float pan)
        {
            var n = left.Length;
            var total = (int)(decay * 5f * SampleRate);
            var begin = (int)(start * SampleRate);
            var gainLeft = (float)Math.Sqrt(0.5 * (1.0 - pan));
            var gainRight = (float)Math.Sqrt(0.5 * (1.0 + pan));
            for (var k = 0; k < total; k++)
            {
                var t = k / (double)SampleRate;
                var envelope = Math.Exp(-t / decay) * Math.Min(1.0, t * 60.0);
                var w = 2.0 * Math.PI * frequency * t;
                var s = Math.Sin(w) + 0.45 * Math.Sin(w * 2.0) * Math.Exp(-t / (decay * 0.5)) +
                        0.25 * Math.Sin(w * 2.76) * Math.Exp(-t / (decay * 0.3)) + 0.12 * Math.Sin(w * 5.4) * Math.Exp(-t / (decay * 0.15));
                var sample = (float)(s * envelope * amplitude);
                var index = (begin + k) % n;
                left[index] += sample * gainLeft;
                right[index] += sample * gainRight;
            }
        }

        // A Schroeder reverb (parallel combs, then two allpasses), mixed with the dry sound. Run over the loop three
        // times and the last pass kept: the tail of the end rings on into the start, as it does when the loop repeats.
        static float[] Reverb(float[] dry, int[] combDelays, float feedback, float wet)
        {
            var n = dry.Length;
            var combs = new float[combDelays.Length][];
            var combIndex = new int[combDelays.Length];
            var damp = new float[combDelays.Length];
            for (var c = 0; c < combs.Length; c++)
                combs[c] = new float[combDelays[c]];
            var allpassDelays = new[] { 556, 441 };
            var allpasses = new float[allpassDelays.Length][];
            var allpassIndex = new int[allpassDelays.Length];
            for (var a = 0; a < allpasses.Length; a++)
                allpasses[a] = new float[allpassDelays[a]];

            var output = new float[n];
            for (var pass = 0; pass < 3; pass++)
                for (var i = 0; i < n; i++)
                {
                    var input = dry[i] * 0.3f;
                    var sum = 0f;
                    for (var c = 0; c < combs.Length; c++)
                    {
                        var buffer = combs[c];
                        var delayed = buffer[combIndex[c]];
                        // A gentle low-pass in the feedback darkens the tail, as stone would.
                        damp[c] = delayed * 0.7f + damp[c] * 0.3f;
                        buffer[combIndex[c]] = input + damp[c] * feedback;
                        combIndex[c] = (combIndex[c] + 1) % buffer.Length;
                        sum += delayed;
                    }
                    for (var a = 0; a < allpasses.Length; a++)
                    {
                        var buffer = allpasses[a];
                        var delayed = buffer[allpassIndex[a]];
                        var value = -sum * 0.5f + delayed;
                        buffer[allpassIndex[a]] = sum + delayed * 0.5f;
                        allpassIndex[a] = (allpassIndex[a] + 1) % buffer.Length;
                        sum = value;
                    }
                    if (pass == 2)
                        output[i] = dry[i] * (1f - wet) + sum * wet;
                }
            return output;
        }

        static double OnePole(float cutoff) => 1.0 - Math.Exp(-2.0 * Math.PI * cutoff / SampleRate);
    }
}
