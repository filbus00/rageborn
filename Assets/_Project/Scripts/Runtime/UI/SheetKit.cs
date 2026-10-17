using System;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The parts every sheet is built from (2026-10-17, the owner: "make the menus better organized and better looking.
    /// Make sure that the menus that need to be scrolled can be scrolled"): a header with a gold rule under the title, a
    /// scrolling list with a slim scrollbar that shows when the list is longer than the sheet, rows as cards that grow to
    /// fit their text, section headings with a rule, and action buttons coloured by what they can do (bronze when they
    /// can be pressed, dim when not, red when switched on). Used by the town's sheets, Lyriel's sheet and the Bag's
    /// Boons tab.
    /// </summary>
    public static class SheetKit
    {
        public static readonly Color CardFill = new Color(0.11f, 0.085f, 0.075f, 0.96f);
        public static readonly Color CardEdge = new Color(0.3f, 0.24f, 0.17f, 1f);
        public static readonly Color Accent = new Color(0.62f, 0.46f, 0.24f, 1f);
        public static readonly Color ReadyFill = new Color(0.36f, 0.24f, 0.1f, 1f);
        public static readonly Color DoneFill = new Color(0.12f, 0.1f, 0.09f, 1f);

        const float ScrollbarWidth = 12f;

        /// <summary>A thin gold rule across its parent's width, fading at the ends.</summary>
        public static Image Rule(Transform parent, float height = 4f)
        {
            var go = new GameObject("Rule", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = RuleSprite;
            image.color = UiStyle.Gold;
            image.raycastTarget = false;
            var element = go.GetComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = height;
            return image;
        }

        /// <summary>
        /// A list that scrolls inside its parent (a vertical layout), masked, taking the space left; a slim scrollbar on
        /// its right shows when the rows are longer than the space. Returns the rows' parent.
        /// </summary>
        public static RectTransform ScrollList(Transform parent, out ScrollRect scroll)
        {
            var view = new GameObject("View", typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect), typeof(Image), typeof(LayoutElement));
            view.transform.SetParent(parent, false);
            view.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            view.GetComponent<LayoutElement>().flexibleHeight = 1f;

            var list = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            list.transform.SetParent(view.transform, false);
            var listRect = (RectTransform)list.transform;
            listRect.anchorMin = new Vector2(0f, 1f);
            listRect.anchorMax = new Vector2(1f, 1f);
            listRect.pivot = new Vector2(0.5f, 1f);
            listRect.offsetMin = Vector2.zero;
            listRect.offsetMax = new Vector2(-(ScrollbarWidth + 10f), 0f);
            var layout = list.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(0, 0, 4, 24);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            list.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var bar = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            bar.transform.SetParent(view.transform, false);
            var barRect = (RectTransform)bar.transform;
            barRect.anchorMin = new Vector2(1f, 0f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(1f, 0.5f);
            barRect.sizeDelta = new Vector2(ScrollbarWidth, -8f);
            bar.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(bar.transform, false);
            var handleRect = (RectTransform)handle.transform;
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = handleRect.offsetMax = Vector2.zero;
            var handleImage = handle.GetComponent<Image>();
            handleImage.color = new Color(UiStyle.Gold.r, UiStyle.Gold.g, UiStyle.Gold.b, 0.75f);
            var scrollbar = bar.GetComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            scroll = view.GetComponent<ScrollRect>();
            scroll.viewport = (RectTransform)view.transform;
            scroll.content = listRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.08f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            scroll.scrollSensitivity = 40f;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return listRect;
        }

        /// <summary>A section heading inside a list: gold small capitals over a rule.</summary>
        public static void Heading(Transform content, string label)
        {
            var group = new GameObject("Heading", typeof(RectTransform), typeof(VerticalLayoutGroup));
            group.transform.SetParent(content, false);
            var layout = group.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 18, 4);
            layout.spacing = 6f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            var text = UiStyle.Text(group.transform, label, 36, UiStyle.Gold, TextAnchor.LowerLeft, true);
            text.gameObject.AddComponent<LayoutElement>().minHeight = 46f;
            var rule = Rule(group.transform, 3f);
            rule.color = new Color(UiStyle.Gold.r, UiStyle.Gold.g, UiStyle.Gold.b, 0.55f);
        }

        /// <summary>A line of text in the list, sized to what it says.</summary>
        public static Text Note(Transform content, string text, int size = 30, Color? color = null, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var line = UiStyle.Text(content, text, size, color ?? UiStyle.TextDim, alignment, true);
            line.gameObject.AddComponent<LayoutElement>().minHeight = size * 1.6f;
            return line;
        }

        /// <summary>
        /// A card with text on the left (a bold first line, then smaller lines) and, when <paramref name="action"/> is
        /// given, a button on the right. It grows to fit its text, never shorter than <paramref name="minHeight"/>.
        /// Returns the card's transform (an icon can be put first with <see cref="Icon"/>).
        /// </summary>
        public static RectTransform Card(Transform content, string label, string action, bool enabled, Action onClick, float minHeight = 130f,
            float buttonWidth = 300f, bool highlighted = false, Sprite icon = null, Color? accent = null)
        {
            var row = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(content, false);
            var fill = row.GetComponent<Image>();
            fill.color = CardFill;
            var edge = row.AddComponent<Outline>();
            edge.effectColor = CardEdge;
            edge.effectDistance = new Vector2(2f, -2f);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(0, 16, 14, 14);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            row.GetComponent<LayoutElement>().minHeight = minHeight;

            // The accent stripe down the left edge.
            var stripe = new GameObject("Stripe", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            stripe.transform.SetParent(row.transform, false);
            stripe.GetComponent<Image>().color = accent ?? Accent;
            stripe.GetComponent<Image>().raycastTarget = false;
            var stripeElement = stripe.GetComponent<LayoutElement>();
            stripeElement.minWidth = stripeElement.preferredWidth = 6f;
            stripeElement.minHeight = minHeight - 28f;
            stripeElement.flexibleHeight = 1f;

            if (icon != null)
            {
                var image = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                image.transform.SetParent(row.transform, false);
                var picture = image.GetComponent<Image>();
                picture.sprite = icon;
                picture.preserveAspect = true;
                picture.raycastTarget = false;
                var element = image.GetComponent<LayoutElement>();
                element.minWidth = element.preferredWidth = 84f;
                element.minHeight = element.preferredHeight = 84f;
            }

            var text = UiStyle.Text(row.transform, label, 32, UiStyle.TextMain, TextAnchor.MiddleLeft, true);
            var textElement = text.gameObject.AddComponent<LayoutElement>();
            textElement.flexibleWidth = 1f;
            textElement.minWidth = 10f;

            if (action != null)
            {
                var button = ActionButton(row.transform, action, enabled, highlighted, onClick);
                var element = button.gameObject.AddComponent<LayoutElement>();
                element.minWidth = element.preferredWidth = buttonWidth;
                element.minHeight = element.preferredHeight = Mathf.Max(96f, minHeight - 32f);
            }
            return (RectTransform)row.transform;
        }

        /// <summary>A button styled by what it can do: bronze with gold text when it can be pressed, dim when not, red
        /// when it stands for something switched on.</summary>
        public static Button ActionButton(Transform parent, string label, bool enabled, bool highlighted, Action onClick)
        {
            var go = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = highlighted ? UiStyle.Selected : enabled ? ReadyFill : DoneFill;
            UiStyle.Rim(go, enabled || highlighted ? 3f : 2f);
            var outline = go.GetComponent<Outline>();
            if (outline != null && !enabled && !highlighted)
                outline.effectColor = CardEdge;
            var button = go.GetComponent<Button>();
            button.interactable = enabled;
            var colors = button.colors;
            colors.disabledColor = Color.white;
            colors.pressedColor = new Color(1.3f, 1.2f, 1f, 1f);
            button.colors = colors;
            if (onClick != null)
                button.onClick.AddListener(() => onClick());
            var text = UiStyle.Text(go.transform, label, 30, enabled || highlighted ? UiStyle.Gold : UiStyle.TextDim, TextAnchor.MiddleCenter, true);
            UiStyle.Stretch(text.rectTransform, 8f);
            return button;
        }

        /// <summary>Rank pips for a card's first line: filled for each rank had, hollow for the rest (rich text).</summary>
        public static string Pips(int have, int max)
        {
            if (max <= 1)
                return "";
            var text = new System.Text.StringBuilder("  <size=40><color=#E8C66A>");
            for (var i = 0; i < max; i++)
            {
                if (i == have)
                    text.Append("</color><color=#5A4A3A>");
                text.Append(i < have ? "◆" : "◇");
            }
            return text.Append("</color></size>").ToString();
        }

        /// <summary>
        /// Fits a bottom sheet to its list (2026-10-17): as tall as its rows need plus its fixed parts, between
        /// <paramref name="least"/> and <paramref name="most"/>, so a short list does not sit in a screen of empty leather.
        /// </summary>
        public static void FitSheet(RectTransform sheet, RectTransform rows, float least = 900f, float most = 2060f)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rows);
            var view = (RectTransform)rows.parent;
            var chrome = sheet.rect.height - view.rect.height;
            var wanted = LayoutUtility.GetPreferredHeight(rows) + chrome;
            sheet.sizeDelta = new Vector2(sheet.sizeDelta.x, Mathf.Clamp(wanted, least, most));
        }

        static Sprite ruleSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => ruleSprite = null;

        // A white bar 64 x 4, fading out at both ends, stretched across a sheet.
        static Sprite RuleSprite
        {
            get
            {
                if (ruleSprite != null)
                    return ruleSprite;
                const int w = 64, h = 4;
                var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "Sheet Rule", wrapMode = TextureWrapMode.Clamp };
                var pixels = new Color32[w * h];
                for (var x = 0; x < w; x++)
                {
                    var edge = Mathf.Min(x, w - 1 - x) / 16f;
                    var a = (byte)(255 * Mathf.Clamp01(edge));
                    for (var y = 0; y < h; y++)
                        pixels[y * w + x] = new Color32(255, 255, 255, y == 0 || y == h - 1 ? (byte)(a / 3) : a);
                }
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                return ruleSprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                    Vector4.zero, false);
            }
        }
    }
}
