using UnityEngine;
using UnityEngine.Tilemaps;

namespace ARPG
{
    /// <summary>Builds a <see cref="NavGrid"/> from a tilemap: a cell is walkable when it has a tile and no obstacle on it.</summary>
    public static class NavGridBaker
    {
        /// <param name="tilemap">The ground tilemap. Its Grid must sit at the world origin, see <see cref="IsoMath.CellToGround"/>.</param>
        /// <param name="obstacleMask">Physics layers that block movement. Checked at each cell center.</param>
        public static NavGrid Bake(Tilemap tilemap, int obstacleMask)
        {
            var bounds = tilemap.cellBounds;
            var grid = new NavGrid(new Vector2Int(bounds.xMin, bounds.yMin), bounds.size.x, bounds.size.y);

            foreach (var position in bounds.allPositionsWithin)
            {
                if (!tilemap.HasTile(position))
                    continue;

                var center = (Vector2)tilemap.GetCellCenterWorld(position);
                var blocked = obstacleMask != 0 && Physics2D.OverlapPoint(center, obstacleMask) != null;
                grid.SetWalkable(new Vector2Int(position.x, position.y), !blocked);
            }

            return grid;
        }
    }
}
