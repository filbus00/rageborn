using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Her aim on the ground (the owner, 2026-10-08): a faint wedge from her toward her target, as wide as her arrows
    /// may stray (<see cref="AimRules"/>): a thin line at perfect aim, wider at 80 percent, widest at 60, tinted white,
    /// amber and red. Shown only while she has a target and is fighting. Added by <see cref="PlayerCombat"/>.
    /// </summary>
    public class AimCone : MonoBehaviour
    {
        /// <summary>The wedge's width at its far end at perfect aim, in world units: a thin line.</summary>
        const float LineWidth = 0.05f;

        static readonly Color Steady = new Color(1f, 1f, 1f, 0.22f);
        static readonly Color Walking = new Color(1f, 0.78f, 0.3f, 0.26f);
        static readonly Color Running = new Color(1f, 0.32f, 0.22f, 0.3f);

        PlayerCombat combat;
        PlayerController player;
        SpriteRenderer wedge;
        static Sprite wedgeSprite;

        void Awake()
        {
            combat = GetComponent<PlayerCombat>();
            var go = new GameObject("Aim Cone");
            go.transform.SetParent(transform, false);
            wedge = go.AddComponent<SpriteRenderer>();
            wedge.sprite = WedgeSprite;
            wedge.sortingLayerName = GameSortingLayers.Decals;
            wedge.sortingOrder = 500;
            wedge.enabled = false;
        }

        void LateUpdate()
        {
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
            var target = combat != null ? combat.Target : null;
            if (player == null || target == null || !target.IsAlive || player.Disengaged)
            {
                wedge.enabled = false;
                return;
            }

            var aim = AimRules.Aim(player.StickPush);
            var origin = IsoMath.WorldToGround(player.transform.position);
            var toTarget = target.GroundPosition - origin;
            var length = toTarget.magnitude;
            if (length < 0.5f)
            {
                wedge.enabled = false;
                return;
            }

            // The wedge's tip at her feet and its far end across the target, both projected from the ground, so it
            // lies on the floor in the isometric view.
            var stray = AimRules.MaxStrayDegrees(aim) * Mathf.Deg2Rad;
            var along = toTarget / length;
            var across = new Vector2(-along.y, along.x);
            var halfWidth = Mathf.Tan(stray) * length;
            Vector2 tip = IsoMath.GroundToWorld(origin);
            Vector2 left = IsoMath.GroundToWorld(target.GroundPosition + across * halfWidth);
            Vector2 right = IsoMath.GroundToWorld(target.GroundPosition - across * halfWidth);
            var end = (left + right) * 0.5f;
            var delta = end - tip;

            var t = wedge.transform;
            t.position = new Vector3(tip.x, tip.y, 0f);
            t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            t.localScale = new Vector3(delta.magnitude, Mathf.Max(LineWidth, (left - right).magnitude), 1f);
            wedge.color = aim >= 1f ? Steady : aim >= AimRules.WalkingAim ? Walking : Running;
            wedge.enabled = true;
        }

        /// <summary>A white wedge one unit long and one wide: its tip at the pivot on the left, its base on the right, a
        /// brighter rim along its two sides.</summary>
        static Sprite WedgeSprite
        {
            get
            {
                if (wedgeSprite != null)
                    return wedgeSprite;
                const int width = 64, height = 64;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    name = "Aim Cone",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                };
                var pixels = new Color32[width * height];
                for (var y = 0; y < height; y++)
                    for (var x = 0; x < width; x++)
                    {
                        var u = (x + 0.5f) / width;
                        var v = Mathf.Abs((y + 0.5f) / height - 0.5f) * 2f;
                        byte alpha = 0;
                        if (v <= u)
                            alpha = (byte)(u - v < 0.08f ? 255 : 110);
                        pixels[x + y * width] = new Color32(255, 255, 255, alpha);
                    }
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                // Full rect, no physics shape: the texture is no longer readable (CLAUDE.md, pixel art).
                wedgeSprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0f, 0.5f), width, 0,
                    SpriteMeshType.FullRect, Vector4.zero, false);
                return wedgeSprite;
            }
        }
    }
}
