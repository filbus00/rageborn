using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The ground pools drawn in code (2026-11-03, the owner: "remove the damage pool assets that are imported. Use your
    /// own, add some effects to them"; the Gemini pools of 2026-10-31 are gone). Each kind is a pixel-art pool one world
    /// unit across (<see cref="Size"/> pixels, drawn with hard pixels like the rest of the art): an uneven edge with a dark
    /// outline and a lit rim, and inside it its own pattern in a ramp of five tones: fire a molten core under a cracked
    /// crust, poison a slime with bubbles, frost ice with white cracks, void a spiral round a dark heart, blood a glossy
    /// pool, water ripples. <see cref="GroundMarker"/> turns two copies over it, pulses its light, lays a glow under it and
    /// throws off its spray (<see cref="PoolLooks"/>). Pure: the pixels are worked out here and tested.
    /// Since 2026-11-05 a pool is drawn on the screen's grid (the owner: "do it", the pools and blood next): at its own
    /// size, <see cref="WidthFor"/> by <see cref="HeightFor"/> screen pixels (flat on the ground, twice as wide as tall),
    /// shown unscaled, and its two turning copies are baked into <see cref="Frames"/> frames (<see cref="Frame"/>) where
    /// they rock to and fro, since a picture turned or stretched on screen has uneven pixels. Its pattern keeps its size
    /// in the world, so a big pool is not a small one blown up.
    /// </summary>
    public static class PoolArt
    {
        /// <summary>Pixels across: one world unit at the art's 40 a unit (48 until 2026-11-04), so they share its grid.</summary>
        public const int Size = 40;

        static readonly Color32[] Fire =
            { new Color32(52, 14, 8, 255), new Color32(140, 28, 10, 255), new Color32(226, 92, 18, 255), new Color32(255, 186, 48, 255), new Color32(255, 242, 170, 255) };

        static readonly Color32[] Poison =
            { new Color32(18, 44, 10, 255), new Color32(46, 104, 18, 255), new Color32(96, 172, 30, 255), new Color32(166, 226, 66, 255), new Color32(226, 255, 156, 255) };

        static readonly Color32[] Frost =
            { new Color32(36, 72, 122, 255), new Color32(84, 144, 198, 255), new Color32(156, 206, 238, 255), new Color32(216, 242, 255, 255), new Color32(255, 255, 255, 255) };

        static readonly Color32[] Void =
            { new Color32(8, 0, 18, 255), new Color32(46, 8, 78, 255), new Color32(104, 28, 164, 255), new Color32(176, 78, 236, 255), new Color32(236, 186, 255, 255) };

        static readonly Color32[] Blood =
            { new Color32(36, 0, 0, 255), new Color32(84, 4, 4, 255), new Color32(134, 14, 12, 255), new Color32(184, 38, 32, 255), new Color32(240, 144, 134, 255) };

        static readonly Color32[] Water =
            { new Color32(8, 26, 48, 255), new Color32(20, 60, 98, 255), new Color32(40, 104, 150, 255), new Color32(90, 160, 200, 255), new Color32(190, 228, 245, 255) };

        /// <summary>The five tones of a kind, darkest first.</summary>
        public static Color32[] Ramp(string name) => name switch
        {
            "pool_fire" => Fire,
            "pool_poison" => Poison,
            "pool_frost" => Frost,
            "pool_void" => Void,
            "pool_blood" => Blood,
            _ => Water,
        };

        /// <summary>The pixels a unit the world is rendered at (<see cref="GroundPainter.ScreenUnit"/>).</summary>
        public const int ScreenUnit = GroundPainter.ScreenUnit;

        /// <summary>Screen pixels a drawn pixel of the pools, the blood and the telegraph rims: two, as the characters drawn
        /// in the 16x16 style (2026-11-08, the owner: "redraw the effects and UI in 16x16 too"; one until then).</summary>
        public const int Block = 2;

        /// <summary>The frames of a pool's loop, and how many a second it plays.</summary>
        public const int Frames = 12;
        public const float FrameRate = 10f;

        /// <summary>A pool of <paramref name="radius"/> (ground units) on screen: its width in pixels, even.</summary>
        public static int WidthFor(float radius) => Mathf.Max(8, 2 * Mathf.RoundToInt(radius * ScreenUnit));

        /// <summary>Its height, half its width.</summary>
        public static int HeightFor(float radius) => WidthFor(radius) / 2;

        /// <summary>A kind's pixels, row by row from the bottom (Unity's order).</summary>
        public static Color32[] Pixels(string name)
        {
            var ramp = Ramp(name);
            var pixels = new Color32[Size * Size];
            var seed = Seed(name);
            for (var y = 0; y < Size; y++)
                for (var x = 0; x < Size; x++)
                {
                    var tone = Tone(name, (x + 0.5f) / Size * 2f - 1f, (y + 0.5f) / Size * 2f - 1f, 1f, 2f / Size, seed);
                    if (tone >= 0)
                        pixels[x + y * Size] = ramp[tone];
                }
            return pixels;
        }

        /// <summary>
        /// One frame of a pool of <paramref name="radius"/> on the screen's grid, <see cref="WidthFor"/> by
        /// <see cref="HeightFor"/>, row by row from the bottom: the picture, and over it its two copies at their strengths
        /// (<see cref="PoolLooks.Motion"/>), each turned by its own angle that rocks with the frame.
        /// </summary>
        public static Color32[] Frame(string name, float radius, int frame)
        {
            int w = WidthFor(radius), h = HeightFor(radius);
            var ramp = Ramp(name);
            var motion = PoolLooks.MotionOf(name);
            var seed = Seed(name);
            // Its pattern a constant size in the world: radius 0.5 is the picture as drawn.
            var scale = Mathf.Max(1f, radius / 0.5f);
            // One pixel across, in the pool's own -1 to 1.
            var pixel = 2f / w;
            var phase = frame / (float)Frames * Mathf.PI * 2f;
            var turnA = Mathf.Deg2Rad * motion.SpinA * 0.35f * Mathf.Sin(phase);
            var turnB = Mathf.Deg2Rad * motion.SpinB * 0.35f * Mathf.Sin(phase + 1.9f);
            float cosA = Mathf.Cos(turnA), sinA = Mathf.Sin(turnA), cosB = Mathf.Cos(turnB), sinB = Mathf.Sin(turnB);
            var pixels = new Color32[w * h];
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    var u = (x + 0.5f) / w * 2f - 1f;
                    var v = (y + 0.5f) / h * 2f - 1f;
                    var tone = Tone(name, u, v, scale, pixel, seed);
                    if (tone < 0)
                        continue;
                    Color c = ramp[tone];
                    var a = Tone(name, u * cosA - v * sinA, u * sinA + v * cosA, scale, pixel, seed);
                    if (a > 0)
                        c = Color.Lerp(c, ramp[a], motion.AlphaA);
                    var b = Tone(name, u * cosB - v * sinB, u * sinB + v * cosB, scale, pixel, seed);
                    if (b > 0)
                        c = Color.Lerp(c, ramp[b], motion.AlphaB);
                    // Back to the nearest of its five tones, so the copies over it do not speckle it.
                    pixels[x + y * w] = Nearest(ramp, c);
                }
            return Despeckle(pixels, w, h);
        }

        // A lone pixel whose four neighbours share one other tone takes theirs, so the pattern reads as patches, not grain
        // (the noise's fine octave comes out a pixel wide at the screen's size).
        static Color32[] Despeckle(Color32[] pixels, int w, int h)
        {
            var clean = (Color32[])pixels.Clone();
            for (var y = 1; y < h - 1; y++)
                for (var x = 1; x < w - 1; x++)
                {
                    var i = x + y * w;
                    Color32 l = pixels[i - 1], r = pixels[i + 1], d = pixels[i - w], u = pixels[i + w];
                    if (pixels[i].a == 0 || l.a == 0 || !l.Equals(r) || !l.Equals(d) || !l.Equals(u) || l.Equals(pixels[i]))
                        continue;
                    clean[i] = l;
                }
            return clean;
        }

        static Color32 Nearest(Color32[] ramp, Color c)
        {
            var best = ramp[0];
            var bestD = float.MaxValue;
            foreach (Color r in ramp)
            {
                var d = (r.r - c.r) * (r.r - c.r) + (r.g - c.g) * (r.g - c.g) + (r.b - c.b) * (r.b - c.b);
                if (d < bestD)
                {
                    bestD = d;
                    best = r;
                }
            }
            return best;
        }

        static int Seed(string name) => name.Length * 31 + name[name.Length - 1];

        // The tone at a point of the pool (-1 to 1 across), -1 outside: the dark lip a pixel or so wide, a lit band
        // inside it, then the pattern. `scale` sizes the pattern's noise to the pool, `pixel` is one pixel across.
        static int Tone(string name, float u, float v, float scale, float pixel, int seed)
        {
            var r = Mathf.Sqrt(u * u + v * v);
            var a = Mathf.Atan2(v, u);
            var edge = 0.86f + 0.06f * Mathf.Sin(a * 3f + seed) + 0.04f * Mathf.Sin(a * 5f + seed * 0.7f) + 0.025f * Mathf.Sin(a * 11f);
            if (r > edge)
                return -1;
            // At least a pixel each (counted in the pool's height, its pixels' long side in -1 to 1).
            var lip = Mathf.Max(0.06f, pixel * 2.2f);
            if (r > edge - lip)
                return 0;
            if (r > edge - lip - Mathf.Max(0.07f, pixel * 2.2f))
                return 2;
            return Mathf.Clamp(Inside(name, u, v, r / edge, a, seed, scale), 0, 4);
        }

        // The pattern inside the rim, as a tone 0 to 4.
        static int Inside(string name, float u, float v, float r, float a, int seed, float scale)
        {
            // The noise read in the world's size: a bigger pool shows more of it, not bigger blots.
            u *= scale;
            v *= scale;
            var n = Noise(u * 3.2f, v * 3.2f, seed);
            switch (name)
            {
                case "pool_fire":
                {
                    // A molten core, brightest in the middle, under a dark crust broken by glowing cracks.
                    var heat = (1f - r) * 1.1f + (n - 0.5f) * 0.9f;
                    var crack = Mathf.Abs(Noise(u * 5f, v * 5f, seed + 7) - 0.5f);
                    if (crack < 0.035f)
                        return 4;
                    if (heat < 0.25f)
                        return 1;
                    return heat < 0.5f ? 2 : heat < 0.8f ? 3 : 4;
                }
                case "pool_poison":
                {
                    // Slime in swirls, a few bright bubbles with a dark ring.
                    // The bubbles repeat over a big pool.
                    float tu = Mathf.Repeat(u + 1f, 2f) - 1f, tv = Mathf.Repeat(v + 1f, 2f) - 1f;
                    foreach (var (bx, by, bs) in Bubbles)
                    {
                        var d = Mathf.Sqrt((tu - bx) * (tu - bx) + (tv - by) * (tv - by));
                        if (d < bs * 0.55f)
                            return 4;
                        if (d < bs)
                            return 1;
                    }
                    var swirl = Noise(u * 2f + Mathf.Sin(v * 3f) * 0.5f, v * 2f, seed + 3);
                    return swirl < 0.4f ? 2 : 3;
                }
                case "pool_frost":
                {
                    // Pale ice, lighter toward the middle, with white cracks running across it.
                    var crack = Mathf.Abs(Noise(u * 4f, v * 4f, seed + 5) - 0.5f);
                    if (crack < 0.035f)
                        return 4;
                    var ice = (1f - r) * 0.6f + n * 0.6f;
                    return ice < 0.4f ? 1 : ice < 0.7f ? 2 : 3;
                }
                case "pool_void":
                {
                    // A spiral of light round a black heart.
                    if (r < 0.18f)
                        return 0;
                    var arms = Mathf.Sin(a * 3f + r * 9f + n * 2f);
                    if (arms > 0.75f)
                        return r < 0.45f ? 4 : 3;
                    return arms > 0.1f ? 2 : 1;
                }
                case "pool_blood":
                {
                    // A dark glossy pool with a bright sheen up to the left.
                    var sheen = Mathf.Sqrt((u / scale + 0.3f) * (u / scale + 0.3f) + (v / scale - 0.32f) * (v / scale - 0.32f));
                    if (sheen < 0.1f)
                        return 4;
                    if (sheen < 0.2f)
                        return 3;
                    return n < 0.45f ? 1 : 2;
                }
                default:
                {
                    // Water: rings of ripples.
                    var ripple = Mathf.Sin(r * 22f + n * 3f);
                    return ripple > 0.7f ? 3 : ripple > -0.2f ? 2 : 1;
                }
            }
        }

        static readonly (float x, float y, float size)[] Bubbles =
            { (0.32f, 0.22f, 0.13f), (-0.36f, 0.34f, 0.1f), (-0.12f, -0.4f, 0.15f), (0.44f, -0.26f, 0.08f), (0.04f, 0.5f, 0.07f), (-0.5f, -0.08f, 0.09f) };

        /// <summary>Smooth value noise, 0 to 1, two octaves.</summary>
        public static float Noise(float x, float y, int seed) => 0.65f * Value(x, y, seed) + 0.35f * Value(x * 2.1f, y * 2.1f, seed + 11);

        static float Value(float x, float y, int seed)
        {
            var x0 = Mathf.FloorToInt(x);
            var y0 = Mathf.FloorToInt(y);
            var fx = x - x0;
            var fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            var a = Hash(x0, y0, seed);
            var b = Hash(x0 + 1, y0, seed);
            var c = Hash(x0, y0 + 1, seed);
            var d = Hash(x0 + 1, y0 + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                var h = (uint)(x * 374761393 + y * 668265263 + seed * 982451653);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }
    }
}
