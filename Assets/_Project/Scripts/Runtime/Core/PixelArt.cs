using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The game's pixel-art look (the owner, 2026-09-30: "I am not liking the art style. I want it pixelated", both
    /// bigger pixels and art that reads as pixel art). Every world sprite is drawn at <see cref="PixelsPerUnit"/> so one
    /// art pixel is one rendered pixel (<see cref="RenderResolution"/>), its colours snapped to one dark-fantasy palette
    /// of ramps, its alpha made hard, and characters and props get a one-pixel dark outline. Used by the sprite bake on
    /// every frame and by the code-drawn dungeon art. Pure.
    /// </summary>
    public static class PixelArt
    {
        /// <summary>Art pixels per ground unit, for every world sprite, and the rendered pixels per unit on every phone.</summary>
        public const int PixelsPerUnit = 40;

        /// <summary>The outline: a warm near-black, the palette's darkest.</summary>
        public static readonly Color32 Outline = new Color32(14, 10, 10, 255);

        // Ramps, dark to light: stone greys, warm browns, blood reds, skin, moss greens, cold corpse blue-greys, ember
        // and gold, and bone. About 45 colours, enough for shading steps without smooth gradients.
        static readonly Color32[] Palette =
        {
            new Color32(14, 10, 10, 255), new Color32(30, 26, 26, 255), new Color32(50, 46, 45, 255), new Color32(74, 69, 66, 255),
            new Color32(102, 96, 91, 255), new Color32(134, 128, 120, 255), new Color32(170, 164, 154, 255), new Color32(214, 208, 196, 255),
            new Color32(28, 18, 13, 255), new Color32(48, 31, 21, 255), new Color32(72, 47, 30, 255), new Color32(100, 68, 43, 255),
            new Color32(132, 94, 60, 255), new Color32(168, 128, 86, 255),
            new Color32(38, 9, 9, 255), new Color32(70, 15, 14, 255), new Color32(108, 24, 20, 255), new Color32(150, 38, 28, 255),
            new Color32(192, 62, 40, 255),
            new Color32(62, 40, 32, 255), new Color32(102, 68, 52, 255), new Color32(144, 102, 78, 255), new Color32(186, 142, 110, 255),
            new Color32(222, 186, 150, 255),
            new Color32(18, 26, 16, 255), new Color32(34, 48, 28, 255), new Color32(54, 72, 40, 255), new Color32(82, 100, 58, 255),
            new Color32(118, 132, 82, 255),
            new Color32(20, 24, 32, 255), new Color32(38, 46, 58, 255), new Color32(62, 74, 90, 255), new Color32(94, 108, 124, 255),
            new Color32(136, 150, 164, 255), new Color32(184, 196, 204, 255),
            new Color32(88, 40, 12, 255), new Color32(148, 70, 18, 255), new Color32(204, 110, 30, 255), new Color32(236, 160, 60, 255),
            new Color32(252, 214, 120, 255),
            new Color32(150, 142, 118, 255), new Color32(196, 188, 162, 255), new Color32(232, 224, 198, 255),
            new Color32(60, 30, 70, 255), new Color32(110, 60, 130, 255),
        };

        static byte[] nearest;

        /// <summary>The palette colour closest to a colour (weighted for the eye), alpha kept.</summary>
        public static Color32 Snap(Color32 c)
        {
            nearest ??= BuildLookup();
            var index = nearest[(c.r >> 3) << 10 | (c.g >> 3) << 5 | c.b >> 3];
            var p = Palette[index];
            return new Color32(p.r, p.g, p.b, c.a);
        }

        static byte[] BuildLookup()
        {
            var table = new byte[32 * 32 * 32];
            for (var r = 0; r < 32; r++)
                for (var g = 0; g < 32; g++)
                    for (var b = 0; b < 32; b++)
                    {
                        int cr = r * 8 + 4, cg = g * 8 + 4, cb = b * 8 + 4;
                        var best = 0;
                        var bestDistance = int.MaxValue;
                        for (var i = 0; i < Palette.Length; i++)
                        {
                            int dr = cr - Palette[i].r, dg = cg - Palette[i].g, db = cb - Palette[i].b;
                            var d = 3 * dr * dr + 4 * dg * dg + 2 * db * db;
                            if (d < bestDistance)
                            {
                                bestDistance = d;
                                best = i;
                            }
                        }
                        table[r << 10 | g << 5 | b] = (byte)best;
                    }
            return table;
        }

        /// <summary>
        /// Turns an image into pixel art in place: alpha hard at half, every opaque pixel snapped to the palette, and,
        /// when asked, every clear pixel touching an opaque one along an edge becomes the outline. Rows from the bottom.
        /// </summary>
        public static void Process(Color32[] pixels, int width, int height, bool outline)
        {
            for (var i = 0; i < pixels.Length; i++)
            {
                var c = pixels[i];
                pixels[i] = c.a < 128 ? new Color32(0, 0, 0, 0) : Snap(new Color32(c.r, c.g, c.b, 255));
            }
            if (!outline)
                return;

            var solid = new bool[pixels.Length];
            for (var i = 0; i < pixels.Length; i++)
                solid[i] = pixels[i].a > 0;
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                {
                    var i = x + y * width;
                    if (solid[i])
                        continue;
                    if ((x > 0 && solid[i - 1]) || (x < width - 1 && solid[i + 1]) ||
                        (y > 0 && solid[i - width]) || (y < height - 1 && solid[i + width]))
                        pixels[i] = Outline;
                }
        }

        /// <summary>
        /// Averages an image down to a smaller size (rows from the bottom): colour weighted by coverage, alpha the
        /// coverage. A whole-number shrink (art drawn at 2x or 4x) averages exact blocks.
        /// </summary>
        public static Color32[] Downscale(Color32[] src, int sw, int sh, int dw, int dh)
        {
            var dst = new Color32[dw * dh];
            for (var y = 0; y < dh; y++)
                for (var x = 0; x < dw; x++)
                {
                    int x0 = x * sw / dw, x1 = Mathf.Max(x0 + 1, (x + 1) * sw / dw);
                    int y0 = y * sh / dh, y1 = Mathf.Max(y0 + 1, (y + 1) * sh / dh);
                    float r = 0, g = 0, b = 0, a = 0;
                    var n = 0;
                    for (var sy = y0; sy < y1; sy++)
                        for (var sx = x0; sx < x1; sx++)
                        {
                            var c = src[sx + sy * sw];
                            var w = c.a / 255f;
                            r += c.r * w;
                            g += c.g * w;
                            b += c.b * w;
                            a += w;
                            n++;
                        }
                    dst[x + y * dw] = a <= 0f ? new Color32(0, 0, 0, 0)
                        : new Color32((byte)(r / a), (byte)(g / a), (byte)(b / a), (byte)(255f * a / n));
                }
            return dst;
        }

        /// <summary>Enlarges an image by repeating pixels (rows from the bottom), for drawing on it at a larger size.</summary>
        public static Color32[] Enlarge(Color32[] src, int sw, int sh, int dw, int dh)
        {
            var dst = new Color32[dw * dh];
            for (var y = 0; y < dh; y++)
                for (var x = 0; x < dw; x++)
                    dst[x + y * dw] = src[Mathf.Min(sw - 1, x * sw / dw) + Mathf.Min(sh - 1, y * sh / dh) * sw];
            return dst;
        }

        /// <summary>
        /// Cuts a floor tile of <paramref name="width"/> x width / 2 to its diamond, a touch wide so neighbouring tiles
        /// meet with no gap; inside it every pixel is made opaque, and a clear one there takes the colour of the nearest
        /// coloured pixel toward the middle of its row.
        /// </summary>
        public static void CutDiamond(Color32[] pixels, int width)
        {
            var height = width / 2;
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                {
                    var dx = Mathf.Abs(x + 0.5f - width * 0.5f) / (width * 0.5f);
                    var dy = Mathf.Abs(y + 0.5f - height * 0.5f) / (height * 0.5f);
                    var i = x + y * width;
                    if (dx + dy > 1.06f)
                    {
                        pixels[i] = new Color32(0, 0, 0, 0);
                        continue;
                    }
                    var c = pixels[i];
                    var step = x < width / 2 ? 1 : -1;
                    for (var k = x; c.a == 0 && k >= 0 && k < width && (step > 0 ? k < width / 2 + 1 : k >= width / 2 - 1); k += step)
                        c = pixels[k + y * width];
                    pixels[i] = new Color32(c.r, c.g, c.b, 255);
                }
        }

        /// <summary>How far an image's opaque colours sit from the palette on average (0 already on it, about 10 close,
        /// 30 or more a different palette), for the importer's report.</summary>
        public static float PaletteDistance(Color32[] pixels)
        {
            double sum = 0;
            var n = 0;
            foreach (var c in pixels)
            {
                if (c.a < 128)
                    continue;
                var s = Snap(c);
                int dr = c.r - s.r, dg = c.g - s.g, db = c.b - s.b;
                sum += System.Math.Sqrt(dr * dr + dg * dg + db * db);
                n++;
            }
            return n == 0 ? 0f : (float)(sum / n);
        }

        /// <summary>The same for linear-free <see cref="Color"/> arrays (the bake's frames, already in sRGB).</summary>
        public static void Process(Color[] pixels, int width, int height, bool outline)
        {
            var bytes = new Color32[pixels.Length];
            for (var i = 0; i < pixels.Length; i++)
                bytes[i] = pixels[i];
            Process(bytes, width, height, outline);
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = bytes[i];
        }
    }
}
