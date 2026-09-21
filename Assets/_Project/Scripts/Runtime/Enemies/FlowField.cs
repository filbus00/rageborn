using System;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// A cost-to-target field over a <see cref="NavGrid"/>. One Dijkstra pass from the player's cell gives every
    /// enemy its next step toward the player, so the cost does not grow with the number of enemies.
    /// Moves are 8-way with true ground-space lengths and never cut across a blocked corner.
    /// </summary>
    public sealed class FlowField
    {
        // Ground length of a step to an edge neighbour of a diamond cell (0.5, 0.5) and to a corner neighbour (0, 1) or (1, 0).
        public const float EdgeStep = 0.70710678f;
        public const float CornerStep = 1f;

        static readonly Vector2Int[] Offsets =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
        };

        static readonly float[] Steps = new float[Offsets.Length];

        // Unit ground direction of a step from the neighbour back to the cell that relaxed it.
        static readonly Vector2[] TowardParent = new Vector2[Offsets.Length];

        readonly NavGrid grid;
        readonly float[] cost;
        readonly Vector2[] direction;

        // Binary min-heap with lazy deletion: a cell can be queued more than once, stale entries are skipped.
        float[] heapCost;
        int[] heapCell;
        int heapCount;

        static FlowField()
        {
            for (var i = 0; i < Offsets.Length; i++)
            {
                var offset = Offsets[i];
                Steps[i] = offset.x != 0 && offset.y != 0 ? CornerStep : EdgeStep;

                var ground = new Vector2((offset.x - offset.y) * 0.5f, (offset.x + offset.y) * 0.5f);
                TowardParent[i] = -ground.normalized;
            }
        }

        public FlowField(NavGrid grid)
        {
            this.grid = grid;
            cost = new float[grid.CellCount];
            direction = new Vector2[grid.CellCount];
            heapCost = new float[grid.CellCount * 2];
            heapCell = new int[grid.CellCount * 2];
        }

        /// <summary>The cell the field leads to, valid when <see cref="HasTarget"/> is true.</summary>
        public Vector2Int Target { get; private set; }

        public bool HasTarget { get; private set; }

        /// <summary>Recomputes the field toward <paramref name="target"/>. Cells costing more than maxCost stay unreached.</summary>
        public void Compute(Vector2Int target, float maxCost)
        {
            Array.Fill(cost, float.PositiveInfinity);
            Array.Clear(direction, 0, direction.Length);
            heapCount = 0;
            Target = target;
            HasTarget = false;

            if (!grid.IsWalkable(target))
                return;

            var start = grid.IndexOf(target);
            cost[start] = 0f;
            Push(0f, start);
            HasTarget = true;

            while (heapCount > 0)
            {
                Pop(out var currentCost, out var index);
                if (currentCost > cost[index])
                    continue;

                var cell = grid.CellAt(index);
                for (var i = 0; i < Offsets.Length; i++)
                {
                    var offset = Offsets[i];
                    var neighbour = cell + offset;
                    if (!grid.IsWalkable(neighbour))
                        continue;

                    // A corner step touches only a vertex, so both cells beside it must be open.
                    if (offset.x != 0 && offset.y != 0 &&
                        !(grid.IsWalkable(new Vector2Int(cell.x + offset.x, cell.y)) &&
                          grid.IsWalkable(new Vector2Int(cell.x, cell.y + offset.y))))
                        continue;

                    var newCost = currentCost + Steps[i];
                    if (newCost > maxCost)
                        continue;

                    var neighbourIndex = grid.IndexOf(neighbour);
                    if (newCost >= cost[neighbourIndex])
                        continue;

                    cost[neighbourIndex] = newCost;
                    direction[neighbourIndex] = TowardParent[i];
                    Push(newCost, neighbourIndex);
                }
            }
        }

        /// <summary>Ground distance along the lattice from the cell to the target, or infinity when unreached.</summary>
        public float CostAt(Vector2Int cell) => grid.Contains(cell) ? cost[grid.IndexOf(cell)] : float.PositiveInfinity;

        /// <summary>
        /// The unit ground direction of the next step toward the target from the cell containing the position.
        /// False when the cell is unreached, off the grid, or is the target itself.
        /// </summary>
        public bool TryGetDirection(Vector2 ground, out Vector2 result)
        {
            result = Vector2.zero;
            if (!HasTarget)
                return false;

            var cell = IsoMath.GroundToCell(ground);
            if (!grid.Contains(cell))
                return false;

            var index = grid.IndexOf(cell);
            if (float.IsPositiveInfinity(cost[index]) || cell == Target)
                return false;

            result = direction[index];
            return true;
        }

        void Push(float priority, int cell)
        {
            if (heapCount == heapCost.Length)
            {
                Array.Resize(ref heapCost, heapCount * 2);
                Array.Resize(ref heapCell, heapCount * 2);
            }

            var i = heapCount++;
            while (i > 0)
            {
                var parent = (i - 1) / 2;
                if (heapCost[parent] <= priority)
                    break;
                heapCost[i] = heapCost[parent];
                heapCell[i] = heapCell[parent];
                i = parent;
            }
            heapCost[i] = priority;
            heapCell[i] = cell;
        }

        void Pop(out float priority, out int cell)
        {
            priority = heapCost[0];
            cell = heapCell[0];

            heapCount--;
            if (heapCount == 0)
                return;

            var lastCost = heapCost[heapCount];
            var lastCell = heapCell[heapCount];
            var i = 0;
            while (true)
            {
                var child = i * 2 + 1;
                if (child >= heapCount)
                    break;
                if (child + 1 < heapCount && heapCost[child + 1] < heapCost[child])
                    child++;
                if (heapCost[child] >= lastCost)
                    break;
                heapCost[i] = heapCost[child];
                heapCell[i] = heapCell[child];
                i = child;
            }
            heapCost[i] = lastCost;
            heapCell[i] = lastCell;
        }
    }
}
