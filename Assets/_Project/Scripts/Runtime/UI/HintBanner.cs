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

        static readonly Color PanelColor = new Color(0.09f, 0.08f, 0.08f, 0.94f);
        static readonly Color EmberColor = new Color(1f, 0.45f, 0.15f);

        CanvasGroup group;
        Text text;
        float remaining;

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
            banner.Build();
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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 36;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        public void Show(string message)
        {
            text.text = message;
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
