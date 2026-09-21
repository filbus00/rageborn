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
    }
}
