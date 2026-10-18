using System.Collections.Generic;
using NUnit.Framework;

namespace ARPG.Tests
{
    /// <summary>The warning symbols round her (the owner, 2026-10-19: "warning symbols around the character").</summary>
    public class IncomingWarningTests
    {
        [Test]
        public void Warnings_FromNearlyTheSameSide_StackOutward_OthersStayOnTheFirstRing()
        {
            var placed = new List<float> { 135f };
            Assert.AreEqual(1, WarningRules.RingFor(140f, placed), "top left again");
            Assert.AreEqual(0, WarningRules.RingFor(45f, placed), "top right");
            placed.Add(140f);
            Assert.AreEqual(2, WarningRules.RingFor(130f, placed));
            // Across the -180/180 seam.
            Assert.AreEqual(1, WarningRules.RingFor(-175f, new List<float> { 178f }));
        }

        [Test]
        public void TheSymbol_PopsIn_ThenThrobsAroundFullSize()
        {
            Assert.Less(WarningRules.Scale(0f, 1f), 0.5f);
            for (var t = 0.1f; t < 1f; t += 0.05f)
            {
                var scale = WarningRules.Scale(t, 1f);
                Assert.That(scale, Is.InRange(0.85f, 1.15f), t.ToString());
            }
        }
    }
}
