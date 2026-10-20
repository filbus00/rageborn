using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Walkability of the isometric tilemap cells, stored in cell coordinates. Built once per zone by
    /// <see cref="NavGridBaker"/> and read by the flow field and by enemy steering.
    /// </summary>
    public sealed class NavGrid
    {
        // Line of sight samples this far apart on the ground. A cell diamond is 0.7 units across at its narrowest.
        const float LineOfSightStep = 0.25f;

        readonly bool[] walkable;
        // Low cells (2026-10-20): a dais's or pit's rim, a terrace's ledge. They block walking but not sight or shots, so
        // archers on a dais shoot down and she shoots back up.
        readonly bool[] low;

        public NavGrid(Vector2Int min, int width, int height)
        {
            Min = min;
            Width = width;
            Height = height;
            walkable = new bool[width * height];
            low = new bool[width * height];
        }

        /// <summary>The lowest cell coordinate covered by the grid.</summary>
        public Vector2Int Min { get; }

        public int Width { get; }

        public int Height { get; }

        public int CellCount => walkable.Length;

        public bool Contains(Vector2Int cell) =>
            cell.x >= Min.x && cell.y >= Min.y && cell.x < Min.x + Width && cell.y < Min.y + Height;

        public bool IsWalkable(Vector2Int cell) => Contains(cell) && walkable[IndexOf(cell)];

        public void SetWalkable(Vector2Int cell, bool value)
        {
            if (Contains(cell))
                walkable[IndexOf(cell)] = value;
        }

        /// <summary>Marks a cell low: not walkable, but open to sight and to shots.</summary>
        public void SetLow(Vector2Int cell, bool value)
        {
            if (Contains(cell))
                low[IndexOf(cell)] = value;
        }

        /// <summary>Whether sight and shots pass a cell: walkable, or low (a rim).</summary>
        public bool IsOpenToShots(Vector2Int cell) => Contains(cell) && (walkable[IndexOf(cell)] || low[IndexOf(cell)]);

        public int IndexOf(Vector2Int cell) => (cell.y - Min.y) * Width + (cell.x - Min.x);

        public Vector2Int CellAt(int index) => new Vector2Int(Min.x + index % Width, Min.y + index / Width);

        /// <summary>True when every point on the straight ground-space segment from a to b lies on a cell open to sight
        /// (walkable, or low: shots and sight pass over a rim).</summary>
        public bool HasLineOfSight(Vector2 a, Vector2 b)
        {
            var delta = b - a;
            var steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / LineOfSightStep));

            for (var i = 0; i <= steps; i++)
                if (!IsOpenToShots(IsoMath.GroundToCell(a + delta * (i / (float)steps))))
                    return false;
            return true;
        }

        /// <summary>True when every point on the straight segment lies on a walkable cell: a straight walk from a to b
        /// (a rim blocks it, though it does not block sight).</summary>
        public bool HasClearWalk(Vector2 a, Vector2 b)
        {
            var delta = b - a;
            var steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / LineOfSightStep));

            for (var i = 0; i <= steps; i++)
                if (!IsWalkable(IsoMath.GroundToCell(a + delta * (i / (float)steps))))
                    return false;
            return true;
        }
    }
}
