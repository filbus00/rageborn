using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// A baked figure made to read as defined pixel art (2026-11-05, the owner: "Start remaking the character into pixel
    /// art. Make her much more defined. She has a very weak presence on screen right now"). Two passes on a frame, rows
    /// from the bottom: <see cref="Shape"/> before the palette (richer colour, shade pushed into harder bands, shadows
    /// cooler and lights warmer, as pixel artists shade, in five flat steps), and <see cref="Volume"/> after the outline
    /// (lone pixels cleaned up, the inner edge on the lower right shaded and on the upper left lifted, and a dark line
    /// where two parts of very different light meet: her face and her hood, her arm and her tunic). Pure.
    /// </summary>
    public static class PixelDefine
    {
        public const float Saturation = 1.4f;
        public const float Contrast = 1.45f;

        // The shade steps every colour is pushed to (2026-11-05, the owner: "lets push it further, i want her more
        // defined"): five, so a form reads as a few flat bands, not a smooth blur.
        static readonly float[] Steps = { 0.16f, 0.32f, 0.5f, 0.7f, 0.9f };

        /// <summary>Richer colour and harder shade on every opaque pixel.</summary>
        public static void Shape(Color[] pixels)
        {
            for (var i = 0; i < pixels.Length; i++)
            {
                var c = pixels[i];
                if (c.a < 0.5f)
                    continue;
                Color.RGBToHSV(c, out var h, out var s, out var v);
                // Skin keeps its own colour (more saturation and a turn toward gold made it orange).
                var skin = h > 0.02f && h < 0.12f && s < 0.65f;
                s = Mathf.Clamp01(s * (skin ? 1.05f : Saturation));
                // The darks lifted first, so the shade bands open out instead of sinking into the outline's black.
                v = Mathf.Clamp01(0.5f + (Mathf.Pow(v, 0.75f) - 0.5f) * Contrast);
                var step = Steps[0];
                foreach (var candidate in Steps)
                    if (Mathf.Abs(candidate - v) < Mathf.Abs(step - v))
                        step = candidate;
                v = step;
                // Shadows lean toward blue, lights toward gold (hue turns the short way round).
                var shift = skin ? 0f : v < 0.45f ? -0.025f * (0.45f - v) / 0.45f : 0.015f * (v - 0.45f) / 0.55f;
                h = Mathf.Repeat(h + (h > 0.6f && h < 0.95f ? -shift : shift), 1f);
                var o = Color.HSVToRGB(h, s, v);
                pixels[i] = new Color(o.r, o.g, o.b, c.a);
            }
        }

        /// <summary>The shaded lower-right inner edge and the dark lines between parts, snapped to the palette again.</summary>
        public static void Volume(Color[] pixels, int size)
        {
            Despeckle(pixels, size);
            Despeckle(pixels, size);
            var source = (Color[])pixels.Clone();
            bool Inside(int x, int y) => x >= 0 && y >= 0 && x < size && y < size && source[x + y * size].a >= 0.5f && !IsOutline(source[x + y * size]);
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var i = x + y * size;
                    if (!Inside(x, y))
                        continue;
                    var c = source[i];
                    var k = 1f;
                    // On the dark side: the pixel below or to the right is the outline or clear; on the lit side, above or
                    // to the left, it is lifted (on top of the bake's own light edge).
                    if (!Inside(x, y - 1) || !Inside(x + 1, y))
                        k = 0.62f;
                    else if (!Inside(x, y + 1) || !Inside(x - 1, y))
                        k = 1.22f;
                    // A part line: a much lighter neighbour beside or above it makes this, the darker, darker still.
                    var light = Luma(c);
                    if ((Inside(x - 1, y) && Luma(source[i - 1]) - light > 0.2f) || (Inside(x + 1, y) && Luma(source[i + 1]) - light > 0.2f) ||
                        (Inside(x, y + 1) && Luma(source[i + size]) - light > 0.2f) || (Inside(x, y - 1) && Luma(source[i - size]) - light > 0.2f))
                        k = Mathf.Min(k, 0.66f);
                    if (k != 1f)
                    {
                        var dark = PixelArt.Snap(new Color32((byte)Mathf.Min(255f, c.r * k * 255f), (byte)Mathf.Min(255f, c.g * k * 255f), (byte)Mathf.Min(255f, c.b * k * 255f), 255));
                        pixels[i] = new Color(dark.r / 255f, dark.g / 255f, dark.b / 255f, c.a);
                    }
                }
        }

        // A lone pixel inside the figure whose four neighbours share one other colour takes theirs: shapes read as clean
        // flat clusters, not grain (the model's texture comes out as single pixels at this size).
        static void Despeckle(Color[] pixels, int size)
        {
            var source = (Color[])pixels.Clone();
            for (var y = 1; y < size - 1; y++)
                for (var x = 1; x < size - 1; x++)
                {
                    var i = x + y * size;
                    var c = source[i];
                    if (c.a < 0.5f || IsOutline(c))
                        continue;
                    Color l = source[i - 1], r = source[i + 1], d = source[i - size], u = source[i + size];
                    if (l.a < 0.5f || IsOutline(l) || l != r || l != d || l != u || l == c)
                        continue;
                    pixels[i] = l;
                }
        }

        static float Luma(Color c) => c.r * 0.3f + c.g * 0.55f + c.b * 0.15f;

        static bool IsOutline(Color c)
        {
            Color32 o = PixelArt.Outline;
            return Mathf.Abs(c.r * 255f - o.r) < 1.5f && Mathf.Abs(c.g * 255f - o.g) < 1.5f && Mathf.Abs(c.b * 255f - o.b) < 1.5f;
        }
    }
}
