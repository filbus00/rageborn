using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class SweepGeometryTests
    {
        static readonly Vector2 Origin = new Vector2(3f, -2f);

        [Test]
        public void Contains_PointsInFrontWithinRange()
        {
            Assert.IsTrue(SweepGeometry.Contains(Origin, Vector2.right, Origin + new Vector2(1.5f, 0f), 2f, 120f));
        }

        [Test]
        public void Contains_RejectsPointsBeyondTheRange()
        {
            Assert.IsFalse(SweepGeometry.Contains(Origin, Vector2.right, Origin + new Vector2(2.1f, 0f), 2f, 120f));
        }

        [Test]
        public void Contains_RejectsPointsBehindTheAim()
        {
            Assert.IsFalse(SweepGeometry.Contains(Origin, Vector2.right, Origin + new Vector2(-1f, 0f), 2f, 120f));
        }

        [Test]
        public void Contains_AcceptsTheArcEdge_AndRejectsJustPastIt()
        {
            // A 120 degree arc reaches 60 degrees to each side.
            Vector2 At(float degrees) => Origin + new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));

            Assert.IsTrue(SweepGeometry.Contains(Origin, Vector2.right, At(59f), 2f, 120f));
            Assert.IsTrue(SweepGeometry.Contains(Origin, Vector2.right, At(-59f), 2f, 120f));
            Assert.IsFalse(SweepGeometry.Contains(Origin, Vector2.right, At(61f), 2f, 120f));
            Assert.IsFalse(SweepGeometry.Contains(Origin, Vector2.right, At(-61f), 2f, 120f));
        }

        [Test]
        public void Contains_WiderArcCatchesMore()
        {
            var behindSide = Origin + new Vector2(Mathf.Cos(95f * Mathf.Deg2Rad), Mathf.Sin(95f * Mathf.Deg2Rad));

            Assert.IsFalse(SweepGeometry.Contains(Origin, Vector2.right, behindSide, 2f, 120f));
            Assert.IsTrue(SweepGeometry.Contains(Origin, Vector2.right, behindSide, 2f, 200f), "a 200 degree sweep reaches 100 degrees to each side");
        }

        [Test]
        public void Contains_FollowsTheAimDirection()
        {
            var above = Origin + new Vector2(0f, 1.5f);

            Assert.IsTrue(SweepGeometry.Contains(Origin, Vector2.up, above, 2f, 120f));
            Assert.IsFalse(SweepGeometry.Contains(Origin, Vector2.down, above, 2f, 120f));
        }

        [Test]
        public void Contains_TheOriginItself_IsInside()
        {
            Assert.IsTrue(SweepGeometry.Contains(Origin, Vector2.right, Origin, 2f, 120f));
        }

        [Test]
        public void PickTarget_ReturnsMinusOne_WithoutCandidates()
        {
            Assert.AreEqual(-1, SweepGeometry.PickTarget(Origin, Vector2.right, new List<Vector2>()));
        }

        [Test]
        public void PickTarget_ChoosesTheNearest_InsideTheForwardCone()
        {
            var positions = new List<Vector2>
            {
                Origin + new Vector2(2f, 0f),
                Origin + new Vector2(1f, 0.2f),
                Origin + new Vector2(-0.5f, 0f),
            };

            // Facing right, the enemy behind is nearest overall, but it sits at 180 degrees and the cone only reaches
            // 100 degrees to each side. The nearest enemy in front wins.
            Assert.AreEqual(1, SweepGeometry.PickTarget(Origin, Vector2.right, positions));
        }

        [Test]
        public void PickTarget_FallsBackToTheNearestOverall_WhenNothingIsInTheCone()
        {
            var positions = new List<Vector2>
            {
                Origin + new Vector2(-2f, 0f),
                Origin + new Vector2(-1f, 0.1f),
            };

            Assert.AreEqual(1, SweepGeometry.PickTarget(Origin, Vector2.right, positions));
        }

        [Test]
        public void PickTarget_TheConeIsTwoHundredDegreesWide()
        {
            Vector2 At(float degrees, float distance) => Origin + new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad)) * distance;

            // Facing right: 95 degrees is inside the cone (limit 100), 105 degrees is outside. The closer 105 degree
            // enemy loses to the farther one at 95 degrees.
            var positions = new List<Vector2> { At(105f, 1f), At(95f, 2f) };

            Assert.AreEqual(1, SweepGeometry.PickTarget(Origin, Vector2.right, positions));
        }
    }
}
