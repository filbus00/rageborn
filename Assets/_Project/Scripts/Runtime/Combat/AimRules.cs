using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Her aim by how far the thumb is pushed from the stick's middle (the owner, 2026-10-08): standing still or
    /// walking slowly she aims true, faster 80 percent, at full run 60 percent. Bad aim makes arrows leave off target by
    /// a random angle, so they can miss for real; Homing Arrow steers itself and never strays.
    /// </summary>
    public static class AimRules
    {
        /// <summary>Up to this much push (0 at the middle, 1 at the ring's edge) the aim is perfect.</summary>
        public const float SteadyPush = 1f / 3f;

        /// <summary>Up to this much push the aim is <see cref="WalkingAim"/>; past it <see cref="RunningAim"/>.</summary>
        public const float WalkingPush = 2f / 3f;

        public const float WalkingAim = 0.8f;
        public const float RunningAim = 0.6f;

        /// <summary>The widest angle that still hits an ordinary enemy (body 0.3, arrow 0.3) 5 units away. An arrow
        /// strays evenly up to this divided by the aim either side, so at that distance it hits about aim percent of the
        /// time: 80 percent aim strays up to 8.6 degrees, 60 percent up to 11.4.</summary>
        public static readonly float ReferenceDegrees = Mathf.Atan2(0.6f, 5f) * Mathf.Rad2Deg;

        /// <summary>The aim (1, 0.8 or 0.6) for a push of the stick from 0 to 1.</summary>
        public static float Aim(float push) => push <= SteadyPush ? 1f : push <= WalkingPush ? WalkingAim : RunningAim;

        /// <summary>The widest stray either side, in degrees, for an aim. Zero at perfect aim.</summary>
        public static float MaxStrayDegrees(float aim) => aim >= 1f ? 0f : ReferenceDegrees / Mathf.Max(0.1f, aim);

        /// <summary>A stray angle in degrees for an aim, from a roll between 0 and 1.</summary>
        public static float StrayDegrees(float aim, float roll) => (roll * 2f - 1f) * MaxStrayDegrees(aim);
    }
}
