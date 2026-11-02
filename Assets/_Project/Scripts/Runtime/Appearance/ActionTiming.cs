using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// How a one-shot's frames spread over its time (2026-11-01, the owner: "I feel that the animations are off"): even, as
    /// baked; a snap for her shots and skills, racing through the draw and the loose (she fires when it starts) and
    /// settling in the follow-through; a burst (the dive's until 2026-11-02, when her new dive's roll needed its frames
    /// spread evenly over the travel). Pure.
    /// </summary>
    public enum ActionShape { Even, Snap, Burst }

    public static class ActionTiming
    {
        /// <summary>How far through its frames a one-shot is at this share of its time, both 0 to 1.</summary>
        public static float Shape(ActionShape shape, float progress)
        {
            var t = Mathf.Clamp01(progress);
            var rest = 1f - t;
            return shape switch
            {
                // Three quarters of the frames in the first half of the time.
                ActionShape.Snap => 1f - rest * rest,
                // Seven eighths in the first half.
                ActionShape.Burst => 1f - rest * rest * rest,
                _ => t,
            };
        }
    }
}
