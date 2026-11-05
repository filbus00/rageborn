using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Pixel art turned to face its flight (2026-11-05, the owner: the arrows and effects next, after the pools): a sprite
    /// drawn pointing right at the screen's 27 a unit is turned to the nearest of <see cref="Directions"/> directions a
    /// pixel at a time (<see cref="PixelTurn"/>) and shown unturned, since a turned sprite's pixels come out uneven on
    /// screen. Each direction is made when first asked for and kept. Only readable sprites can be turned
    /// (WorldArtImporter keeps those in world27's pivots.json readable); <see cref="Toward"/> returns null for others, and
    /// the caller turns the transform as before.
    /// </summary>
    public static class TurnedArt
    {
        public const int Directions = 16;

        static readonly Dictionary<Sprite, Sprite[]> turned = new Dictionary<Sprite, Sprite[]>();
        static readonly Dictionary<Sprite, Sprite> sourceOf = new Dictionary<Sprite, Sprite>();
        static readonly HashSet<Sprite> unreadable = new HashSet<Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            turned.Clear();
            sourceOf.Clear();
            unreadable.Clear();
        }

        /// <summary>The direction nearest <paramref name="degrees"/> (0 is right, counter-clockwise), 0 to 15.</summary>
        public static int DirectionOf(float degrees) => ((Mathf.RoundToInt(degrees / (360f / Directions)) % Directions) + Directions) % Directions;

        /// <summary><paramref name="source"/> facing <paramref name="degrees"/>, or null when it cannot be turned.</summary>
        public static Sprite Toward(Sprite source, float degrees)
        {
            if (source == null || unreadable.Contains(source))
                return null;
            source = SourceOf(source);
            if (!turned.TryGetValue(source, out var all))
            {
                if (!source.texture.isReadable)
                {
                    unreadable.Add(source);
                    return null;
                }
                turned[source] = all = new Sprite[Directions];
                all[0] = source;
                sourceOf[source] = source;
            }
            var d = DirectionOf(degrees);
            if (all[d] == null)
            {
                all[d] = Make(source, d * (360f / Directions));
                sourceOf[all[d]] = source;
            }
            return all[d];
        }

        /// <summary>The sprite a turned one was made from (itself when it is not one).</summary>
        public static Sprite SourceOf(Sprite sprite) => sprite != null && sourceOf.TryGetValue(sprite, out var source) ? source : sprite;

        /// <summary>Whether a sprite is shown turned by <see cref="Toward"/>, so its transform must not turn too.</summary>
        public static bool IsTurned(Sprite sprite) => sprite != null && sourceOf.ContainsKey(sprite);

        static Sprite Make(Sprite source, float degrees)
        {
            var rect = source.textureRect;
            int w = (int)rect.width, h = (int)rect.height;
            var pixels = source.texture.GetPixels32();
            var texWidth = source.texture.width;
            var own = new Color32[w * h];
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                    own[x + y * w] = pixels[(int)rect.x + x + ((int)rect.y + y) * texWidth];
            var result = PixelTurn.Turn(own, w, h, source.pivot, degrees * Mathf.Deg2Rad);
            var texture = new Texture2D(result.Width, result.Height, TextureFormat.RGBA32, false)
            {
                name = source.name + " " + Mathf.RoundToInt(degrees),
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(result.Pixels);
            texture.Apply(false, true);
            // Full rect, no physics shape: the texture is no longer readable, and tracing its outline fails on iOS.
            return Sprite.Create(texture, new Rect(0, 0, result.Width, result.Height),
                new Vector2(result.Pivot.x / result.Width, result.Pivot.y / result.Height), source.pixelsPerUnit, 0,
                SpriteMeshType.FullRect, Vector4.zero, false);
        }
    }

    /// <summary>
    /// Turning pixel art by an angle without smearing it (pure): each new pixel looks at 4 x 4 points across itself,
    /// turned back into the picture, and takes the colour most of them land on, or stays clear when more than five in eight land outside (a
    /// diagonal line a pixel wide must not break up).
    /// So lines stay one pixel wide and no new colours appear.
    /// </summary>
    public static class PixelTurn
    {
        public readonly struct Result
        {
            public readonly Color32[] Pixels;
            public readonly int Width, Height;
            public readonly Vector2 Pivot;

            public Result(Color32[] pixels, int width, int height, Vector2 pivot)
            {
                Pixels = pixels;
                Width = width;
                Height = height;
                Pivot = pivot;
            }
        }

        const int Samples = 4;

        /// <summary><paramref name="pixels"/> (<paramref name="w"/> by <paramref name="h"/>, rows from the bottom) turned
        /// counter-clockwise by <paramref name="radians"/> round <paramref name="pivot"/> (pixels from the bottom left; best
        /// at a pixel's middle).</summary>
        public static Result Turn(Color32[] pixels, int w, int h, Vector2 pivot, float radians)
        {
            float cos = Mathf.Cos(radians), sin = Mathf.Sin(radians);
            // The turned corners, round the pivot, give the new picture's bounds.
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var (cx, cy) in new[] { (0f, 0f), (w, 0f), (0f, h), (w, (float)h) })
            {
                float dx = cx - pivot.x, dy = cy - pivot.y;
                float x = dx * cos - dy * sin, y = dx * sin + dy * cos;
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }
            // The new pixels are centred on whole steps from the pivot, so a line through the pivot's pixel stays on one
            // row of pixels at every angle (a grid through the pivot itself split it in two at a quarter turn).
            // (A thousandth spare, so a corner a rounding error past a pixel's edge adds no empty row.)
            int left = Mathf.FloorToInt(minX + 0.501f), bottom = Mathf.FloorToInt(minY + 0.501f);
            int ow = Mathf.CeilToInt(maxX - 0.501f) - left + 1, oh = Mathf.CeilToInt(maxY - 0.501f) - bottom + 1;
            var output = new Color32[ow * oh];
            var tally = new Color32[Samples * Samples];
            var counts = new int[Samples * Samples];
            for (var y = 0; y < oh; y++)
                for (var x = 0; x < ow; x++)
                {
                    var kinds = 0;
                    var clear = 0;
                    for (var sy = 0; sy < Samples; sy++)
                        for (var sx = 0; sx < Samples; sx++)
                        {
                            // The sample in the new picture, round the pivot, turned back into the old one.
                            float px = left + x - 0.5f + (sx + 0.5f) / Samples, py = bottom + y - 0.5f + (sy + 0.5f) / Samples;
                            int ix = Mathf.FloorToInt(px * cos + py * sin + pivot.x);
                            int iy = Mathf.FloorToInt(-px * sin + py * cos + pivot.y);
                            if (ix < 0 || iy < 0 || ix >= w || iy >= h || pixels[ix + iy * w].a == 0)
                            {
                                clear++;
                                continue;
                            }
                            var c = pixels[ix + iy * w];
                            var k = 0;
                            while (k < kinds && !tally[k].Equals(c))
                                k++;
                            if (k == kinds)
                            {
                                tally[kinds] = c;
                                counts[kinds++] = 0;
                            }
                            counts[k]++;
                        }
                    if (clear > Samples * Samples * 5 / 8 || kinds == 0)
                        continue;
                    var best = 0;
                    for (var k = 1; k < kinds; k++)
                        if (counts[k] > counts[best])
                            best = k;
                    output[x + y * ow] = tally[best];
                }
            return new Result(output, ow, oh, new Vector2(0.5f - left, 0.5f - bottom));
        }
    }
}
