using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The floating stick's maths, kept free of touch handling so it can be unit tested.
    /// Rules come from Docs/01-core-gameplay.md, Input model.
    /// </summary>
    public static class StickMath
    {
        /// <summary>The base follows the thumb once it is further than this many radii from the base.</summary>
        public const float DriftFactor = 1.6f;

        /// <summary>
        /// Evaluates the stick for one thumb position. Returns the analog value: direction is continuous,
        /// magnitude is 0 inside the dead zone and rises to 1 at full radius. When the thumb has slid past
        /// <see cref="DriftFactor"/> radii, <paramref name="origin"/> is dragged along behind it.
        /// </summary>
        /// <param name="origin">Where the stick base sits, in pixels. Updated when the base follows the thumb.</param>
        /// <param name="thumb">Current thumb position, in pixels.</param>
        /// <param name="radius">Stick radius in pixels.</param>
        /// <param name="deadZone">Dead zone as a fraction of the radius.</param>
        public static Vector2 Evaluate(ref Vector2 origin, Vector2 thumb, float radius, float deadZone)
        {
            var offset = thumb - origin;
            var distance = offset.magnitude;
            if (distance <= 0f)
                return Vector2.zero;

            var direction = offset / distance;

            var maxDistance = radius * DriftFactor;
            if (distance > maxDistance)
            {
                origin = thumb - direction * maxDistance;
                distance = maxDistance;
            }

            var deflection = Mathf.Min(distance / radius, 1f);
            if (deflection <= deadZone)
                return Vector2.zero;

            var strength = (deflection - deadZone) / (1f - deadZone);
            return direction * strength;
        }
    }
}
