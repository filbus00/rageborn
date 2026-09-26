using System;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class SoundTests
    {
        [Test]
        public void EverySound_IsAudible_InRange_AndEndsWithoutAClick()
        {
            foreach (SoundId id in Enum.GetValues(typeof(SoundId)))
            {
                var samples = SoundSynth.Make(id);
                Assert.Greater(samples.Length, SoundSynth.Samples(0.05f), id.ToString());

                var peak = 0f;
                foreach (var sample in samples)
                {
                    Assert.IsFalse(float.IsNaN(sample), id.ToString());
                    peak = Mathf.Max(peak, Mathf.Abs(sample));
                }
                Assert.That(peak, Is.InRange(0.2f, 0.9001f), id + " peak");
                Assert.AreEqual(0f, samples[samples.Length - 1], 1e-4f, id + " ends on silence");
            }
        }

        [Test]
        public void RaritySounds_GetLongerWithRarity()
        {
            var common = SoundSynth.Make(SoundId.DropCommon).Length;
            var magic = SoundSynth.Make(SoundId.DropMagic).Length;
            var rare = SoundSynth.Make(SoundId.DropRare).Length;
            var legendary = SoundSynth.Make(SoundId.DropLegendary).Length;
            Assert.Less(common, magic);
            Assert.Less(magic, rare);
            Assert.Less(rare, legendary);
        }

        [Test]
        public void AFreeVoice_IsTakenFirst()
        {
            var busy = new[] { true, false, true };
            Assert.AreEqual(1, VoicePicker.Pick(busy, new[] { 1, 1, 1 }, new[] { 0f, 0f, 0f }, 1));
        }

        [Test]
        public void WhenAllVoicesAreBusy_TheOldestOfNoHigherPriorityIsReplaced()
        {
            var busy = new[] { true, true, true };
            var priorities = new[] { 1, 3, 1 };
            var started = new[] { 2f, 0f, 1f };
            Assert.AreEqual(2, VoicePicker.Pick(busy, priorities, started, 2), "the oldest low priority voice");
            Assert.AreEqual(1, VoicePicker.Pick(busy, priorities, started, 3), "equal priority counts");
        }

        [Test]
        public void ALowPrioritySound_IsDropped_WhenEveryVoicePlaysSomethingMoreImportant()
        {
            var busy = new[] { true, true };
            Assert.AreEqual(-1, VoicePicker.Pick(busy, new[] { 3, 4 }, new[] { 0f, 1f }, 1));
        }
    }
}
