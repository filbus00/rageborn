using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The inside of one hand-authored room (Docs/05-world-and-content.md, room library): a square of cells, each floor
    /// or blocked (a pillar or wall piece). Written as text, one row per line, '.' for floor and '#' for blocked, top row
    /// first, so a designer can draw a room in the inspector. The generator adds the surrounding walls and the doorways
    /// at the middle of each side. Pure.
    /// </summary>
    public sealed class RoomShape
    {
        /// <summary>Doorways are this many cells wide, matching the corridors. 5 (it was 3) keeps the level open and
        /// lets a pack follow the player through without jamming.</summary>
        public const int DoorWidth = 5;

        /// <summary>Cells kept clear inside each doorway, so a pillar can never close a door.</summary>
        public const int DoorApproachDepth = 2;

        readonly bool[,] blocked;

        public RoomShape(string name, bool[,] blocked)
        {
            Name = name;
            this.blocked = blocked;
            Size = blocked.GetLength(0);
        }

        public string Name { get; }

        /// <summary>Width and height in cells.</summary>
        public int Size { get; }

        /// <summary>Whether the cell is blocked. x runs along a row, y up from the bottom row, both 0 to Size - 1.</summary>
        public bool IsBlocked(int x, int y) => blocked[x, y];

        public bool IsBlocked(Vector2Int cell) => blocked[cell.x, cell.y];

        /// <summary>
        /// Reads a room from text. Throws <see cref="FormatException"/> for a room the generator could not use: not
        /// square, an unknown character, smaller than a doorway allows, a doorway approach that is blocked, or floor
        /// that cannot be reached from the doorways.
        /// </summary>
        public static RoomShape Parse(string name, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new FormatException($"Room {name} is empty.");

            var rows = new List<string>();
            foreach (var line in text.Split('\n'))
            {
                var row = line.Trim();
                if (row.Length > 0)
                    rows.Add(row);
            }

            var size = rows.Count;
            if (size < DoorWidth + 2 * DoorApproachDepth)
                throw new FormatException($"Room {name} is {size} rows tall; it needs at least {DoorWidth + 2 * DoorApproachDepth}.");

            var blocked = new bool[size, size];
            for (var r = 0; r < size; r++)
            {
                if (rows[r].Length != size)
                    throw new FormatException($"Room {name} is not square: row {r + 1} has {rows[r].Length} cells, the room has {size} rows.");

                // Text rows run top to bottom; cell y runs bottom to top.
                var y = size - 1 - r;
                for (var x = 0; x < size; x++)
                {
                    var c = rows[r][x];
                    if (c == '#')
                        blocked[x, y] = true;
                    else if (c != '.')
                        throw new FormatException($"Room {name} has an unknown character '{c}' at row {r + 1}, column {x + 1}. Use '.' or '#'.");
                }
            }

            var shape = new RoomShape(name, blocked);
            shape.Validate();
            return shape;
        }

        /// <summary>The floor cells just inside the doorway on one side, for the given outward direction.</summary>
        public IEnumerable<Vector2Int> DoorApproach(Vector2Int side)
        {
            var mid = Size / 2;
            for (var depth = 0; depth < DoorApproachDepth; depth++)
            {
                for (var w = -(DoorWidth / 2); w <= DoorWidth / 2; w++)
                {
                    if (side == Vector2Int.right)
                        yield return new Vector2Int(Size - 1 - depth, mid + w);
                    else if (side == Vector2Int.left)
                        yield return new Vector2Int(depth, mid + w);
                    else if (side == Vector2Int.up)
                        yield return new Vector2Int(mid + w, Size - 1 - depth);
                    else
                        yield return new Vector2Int(mid + w, depth);
                }
            }
        }

        public static readonly Vector2Int[] Sides = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };

        void Validate()
        {
            foreach (var side in Sides)
                foreach (var cell in DoorApproach(side))
                    if (IsBlocked(cell))
                        throw new FormatException($"Room {Name} blocks its doorway at cell {cell}; keep the middle {DoorWidth} cells of each side and the {DoorApproachDepth} rows inside them clear.");

            // Every floor cell must be reachable from a doorway, or packs and chests could be placed where the player cannot go.
            var seen = new bool[Size, Size];
            var queue = new Queue<Vector2Int>();
            foreach (var start in DoorApproach(Vector2Int.right))
            {
                seen[start.x, start.y] = true;
                queue.Enqueue(start);
            }

            var reached = 0;
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                reached++;
                foreach (var step in Sides)
                {
                    var next = cell + step;
                    if (next.x < 0 || next.y < 0 || next.x >= Size || next.y >= Size || seen[next.x, next.y] || IsBlocked(next))
                        continue;
                    seen[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }

            var floor = 0;
            for (var x = 0; x < Size; x++)
                for (var y = 0; y < Size; y++)
                    if (!blocked[x, y])
                        floor++;

            if (reached != floor)
                throw new FormatException($"Room {Name} has {floor - reached} floor cells that cannot be reached from its doorways.");
        }
    }
}
