using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Which cells of a dungeon level the player has seen, for the mini-map (Docs/05-world-and-content.md: it shows
    /// explored rooms). Cells within <see cref="RevealRadius"/> of the player are revealed as they walk, which uncovers
    /// corridors a stretch at a time, and stepping into a room reveals the whole room and its walls. The flags live in
    /// an array the caller owns, so the session can keep one per level. Pure.
    /// </summary>
    public sealed class MinimapReveal
    {
        /// <summary>In cells. About the half-width of the portrait view at camera size 10.</summary>
        public const int RevealRadius = 10;

        readonly DungeonLayout layout;
        readonly bool[] explored;
        Vector2Int lastCell = new Vector2Int(int.MinValue, int.MinValue);

        /// <param name="flags">One flag per cell of the layout's bounds, row by row; reused across visits to the level.</param>
        public MinimapReveal(DungeonLayout layout, bool[] flags)
        {
            this.layout = layout;
            explored = flags;
        }

        public static int CellCount(DungeonLayout layout) => layout.Bounds.width * layout.Bounds.height;

        public bool IsExplored(Vector2Int cell)
        {
            var i = Index(cell);
            return i >= 0 && explored[i];
        }

        /// <summary>
        /// Reveals around the player's cell. Does nothing while the player stays in the same cell. Returns whether any
        /// cell became explored, so the map only redraws when it changed.
        /// </summary>
        public bool RevealAround(Vector2Int cell)
        {
            if (cell == lastCell)
                return false;
            lastCell = cell;

            var changed = false;
            var r2 = RevealRadius * RevealRadius;
            for (var dx = -RevealRadius; dx <= RevealRadius; dx++)
                for (var dy = -RevealRadius; dy <= RevealRadius; dy++)
                    if (dx * dx + dy * dy <= r2)
                        changed |= Reveal(new Vector2Int(cell.x + dx, cell.y + dy));

            foreach (var room in layout.Rooms)
            {
                if (!room.Interior.Contains(cell))
                    continue;
                // The room and the ring of walls around it.
                for (var x = room.Interior.xMin - 1; x <= room.Interior.xMax; x++)
                    for (var y = room.Interior.yMin - 1; y <= room.Interior.yMax; y++)
                        changed |= Reveal(new Vector2Int(x, y));
            }
            return changed;
        }

        bool Reveal(Vector2Int cell)
        {
            var i = Index(cell);
            if (i < 0 || explored[i] || layout.Get(cell) == DungeonCell.Void)
                return false;
            explored[i] = true;
            return true;
        }

        int Index(Vector2Int cell)
        {
            var b = layout.Bounds;
            var x = cell.x - b.xMin;
            var y = cell.y - b.yMin;
            if (x < 0 || y < 0 || x >= b.width || y >= b.height)
                return -1;
            return x + y * b.width;
        }
    }
}
