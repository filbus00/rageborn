using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// A small health bar over a hurt enemy (the owner, 2026-10-17: "add small discrete health bars for enemies"),
    /// shown only once it has lost life, hidden at full life and in death. Bosses have the boss bar instead. Kept the
    /// same size whatever the enemy's scale. Allocation free after it is made.
    /// In the 16x16 style since 2026-11-08 (the owner: "make the health bars ... pixel style"; it was a thin dark strip
    /// with a red fill stretched smoothly): a drawn bar of <see cref="Pixels"/> pixels (each 2 screen pixels, as the
    /// characters) with a dark outline, a dark track and a red fill with a lighter top row, the fill a whole number of
    /// pixels; one sprite for each fill, made once.
    /// </summary>
    public sealed class EnemyHealthBar
    {
        /// <summary>The fill's length in drawn pixels.</summary>
        public const int Pixels = 9;

        /// <summary>How high over the feet it floats (before the enemy's scale): over the head of a man-sized baked
        /// figure (about 1.8 tall), lower for the small kinds.</summary>
        public static float HeightFor(float bodyRadius) => bodyRadius <= 0.3f ? 1.15f : 1.85f;

        /// <summary>How many of the bar's pixels are filled for a fraction of life left: at least one while alive.</summary>
        public static int Filled(float fraction) => fraction <= 0f ? 0 : Mathf.Clamp(Mathf.RoundToInt(fraction * Pixels), 1, Pixels);

        static Sprite[] drawn;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => drawn = null;

        readonly Transform root;
        readonly SpriteRenderer bar;
        int shownFilled = -1;
        float shownScale = -1f;
        float shownRadius = -1f;

        public EnemyHealthBar(Transform enemy)
        {
            root = new GameObject("Health Bar").transform;
            root.SetParent(enemy, false);
            var go = new GameObject("Bar", typeof(SpriteRenderer));
            go.transform.SetParent(root, false);
            bar = go.GetComponent<SpriteRenderer>();
            bar.sprite = Drawn()[Pixels];
            bar.sortingLayerName = GameSortingLayers.WorldUI;
            root.gameObject.SetActive(false);
        }

        static Sprite[] Drawn()
        {
            if (drawn != null)
                return drawn;
            drawn = new Sprite[Pixels + 1];
            const int w = Pixels + 2, h = 4;
            Color32 outline = new Color32(20, 15, 28, 230), track = new Color32(58, 26, 26, 220),
                red = new Color32(200, 50, 42, 255), light = new Color32(240, 96, 74, 255);
            for (var filled = 0; filled <= Pixels; filled++)
            {
                var pixels = new Color32[w * h];
                for (var y = 0; y < h; y++)
                    for (var x = 0; x < w; x++)
                    {
                        var edge = x == 0 || y == 0 || x == w - 1 || y == h - 1;
                        // Rows from the bottom: row 2 is the bar's top row.
                        pixels[x + y * w] = edge ? outline : x <= filled ? (y == 2 ? light : red) : track;
                    }
                var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
                {
                    name = "Health Bar " + filled, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
                };
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                drawn[filled] = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), (float)PoolArt.ScreenUnit / PoolArt.Block, 0,
                    SpriteMeshType.FullRect, Vector4.zero, false);
            }
            return drawn;
        }

        /// <param name="fraction">Life left, 0 to 1; 1 (or more) hides the bar.</param>
        /// <param name="enemyScale">The enemy's drawn scale, which the bar cancels to keep its size.</param>
        /// <param name="bodyRadius">Its body's radius, which tells a small kind (<see cref="HeightFor"/>).</param>
        public void Show(float fraction, float enemyScale, float bodyRadius)
        {
            var visible = fraction > 0f && fraction < 0.999f;
            if (root.gameObject.activeSelf != visible)
                root.gameObject.SetActive(visible);
            if (!visible)
                return;
            if (!Mathf.Approximately(enemyScale, shownScale) || !Mathf.Approximately(bodyRadius, shownRadius))
            {
                shownScale = enemyScale;
                shownRadius = bodyRadius;
                var inverse = enemyScale > 0f ? 1f / enemyScale : 1f;
                root.localScale = new Vector3(inverse, inverse, 1f);
                root.localPosition = new Vector3(0f, HeightFor(bodyRadius), 0f);
            }
            var filled = Filled(fraction);
            if (filled == shownFilled)
                return;
            shownFilled = filled;
            bar.sprite = Drawn()[filled];
        }
    }
}
