using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Which drawn pool a lasting ground pool shows (2026-10-31, the owner's Gemini pools in Resources/Art/Effects): the
    /// pools are known only by their colour, so by its hue. Red is blood, orange fire, green poison, pale blue frost and
    /// purple void; greys, bone and deep blue water keep the tinted blob (water drawn as ice would be wrong). Pure.
    /// </summary>
    public static class PoolLooks
    {
        public const string Folder = "Art/Effects/";

        /// <summary>What a drawn pool throws off as it lies (2026-11-01).</summary>
        public enum Spray { None, Embers, Bubbles, Glints, VoidSparks }

        /// <summary>
        /// How a drawn pool moves (2026-11-01, the owner: the pools "need to be animated"; a Gemini sprite sheet looked
        /// bad): two copies of its picture turn over it in opposite ways (degrees a second) at their strengths, its light
        /// pulses (how fast and how deep), and it throws off its spray, this many a second for a pool of radius 1. Claude's
        /// numbers, to review.
        /// </summary>
        public readonly struct Motion
        {
            public readonly float SpinA, SpinB, AlphaA, AlphaB, PulseSpeed, PulseDepth, SprayRate;
            public readonly Spray Spray;

            public Motion(float spinA, float spinB, float alphaA, float alphaB, float pulseSpeed, float pulseDepth, Spray spray, float sprayRate)
            {
                SpinA = spinA;
                SpinB = spinB;
                AlphaA = alphaA;
                AlphaB = alphaB;
                PulseSpeed = pulseSpeed;
                PulseDepth = pulseDepth;
                Spray = spray;
                SprayRate = sprayRate;
            }
        }

        /// <summary>The motion of a drawn pool by its name.</summary>
        public static Motion MotionOf(string name) => name switch
        {
            // Fire licks quickly and flickers hard, throwing embers.
            "pool_fire" => new Motion(38f, -55f, 0.5f, 0.35f, 9f, 0.22f, Spray.Embers, 7f),
            // Slime turns slowly and bubbles.
            "pool_poison" => new Motion(14f, -20f, 0.45f, 0.3f, 2.2f, 0.1f, Spray.Bubbles, 4f),
            // Ice hardly moves; frost glints over it.
            "pool_frost" => new Motion(5f, -7f, 0.3f, 0.2f, 1.6f, 0.08f, Spray.Glints, 4f),
            // The void swirls fast and pulls sparks into its heart.
            "pool_void" => new Motion(70f, 110f, 0.55f, 0.4f, 3f, 0.15f, Spray.VoidSparks, 6f),
            // Blood turns slowly and pulses like a heartbeat.
            "pool_blood" => new Motion(8f, -12f, 0.35f, 0.25f, 4.5f, 0.12f, Spray.None, 0f),
            _ => new Motion(10f, -14f, 0.35f, 0.25f, 2f, 0.1f, Spray.None, 0f),
        };

        /// <summary>The drawn pool's name for a colour, or null for none.</summary>
        public static string For(Color color)
        {
            Color.RGBToHSV(color, out var h, out var s, out _);
            if (s < 0.3f)
                return null;
            var hue = h * 360f;
            if (hue < 12f || hue >= 330f)
                return "pool_blood";
            if (hue < 50f)
                return "pool_fire";
            if (hue < 165f)
                return "pool_poison";
            if (hue < 250f)
                return s < 0.55f ? "pool_frost" : null;
            return "pool_void";
        }
    }
}
