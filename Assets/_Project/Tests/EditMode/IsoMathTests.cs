using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class IsoMathTests
    {
        [Test]
        public void GroundToWorld_HalvesVertical_AndRoundTrips()
        {
            var ground = new Vector2(3f, 4f);

            var world = IsoMath.GroundToWorld(ground);

            Assert.AreEqual(new Vector2(3f, 2f), world);
            Assert.AreEqual(ground, IsoMath.WorldToGround(world));
        }

        [Test]
        public void StickToGround_KeepsMagnitude_InEveryDirection()
        {
            for (var degrees = 0; degrees < 360; degrees += 15)
            {
                var radians = degrees * Mathf.Deg2Rad;
                var stick = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * 0.7f;

                var ground = IsoMath.StickToGround(stick);

                Assert.AreEqual(0.7f, ground.magnitude, 1e-5f, $"at {degrees} degrees");
            }
        }

        [Test]
        public void StickToGround_ThenToWorld_KeepsTheThumbDirectionOnScreen()
        {
            var stick = new Vector2(1f, 1f).normalized;

            var world = IsoMath.GroundToWorld(IsoMath.StickToGround(stick));

            Assert.AreEqual(stick.x, world.normalized.x, 1e-5f);
            Assert.AreEqual(stick.y, world.normalized.y, 1e-5f);
        }

        [Test]
        public void StickToGround_UpOnScreen_MovesSlowerOnScreenThanSideways()
        {
            var up = IsoMath.GroundToWorld(IsoMath.StickToGround(Vector2.up));
            var right = IsoMath.GroundToWorld(IsoMath.StickToGround(Vector2.right));

            Assert.AreEqual(right.magnitude * IsoMath.GroundSquash, up.magnitude, 1e-5f);
        }

        [Test]
        public void StickToGround_Zero_ReturnsZero()
        {
            Assert.AreEqual(Vector2.zero, IsoMath.StickToGround(Vector2.zero));
        }
    }
}
