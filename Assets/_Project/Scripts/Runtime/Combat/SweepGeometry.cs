using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Geometry of melee sweeps and target choice, in ground space (Docs/01-core-gameplay.md). Pure.
    /// </summary>
    public static class SweepGeometry
    {
        /// <summary>The forward cone that decides which enemy is preferred as a target, in degrees.</summary>
        public const float ForwardConeDegrees = 200f;

        /// <summary>
        /// True when the point lies inside the sweep: within range of the origin and within half the arc
        /// on either side of the aim direction. The origin itself counts as inside.
        /// </summary>
        public static bool Contains(Vector2 origin, Vector2 aimDirection, Vector2 point, float range, float arcDegrees)
        {
            var offset = point - origin;
            var sqrDistance = offset.sqrMagnitude;
            if (sqrDistance > range * range)
                return false;
            if (sqrDistance < 1e-8f)
                return true;

            return Vector2.Angle(aimDirection, offset) <= arcDegrees * 0.5f;
        }

        /// <summary>
        /// Chooses the target from the candidates: the nearest one inside the forward cone, or the nearest overall
        /// when none is inside it. Returns its index, or -1 for no candidates.
        /// </summary>
        public static int PickTarget(Vector2 origin, Vector2 facing, IReadOnlyList<Vector2> positions)
        {
            var nearest = -1;
            var nearestDistance = float.MaxValue;
            var nearestInCone = -1;
            var nearestInConeDistance = float.MaxValue;

            for (var i = 0; i < positions.Count; i++)
            {
                var offset = positions[i] - origin;
                var distance = offset.sqrMagnitude;

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = i;
                }

                var inCone = distance < 1e-8f || Vector2.Angle(facing, offset) <= ForwardConeDegrees * 0.5f;
                if (inCone && distance < nearestInConeDistance)
                {
                    nearestInConeDistance = distance;
                    nearestInCone = i;
                }
            }

            return nearestInCone >= 0 ? nearestInCone : nearest;
        }
    }
}
