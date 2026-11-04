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

        /// <summary>A kind's pixels, row by row from the bottom (Unity's order).</summary>
        public static Color32[] Pixels(string name)
        {
            var ramp = Ramp(name);
            var pixels = new Color32[Size * Size];
            var seed = name.Length * 31 + name[name.Length - 1];
            for (var y = 0; y < Size; y++)
                for (var x = 0; x < Size; x++)
                {
                    var u = (x + 0.5f) / Size * 2f - 1f;
                    var v = (y + 0.5f) / Size * 2f - 1f;
                    var r = Mathf.Sqrt(u * u + v * v);
                    var a = Mathf.Atan2(v, u);
                    var edge = 0.86f + 0.06f * Mathf.Sin(a * 3f + seed) + 0.04f * Mathf.Sin(a * 5f + seed * 0.7f) + 0.025f * Mathf.Sin(a * 11f);
                    if (r > edge)
                        continue;
                    int tone;
                    if (r > edge - 0.06f)
                        tone = 0;
                    else if (r > edge - 0.13f)
                        tone = 2;
                    else
                        tone = Inside(name, u, v, r / edge, a, seed);
                    pixels[x + y * Size] = ramp[Mathf.Clamp(tone, 0, 4)];
                }
            return pixels;
        }

        // The pattern inside the rim, as a tone 0 to 4.
        static int Inside(string name, float u, float v, float r, float a, int seed)
        {
            var n = Noise(u * 3.2f, v * 3.2f, seed);
            switch (name)
            {
                case "pool_fire":
                {
                    // A molten core, brightest in the middle, under a dark crust broken by glowing cracks.
                    var heat = (1f - r) * 1.1f + (n - 0.5f) * 0.9f;
                    var crack = Mathf.Abs(Noise(u * 5f, v * 5f, seed + 7) - 0.5f);
                    if (crack < 0.05f)
                        return 4;
                    if (heat < 0.25f)
                        return 1;
                    return heat < 0.5f ? 2 : heat < 0.8f ? 3 : 4;
                }
                case "pool_poison":
                {
                    // Slime in swirls, a few bright bubbles with a dark ring.
                    foreach (var (bx, by, bs) in Bubbles)
                    {
                        var d = Mathf.Sqrt((u - bx) * (u - bx) + (v - by) * (v - by));
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
                    var sheen = Mathf.Sqrt((u + 0.3f) * (u + 0.3f) + (v - 0.32f) * (v - 0.32f));
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
