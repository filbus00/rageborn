using UnityEngine;
using UnityEngine.Rendering;

namespace ARPG
{
    /// <summary>
    /// A short-lived slash wedge that shows where a sweep landed. Placeholder feedback so combat is readable.
    /// Effects live under a parent scaled to half height, which projects ground space onto the isometric view,
    /// so this object is positioned and rotated in ground space.
    /// </summary>
    public class SweepEffect : MonoBehaviour
    {
        SpriteRenderer spriteRenderer;
        Color color;
        float remaining;
        float duration;

        /// <summary>Creates a hidden effect under a ground space parent (see the class summary).</summary>
        public static SweepEffect Create(Transform groundSpaceParent)
        {
            var go = new GameObject("Sweep Effect", typeof(SpriteRenderer), typeof(SweepEffect));
            go.transform.SetParent(groundSpaceParent, false);

            var effect = go.GetComponent<SweepEffect>();
            effect.spriteRenderer = go.GetComponent<SpriteRenderer>();
            effect.spriteRenderer.sortingLayerName = GameSortingLayers.Effects;
            effect.spriteRenderer.enabled = false;

            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline != null && pipeline.default2DMaterial != null)
                effect.spriteRenderer.sharedMaterial = pipeline.default2DMaterial;

            return effect;
        }

        /// <summary>Shows a wedge at a ground position, pointing along the direction, reaching out to range.</summary>
        public void Play(Vector2 ground, Vector2 direction, float range, Sprite sprite, Color tint, float seconds)
        {
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.localPosition = new Vector3(ground.x, ground.y, 0f);
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            // The sprite is one unit across, so twice the range makes its radius equal the range.
            var diameter = range * 2f;
            transform.localScale = new Vector3(diameter, diameter, 1f);

            spriteRenderer.sprite = sprite;
            color = tint;
            duration = Mathf.Max(seconds, 1e-4f);
            remaining = seconds;
            spriteRenderer.color = tint;
            spriteRenderer.enabled = true;
        }

        void Update()
        {
            if (remaining <= 0f)
                return;

            remaining -= Time.deltaTime;
            if (remaining <= 0f)
            {
                spriteRenderer.enabled = false;
                return;
            }

            var faded = color;
            faded.a *= remaining / duration;
            spriteRenderer.color = faded;
        }
    }
}
