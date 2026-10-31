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
