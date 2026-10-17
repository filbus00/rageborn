using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// A small pixel shield over her head while she is in cover (2026-10-17, <see cref="CoverRules"/>), grey when covered
    /// and gold when the next shot is an ambush.
    /// </summary>
    public sealed class CoverShield
    {
        const float LocalY = 2.15f;

        static Sprite sprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => sprite = null;

        static readonly Color Covered = new Color(0.78f, 0.82f, 0.9f, 0.85f);
        static readonly Color Ready = new Color(1f, 0.82f, 0.35f, 0.95f);

        readonly SpriteRenderer renderer;
        bool shown;
        bool ready;

        public static CoverShield Create(Transform player) => new CoverShield(player);

        CoverShield(Transform player)
        {
            var go = new GameObject("Cover Shield", typeof(SpriteRenderer));
            go.transform.SetParent(player, false);
            go.transform.localPosition = new Vector3(0f, LocalY, 0f);
            renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = Sprite_();
            renderer.sortingLayerName = GameSortingLayers.WorldUI;
            renderer.color = Covered;
            go.SetActive(false);
        }

        public void Show(bool covered, bool ambushReady)
        {
            if (renderer == null)
                return;
            if (covered != shown)
            {
                shown = covered;
                renderer.gameObject.SetActive(covered);
            }
            if (covered && ambushReady != ready)
            {
                ready = ambushReady;
                renderer.color = ambushReady ? Ready : Covered;
            }
        }

        // A 9 x 11 heater shield, white so the colour tints it; drawn at 40 px a unit like the world's pixel art.
        static Sprite Sprite_()
        {
            if (sprite != null)
                return sprite;
            string[] rows =
            {
                "#########",
                "#.......#",
                "#.......#",
                "#.......#",
                "#.......#",
                "#.......#",
                ".#.....#.",
                ".#.....#.",
                "..#...#..",
                "...#.#...",
                "....#....",
            };
            var w = rows[0].Length;
            var h = rows.Length;
            var pixels = new Color32[w * h];
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    var c = rows[h - 1 - y][x];
                    pixels[y * w + x] = c == '#' ? new Color32(255, 255, 255, 255) : c == '.' && Inside(rows, h - 1 - y, x) ? new Color32(150, 150, 160, 200) : new Color32(0, 0, 0, 0);
                }
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), PixelArt.PixelsPerUnit, 0, SpriteMeshType.FullRect, Vector4.zero, false);
            return sprite;
        }

        // A dot between two outline pixels on its row is inside the shield.
        static bool Inside(string[] rows, int row, int x)
        {
            var line = rows[row];
            return line.IndexOf('#') < x && line.LastIndexOf('#') > x;
        }
    }
}
