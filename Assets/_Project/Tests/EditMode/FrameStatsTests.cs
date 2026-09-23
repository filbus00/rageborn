using NUnit.Framework;

namespace ARPG.Tests
{
    public class FrameStatsTests
    {
        [Test]
        public void AnEmptyWindow_ReportsZeros()
        {
            var stats = new FrameStats(4);

            Assert.AreEqual(0, stats.Count);
            Assert.AreEqual(0f, stats.AverageFps);
            Assert.AreEqual(0f, stats.WorstSeconds);
            Assert.AreEqual(0, stats.CountOver(0.01f));
        }

        [Test]
        public void AverageFps_ComesFromTheAverageFrameTime()
        {
            var stats = new FrameStats(4);

            // 10 ms and 30 ms average to 20 ms, which is 50 fps (averaging 100 and 33 fps would say 67).
            stats.Record(0.010f);
            stats.Record(0.030f);

            Assert.AreEqual(0.020f, stats.AverageSeconds, 1e-6f);
            Assert.AreEqual(50f, stats.AverageFps, 1e-3f);
        }

        [Test]
        public void TheWindow_DropsTheOldestFrame_WhenFull()
        {
            var stats = new FrameStats(3);
            stats.Record(0.100f); // the hitch
            stats.Record(0.016f);
            stats.Record(0.016f);
            Assert.AreEqual(0.100f, stats.WorstSeconds, 1e-6f);

            stats.Record(0.016f); // pushes the hitch out

            Assert.AreEqual(3, stats.Count);
            Assert.AreEqual(0.016f, stats.WorstSeconds, 1e-6f);
            Assert.AreEqual(0.016f, stats.AverageSeconds, 1e-6f);
        }

        [Test]
        public void CountOver_CountsOnlyFramesStrictlyAboveTheThreshold()
        {
            var stats = new FrameStats(8);
            stats.Record(0.016f);
            stats.Record(0.025f);
            stats.Record(0.040f);

            Assert.AreEqual(1, stats.CountOver(0.025f));
            Assert.AreEqual(2, stats.CountOver(0.020f));
        }
    }
}
