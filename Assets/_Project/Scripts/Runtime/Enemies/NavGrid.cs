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

        public NavGrid(Vector2Int min, int width, int height)
        {
            Min = min;
            Width = width;
            Height = height;
            walkable = new bool[width * height];
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

        public int IndexOf(Vector2Int cell) => (cell.y - Min.y) * Width + (cell.x - Min.x);

        public Vector2Int CellAt(int index) => new Vector2Int(Min.x + index % Width, Min.y + index / Width);

        /// <summary>True when every point on the straight ground-space segment from a to b lies on a walkable cell.</summary>
        public bool HasLineOfSight(Vector2 a, Vector2 b)
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
