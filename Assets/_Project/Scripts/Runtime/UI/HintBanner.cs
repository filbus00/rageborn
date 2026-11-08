using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// A one-line hint across the top of the screen, for the onboarding moments in Docs/06-ui-ux.md (the Forge after the
    /// first Rare, the first Legendary). A dark stone panel with a thin ember stroke; it fades in, stays a few seconds and
    /// fades out, and a tap dismisses it. Built in code on its own overlay canvas, kept across scenes.
    /// </summary>
    public class HintBanner : MonoBehaviour
    {
        const float ShowSeconds = 7f;
        const float FadeSeconds = 0.3f;

        static readonly Color PanelColor = UiStyle.Sheet;
        static readonly Color EmberColor = UiStyle.Blood;

        CanvasGroup group;
        Text text;
        float remaining;

        public static HintBanner Current { get; private set; }

        public static HintBanner Create()
        {
            var canvasObject = new GameObject("Hint Banner Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            DontDestroyOnLoad(canvasObject);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 45; // Above the HUD and the boss bar, under the inventory and Forge sheets (50).
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight = 0.5f;

            var banner = canvasObject.AddComponent<HintBanner>();
            if (PixelUi.Available)
            {
                // The menus' pixel look (2026-11-08, the owner: "make the portal banner ... pixel font too").
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = PixelUi.Scale(Screen.width);
                canvas.pixelPerfect = true;
                banner.BuildPixel();
            }
            else
                banner.Build();
            Current = banner;
            return banner;
        }

        void Build()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            // Under the life and XP bars, clear of the mini-map on the right.
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(Button));
            panel.transform.SetParent(transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -560f);
            rect.sizeDelta = new Vector2(1060f, 190f);
            panel.GetComponent<Image>().color = PanelColor;
            panel.GetComponent<Button>().onClick.AddListener(() => remaining = Mathf.Min(remaining, FadeSeconds));

            var stroke = new GameObject("Stroke", typeof(RectTransform), typeof(Image));
            stroke.transform.SetParent(panel.transform, false);
            var strokeRect = (RectTransform)stroke.transform;
            strokeRect.anchorMin = new Vector2(0f, 1f);
            strokeRect.anchorMax = new Vector2(1f, 1f);
            strokeRect.pivot = new Vector2(0.5f, 1f);
            strokeRect.sizeDelta = new Vector2(0f, 4f);
            stroke.GetComponent<Image>().color = EmberColor;
            stroke.GetComponent<Image>().raycastTarget = false;

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(36f, 16f);
            textRect.offsetMax = new Vector2(-36f, -16f);
            text = textObject.GetComponent<Text>();
            text.font = UiStyle.Title;
            text.fontSize = 36;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = UiStyle.TextMain;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        // A framed panel in the company menu's look across the screen under the HUD's bars, the words in the body font.
        void BuildPixel()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            var panel = PixelUi.Frame(transform, "Panel", "frame_panel_b4", true);
            var rect = panel.rectTransform;
            rect.anchorMin = new Vector2(0f, 0.78f);
            rect.anchorMax = new Vector2(1f, 0.78f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(12f, -38f);
            rect.offsetMax = new Vector2(-12f, 0f);
            var button = panel.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => remaining = Mathf.Min(remaining, FadeSeconds));
            text = PixelUi.Label(panel.transform, "", PixelUi.BodyFont, PixelUi.Text, TextAnchor.MiddleCenter);
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 4f);
            textRect.offsetMax = new Vector2(-10f, -4f);
        }

        // Tags the pixel font cannot show.
        static readonly System.Text.RegularExpressions.Regex Tags = new System.Text.RegularExpressions.Regex(@"</?(size|b|i)(=[^>]*)?>");

        public void Show(string message)
        {
            text.text = PixelUi.Available ? Tags.Replace(message, "") : message;
            remaining = ShowSeconds;
            Sfx.Play(SoundId.Hint);
        }

        void Update()
        {
            // Unscaled: the inventory screen pauses the game, and a hint about the Bag should still fade then.
            remaining = Mathf.Max(0f, remaining - Time.unscaledDeltaTime);
            var shown = ShowSeconds - remaining;
            var alpha = remaining <= 0f ? 0f : Mathf.Min(1f, Mathf.Min(shown, remaining) / FadeSeconds);
            group.alpha = alpha;
            group.blocksRaycasts = alpha > 0.5f;
        }
    }
}
