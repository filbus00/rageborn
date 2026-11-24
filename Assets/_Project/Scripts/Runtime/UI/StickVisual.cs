using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Draws the floating stick: a ring where the thumb landed and a knob that follows it.
    /// Both images anchor to the bottom-left of a screen-space overlay canvas.
    /// </summary>
    public class StickVisual : MonoBehaviour
    {
        // The knob is smaller than the ring; both are sized from the stick radius.
        const float KnobDiameterFactor = 0.9f;

        [SerializeField] FloatingStickInput input;
        [SerializeField] Canvas canvas;
        [SerializeField] RectTransform baseImage;
        [SerializeField] RectTransform knobImage;

        // Wild Volley's cooldown round the stick (2026-11-24, the owner: "put the cooldown timer around the thumb control"):
        // a thin ring just outside the stick's that fills clockwise in yellow as the cooldown passes and turns green when the
        // double tap is ready. Shown only while the stick is and once the chosen double-tap skill is learned.
        const float CooldownRingFactor = 1.22f;
        // Yellow while it loads, green when ready (2026-11-24, the owner).
        static readonly Color Cooling = new Color(1f, 0.85f, 0.25f, 0.85f);
        static readonly Color Ready = new Color(0.4f, 0.95f, 0.4f, 0.95f);
        UnityEngine.UI.Image cooldownRing;
        PlayerCombat combat;
        float readyFlash;
        bool wasReady;

        public void Configure(FloatingStickInput stickInput, Canvas parentCanvas, RectTransform ring, RectTransform knob)
        {
            input = stickInput;
            canvas = parentCanvas;
            baseImage = ring;
            knobImage = knob;
        }

        void LateUpdate()
        {
            var active = input != null && input.IsActive;
            baseImage.gameObject.SetActive(active);
            knobImage.gameObject.SetActive(active);
            UpdateCooldown(active);
            if (!active)
                return;

            // On an overlay canvas, canvas units are screen pixels divided by the canvas scale factor.
            var toCanvas = 1f / canvas.scaleFactor;
            var radius = input.RadiusPixels;

            baseImage.sizeDelta = Vector2.one * (radius * 2f * toCanvas);
            baseImage.anchoredPosition = input.BaseScreenPosition * toCanvas;

            // The knob follows the thumb out of the ring (as far as the base lets it drift), so leaving the ring to
            // disengage is seen.
            var knobOffset = Vector2.ClampMagnitude(input.ThumbScreenPosition - input.BaseScreenPosition, radius * StickMath.DriftFactor);
            knobImage.sizeDelta = Vector2.one * (radius * KnobDiameterFactor * toCanvas);
            knobImage.anchoredPosition = (input.BaseScreenPosition + knobOffset) * toCanvas;
            if (cooldownRing != null && cooldownRing.gameObject.activeSelf)
            {
                var size = radius * 2f * CooldownRingFactor * toCanvas * (1f + readyFlash * 0.15f);
                cooldownRing.rectTransform.sizeDelta = Vector2.one * size;
                cooldownRing.rectTransform.anchoredPosition = input.BaseScreenPosition * toCanvas;
            }
        }

        void UpdateCooldown(bool active)
        {
            if (combat == null)
                combat = FindAnyObjectByType<PlayerCombat>();
            var learned = combat != null && combat.WildVolleyTier >= WildVolleyRules.Learned;
            if (cooldownRing == null)
            {
                if (!learned || baseImage == null)
                    return;
                var go = new GameObject("Volley Cooldown", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                go.transform.SetParent(baseImage.parent, false);
                go.transform.SetSiblingIndex(baseImage.GetSiblingIndex());
                cooldownRing = go.GetComponent<UnityEngine.UI.Image>();
                cooldownRing.sprite = RingSprite();
                cooldownRing.type = UnityEngine.UI.Image.Type.Filled;
                cooldownRing.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
                cooldownRing.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
                cooldownRing.fillClockwise = true;
                cooldownRing.raycastTarget = false;
                var rect = cooldownRing.rectTransform;
                rect.anchorMin = rect.anchorMax = baseImage.anchorMin;
                rect.pivot = new Vector2(0.5f, 0.5f);
            }
            var show = active && learned;
            if (cooldownRing.gameObject.activeSelf != show)
                cooldownRing.gameObject.SetActive(show);
            if (!learned)
                return;
            var fill = combat.WildVolleyReady;
            var ready = fill >= 1f;
            // A short swell and flash the moment it comes ready.
            if (ready && !wasReady)
                readyFlash = 1f;
            wasReady = ready;
            readyFlash = Mathf.Max(0f, readyFlash - Time.unscaledDeltaTime * 3f);
            if (!show)
                return;
            cooldownRing.fillAmount = fill;
            cooldownRing.color = ready ? Color.Lerp(Ready, Color.white, readyFlash) : Cooling;
        }

        static Sprite ring;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => ring = null;

        // A thin ring, drawn once: white, tinted by the image's colour.
        static Sprite RingSprite()
        {
            if (ring != null)
                return ring;
            const int size = 128;
            // Half as thick as the first (2026-11-24, the owner: "the ring is too thick").
            const float outer = 63f, inner = 59f;
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f));
                    var a = Mathf.Clamp01(outer - d + 0.5f) * Mathf.Clamp01(d - inner + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            ring = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            return ring;
        }
    }
}
