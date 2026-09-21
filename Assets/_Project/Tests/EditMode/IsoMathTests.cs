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

        [Test]
        public void CellToGround_MatchesTheIsometricGridMeasuredInTheEditor()
        {
            // Cell centers measured from Tilemap.GetCellCenterWorld on the Sandbox grid, with world y doubled.
            Assert.AreEqual(new Vector2(0f, 0.5f), IsoMath.CellToGround(new Vector2Int(0, 0)));
            Assert.AreEqual(new Vector2(0.5f, 1f), IsoMath.CellToGround(new Vector2Int(1, 0)));
            Assert.AreEqual(new Vector2(-0.5f, 1f), IsoMath.CellToGround(new Vector2Int(0, 1)));
            Assert.AreEqual(new Vector2(0f, 1.5f), IsoMath.CellToGround(new Vector2Int(1, 1)));
            Assert.AreEqual(new Vector2(-2.5f, 0f), IsoMath.CellToGround(new Vector2Int(-3, 2)));
            Assert.AreEqual(new Vector2(-1f, 6.5f), IsoMath.CellToGround(new Vector2Int(5, 7)));
        }

        [Test]
        public void GroundToCell_RoundTripsCellCenters()
        {
            for (var y = -20; y <= 20; y++)
            for (var x = -20; x <= 20; x++)
            {
                var cell = new Vector2Int(x, y);
                Assert.AreEqual(cell, IsoMath.GroundToCell(IsoMath.CellToGround(cell)), $"cell {cell}");
            }
        }

        [Test]
        public void GroundToCell_StaysInTheCell_NearEveryEdgeOfTheDiamond()
        {
            var cell = new Vector2Int(3, -2);
            var center = IsoMath.CellToGround(cell);

            // The diamond has half-diagonals of 0.5 on the ground; 0.45 is inside, 0.55 is in a neighbour.
            foreach (var offset in new[] { Vector2.right, Vector2.left, Vector2.up, Vector2.down })
            {
                Assert.AreEqual(cell, IsoMath.GroundToCell(center + offset * 0.45f), $"inside toward {offset}");
                Assert.AreNotEqual(cell, IsoMath.GroundToCell(center + offset * 0.55f), $"outside toward {offset}");
            }
        }
    }
}
