using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// A baked figure made to read as defined pixel art (2026-11-05, the owner: "Start remaking the character into pixel
    /// art. Make her much more defined. She has a very weak presence on screen right now"). Two passes on a frame, rows
    /// from the bottom: <see cref="Shape"/> before the palette (richer colour, shade pushed into harder bands, shadows
    /// cooler and lights warmer, as pixel artists shade), and <see cref="Volume"/> after the outline (the inner edge on the
    /// lower right shaded, so she has a dark side as well as the lit edge, and a dark line where two parts of very
    /// different light meet: her face and her hood, her arm and her tunic). Pure.
    /// </summary>
    public static class PixelDefine
    {
        public const float Saturation = 1.25f;
        public const float Contrast = 1.3f;

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
                    // On the dark side: the pixel below or to the right is the outline or clear.
                    if (!Inside(x, y - 1) || !Inside(x + 1, y))
                        k = 0.7f;
                    // A part line: a much lighter neighbour beside or above it makes this, the darker, darker still.
                    var light = Luma(c);
                    if ((Inside(x - 1, y) && Luma(source[i - 1]) - light > 0.28f) || (Inside(x + 1, y) && Luma(source[i + 1]) - light > 0.28f) ||
                        (Inside(x, y + 1) && Luma(source[i + size]) - light > 0.28f) || (Inside(x, y - 1) && Luma(source[i - size]) - light > 0.28f))
                        k = Mathf.Min(k, 0.72f);
                    if (k < 1f)
                    {
                        var dark = PixelArt.Snap(new Color32((byte)(c.r * k * 255f), (byte)(c.g * k * 255f), (byte)(c.b * k * 255f), 255));
                        pixels[i] = new Color(dark.r / 255f, dark.g / 255f, dark.b / 255f, c.a);
                    }
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
