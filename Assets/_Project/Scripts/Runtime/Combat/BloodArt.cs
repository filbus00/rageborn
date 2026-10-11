using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Blood on the floor as pixel art (2026-10-11, the owner: "add more gore and blood"): irregular splats, flat on the
    /// ground (twice as wide as tall, as the floor is drawn), each a lumpy blob with a darker rim and a few drops flung
    /// around it, and small chunks of flesh. Drawn in code at the game's 40 pixels a unit from <see cref="SplatShade"/>
    /// (pure), hard alpha, in <see cref="PixelArt"/>'s blood ramp.
    /// </summary>
    public static class BloodArt
    {
        public const int SplatWidth = 32;
        public const int SplatHeight = 16;
        public const int Variants = 6;
        const int ChunkSize = 6;

        static Sprite[] splats;
        static Sprite[] chunks;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            splats = null;
            chunks = null;
        }

        /// <summary>The splat sprites, <see cref="Variants"/> of them, <see cref="SplatWidth"/> pixels across (0.8 units).</summary>
        public static Sprite Splat(int variant)
        {
            splats ??= new Sprite[Variants];
            variant = ((variant % Variants) + Variants) % Variants;
            return splats[variant] != null ? splats[variant] : splats[variant] = MakeSplat(variant);
        }

        /// <summary>A chunk of flesh, a few pixels across.</summary>
        public static Sprite Chunk(int variant)
        {
            chunks ??= new Sprite[3];
            variant = ((variant % 3) + 3) % 3;
            return chunks[variant] != null ? chunks[variant] : chunks[variant] = MakeChunk(variant);
        }

        /// <summary>
        /// The splat's pixel at (x, y), 0 empty, 1 the dark rim, 2 the wet middle, 3 a glint: a blob whose edge wanders
        /// with the angle (two sine lumps of the variant's own phases), and five to eight drops thrown round it. Pure.
        /// </summary>
        public static int SplatShade(int variant, int x, int y)
        {
            var random = new System.Random(9173 + variant * 7919);
            float a1 = (float)random.NextDouble() * 6.283f, a2 = (float)random.NextDouble() * 6.283f;
            var lump1 = 0.18f + 0.1f * (float)random.NextDouble();
            var lump2 = 0.1f + 0.08f * (float)random.NextDouble();
            // Pixel centre in the splat's own ground space: x across, y doubled (the floor is drawn at half height).
            var px = (x + 0.5f - SplatWidth / 2f) / (SplatWidth / 2f);
            var py = (y + 0.5f - SplatHeight / 2f) / (SplatHeight / 2f);
            var r = Mathf.Sqrt(px * px + py * py);
            var angle = Mathf.Atan2(py, px);
            var edge = 0.62f * (1f + lump1 * Mathf.Sin(3f * angle + a1) + lump2 * Mathf.Sin(5f * angle + a2));
            if (r < edge)
            {
                if (r < edge * 0.3f && px < 0f && py > 0f)
                    return 3;
                return r > edge - 0.14f ? 1 : 2;
            }
            var drops = 5 + random.Next(4);
            for (var k = 0; k < drops; k++)
            {
                var da = (float)random.NextDouble() * 6.283f;
                var dr = 0.72f + 0.25f * (float)random.NextDouble();
                var size = 0.07f + 0.07f * (float)random.NextDouble();
                var dx = px - Mathf.Cos(da) * dr;
                var dy = py - Mathf.Sin(da) * dr;
                if (dx * dx + dy * dy < size * size)
                    return 1;
            }
            return 0;
        }

        static Sprite MakeSplat(int variant)
        {
            var rim = new Color32(70, 4, 8, 255);
            var wet = new Color32(118, 10, 14, 255);
            var glint = new Color32(168, 34, 34, 255);
            var pixels = new Color32[SplatWidth * SplatHeight];
            for (var y = 0; y < SplatHeight; y++)
                for (var x = 0; x < SplatWidth; x++)
                {
                    var shade = SplatShade(variant, x, y);
                    pixels[x + y * SplatWidth] = shade == 0 ? new Color32(0, 0, 0, 0) : shade == 1 ? rim : shade == 2 ? wet : glint;
                }
            return Make("Blood Splat " + variant, pixels, SplatWidth, SplatHeight);
        }

        static Sprite MakeChunk(int variant)
        {
            var dark = new Color32(60, 6, 8, 255);
            var flesh = new Color32(132, 28, 30, 255);
            var light = new Color32(176, 72, 66, 255);
            var pixels = new Color32[ChunkSize * ChunkSize];
            var random = new System.Random(311 + variant * 101);
            for (var y = 0; y < ChunkSize; y++)
                for (var x = 0; x < ChunkSize; x++)
                {
                    var dx = x + 0.5f - ChunkSize / 2f;
                    var dy = y + 0.5f - ChunkSize / 2f;
                    var inside = dx * dx + dy * dy < 6.5f + random.Next(3);
                    pixels[x + y * ChunkSize] = !inside ? new Color32(0, 0, 0, 0) : dy > 0.5f && dx < 0.5f ? light : dx + dy > 1f ? dark : flesh;
                }
            return Make("Gore Chunk " + variant, pixels, ChunkSize, ChunkSize);
        }

        static Sprite Make(string name, Color32[] pixels, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            // Full rect, no physics shape: the texture is no longer readable, and tracing its outline fails on iOS.
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), PixelArt.PixelsPerUnit, 0,
                SpriteMeshType.FullRect, Vector4.zero, false);
        }
    }
}
