using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The boss bar (Docs/06-ui-ux.md: a wide bar just below the top row with phase markers), with the stagger meter
    /// under it, and the screen flash on a phase change (Docs/06-ui-ux.md, feedback). Built in code by the fight.
    /// </summary>
    public class BossBar : MonoBehaviour
    {
        const float Width = 960f;
        const float FlashSeconds = 0.35f;
        const float FlashAlpha = 0.35f;
        const float GentleFlashSeconds = 1.2f;

        static readonly Color LifeColor = new Color(0.86f, 0.36f, 0.12f);
        static readonly Color StaggerColor = new Color(0.95f, 0.85f, 0.3f);

        GameObject panel;
        RectTransform lifeFill;
        RectTransform staggerFill;
        Text title;
        Text staggerLabel;
        Image flash;
        float flashTimer;
        bool gentleFlash;

        public static BossBar Create(Transform parent, string bossName)
        {
            var canvasObject = new GameObject("Boss Bar Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(parent, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 16; // Above the HUD and the mini-map, below the inventory screen and the fade.
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight = 0.5f;

            var bar = canvasObject.AddComponent<BossBar>();
            bar.Build(canvasObject.transform, bossName);
            return bar;
        }

        void Build(Transform canvas, string bossName)
        {
            flash = NewImage("Flash", canvas, new Color(1f, 0.9f, 0.75f, 0f));
            Stretch(flash.rectTransform);

            panel = new GameObject("Boss Bar", typeof(RectTransform));
            panel.transform.SetParent(canvas, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            // Under the life bar and the mini-map, which reach down to about 490 from the top.
            rect.anchoredPosition = new Vector2(0f, -500f);
            rect.sizeDelta = new Vector2(Width, 120f);

            title = NewText(panel.transform, bossName, 38, TextAnchor.LowerCenter);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.sizeDelta = new Vector2(0f, 48f);
            title.rectTransform.anchoredPosition = Vector2.zero;

            lifeFill = NewBar(panel.transform, -52f, 36f, LifeColor);
            // Phase markers where phases 2 and 3 begin.
            NewMarker(lifeFill.parent, BossPhases.PhaseTwoBelow);
            NewMarker(lifeFill.parent, BossPhases.PhaseThreeBelow);

            staggerFill = NewBar(panel.transform, -94f, 12f, StaggerColor);
            staggerLabel = NewText(panel.transform, "STAGGERED", 26, TextAnchor.MiddleCenter);
            staggerLabel.color = StaggerColor;
            staggerLabel.rectTransform.anchorMin = staggerLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            staggerLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            staggerLabel.rectTransform.anchoredPosition = new Vector2(0f, -108f);
            staggerLabel.rectTransform.sizeDelta = new Vector2(400f, 34f);
            staggerLabel.gameObject.SetActive(false);

            panel.SetActive(false);
        }

        public void Show() => panel.SetActive(true);

        public void Hide() => panel.SetActive(false);

        public bool IsShown => panel.activeSelf;

        public void SetTitle(string text) => title.text = text;

        public void SetLife(float fraction) => lifeFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);

        public void SetStagger(float fraction, bool staggered)
        {
            staggerFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            if (staggerLabel.gameObject.activeSelf != staggered)
                staggerLabel.gameObject.SetActive(staggered);
        }

        /// <summary>A brief full-screen flash (Docs/06-ui-ux.md: on a boss phase change). With reduce flashing on it is
        /// a fade instead: a quarter as bright, rising and falling over 1.2 s rather than popping on.</summary>
        public void Flash()
        {
            gentleFlash = SettingsDirector.Current.reduceFlashing;
            flashTimer = gentleFlash ? GentleFlashSeconds : FlashSeconds;
        }

        void Update()
        {
            if (flashTimer <= 0f)
                return;
            flashTimer = Mathf.Max(0f, flashTimer - Time.unscaledDeltaTime);
            var c = flash.color;
            c.a = gentleFlash
                ? FlashAlpha * 0.25f * Mathf.Sin(Mathf.PI * flashTimer / GentleFlashSeconds)
                : FlashAlpha * flashTimer / FlashSeconds;
            flash.color = c;
        }

        static RectTransform NewBar(Transform parent, float y, float height, Color color)
        {
            var back = NewImage("Bar", parent, new Color(0f, 0f, 0f, 0.65f));
            var rect = back.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(0f, height);

            var fill = NewImage("Fill", back.transform, color);
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            return fillRect;
        }

        static void NewMarker(Transform bar, float fraction)
        {
            var marker = NewImage("Phase Marker", bar, new Color(1f, 1f, 1f, 0.85f));
            var rect = marker.rectTransform;
            rect.anchorMin = new Vector2(fraction, 0f);
            rect.anchorMax = new Vector2(fraction, 1f);
            rect.sizeDelta = new Vector2(4f, 10f);
        }

        static Image NewImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static Text NewText(Transform parent, string content, int size, TextAnchor anchor)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = anchor;
            text.color = Color.white;
            text.text = content;
            text.raycastTarget = false;
            text.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2f, -2f);
            return text;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
