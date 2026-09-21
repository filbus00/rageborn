using NUnit.Framework;
using UnityEngine;

namespace ARPG.Tests
{
    public class FlowFieldTests
    {
        // A square block of walkable cells centered on the origin, with the given cells blocked.
        static NavGrid OpenGrid(int halfExtent, params Vector2Int[] blocked)
        {
            var size = halfExtent * 2 + 1;
            var grid = new NavGrid(new Vector2Int(-halfExtent, -halfExtent), size, size);
            for (var y = -halfExtent; y <= halfExtent; y++)
                for (var x = -halfExtent; x <= halfExtent; x++)
                    grid.SetWalkable(new Vector2Int(x, y), true);
            foreach (var cell in blocked)
                grid.SetWalkable(cell, false);
            return grid;
        }

        // The neighbour cell a flow direction points at, found by matching it to the eight lattice steps.
        static Vector2Int Step(FlowField field, Vector2Int cell)
        {
            Assert.IsTrue(field.TryGetDirection(IsoMath.CellToGround(cell), out var direction), $"no direction at {cell}");

            foreach (var offset in new[]
                     {
                         new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
                         new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
                     })
            {
                var ground = IsoMath.CellToGround(cell + offset) - IsoMath.CellToGround(cell);
                if (Vector2.Dot(ground.normalized, direction) > 0.999f)
                    return cell + offset;
            }

            Assert.Fail($"direction {direction} at {cell} matches no lattice step");
            return cell;
        }

        [Test]
        public void Cost_UsesGroundLengths_ForEdgeAndCornerSteps()
        {
            var field = new FlowField(OpenGrid(5));

            field.Compute(Vector2Int.zero, 100f);

            Assert.AreEqual(0f, field.CostAt(Vector2Int.zero), 1e-5f);
            Assert.AreEqual(FlowField.EdgeStep, field.CostAt(new Vector2Int(1, 0)), 1e-5f);
            Assert.AreEqual(2f * FlowField.EdgeStep, field.CostAt(new Vector2Int(2, 0)), 1e-5f);
            Assert.AreEqual(FlowField.CornerStep, field.CostAt(new Vector2Int(1, 1)), 1e-5f);
            Assert.AreEqual(2f * FlowField.CornerStep, field.CostAt(new Vector2Int(2, 2)), 1e-5f);
        }

        [Test]
        public void OpenField_EveryDirectionPointsTowardTheTarget()
        {
            var field = new FlowField(OpenGrid(6));
            var target = new Vector2Int(1, -2);

            field.Compute(target, 100f);

            var targetGround = IsoMath.CellToGround(target);
            for (var y = -6; y <= 6; y++)
            for (var x = -6; x <= 6; x++)
            {
                var cell = new Vector2Int(x, y);
                if (cell == target)
                    continue;

                Assert.IsTrue(field.TryGetDirection(IsoMath.CellToGround(cell), out var direction), $"cell {cell}");
                var toTarget = (targetGround - IsoMath.CellToGround(cell)).normalized;
                Assert.Greater(Vector2.Dot(direction, toTarget), 0f, $"cell {cell}");
                Assert.AreEqual(1f, direction.magnitude, 1e-5f, $"cell {cell}");
            }
        }

        [Test]
        public void Target_HasNoDirection()
        {
            var field = new FlowField(OpenGrid(3));

            field.Compute(Vector2Int.zero, 100f);

            Assert.IsFalse(field.TryGetDirection(IsoMath.CellToGround(Vector2Int.zero), out _));
        }

        [Test]
        public void WallWithAGap_ForcesADetour_AndFollowingTheFieldReachesTheTarget()
        {
            // A wall along x = 0 from y = -5 to 5, open only at y = 5.
            var wall = new System.Collections.Generic.List<Vector2Int>();
            for (var y = -5; y <= 4; y++)
                wall.Add(new Vector2Int(0, y));
            var grid = OpenGrid(6, wall.ToArray());
            var field = new FlowField(grid);
            var target = new Vector2Int(3, 0);
            var start = new Vector2Int(-3, 0);

            field.Compute(target, 100f);

            var straightLine = Vector2.Distance(IsoMath.CellToGround(start), IsoMath.CellToGround(target));
            Assert.Greater(field.CostAt(start), straightLine + 3f, "the path should go around the wall");

            var cell = start;
            for (var steps = 0; cell != target; steps++)
            {
                Assert.Less(steps, grid.CellCount, "the walk should end");
                cell = Step(field, cell);
                Assert.IsTrue(grid.IsWalkable(cell), $"walked into a wall at {cell}");
            }
        }

        [Test]
        public void Corner_IsNotCut_WhenBothSideCellsAreBlocked()
        {
            var grid = new NavGrid(Vector2Int.zero, 2, 2);
            grid.SetWalkable(new Vector2Int(0, 0), true);
            grid.SetWalkable(new Vector2Int(1, 1), true);
            var field = new FlowField(grid);

            field.Compute(new Vector2Int(1, 1), 100f);

            Assert.IsTrue(float.IsPositiveInfinity(field.CostAt(new Vector2Int(0, 0))));
        }

        [Test]
        public void Corner_IsNotCut_WhenOneSideCellIsBlocked()
        {
            var grid = new NavGrid(Vector2Int.zero, 2, 2);
            grid.SetWalkable(new Vector2Int(0, 0), true);
            grid.SetWalkable(new Vector2Int(1, 0), true);
            grid.SetWalkable(new Vector2Int(1, 1), true);
            var field = new FlowField(grid);

            field.Compute(new Vector2Int(1, 1), 100f);

            // The diagonal (cost 1) is refused, so the path goes around through (1, 0): 0.707 + 0.707.
            Assert.AreEqual(2f * FlowField.EdgeStep, field.CostAt(new Vector2Int(0, 0)), 1e-5f);
        }

        [Test]
        public void EnclosedCell_IsUnreachable_AndHasNoDirection()
        {
            var ring = new[]
            {
                new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
                new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
            };
            var field = new FlowField(OpenGrid(4, ring));

            field.Compute(new Vector2Int(3, 3), 100f);

            Assert.IsTrue(float.IsPositiveInfinity(field.CostAt(Vector2Int.zero)));
            Assert.IsFalse(field.TryGetDirection(IsoMath.CellToGround(Vector2Int.zero), out _));
        }

        [Test]
        public void MaxCost_LeavesFarCellsUnreached()
        {
            var field = new FlowField(OpenGrid(8));

            field.Compute(Vector2Int.zero, 3f);

            Assert.IsFalse(float.IsPositiveInfinity(field.CostAt(new Vector2Int(2, 2))));
            Assert.IsTrue(float.IsPositiveInfinity(field.CostAt(new Vector2Int(6, 6))));
        }

        [Test]
        public void BlockedTarget_ProducesNoField()
        {
            var field = new FlowField(OpenGrid(3, new Vector2Int(0, 0)));

            field.Compute(Vector2Int.zero, 100f);

            Assert.IsFalse(field.HasTarget);
            Assert.IsFalse(field.TryGetDirection(IsoMath.CellToGround(new Vector2Int(2, 2)), out _));
        }

        [Test]
        public void Compute_CanBeRepeated_AndForgetsThePreviousTarget()
        {
            var field = new FlowField(OpenGrid(4));

            field.Compute(new Vector2Int(-3, -3), 100f);
            field.Compute(new Vector2Int(3, 3), 100f);

            Assert.AreEqual(0f, field.CostAt(new Vector2Int(3, 3)), 1e-5f);
            Assert.Greater(field.CostAt(new Vector2Int(-3, -3)), 5f);
        }

        [Test]
        public void LineOfSight_IsBlockedByAWall_AndOpenOtherwise()
        {
            var grid = OpenGrid(6, new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 1), new Vector2Int(-1, -1),
                new Vector2Int(1, -1), new Vector2Int(-1, 1));

            var across = grid.HasLineOfSight(IsoMath.CellToGround(new Vector2Int(-4, 0)), IsoMath.CellToGround(new Vector2Int(4, 0)));
            var clear = grid.HasLineOfSight(IsoMath.CellToGround(new Vector2Int(-4, -4)), IsoMath.CellToGround(new Vector2Int(-4, 4)));

            Assert.IsFalse(across);
            Assert.IsTrue(clear);
        }
    }
}
