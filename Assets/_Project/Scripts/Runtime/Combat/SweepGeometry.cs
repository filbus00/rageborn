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

        /// <summary>Docs/01-core-gameplay.md: elites and bosses get a 30 percent range weight bonus so they are
        /// preferred when near. Applied as a shrink on the effective distance used to pick a target.</summary>
        public const float PreferredTargetWeight = 0.7f;

        /// <summary>
        /// Chooses the target from the candidates: the nearest one inside the forward cone, or the nearest overall
        /// when none is inside it. Returns its index, or -1 for no candidates.
        /// </summary>
        /// <param name="priorityWeights">Optional, one per candidate: an effective-distance multiplier below 1
        /// makes that candidate preferred over an equally distant or somewhat closer one (see
        /// <see cref="PreferredTargetWeight"/> for elites and bosses). Null or omitted treats every candidate the
        /// same, as before this parameter existed.</param>
        /// <summary>A bow's target keeps being shot until another is closer by this much (ground units, after the elite
        /// weight), so two enemies at about the same distance do not take turns.</summary>
        public const float KeepTargetMargin = 0.75f;

        /// <summary>
        /// The Wild Arrow's target (the owner, 2026-10-03: she fired at a far enemy while one stood right by her): the
        /// nearest candidate whichever way she moves, elites and bosses weighted as in <see cref="PickTarget"/>. The
        /// current target, <paramref name="current"/> (an index into the candidates, or -1), is kept unless another is
        /// closer by more than <see cref="KeepTargetMargin"/>. Returns an index, or -1 for no candidates.
        /// </summary>
        public static int PickNearest(Vector2 origin, IReadOnlyList<Vector2> positions, IReadOnlyList<float> priorityWeights = null, int current = -1)
        {
            var nearest = -1;
            var nearestDistance = float.MaxValue;
            var currentDistance = float.MaxValue;
            for (var i = 0; i < positions.Count; i++)
            {
                var weight = priorityWeights != null ? priorityWeights[i] : 1f;
                var distance = (positions[i] - origin).magnitude * weight;
                if (i == current)
                    currentDistance = distance;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = i;
                }
            }
            return current >= 0 && current < positions.Count && currentDistance <= nearestDistance + KeepTargetMargin ? current : nearest;
        }

        public static int PickTarget(Vector2 origin, Vector2 facing, IReadOnlyList<Vector2> positions, IReadOnlyList<float> priorityWeights = null)
        {
            var nearest = -1;
            var nearestDistance = float.MaxValue;
            var nearestInCone = -1;
            var nearestInConeDistance = float.MaxValue;

            for (var i = 0; i < positions.Count; i++)
            {
                var offset = positions[i] - origin;
                var weight = priorityWeights != null ? priorityWeights[i] : 1f;
                var distance = offset.sqrMagnitude * weight * weight;

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = i;
                }

                var inCone = offset.sqrMagnitude < 1e-8f || Vector2.Angle(facing, offset) <= ForwardConeDegrees * 0.5f;
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
