using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// Floating combat text: a number pops up at a world position, rises and fades, then returns to the pool.
    /// Screen Space Overlay UI following the camera each frame (the usual trick: for an Overlay canvas, setting a
    /// RectTransform's world position to a screen-space point places it correctly regardless of the canvas's own
    /// scale), so it works at any zoom without ground-space sorting concerns. Runs on unscaled time so a number
    /// still animates through a <see cref="HitStop"/>. Docs give no numbers for the look of this; all values here
    /// are tuning.
    /// </summary>
    public class DamageNumbers : MonoBehaviour
    {
        const int PoolSize = 20;
        const float RiseScreenPixels = 90f;
        // Smaller and shorter since 2026-10-19 (the owner: "visually cluttered"; 40 and 58 points, 0.7 s before).
        const float DurationSeconds = 0.55f;
        const int NormalFontSize = 28;
        const int CriticalFontSize = 42;

        static readonly Color NormalColor = Color.white;
        static readonly Color CriticalColor = new Color(1f, 0.65f, 0.15f);
        static readonly Color PlayerDamageColor = new Color(1f, 0.35f, 0.35f);

        [Tooltip("Parent for the pooled number labels. A RectTransform on this screen space overlay canvas.")]
        [SerializeField] RectTransform pool;

        struct Slot
        {
            public RectTransform Rect;
            public Text Label;
            public Vector3 WorldOrigin;
            public float Elapsed;
            public bool Active;
        }

        Slot[] slots;
        Camera cam;

        /// <summary>The instance in the current scene, or null when it has none.</summary>
        public static DamageNumbers Current { get; private set; }

        /// <summary>In the menus' pixel fonts (2026-11-08, the owner: "make the damage numbers pixel font too"): the
        /// canvas at the menus' pixel size, numbers in the body font, crits and callouts in the title font, an outline a
        /// pixel wide, placed on whole menu pixels.</summary>
        bool pixel;
        float pixelScale = 1f;

        void Awake()
        {
            Current = this;
            cam = Camera.main;
            pixel = PixelUi.Available && pool != null;
            if (pixel)
            {
                pixelScale = PixelUi.Scale(Screen.width);
                var canvas = pool.GetComponentInParent<Canvas>();
                if (canvas != null)
                    canvas.pixelPerfect = true;
                var scaler = pool.GetComponentInParent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    scaler.scaleFactor = pixelScale;
                }
            }
            slots = new Slot[PoolSize];
            for (var i = 0; i < slots.Length; i++)
                slots[i] = CreateSlot();
        }

        void OnDestroy()
        {
            if (Current == this)
                Current = null;
        }

        Slot CreateSlot()
        {
            var go = new GameObject("Number", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(pool, false);

            var text = go.GetComponent<Text>();
            text.font = pixel ? PixelUi.BodyFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = pixel ? FontStyle.Normal : FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow; // a callout like "LEVEL 12" is wider than a number
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            if (pixel)
            {
                var outline = go.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
                outline.effectDistance = new Vector2(1f, -1f);
            }
            else
                go.AddComponent<Shadow>().effectDistance = new Vector2(2f, -2f);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = pixel ? new Vector2(120f, 20f) : new Vector2(320f, 90f);
            go.SetActive(false);

            return new Slot { Rect = rect, Label = text };
        }

        /// <summary>Pops a number at a world position. <paramref name="isDamageToPlayer"/> tints it differently
        /// from damage the player deals.</summary>
        public void Show(Vector3 worldPosition, float amount, bool critical, bool isDamageToPlayer)
        {
            if (slots == null || slots.Length == 0)
                return;

            var index = FindFreeOrOldest();
            slots[index].WorldOrigin = worldPosition;
            slots[index].Elapsed = 0f;
            slots[index].Active = true;
            slots[index].Label.text = Mathf.RoundToInt(amount).ToString();
            slots[index].Label.color = isDamageToPlayer ? PlayerDamageColor : critical ? CriticalColor : NormalColor;
            SetSize(slots[index].Label, critical ? CriticalFontSize : NormalFontSize);
            slots[index].Rect.gameObject.SetActive(true);

            if (cam != null)
                Reposition(ref slots[index]); // avoid a one-frame flash at wherever this slot last was
        }

        /// <summary>Pops a line of text at a world position, on the same float-and-fade path as a number. Used for the
        /// level-up callout.</summary>
        public void ShowText(Vector3 worldPosition, string text, Color color, int fontSize)
        {
            if (slots == null || slots.Length == 0)
                return;

            var index = FindFreeOrOldest();
            slots[index].WorldOrigin = worldPosition;
            slots[index].Elapsed = 0f;
            slots[index].Active = true;
            slots[index].Label.text = text;
            slots[index].Label.color = color;
            SetSize(slots[index].Label, fontSize);
            slots[index].Rect.gameObject.SetActive(true);

            if (cam != null)
                Reposition(ref slots[index]);
        }

        int FindFreeOrOldest()
        {
            for (var i = 0; i < slots.Length; i++)
                if (!slots[i].Active)
                    return i;

            // The pool is exhausted (a big fight landed a lot of hits at once): steal the one closest to fading out.
            var oldest = 0;
            for (var i = 1; i < slots.Length; i++)
                if (slots[i].Elapsed > slots[oldest].Elapsed)
                    oldest = i;
            return oldest;
        }

        void Update()
        {
            if (cam == null)
                cam = Camera.main;
            if (cam == null)
                return;

            for (var i = 0; i < slots.Length; i++)
            {
                if (!slots[i].Active)
                    continue;

                slots[i].Elapsed += Time.unscaledDeltaTime;
                if (slots[i].Elapsed >= DurationSeconds)
                {
                    slots[i].Active = false;
                    slots[i].Rect.gameObject.SetActive(false);
                    continue;
                }

                Reposition(ref slots[i]);
            }
        }

        // A pixel font has one size: the title font for crits and big callouts, the body font for the rest.
        void SetSize(Text label, int size)
        {
            if (!pixel)
            {
                label.fontSize = size;
                return;
            }
            var font = size >= CriticalFontSize ? PixelUi.TitleFont : PixelUi.BodyFont;
            label.font = font;
            label.fontSize = font.fontSize;
        }

        void Reposition(ref Slot slot)
        {
            var t = slot.Elapsed / DurationSeconds;
            var screen = cam.WorldToScreenPoint(slot.WorldOrigin);
            screen.y += RiseScreenPixels * t;
            if (pixel)
            {
                screen.x = Mathf.Round(screen.x / pixelScale) * pixelScale;
                screen.y = Mathf.Round(screen.y / pixelScale) * pixelScale;
            }
            slot.Rect.position = screen;

            var color = slot.Label.color;
            color.a = 1f - t;
            slot.Label.color = color;
        }
    }
}
