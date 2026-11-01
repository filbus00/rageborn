using NUnit.Framework;

namespace ARPG.Tests
{
    /// <summary>Her one-shots' timing (2026-11-01, the owner: "I feel that the animations are off").</summary>
    public class ActionTimingTests
    {
        [Test]
        public void Shots_RaceThroughTheLoose_AndTheDive_BurstsHarder()
        {
            Assert.AreEqual(0.5f, ActionTiming.Shape(ActionShape.Even, 0.5f), 1e-5f);
            Assert.AreEqual(0.75f, ActionTiming.Shape(ActionShape.Snap, 0.5f), 1e-5f, "three quarters of a shot's frames in half its time");
            Assert.AreEqual(0.875f, ActionTiming.Shape(ActionShape.Burst, 0.5f), 1e-5f);
            foreach (var shape in new[] { ActionShape.Even, ActionShape.Snap, ActionShape.Burst })
            {
                Assert.AreEqual(0f, ActionTiming.Shape(shape, 0f), 1e-5f, shape + " starts at its first frame");
                Assert.AreEqual(1f, ActionTiming.Shape(shape, 1f), 1e-5f, shape + " ends on its last");
                Assert.AreEqual(1f, ActionTiming.Shape(shape, 1.4f), 1e-5f, shape + " holds its last frame past its end");
                var previous = 0f;
                for (var t = 0.05f; t <= 1f; t += 0.05f)
                {
                    var now = ActionTiming.Shape(shape, t);
                    Assert.GreaterOrEqual(now, previous, shape + " never plays backwards");
                    previous = now;
                }
            }
        }
    }
}
