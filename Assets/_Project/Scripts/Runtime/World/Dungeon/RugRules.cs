using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The long carpets (2026-10-20, the owner: "do add long carpets"): a runner two cells wide down a hall, each cell
    /// a <see cref="DecalKind"/> Rug kind drawn in code over the floor. Pure: the colour at a point of a cell.
    /// </summary>
    public static class RugRules
    {
        static readonly Color32 Red = new Color32(104, 22, 22, 255);
        static readonly Color32 DarkRed = new Color32(76, 14, 16, 255);
        static readonly Color32 Gold = new Color32(168, 124, 50, 255);
        static readonly Color32 Edge = new Color32(36, 18, 14, 255);

        public static bool IsRug(DecalKind kind) =>
            kind == DecalKind.RugXLow || kind == DecalKind.RugXHigh || kind == DecalKind.RugYLow || kind == DecalKind.RugYHigh;

        /// <summary>The runner's cell for a runner along x (or y), on its low or high side.</summary>
        public static DecalKind Kind(bool alongX, bool high) =>
            alongX ? high ? DecalKind.RugXHigh : DecalKind.RugXLow : high ? DecalKind.RugYHigh : DecalKind.RugYLow;

        /// <summary>
        /// The colour at a point of a rug cell, from its offsets from the cell's middle along x and y (each -0.5 to 0.5)
        /// and a 0..1 noise value for wear: the gold border along the outer long side, the dark selvedge outside it, red
        /// with a darker band across every half cell.
        /// </summary>
        public static Color32 Colour(DecalKind kind, float u, float v, float noise)
        {
            var alongX = kind == DecalKind.RugXLow || kind == DecalKind.RugXHigh;
            var high = kind == DecalKind.RugXHigh || kind == DecalKind.RugYHigh;
            var along = alongX ? u : v;
            var across = alongX ? v : u;
            // Distance in from the outer long side.
            var d = high ? 0.5f - across : across + 0.5f;
            Color32 c;
            if (d < 0.07f)
                c = Edge;
            else if (d < 0.2f)
                c = d > 0.12f && d < 0.15f ? DarkRed : Gold;
            else
            {
                var band = Mathf.Repeat(along * 2f + 0.5f, 1f);
                c = band < 0.1f ? DarkRed : Red;
            }
            // Worn: a few pixels a shade darker.
            if (noise > 0.88f)
                c = new Color32((byte)(c.r * 0.8f), (byte)(c.g * 0.8f), (byte)(c.b * 0.8f), 255);
            return c;
        }
    }
}
