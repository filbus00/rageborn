using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The Wild Arrow walks while she fires (the owner, 2026-10-03: "when firing at enemies, wild arrow walks", and slows
    /// to a walk rather than only looking like it): each shot or skill slows her for the draw, so the walk animation's
    /// feet do not slide and running away means holding fire. Pure, so it is tested.
    /// </summary>
    public static class FiringPace
    {
        /// <summary>Move speed while firing: about a walk next to her run.</summary>
        public const float MoveMultiplier = 0.5f;

        /// <summary>How long one shot or skill keeps her walking, at most. A slower bow fires less often than this, so
        /// she breaks into a run between its shots.</summary>
        public const float HoldSeconds = 0.6f;

        /// <summary>The walk after a basic shot: the time to the next shot, up to <see cref="HoldSeconds"/>.</summary>
        public static float HoldFor(float attackInterval) => Mathf.Clamp(attackInterval, 0f, HoldSeconds);
    }
}
