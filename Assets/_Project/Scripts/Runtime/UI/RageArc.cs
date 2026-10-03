using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// Focus and the stance stacks, drawn at the character (Docs/01-core-gameplay.md: the resource is an arc around the
    /// character so the player does not look away from the action). An arc under the feet fills with Focus (the Wild Arrow's resource, Docs/02; the class is named for the Rage it drew first); five pips
    /// under it show Momentum (cyan) or, while there is none, Stillness (amber). An overlay canvas that follows the
    /// character's screen position, built in code by <see cref="PlayerCombat"/>.
    /// </summary>
    public class RageArc : MonoBehaviour
    {
        // The arc covers this share of the circle, centered under the character.
        const float ArcShare = 0.4f;
        const float RadiusWorld = 0.75f;
        const int RingTextureSize = 128;

        static readonly Color TrackColor = new Color(0f, 0f, 0f, 0.45f);
        static readonly Color FocusColor = new Color(0.35f, 0.7f, 1f, 0.95f);
        static readonly Color MomentumColor = new Color(0.45f, 0.85f, 1f, 1f);
        static readonly Color StillnessColor = new Color(1f, 0.75f, 0.3f, 1f);
        static readonly Color EmptyPipColor = new Color(0f, 0f, 0f, 0.35f);

        static Sprite ring;

        RectTransform root;
        RectTransform squash;
        Image track;
        Image fill;
        Image[] pips;
        PlayerCombat combat;
        PlayerController player;

        /// <summary>The town has no combat object, so its arc is made here, full (FIRST-RUN expects it there too).</summary>
        public static void EnsureWithoutCombat(PlayerController player)
        {
            if (player != null && FindAnyObjectByType<PlayerCombat>() == null && FindAnyObjectByType<RageArc>() == null)
                Create(null, player);
        }

        public static RageArc Create(PlayerCombat combat, PlayerController player)
        {
            var canvasObject = new GameObject("Rage Arc Canvas", typeof(Canvas));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9; // Under the HUD (10) and every panel.

            var arc = canvasObject.AddComponent<RageArc>();
            arc.combat = combat;
            arc.player = player;
            arc.Build();
            return arc;
        }

        void Build()
        {
            var rootObject = new GameObject("Arc", typeof(RectTransform));
            rootObject.transform.SetParent(transform, false);
            root = (RectTransform)rootObject.transform;

            // The ring lies on the ground: circles under a parent squashed to half height, like every ground shape.
            // Each circle is rotated inside it so the arc's middle sits under the feet (a radial fill starting at the
            // bottom goes clockwise; turning it back by half the arc centers it), and the squash comes after.
            var squashObject = new GameObject("Ground", typeof(RectTransform));
            squashObject.transform.SetParent(root, false);
            squash = (RectTransform)squashObject.transform;
            squash.localScale = new Vector3(1f, IsoMath.GroundSquash, 1f);

            track = NewImage("Track", TrackColor);
            track.fillAmount = ArcShare;
            fill = NewImage("Focus", FocusColor);
            fill.fillAmount = 0f;

            pips = new Image[StanceStacks.MaxStacks];
            for (var i = 0; i < pips.Length; i++)
            {
                var pip = new GameObject("Pip " + i, typeof(RectTransform), typeof(Image));
                pip.transform.SetParent(root, false);
                var image = pip.GetComponent<Image>();
                image.raycastTarget = false;
                pips[i] = image;
            }
        }

        Image NewImage(string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(squash, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.localRotation = Quaternion.Euler(0f, 0f, ArcShare * 180f);
            var image = go.GetComponent<Image>();
            image.sprite = Ring;
            image.color = color;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Bottom;
            image.fillClockwise = true;
            image.raycastTarget = false;
            return image;
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null || player == null)
                return;

            // Sized from the camera each frame, so the arc keeps its world size whatever the zoom.
            var feet = player.transform.position;
            var center = cam.WorldToScreenPoint(feet);
            var edge = cam.WorldToScreenPoint(feet + Vector3.right * RadiusWorld);
            var radius = Mathf.Abs(edge.x - center.x);
            root.position = center;

            var size = new Vector2(radius * 2f, radius * 2f);
            track.rectTransform.sizeDelta = size;
            fill.rectTransform.sizeDelta = size;
            // Without combat (the town) Focus is full: it refills outside fights and nothing spends it there.
            fill.fillAmount = ArcShare * (combat != null ? combat.Focus.Fraction : 1f);

            var stance = player.Stance;
            var momentum = stance.Momentum > 0;
            var stacks = momentum ? stance.Momentum : stance.Stillness;
            var pipSize = Mathf.Max(4f, radius * 0.16f);
            for (var i = 0; i < pips.Length; i++)
            {
                var rect = (RectTransform)pips[i].transform;
                rect.sizeDelta = new Vector2(pipSize, pipSize);
                rect.anchoredPosition = new Vector2((i - (pips.Length - 1) / 2f) * pipSize * 1.6f, -radius * 0.62f);
                pips[i].color = i < stacks ? (momentum ? MomentumColor : StillnessColor) : EmptyPipColor;
            }
        }

        /// <summary>A thin white ring, made once in code.</summary>
        static Sprite Ring
        {
            get
            {
                if (ring != null)
                    return ring;
                var texture = new Texture2D(RingTextureSize, RingTextureSize, TextureFormat.RGBA32, false) { name = "Rage Ring" };
                var pixels = new Color32[RingTextureSize * RingTextureSize];
                var half = RingTextureSize / 2f;
                for (var y = 0; y < RingTextureSize; y++)
                    for (var x = 0; x < RingTextureSize; x++)
                    {
                        var r = Mathf.Sqrt((x + 0.5f - half) * (x + 0.5f - half) + (y + 0.5f - half) * (y + 0.5f - half)) / half;
                        var alpha = r > 1f || r < 0.84f ? 0f : 1f;
                        pixels[x + y * RingTextureSize] = new Color32(255, 255, 255, (byte)(alpha * 255));
                    }
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                // Full rect, no physics shape: the texture is no longer readable, and tracing its outline fails on iOS.
                ring = Sprite.Create(texture, new Rect(0, 0, RingTextureSize, RingTextureSize), new Vector2(0.5f, 0.5f), RingTextureSize, 0, SpriteMeshType.FullRect, Vector4.zero, false);
                return ring;
            }
        }
    }
}
