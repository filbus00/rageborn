using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The thrown things' pixel sprites (2026-10-17), drawn in code at <see cref="PixelArt.PixelsPerUnit"/>, outlined in
    /// near black: a knife, a hand axe, a bomb with a lit fuse and a spinning blade. Each points along +x, so the
    /// flight's angle turns it. Fireballs and plain bolts keep the glowing ember.
    /// </summary>
    public static class ProjectileArt
    {
        // '#' main, '+' second colour, 'o' outline, '*' a hot spark; '.' clear. Top row first.
        static readonly Dictionary<ProjectileKind, (string[] rows, Color32 main, Color32 second)> Glyphs = new Dictionary<ProjectileKind, (string[], Color32, Color32)>
        {
            [ProjectileKind.Knife] = (new[]
            {
                "..........oo..",
                "oooooo...o##o.",
                "o++++oooo####o",
                "oooooo...o##o.",
                "..........oo..",
            }, new Color32(205, 210, 215, 255), new Color32(110, 70, 40, 255)),
            [ProjectileKind.Axe] = (new[]
            {
                "........ooo...",
                ".......o###o..",
                "oooooooo####o.",
                "o+++++++o###o.",
                "oooooooo####o.",
                ".......o###o..",
                "........ooo...",
            }, new Color32(170, 175, 180, 255), new Color32(120, 80, 45, 255)),
            [ProjectileKind.Bomb] = (new[]
            {
                "......*.",
                ".....o+.",
                "..oooo..",
                ".o####o.",
                "o######o",
                "o##+###o",
                "o######o",
                ".o####o.",
                "..oooo..",
            }, new Color32(45, 42, 48, 255), new Color32(200, 150, 80, 255)),
            [ProjectileKind.Bouncing] = (new[]
            {
                "...oo...",
                "..o##o..",
                "oo#++#oo",
                "o#+oo+#o",
                "o#+oo+#o",
                "oo#++#oo",
                "..o##o..",
                "...oo...",
            }, new Color32(200, 205, 212, 255), new Color32(130, 135, 145, 255)),
        };

        static readonly Dictionary<ProjectileKind, Sprite> Sprites = new Dictionary<ProjectileKind, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Sprites.Clear();

        /// <summary>The kind's sprite, or null for the kinds drawn as an ember (plain bolts, fireballs).</summary>
        public static Sprite For(ProjectileKind kind)
        {
            if (!Glyphs.TryGetValue(kind, out var glyph))
                return null;
            if (Sprites.TryGetValue(kind, out var sprite) && sprite != null)
                return sprite;
            var (rows, main, second) = glyph;
            int h = rows.Length, w = rows[0].Length;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "Projectile " + kind,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[w * h];
            var outline = new Color32(20, 14, 12, 255);
            var spark = new Color32(255, 220, 120, 255);
            for (var row = 0; row < h; row++)
                for (var x = 0; x < w; x++)
                {
                    var c = rows[row][x];
                    pixels[(h - 1 - row) * w + x] = c switch
                    {
                        '#' => main,
                        '+' => second,
                        'o' => outline,
                        '*' => spark,
                        _ => new Color32(0, 0, 0, 0),
                    };
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelArt.PixelsPerUnit, 0, SpriteMeshType.FullRect,
                Vector4.zero, false);
            return Sprites[kind] = sprite;
        }
    }
}
