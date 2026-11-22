using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// Wild Volley's cooldown on the HUD (2026-11-22): a small ring above the coins at the bottom right, shown once the
    /// company has learned the double tap. It fills as the volley's cooldown passes, and glows when it is ready.
    /// </summary>
    public class VolleyIndicator : MonoBehaviour
    {
        const float Size = 120f;
        static readonly Color Back = new Color(0.08f, 0.1f, 0.08f, 0.8f);
        static readonly Color Filling = new Color(0.35f, 0.55f, 0.3f, 0.9f);
        static readonly Color Ready = new Color(0.55f, 1f, 0.5f, 1f);

        GameObject body;
        Image ring;
        Image fill;
        Text label;
        PlayerCombat combat;

        public static VolleyIndicator Create(Transform parent)
        {
            var rect = UiStyle.Place(UiStyle.Rect(parent, "Wild Volley"), new Vector2(1f, 0f),
                new Vector2(-HudLayout.Margin - Size * 0.5f, HudLayout.Margin + HudLayout.IconButtonSize + 40f + Size * 0.5f), new Vector2(Size, Size));
            var indicator = rect.gameObject.AddComponent<VolleyIndicator>();
            var body = UiStyle.Rect(rect, "Body");
            UiStyle.Stretch(body);
            indicator.body = body.gameObject;
            indicator.ring = UiStyle.Disc(body, "Back", Back);
            UiStyle.Stretch(indicator.ring.rectTransform);
            indicator.ring.raycastTarget = false;
            indicator.fill = UiStyle.Disc(body, "Fill", Filling);
            UiStyle.Stretch(indicator.fill.rectTransform, 10f);
            indicator.fill.type = Image.Type.Filled;
            indicator.fill.fillMethod = Image.FillMethod.Radial360;
            indicator.fill.fillOrigin = 2;
            indicator.fill.raycastTarget = false;
            var text = UiStyle.Rect(body, "Label");
            UiStyle.Stretch(text);
            indicator.label = text.gameObject.AddComponent<Text>();
            indicator.label.font = UiStyle.Title;
            indicator.label.fontSize = 40;
            indicator.label.text = "x2";
            indicator.label.alignment = TextAnchor.MiddleCenter;
            indicator.label.color = Color.white;
            indicator.label.raycastTarget = false;
            indicator.body.SetActive(false);
            return indicator;
        }

        void Update()
        {
            if (combat == null)
                combat = FindAnyObjectByType<PlayerCombat>();
            var learned = combat != null && combat.WildVolleyTier >= WildVolleyRules.Learned;
            if (body.activeSelf != learned)
                body.SetActive(learned);
            if (!learned)
                return;
            var ready = combat.WildVolleyReady;
            fill.fillAmount = ready;
            fill.color = ready >= 1f ? Color.Lerp(Ready, Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f)) : Filling;
        }
    }
}
