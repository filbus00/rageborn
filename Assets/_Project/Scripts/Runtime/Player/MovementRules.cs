using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// How she moves, the parts that are pure (2026-11-15, the owner: "check the movement ... she seems to walk slower when
    /// walking diagonally", "feels less responsive", "the tree enemies that have a root attack ... the character starts
    /// gliding instead without getting rooted"). Tested.
    /// </summary>
    public static class MovementRules
    {
        /// <summary>
        /// How much of the screen's squash her speed gives back (the owner chose "half way"). The view is 2:1, so at one
        /// speed on the ground she covers 100 percent of it sideways on screen, 63 diagonally and 50 straight up or down.
        /// Her ground speed is raised by the screen share to this power (0 none, 1 an even speed on screen): half way
        /// gives 100, 79 and 71 percent.
        /// </summary>
        public const float ScreenSpeedBlend = 0.5f;

        /// <summary>The share of a ground speed that shows on screen going the way of this ground direction: 1 sideways,
        /// 0.5 straight up or down on the screen.</summary>
        public static float ScreenShare(Vector2 groundDirection)
        {
            if (groundDirection.sqrMagnitude < 1e-8f)
                return 1f;
            var d = groundDirection.normalized;
            var world = IsoMath.GroundToWorld(d);
            return world.magnitude;
        }

        /// <summary>The factor on her ground speed for the way she is heading (1 sideways, up to <c>0.5 ^ -blend</c> up or
        /// down): the screen share's inverse to the power of the blend.</summary>
        public static float DirectionFactor(Vector2 groundDirection, float blend = ScreenSpeedBlend) =>
            Mathf.Pow(1f / ScreenShare(groundDirection), blend);

        /// <summary>A slow this strong or stronger holds her where she stands (the Briar Thrall's Root Grasp is 0.05).</summary>
        public const float RootBelow = 0.1f;

        public static bool IsRooted(bool slowActive, float slowMultiplier) => slowActive && slowMultiplier <= RootBelow;

        /// <summary>How fast her velocity may change a second: what it takes to reach her unslowed speed in the
        /// acceleration time, whatever slows she has. It used to follow her slowed speed, so a slow or a shot's walk made her
        /// slow to start and slower to stop, and a root made her glide on.</summary>
        public static float Acceleration(float unslowedSpeed, float effectiveSpeed, float accelerationTime) =>
            Mathf.Max(unslowedSpeed, effectiveSpeed) / Mathf.Max(0.0001f, accelerationTime);
    }
}
