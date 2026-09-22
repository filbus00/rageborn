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
        const float DurationSeconds = 0.7f;
        const int NormalFontSize = 40;
        const int CriticalFontSize = 58;

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

        void Awake()
        {
            Current = this;
            cam = Camera.main;
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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            go.AddComponent<Shadow>().effectDistance = new Vector2(2f, -2f);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(320f, 90f);
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
            slots[index].Label.fontSize = critical ? CriticalFontSize : NormalFontSize;
            slots[index].Rect.gameObject.SetActive(true);

            if (cam != null)
                Reposition(ref slots[index]); // avoid a one-frame flash at wherever this slot last was
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

        void Reposition(ref Slot slot)
        {
            var t = slot.Elapsed / DurationSeconds;
            var screen = cam.WorldToScreenPoint(slot.WorldOrigin);
            screen.y += RiseScreenPixels * t;
            slot.Rect.position = screen;

            var color = slot.Label.color;
            color.a = 1f - t;
            slot.Label.color = color;
        }
    }
}
