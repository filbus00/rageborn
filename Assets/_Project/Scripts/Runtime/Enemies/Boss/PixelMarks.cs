using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The ground marks drawn on the screen's grid (2026-11-10, the owner: "All enemies that have attacks that are not
    /// pixel art need to be pixel art"): a telegraph's growing fill, a pool's glow, a landing's shock ring, the burning
    /// arena edge, a whirling blade's sweep, and lanes. Each made at its size, <see cref="PoolArt.WidthFor"/> by half that
    /// at <see cref="PoolArt.Block"/> screen pixels a pixel (as <see cref="TelegraphArt.PixelRim"/>), white and tinted by
    /// its renderer, shown unscaled, cached by size. They replace the smooth 128 px placeholders scaled to fit.
    /// </summary>
    public static class PixelMarks
    {
        static readonly Dictionary<long, Sprite> cache = new Dictionary<long, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => cache.Clear();

        /// <summary>Pixels a world unit.</summary>
        public static float PixelsPerUnit => (float)PoolArt.ScreenUnit / PoolArt.Block;

        /// <summary>A telegraph's fill at this radius: a bright edge a pixel wide over a faint floor.</summary>
        public static Sprite Fill(float radius) => Ellipse(1, 0, radius, (r, d, a) => d < 1f ? 0.85f : r > 0.66f ? 0.34f : 0.26f);

        /// <summary>A pool's light on the ground round it, in three steps.</summary>
        public static Sprite Glow(float radius) => Ellipse(2, 0, radius, (r, d, a) => r < 0.55f ? 0.5f : r < 0.8f ? 0.3f : 0.14f);

        /// <summary>A thin ring flying out where an attack lands.</summary>
        public static Sprite Ring(float radius) => Ellipse(3, 0, radius, (r, d, a) => d < 1f ? 1f : d < 2f ? 0.4f : 0f);

        /// <summary>A band from 72 to 100 percent of the radius (the burning arena edge), bright at both edges.</summary>
        public static Sprite Band(float radius) => Ellipse(4, 0, radius, (r, d, a) => r < 0.72f ? 0f : d < 1f || r < 0.75f ? 0.9f : 0.42f);

        /// <summary>The number of frames of a whirl's sweep.</summary>
        public const int SweepFrames = 16;

        /// <summary>A blade's sweep round a ring, frame <paramref name="frame"/> of <see cref="SweepFrames"/>: a bright
        /// head and a tail fading behind it over a third of the circle, in three steps.</summary>
        public static Sprite Sweep(float radius, int frame)
        {
            frame = ((frame % SweepFrames) + SweepFrames) % SweepFrames;
            var head = frame * Mathf.PI * 2f / SweepFrames;
            return Ellipse(5, frame, radius, (r, d, a) =>
            {
                if (d >= 3f)
                    return 0f;
                var behind = Mathf.Repeat(head - a, Mathf.PI * 2f) / (Mathf.PI * 2f / 3f);
                return behind > 1f ? 0f : behind < 0.25f ? 1f : behind < 0.6f ? 0.6f : 0.3f;
            });
        }

        // An ellipse WidthFor(radius) by half that; alpha(r, d, angle): r 0 to 1 from the middle, d pixels in from the
        // edge, angle round it (0 to the right, counter-clockwise on screen).
        static Sprite Ellipse(int kind, int variant, float radius, Func<float, float, float, float> alpha)
        {
            var w = PoolArt.WidthFor(radius / PoolArt.Block);
            var key = ((long)kind << 40) | ((long)variant << 20) | (uint)w;
            if (cache.TryGetValue(key, out var sprite) && sprite != null)
                return sprite;
            var h = Mathf.Max(2, w / 2);
            var pixels = new Color32[w * h];
            float hw = w / 2f, hh = h / 2f;
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    float u = (x + 0.5f - hw) / hw, v = (y + 0.5f - hh) / hh;
                    var r = Mathf.Sqrt(u * u + v * v);
                    if (r > 1f)
                        continue;
                    var grad = Mathf.Sqrt(u / hw * (u / hw) + v / hh * (v / hh)) / Mathf.Max(0.001f, r);
                    var d = (1f - r) / Mathf.Max(0.0001f, grad);
                    var a = Mathf.Clamp01(alpha(r, d, Mathf.Atan2(v, u)));
                    if (a > 0f)
                        pixels[x + y * w] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "Pixel Mark " + kind + " " + w, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return cache[key] = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit, 0,
                SpriteMeshType.FullRect, Vector4.zero, false);
        }

        /// <summary>
        /// A cell's diamond of the ground darkened in a dither (the forest's trail going off into the dark, 2026-11-10):
        /// step 1 a quarter of its pixels, 2 half, 3 three quarters, 4 all; white, tinted by its renderer.
        /// </summary>
        public static Sprite Darkness(int step)
        {
            step = Mathf.Clamp(step, 1, 4);
            var key = (6L << 40) | (uint)step;
            if (cache.TryGetValue(key, out var sprite) && sprite != null)
                return sprite;
            var w = Mathf.RoundToInt(PixelsPerUnit) + 1;
            w += w % 2;
            var h = w / 2;
            var pixels = new Color32[w * h];
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    var u = Mathf.Abs(x + 0.5f - w / 2f) / (w / 2f) + Mathf.Abs(y + 0.5f - h / 2f) / (h / 2f);
                    if (u > 1f)
                        continue;
                    var on = step == 4 || (step == 3 ? (x + y) % 2 == 0 || y % 2 == 0 : step == 2 ? (x + y) % 2 == 0 : x % 2 == 0 && y % 2 == 0);
                    if (on)
                        pixels[x + y * w] = new Color32(255, 255, 255, 255);
                }
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "Darkness " + step, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return cache[key] = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect, Vector4.zero, false);
        }

        /// <summary>The number of steps a lane's fill shows in.</summary>
        public const int LaneSteps = 8;

        /// <summary>
        /// Draws a lane from <paramref name="a"/> to <paramref name="b"/> (world points), <paramref name="width"/> world
        /// units wide, into <paramref name="texture"/> (made or resized as needed): a bright edge a pixel wide, a faint
        /// floor, and the fill <paramref name="filled"/> of the way along from <paramref name="a"/>. Returns the sprite
        /// to show at the lane's middle, unscaled and unturned.
        /// </summary>
        public static Sprite Lane(ref Texture2D texture, Vector2 a, Vector2 b, float width, float filled)
        {
            var ppu = PixelsPerUnit;
            var pa = a * ppu;
            var pb = b * ppu;
            var half = Mathf.Max(1f, width * 0.5f * ppu);
            var w = Mathf.CeilToInt(Mathf.Abs(pb.x - pa.x) + half * 2f) + 2;
            var h = Mathf.CeilToInt(Mathf.Abs(pb.y - pa.y) + half * 2f) + 2;
            if (texture == null)
                texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "Pixel Lane", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            else if (texture.width != w || texture.height != h)
                texture.Reinitialize(w, h);
            var origin = new Vector2(Mathf.Min(pa.x, pb.x) - half - 1f, Mathf.Min(pa.y, pb.y) - half - 1f);
            var axis = pb - pa;
            var length = Mathf.Max(0.001f, axis.magnitude);
            var dir = axis / length;
            var pixels = new Color32[w * h];
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    var p = origin + new Vector2(x + 0.5f, y + 0.5f) - pa;
                    var along = Vector2.Dot(p, dir);
                    if (along < 0f || along > length)
                        continue;
                    var across = Mathf.Abs(p.x * dir.y - p.y * dir.x);
                    if (across > half)
                        continue;
                    var edge = half - across < 1f || along < 1f || length - along < 1f;
                    var alpha = edge ? 0.95f : along <= length * filled ? 0.55f : 0.2f;
                    pixels[x + y * w] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect, Vector4.zero, false);
        }
    }
}
