using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    /// <summary>The warning symbols round her (the owner, 2026-10-19: "warning symbols around the character").</summary>
    public class IncomingWarningTests
    {
        [Test]
        public void Warnings_FromNearlyTheSameSide_MoveAlongTheRing_OthersKeepTheirAngle()
        {
            var placed = new List<float> { 135f };
            Assert.AreEqual(45f, WarningRules.Place(45f, placed), "top right keeps its place");
            var second = WarningRules.Place(140f, placed);
            Assert.AreEqual(140f + WarningRules.CrowdDegrees, second, 0.01f, "top left again: beside it, its own side first");
            placed.Add(second);
            var third = WarningRules.Place(130f, placed);
            Assert.AreEqual(130f - WarningRules.CrowdDegrees, third, 0.01f, "then the other side");
            // Across the -180/180 seam.
            var seam = WarningRules.Place(-175f, new List<float> { 178f });
            Assert.GreaterOrEqual(Mathf.Abs(Mathf.DeltaAngle(seam, 178f)), WarningRules.CrowdDegrees);
        }

        [Test]
        public void Shapes_Round_Triangle_Diamond()
        {
            // The centre is inside every shape; the top corners in none; the triangle's top middle is its point.
            foreach (IncomingWarnings.Shape shape in System.Enum.GetValues(typeof(IncomingWarnings.Shape)))
            {
                Assert.IsTrue(WarningRules.Inside(shape, 7, 7, 15), shape.ToString());
                Assert.IsFalse(WarningRules.Inside(shape, 0, 0, 15), shape.ToString());
            }
            Assert.IsTrue(WarningRules.Inside(IncomingWarnings.Shape.Triangle, 7, 1, 15));
            Assert.IsFalse(WarningRules.Inside(IncomingWarnings.Shape.Triangle, 4, 2, 15));
            Assert.IsTrue(WarningRules.Inside(IncomingWarnings.Shape.Triangle, 0, 14, 15), "the base reaches the sides");
            Assert.IsFalse(WarningRules.Inside(IncomingWarnings.Shape.Diamond, 1, 1, 15));
            Assert.IsTrue(WarningRules.Inside(IncomingWarnings.Shape.Round, 1, 7, 15));
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
