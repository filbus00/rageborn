using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// A small health bar over a hurt enemy (the owner, 2026-10-17: "add small discrete health bars for enemies"): a
    /// thin dark strip with a red fill, shown only once it has lost life, hidden at full life and in death. Bosses have
    /// the boss bar instead. Kept the same size whatever the enemy's scale. Allocation free after it is made.
    /// </summary>
    public sealed class EnemyHealthBar
    {
        public const float Width = 0.6f, Height = 0.06f;

        /// <summary>How high over the feet it floats (before the enemy's scale): over the head of a man-sized baked
        /// figure (about 1.8 tall), lower for the small kinds.</summary>
        public static float HeightFor(float bodyRadius) => bodyRadius <= 0.3f ? 1.0f : 1.7f;

        static Sprite pixel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => pixel = null;

        readonly Transform root;
        readonly Transform fill;
        float shownFraction = -1f;
        float shownScale = -1f;
        float shownRadius = -1f;

        static readonly Color Back = new Color(0.05f, 0.03f, 0.03f, 0.75f);
        static readonly Color Red = new Color(0.78f, 0.12f, 0.1f, 0.95f);

        public EnemyHealthBar(Transform enemy)
        {
            if (pixel == null)
                pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0f, 0.5f), 4f, 0,
                    SpriteMeshType.FullRect, Vector4.zero, false);
            root = new GameObject("Health Bar").transform;
            root.SetParent(enemy, false);
            Strip(root, "Back", Back, 0);
            fill = Strip(root, "Fill", Red, 1);
            root.gameObject.SetActive(false);
        }

        static Transform Strip(Transform parent, string name, Color color, int order)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(-Width / 2f, 0f, 0f);
            go.transform.localScale = new Vector3(Width, Height, 1f);
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = pixel;
            renderer.color = color;
            renderer.sortingLayerName = GameSortingLayers.WorldUI;
            renderer.sortingOrder = order;
            return go.transform;
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
            if (Mathf.Abs(fraction - shownFraction) < 0.005f)
                return;
            shownFraction = fraction;
            fill.localScale = new Vector3(Width * fraction, Height, 1f);
        }
    }
}
