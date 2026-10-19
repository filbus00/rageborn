using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The menus' small line icons (the owner, 2026-10-19: "Match the games menues with the concept art",
    /// menuconcept.png): one colour, drawn in white from glyphs or shapes in code and tinted where they are used, point
    /// filtered so they read as the game's pixel art. Cached.
    /// </summary>
    public static class MenuIcons
    {
        public enum Kind
        {
            Heart, Sword, Speed, Shield, Boot, Star, Backpack, Sort, Book, Cog,
            Stick, DeadZone, Portal, Potion, LootBag, Music, Speaker, Eye, Motion, Wrench,
        }

        /// <summary>The tan the concept draws them in.</summary>
        public static readonly Color Tint = new Color(0.8f, 0.68f, 0.5f, 1f);

        // '#' drawn, anything else clear. Top row first; every row of a glyph the same width.
        static readonly Dictionary<Kind, string[]> Glyphs = new Dictionary<Kind, string[]>
        {
            [Kind.Heart] = new[]
            {
                "...###....###...",
                "..#####..#####..",
                ".##############.",
                ".##############.",
                ".##############.",
                "..############..",
                "...##########...",
                "....########....",
                ".....######.....",
                "......####......",
                ".......##.......",
            },
            [Kind.Sword] = new[]
            {
                "..............##",
                ".............###",
                "............###.",
                "...........###..",
                "..........###...",
                ".........###....",
                "........###.....",
                ".......###......",
                "..#...###.......",
                "..##.###........",
                "...####.........",
                "....##..........",
                "...####.........",
                "..##..##........",
                ".##.............",
                "##..............",
            },
            [Kind.Speed] = new[]
            {
                "...........#....",
                "...........##...",
                "####.......###..",
                "...############.",
                "####.......###..",
                "...........##...",
                "...........#....",
            },
            [Kind.Shield] = new[]
            {
                ".##############.",
                ".##############.",
                ".##..........##.",
                ".##...####...##.",
                ".##...####...##.",
                "..##...##...##..",
                "..##...##...##..",
                "...##......##...",
                "....##....##....",
                ".....##..##.....",
                "......####......",
                ".......##.......",
            },
            [Kind.Boot] = new[]
            {
                "....######......",
                "....######......",
                "....######......",
                "....######......",
                "....######......",
                "....######......",
                "....#########...",
                "....###########.",
                "...############.",
                "...############.",
                "...############.",
            },
            [Kind.Star] = new[]
            {
                ".......#.......",
                "......###......",
                "......###......",
                ".....#####.....",
                "###############",
                ".#############.",
                "...#########...",
                "....#######....",
                "....#######....",
                "...####.####...",
                "...###...###...",
                "..##.......##..",
                "..#.........#..",
            },
            [Kind.Backpack] = new[]
            {
                "......####......",
                ".....#....#.....",
                "..############..",
                ".##############.",
                ".##############.",
                ".###........###.",
                ".###.######.###.",
                ".###.######.###.",
                ".###........###.",
                ".##############.",
                ".##############.",
                "..############..",
            },
            [Kind.Sort] = new[]
            {
                "..#........",
                ".###.......",
                "#####......",
                "..#.....#..",
                "..#.....#..",
                "..#.....#..",
                "..#.....#..",
                "......#####",
                ".......###.",
                "........#..",
            },
            [Kind.Book] = new[]
            {
                ".######..######.",
                "#......##......#",
                "#.####.##.####.#",
                "#......##......#",
                "#.####.##.####.#",
                "#......##......#",
                "#.####.##.####.#",
                "#......##......#",
                ".######..######.",
            },
            [Kind.Cog] = new[]
            {
                "......####......",
                "......####......",
                "..###.####.###..",
                "..############..",
                "..############..",
                "...####..####...",
                "#######..#######",
                "#####......#####",
                "#####......#####",
                "#######..#######",
                "...####..####...",
                "..############..",
                "..############..",
                "..###.####.###..",
                "......####......",
                "......####......",
            },
            [Kind.Stick] = new[]
            {
                "......####......",
                ".....######.....",
                ".....######.....",
                "......####......",
                ".......##.......",
                ".......##.......",
                ".......##.......",
                "...##########...",
                "..############..",
                "..############..",
            },
            [Kind.Potion] = new[]
            {
                "......####......",
                "......#..#......",
                "......####......",
                ".......##.......",
                ".......##.......",
                "......####......",
                "....########....",
                "...##########...",
                "..############..",
                "..############..",
                "..############..",
                "...##########...",
                "....########....",
            },
            [Kind.LootBag] = new[]
            {
                ".....######.....",
                "......####......",
                ".....##..##.....",
                "....##....##....",
                "...##########...",
                "..############..",
                ".######..######.",
                ".#####....#####.",
                ".######..######.",
                ".##############.",
                "..############..",
                "...##########...",
            },
            [Kind.Music] = new[]
            {
                "..........####..",
                "......########..",
                "......####..##..",
                "......##....##..",
                "......##....##..",
                "......##....##..",
                "......##..####..",
                "......##.#####..",
                "..#####...###...",
                ".######.........",
                ".######.........",
                "..####..........",
            },
            [Kind.Speaker] = new[]
            {
                "......##........",
                ".....###...#....",
                "....####....#...",
                "########..#..#..",
                "########...#.#..",
                "########...#.#..",
                "########..#..#..",
                "....####....#...",
                ".....###...#....",
                "......##........",
            },
            [Kind.Wrench] = new[]
            {
                "..........###...",
                ".........#..##..",
                "............###.",
                "........#..####.",
                ".......#######..",
                "......#####.....",
                ".....###........",
                "....###.........",
                "...###..........",
                "..###...........",
                ".###............",
                ".##.............",
            },
        };

        static readonly Dictionary<Kind, Sprite> Sprites = new Dictionary<Kind, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Sprites.Clear();

        /// <summary>Whether a pixel of the icon is drawn, (0, 0) the top left, in a grid <see cref="Size"/> big.</summary>
        public static bool Drawn(Kind kind, int x, int row)
        {
            if (Glyphs.TryGetValue(kind, out var rows))
                return rows[row][x] == '#';
            // The round ones are shapes, on a 16 x 16 grid.
            var dx = x + 0.5f - 8f;
            var dy = row + 0.5f - 8f;
            var r = Mathf.Sqrt(dx * dx + dy * dy);
            switch (kind)
            {
                case Kind.DeadZone:
                    return (r >= 6.2f && r <= 7.6f) || r <= 1.8f;
                case Kind.Portal:
                {
                    // A spiral of about two turns: on the arm when the radius matches the angle's turn.
                    if (r > 7.6f || r < 0.8f)
                        return false;
                    var angle = Mathf.Atan2(dy, dx);
                    if (angle < 0f)
                        angle += Mathf.PI * 2f;
                    for (var turn = 0; turn < 3; turn++)
                    {
                        var armRadius = 1.1f + (angle + turn * Mathf.PI * 2f) / (Mathf.PI * 2f) * 2.9f;
                        if (Mathf.Abs(r - armRadius) < 0.75f)
                            return true;
                    }
                    return false;
                }
                case Kind.Eye:
                {
                    // An almond outline and a pupil.
                    var ex = dx / 7.6f;
                    var ey = dy / 4.4f;
                    var e = ex * ex + ey * ey;
                    return (e <= 1f && e >= 0.62f) || r <= 2.4f;
                }
                case Kind.Motion:
                {
                    // Two waves.
                    var wave = Mathf.Sin((x + 0.5f) / 16f * Mathf.PI * 2f) * 1.6f;
                    return Mathf.Abs(dy + 3f - wave) < 0.8f || Mathf.Abs(dy - 3f - wave) < 0.8f;
                }
                default:
                    return false;
            }
        }

        public static Vector2Int Size(Kind kind) =>
            Glyphs.TryGetValue(kind, out var rows) ? new Vector2Int(rows[0].Length, rows.Length) : new Vector2Int(16, 16);

        public static Sprite For(Kind kind)
        {
            if (Sprites.TryGetValue(kind, out var sprite) && sprite != null)
                return sprite;
            var size = Size(kind);
            var texture = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false)
            {
                name = "Menu Icon " + kind,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[size.x * size.y];
            for (var row = 0; row < size.y; row++)
                for (var x = 0; x < size.x; x++)
                    pixels[(size.y - 1 - row) * size.x + x] = Drawn(kind, x, row) ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            // Full rect, no physics shape: the texture is no longer readable (CLAUDE.md, pixel art).
            sprite = Sprite.Create(texture, new Rect(0, 0, size.x, size.y), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                Vector4.zero, false);
            return Sprites[kind] = sprite;
        }
    }
}
