using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class StickMathTests
    {
        const float Radius = 100f;
        const float DeadZone = 0.08f;

        [Test]
        public void InsideDeadZone_ReturnsZero()
        {
            var origin = new Vector2(500f, 500f);

            var value = StickMath.Evaluate(ref origin, origin + new Vector2(7f, 0f), Radius, DeadZone);

            Assert.AreEqual(Vector2.zero, value);
        }

        [Test]
        public void JustPastDeadZone_ReturnsSmallValueInThumbDirection()
        {
            var origin = new Vector2(500f, 500f);

            var value = StickMath.Evaluate(ref origin, origin + new Vector2(0f, 9f), Radius, DeadZone);

            Assert.Greater(value.y, 0f);
            Assert.Less(value.magnitude, 0.05f);
            Assert.AreEqual(0f, value.x, 1e-5f);
        }

        [Test]
        public void HalfRadius_ScalesBetweenDeadZoneAndFull()
        {
            var origin = new Vector2(500f, 500f);

            var value = StickMath.Evaluate(ref origin, origin + new Vector2(50f, 0f), Radius, DeadZone);

            Assert.AreEqual((0.5f - DeadZone) / (1f - DeadZone), value.magnitude, 1e-5f);
        }

        [Test]
        public void FullRadius_ReturnsFullMagnitude()
        {
            var origin = new Vector2(500f, 500f);

            var value = StickMath.Evaluate(ref origin, origin + new Vector2(0f, -100f), Radius, DeadZone);

            Assert.AreEqual(1f, value.magnitude, 1e-5f);
            Assert.AreEqual(-1f, value.y, 1e-5f);
        }

        [Test]
        public void BeyondRadiusWithinDrift_StaysAtFullMagnitudeAndKeepsBase()
        {
            var origin = new Vector2(500f, 500f);

            var value = StickMath.Evaluate(ref origin, origin + new Vector2(150f, 0f), Radius, DeadZone);

            Assert.AreEqual(1f, value.magnitude, 1e-5f);
            Assert.AreEqual(new Vector2(500f, 500f), origin);
        }

        [Test]
        public void BeyondDriftDistance_BaseFollowsThumb()
        {
            var origin = new Vector2(500f, 500f);
            var thumb = new Vector2(500f + 300f, 500f);

            var value = StickMath.Evaluate(ref origin, thumb, Radius, DeadZone);

            // The base ends up exactly 1.6 radii behind the thumb.
            Assert.AreEqual(thumb.x - Radius * StickMath.DriftFactor, origin.x, 1e-3f);
            Assert.AreEqual(500f, origin.y, 1e-3f);
            Assert.AreEqual(1f, value.magnitude, 1e-5f);
        }

        [Test]
        public void BaseFollow_ThenReturningThumb_ReducesValue()
        {
            var origin = new Vector2(500f, 500f);
            StickMath.Evaluate(ref origin, new Vector2(800f, 500f), Radius, DeadZone);

            var value = StickMath.Evaluate(ref origin, origin + new Vector2(50f, 0f), Radius, DeadZone);

            Assert.Less(value.magnitude, 1f);
        }
    }
}
