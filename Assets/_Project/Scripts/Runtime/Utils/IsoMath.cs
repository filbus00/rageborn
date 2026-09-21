using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Conversions between screen-space input, the ground plane and Unity world space for the 2:1 isometric view.
    /// Ground space is what gameplay uses: 1 unit is one tile width and distances are the same in every direction.
    /// World space is what Unity draws: the ground is squashed to half height, so a ground circle is a 2:1 ellipse.
    /// </summary>
    public static class IsoMath
    {
        /// <summary>Vertical squash of the ground plane on screen (a 2:1 dimetric view).</summary>
        public const float GroundSquash = 0.5f;

        public static Vector2 GroundToWorld(Vector2 ground) => new Vector2(ground.x, ground.y * GroundSquash);

        public static Vector2 WorldToGround(Vector2 world) => new Vector2(world.x, world.y / GroundSquash);

        /// <summary>
        /// Turns a screen-space stick vector (magnitude 0 to 1) into a ground-space vector with the same
        /// magnitude, so the character moves at the same ground speed in every direction while still
        /// travelling in the direction the thumb points on screen.
        /// </summary>
        public static Vector2 StickToGround(Vector2 stick)
        {
            var magnitude = stick.magnitude;
            if (magnitude <= 0f)
                return Vector2.zero;

            var ground = WorldToGround(stick);
            return ground / ground.magnitude * magnitude;
        }

        /// <summary>
        /// Ground position of the center of an isometric tilemap cell. Assumes the Grid sits at the world origin with
        /// a 1 x 0.5 cell size. In ground space the tile lattice is a square grid rotated by 45 degrees: a neighbour
        /// across an edge is 0.707 units away and a neighbour across a corner is 1 unit away.
        /// </summary>
        public static Vector2 CellToGround(Vector2Int cell) =>
            new Vector2((cell.x - cell.y) * 0.5f, (cell.x + cell.y) * 0.5f + 0.5f);

        /// <summary>The tilemap cell whose diamond contains the ground position. Inverse of <see cref="CellToGround"/>.</summary>
        public static Vector2Int GroundToCell(Vector2 ground)
        {
            // A cell diamond is a unit square in (u, v), where u and v are its coordinates along the lattice axes.
            var y = ground.y - 0.5f;
            return new Vector2Int(Mathf.FloorToInt(y + ground.x + 0.5f), Mathf.FloorToInt(y - ground.x + 0.5f));
        }
    }
}
