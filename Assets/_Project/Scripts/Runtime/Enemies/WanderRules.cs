using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Packs that wander (2026-10-26, the owner: "make the enemies wander around on the map. Keep the big packs but make
    /// the packs wander a small radius and add smaller packs, duos or trios that wander around a bigger area"). A calm
    /// pack's middle drifts from point to point within its radius of where it was placed, pausing at each; its idle
    /// members stroll after it, each keeping its place in the pack. Once woken they fight and leash home as before. Pure.
    /// Claude's numbers, to review.
    /// </summary>
    public static class WanderRules
    {
        /// <summary>How far a big pack drifts from where it was placed, in ground units.</summary>
        public const float PackRadius = 3f;

        /// <summary>How far a roaming duo or trio wanders.</summary>
        public const float RoamRadius = 12f;

        /// <summary>How fast a pack's middle drifts, in ground units a second.</summary>
        public const float Speed = 0.8f;

        /// <summary>How long a pack stands at each point before it moves on, in seconds.</summary>
        public const float MinPause = 2f, MaxPause = 6f;

        /// <summary>The share of its speed an idle member strolls at.</summary>
        public const float StrollFactor = 0.45f;

        /// <summary>How far from its place in the pack a member stands before it walks back to it.</summary>
        public const float Slack = 0.35f;

        /// <summary>A point to drift to: within the radius of the origin, spread evenly over the disc, from two rolls
        /// between 0 and 1.</summary>
        public static Vector2 Target(Vector2 origin, float radius, float roll, float turn)
        {
            var distance = radius * Mathf.Sqrt(Mathf.Clamp01(roll));
            var angle = turn * Mathf.PI * 2f;
            return origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
        }

        /// <summary>The pack's middle a step closer to its target; true when it got there.</summary>
        public static bool Step(ref Vector2 anchor, Vector2 target, float deltaTime)
        {
            var to = target - anchor;
            var step = Speed * deltaTime;
            if (to.magnitude <= step)
            {
                anchor = target;
                return true;
            }
            anchor += to.normalized * step;
            return false;
        }
    }
}
