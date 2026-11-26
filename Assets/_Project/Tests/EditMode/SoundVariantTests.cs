using NUnit.Framework;

namespace ARPG.Tests
{
    public class SoundVariantTests
    {
        [Test]
        public void ASoundWithOneVariantAlwaysPlaysIt()
        {
            Assert.AreEqual(0, SoundVariants.Next(1, 0, -1, 0.7f));
            Assert.AreEqual(0, SoundVariants.Next(0, -1, -1, 0.3f));
        }

        [Test]
        public void NeverTheSameTwiceRunning_AndNeverTheLastTwoWithFourOrMore()
        {
            for (var count = 2; count <= 10; count++)
                for (var last = -1; last < count; last++)
                    for (var before = -1; before < count; before++)
                        for (var roll = 0f; roll < 1f; roll += 0.05f)
                        {
                            var next = SoundVariants.Next(count, last, before, roll);
                            Assert.That(next, Is.InRange(0, count - 1));
                            if (last >= 0)
                                Assert.AreNotEqual(last, next, $"count {count} last {last} before {before} roll {roll}");
                            if (count >= 4 && before >= 0)
                                Assert.AreNotEqual(before, next, $"count {count} last {last} before {before} roll {roll}");
                        }
        }

        [Test]
        public void EveryVariantGetsPlayedInTime()
        {
            var seen = new System.Collections.Generic.HashSet<int>();
            int last = -1, before = -1;
            var random = new System.Random(5);
            for (var i = 0; i < 200; i++)
            {
                var next = SoundVariants.Next(7, last, before, (float)random.NextDouble());
                seen.Add(next);
                before = last;
                last = next;
            }
            Assert.AreEqual(7, seen.Count);
        }
    }
}
