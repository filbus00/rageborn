using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ARPG
{
    /// <summary>
    /// Builds a <see cref="NavGrid"/> from tilemaps: a cell is walkable when the ground has a tile there and no
    /// obstacle tilemap does. Walls are a tilemap on the Obstacle layer, so this needs no physics queries.
    /// </summary>
    public static class NavGridBaker
    {
        /// <param name="ground">The ground tilemap. Its Grid must sit at the world origin, see <see cref="IsoMath.CellToGround"/>.</param>
        /// <param name="obstacles">Tilemaps whose tiles block movement, sharing the ground's Grid.</param>
        public static NavGrid Bake(Tilemap ground, IReadOnlyList<Tilemap> obstacles)
        {
            var bounds = ground.cellBounds;
            var grid = new NavGrid(new Vector2Int(bounds.xMin, bounds.yMin), bounds.size.x, bounds.size.y);

            foreach (var position in bounds.allPositionsWithin)
            {
                if (ground.HasTile(position) && !IsBlocked(position, obstacles))
                    grid.SetWalkable(new Vector2Int(position.x, position.y), true);
            }

            return grid;
        }

        static bool IsBlocked(Vector3Int position, IReadOnlyList<Tilemap> obstacles)
        {
            for (var i = 0; i < obstacles.Count; i++)
                if (obstacles[i].HasTile(position))
                    return true;
            return false;
        }
    }
}
