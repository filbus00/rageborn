using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// A uniform grid over ground space for radius queries. Rebuilt every frame with <see cref="Clear"/> and
    /// <see cref="Insert"/>, then queried without allocating. Points outside the bounds are clamped into the edge
    /// cells, so queries stay correct for them, only slower.
    /// </summary>
    public sealed class SpatialHash
    {
        readonly Vector2 origin;
        readonly float inverseCellSize;
        readonly int columns;
        readonly int rows;
        readonly int[] cellHead;

        int[] next;
        Vector2[] points;
        int count;

        public SpatialHash(Vector2 min, Vector2 max, float cellSize, int capacity)
        {
            origin = min;
            inverseCellSize = 1f / cellSize;
            columns = Mathf.Max(1, Mathf.CeilToInt((max.x - min.x) * inverseCellSize));
            rows = Mathf.Max(1, Mathf.CeilToInt((max.y - min.y) * inverseCellSize));
            cellHead = new int[columns * rows];
            next = new int[Mathf.Max(1, capacity)];
            points = new Vector2[next.Length];
            Clear();
        }

        public int Count => count;

        public void Clear()
        {
            Array.Fill(cellHead, -1);
            count = 0;
        }

        /// <summary>Adds a point and returns its id, which is its insertion index for this frame.</summary>
        public int Insert(Vector2 point)
        {
            if (count == points.Length)
            {
                Array.Resize(ref next, count * 2);
                Array.Resize(ref points, count * 2);
            }

            var cell = CellIndex(Column(point.x), Row(point.y));
            next[count] = cellHead[cell];
            cellHead[cell] = count;
            points[count] = point;
            return count++;
        }

        public Vector2 PointAt(int id) => points[id];

        /// <summary>Clears <paramref name="results"/>, then fills it with the ids of every point within radius of center.</summary>
        public void Query(Vector2 center, float radius, List<int> results)
        {
            results.Clear();

            var minColumn = Column(center.x - radius);
            var maxColumn = Column(center.x + radius);
            var minRow = Row(center.y - radius);
            var maxRow = Row(center.y + radius);
            var radiusSquared = radius * radius;

            for (var row = minRow; row <= maxRow; row++)
            for (var column = minColumn; column <= maxColumn; column++)
                for (var i = cellHead[CellIndex(column, row)]; i >= 0; i = next[i])
                    if ((points[i] - center).sqrMagnitude <= radiusSquared)
                        results.Add(i);
        }

        int Column(float x) => Mathf.Clamp(Mathf.FloorToInt((x - origin.x) * inverseCellSize), 0, columns - 1);

        int Row(float y) => Mathf.Clamp(Mathf.FloorToInt((y - origin.y) * inverseCellSize), 0, rows - 1);

        int CellIndex(int column, int row) => row * columns + column;
    }
}
