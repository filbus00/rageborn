using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class MusicTests
    {
        static float[] rendered;

        static float[] Loop => rendered ??= MusicSynth.Render();

        [Test]
        public void TheLoop_Is32SecondsOfStereo_InRange_AndNotSilent()
        {
            Assert.AreEqual(MusicSynth.LoopSamples * 2, Loop.Length);
            Assert.AreEqual(32f, MusicSynth.LoopSeconds);
            var peak = 0f;
            var energy = 0.0;
            foreach (var s in Loop)
            {
                peak = Mathf.Max(peak, Mathf.Abs(s));
                energy += s * s;
            }
            Assert.LessOrEqual(peak, 1f);
            Assert.Greater(System.Math.Sqrt(energy / Loop.Length), 0.05, "loud enough to hear under the effects");
        }

        [Test]
        public void TheLoopPoint_IsSeamless()
        {
            // The jump from the last frame to the first is no bigger than the ordinary steps between frames.
            var n = Loop.Length / 2;
            var biggestStep = 0f;
            for (var i = 1; i < n; i++)
                biggestStep = Mathf.Max(biggestStep, Mathf.Abs(Loop[i * 2] - Loop[(i - 1) * 2]));
            var seam = Mathf.Abs(Loop[0] - Loop[(n - 1) * 2]);
            Assert.LessOrEqual(seam, biggestStep);
            Assert.Less(seam, 0.05f);
        }

        [Test]
        public void EndlessOscillators_HaveWholeCyclesInTheLoop()
        {
            var f = MusicSynth.LoopFrequency(73.42);
            var cycles = f * MusicSynth.LoopSeconds;
            Assert.AreEqual(System.Math.Round(cycles), cycles, 1e-9);
            Assert.Less(System.Math.Abs(f - 73.42), 1.0 / MusicSynth.LoopSeconds);
            Assert.AreEqual(440f, MusicSynth.Frequency(69), 1e-3f);
        }
    }
}
