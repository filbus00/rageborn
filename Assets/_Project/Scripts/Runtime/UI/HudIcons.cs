using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The HUD's pixel icons (the owner, 2026-10-18: "Make an icon with a bag, an icon for settings ... Display the amount
    /// of coins"): a leather sack tied with a gold cord, an iron cog and a gold coin, drawn in code from glyphs and shown
    /// with point filtering, so they read as the game's pixel art. Cached.
    /// </summary>
    public static class HudIcons
    {
        public enum Kind { Bag, Settings, Coin }

        // 'o' outline, '#' main, '+' shade, '*' light, '=' cord or trim; '.' clear. Top row first.
        static readonly Dictionary<Kind, (string[] rows, Color32 main, Color32 shade, Color32 light, Color32 trim)> Glyphs =
            new Dictionary<Kind, (string[], Color32, Color32, Color32, Color32)>
            {
                [Kind.Bag] = (new[]
                {
                    "......oooo......",
                    ".....o=**=o.....",
                    "......o==o......",
                    ".....oo==oo.....",
                    "...oo######oo...",
                    "..o##*#####+#o..",
                    ".o##*#######+#o.",
                    ".o#*########+#o.",
                    "o##*##====###+#o",
                    "o#*###=##=###+#o",
                    "o#*###====###+#o",
                    "o##*########++#o",
                    "o###########++#o",
                    ".o##++++++++++o.",
                    "..oo++++++++oo..",
                    "....oooooooo....",
                }, new Color32(132, 84, 50, 255), new Color32(84, 50, 30, 255), new Color32(178, 124, 78, 255), new Color32(222, 180, 96, 255)),
                [Kind.Settings] = (new[]
                {
                    "......oooo......",
                    "......o**o......",
                    "..ooo.o*#o.ooo..",
                    "..o**oo*#oo##o..",
                    "..o*****#####o..",
                    "...o***oo###o...",
                    "oooo**o..o##oooo",
                    "o****o....o##++o",
                    "o*##*o....o#+++o",
                    "oooo##o..o#+oooo",
                    "...o###oo#++o...",
                    "..o######++++o..",
                    "..o##oo#+oo++o..",
                    "..ooo.o#+o.ooo..",
                    "......o++o......",
                    "......oooo......",
                }, new Color32(150, 146, 140, 255), new Color32(90, 86, 84, 255), new Color32(206, 202, 194, 255), new Color32(150, 146, 140, 255)),
                [Kind.Coin] = (new[]
                {
                    "..ooooo..",
                    ".o#**##o.",
                    "o#*###+#o",
                    "o#*#=#+#o",
                    "o#*#=#+#o",
                    "o#####+#o",
                    ".o#+++#o.",
                    "..ooooo..",
                }, new Color32(226, 176, 64, 255), new Color32(160, 112, 36, 255), new Color32(255, 230, 140, 255), new Color32(170, 120, 40, 255)),
            };

        static readonly Dictionary<Kind, Sprite> Sprites = new Dictionary<Kind, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Sprites.Clear();

        /// <summary>The glyph's width and height in pixels.</summary>
        public static Vector2Int Size(Kind kind)
        {
            var rows = Glyphs[kind].rows;
            return new Vector2Int(rows[0].Length, rows.Length);
        }

        /// <summary>The colour of one glyph pixel, (0, 0) the top left; clear outside the shape.</summary>
        public static Color32 Pixel(Kind kind, int x, int row)
        {
            var (rows, main, shade, light, trim) = Glyphs[kind];
            return rows[row][x] switch
            {
                'o' => new Color32(20, 14, 12, 255),
                '#' => main,
                '+' => shade,
                '*' => light,
                '=' => trim,
                _ => new Color32(0, 0, 0, 0),
            };
        }

        public static Sprite For(Kind kind)
        {
            if (Sprites.TryGetValue(kind, out var sprite) && sprite != null)
                return sprite;
            var size = Size(kind);
            var texture = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false)
            {
                name = "HUD " + kind,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[size.x * size.y];
            for (var row = 0; row < size.y; row++)
                for (var x = 0; x < size.x; x++)
                    pixels[(size.y - 1 - row) * size.x + x] = Pixel(kind, x, row);
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            // Full rect, no physics shape: the texture is no longer readable (CLAUDE.md, pixel art).
            sprite = Sprite.Create(texture, new Rect(0, 0, size.x, size.y), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                Vector4.zero, false);
            return Sprites[kind] = sprite;
        }
    }
}
