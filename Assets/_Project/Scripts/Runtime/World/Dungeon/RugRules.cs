using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The long carpets (2026-10-20, the owner: "do add long carpets"): a runner two cells wide down a hall, each cell
    /// a <see cref="DecalKind"/> Rug kind drawn in code over the floor with velvet shading, a layered gold border and a
    /// diamond lattice (2026-10-26, the owner's rewrite from Gemini). Pure: the colour at a point of a cell.
    /// </summary>
    public static class RugRules
    {
        // Rich velvet red palette
        static readonly Color32 VelvetDeep = new Color32(65, 10, 12, 255);
        static readonly Color32 VelvetBase = new Color32(95, 15, 18, 255);
        static readonly Color32 VelvetHighlight = new Color32(125, 22, 25, 255);

        // Ornate metallic gold palette
        static readonly Color32 GoldShadow = new Color32(110, 80, 20, 255);
        static readonly Color32 GoldBase = new Color32(185, 140, 45, 255);
        static readonly Color32 GoldHighlight = new Color32(235, 205, 95, 255);

        // Edging and wear
        static readonly Color32 EdgeSelvedge = new Color32(25, 8, 8, 255);
        static readonly Color32 DustColor = new Color32(45, 35, 30, 255);

        public static bool IsRug(DecalKind kind) =>
            kind == DecalKind.RugXLow || kind == DecalKind.RugXHigh || kind == DecalKind.RugYLow || kind == DecalKind.RugYHigh;

        /// <summary>The runner's cell for a runner along x (or y), on its low or high side.</summary>
        public static DecalKind Kind(bool alongX, bool high) =>
            alongX ? high ? DecalKind.RugXHigh : DecalKind.RugXLow : high ? DecalKind.RugYHigh : DecalKind.RugYLow;

        /// <summary>The carpet picture in Resources/Art/Ground (2026-10-26): when it exists the game draws carpets from it
        /// and <see cref="Colour"/> is only the fallback.</summary>
        public const string ImageName = "rug_runner";

        /// <summary>How many cells of runner one height of the picture covers (it is two cells across and square).</summary>
        public const float CellsPerImage = 2f;

        /// <summary>
        /// Where a point of a rug cell falls on the carpet picture (0..1 each way; x across the runner from its low side,
        /// y along it, repeating every <see cref="CellsPerImage"/> cells): from the cell and the point's offsets from the
        /// cell's middle along x and y (each -0.5 to 0.5).
        /// </summary>
        public static Vector2 ImagePoint(DecalKind kind, Vector2Int cell, float u, float v)
        {
            var alongX = kind == DecalKind.RugXLow || kind == DecalKind.RugXHigh;
            var high = kind == DecalKind.RugXHigh || kind == DecalKind.RugYHigh;
            var along = alongX ? cell.x + u + 0.5f : cell.y + v + 0.5f;
            var across = Mathf.Clamp01((alongX ? v : u) + 0.5f);
            return new Vector2(((high ? 1f : 0f) + across) * 0.5f, Mathf.Repeat(along / CellsPerImage, 1f));
        }

        /// <summary>
        /// The colour at a point of a rug cell, from its offsets from the cell's middle along x and y (each -0.5 to 0.5)
        /// and a 0..1 noise value for wear: velvet volume shading, a multi-tone gold border, and a procedural diamond
        /// lattice.
        /// </summary>
        public static Color32 Colour(DecalKind kind, float u, float v, float noise)
        {
            var alongX = kind == DecalKind.RugXLow || kind == DecalKind.RugXHigh;
            var high = kind == DecalKind.RugXHigh || kind == DecalKind.RugYHigh;
            var along = alongX ? u : v;
            var across = alongX ? v : u;

            // Distance from the outer long side of the two-cell runner (0 at edge, 0.5 at center seam)
            var d = high ? 0.5f - across : across + 0.5f;

            Color32 c;

            // 1. Outer dark selvedge / border binding
            if (d < 0.05f)
            {
                c = EdgeSelvedge;
            }
            // 2. Multi-layered ornate gold border trim
            else if (d < 0.20f)
            {
                float borderDist = d - 0.05f;
                if (borderDist < 0.02f || borderDist > 0.13f)
                    c = GoldShadow;
                else if (Mathf.Repeat(borderDist * 80f, 1f) < 0.35f)
                    c = GoldHighlight;
                else
                    c = GoldBase;
            }
            // 3. Main carpet body with velvet shading and procedural diamond lattice embroidery
            else
            {
                // Velvet volume gradient (slightly lighter toward the center of the runner)
                float velvetFactor = Mathf.InverseLerp(0.20f, 0.5f, d);
                Color32 baseVelvet = Color.Lerp(VelvetBase, VelvetHighlight, velvetFactor);

                // Procedural diamond / lattice gold embroidery pattern in the center
                float patternAlong = Mathf.Repeat(along * 4f, 1f);
                float patternAcross = Mathf.Abs(d - 0.35f);
                float latticeVal = Mathf.Abs(patternAlong - 0.5f) + patternAcross * 2.2f;

                if (latticeVal > 0.32f && latticeVal < 0.42f)
                {
                    // Gold lattice lines
                    c = Mathf.Repeat(along * 16f, 1f) > 0.5f ? GoldBase : GoldHighlight;
                }
                else if (latticeVal >= 0.28f && latticeVal <= 0.32f)
                {
                    // Dark outline for embroidery depth
                    c = GoldShadow;
                }
                else
                {
                    // Inner rich red velvet with subtle cross-band texture
                    float band = Mathf.Repeat(along * 2f, 1f);
                    c = band < 0.08f ? VelvetDeep : baseVelvet;
                }
            }

            // Organic wear, fraying, and dust accumulation based on noise
            if (noise > 0.85f)
            {
                float wearBlend = Mathf.InverseLerp(0.85f, 1f, noise);
                c = Color.Lerp(c, DustColor, wearBlend * 0.7f);
            }

            return c;
        }
    }
}
