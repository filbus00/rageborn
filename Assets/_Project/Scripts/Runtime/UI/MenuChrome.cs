using System;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// What every full-screen menu shares, after the owner's concept art (2026-10-19, menuconcept.png): the header (her
    /// portrait in its red ring, "Wild Arrow", where she is, a thin life bar, her coins in a framed box and Close) and
    /// the page's title between two ornamental rules with diamonds. Used by the Bag (<see cref="InventoryScreen"/>) and
    /// <see cref="SettingsScreen"/>.
    /// </summary>
    public static class MenuChrome
    {
        public const float HeaderHeight = 200f, TitleHeight = 104f;

        /// <summary>The header across the top of <paramref name="parent"/>, on an opaque band.</summary>
        public static RectTransform Header(Transform parent, GameSession current, float lifeFraction, string subtitle, Action close)
        {
            var header = UiStyle.Place(UiStyle.Rect(parent, "Header"), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, HeaderHeight));
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            // An opaque band, so the HUD under the menu does not show through (2026-10-17).
            var band = UiStyle.Image(header, "Band", UiStyle.Sheet);
            UiStyle.Stretch(band.rectTransform);
            UiStyle.Leather(band);
            band.color = new Color(0.12f, 0.1f, 0.09f, 1f);

            HudLayout.Portrait(header, new Vector2(24f, -12f), 176f, current.Equipment);

            var name = UiStyle.Text(header, "Wild Arrow", 52, UiStyle.Gold, TextAnchor.MiddleLeft, true);
            UiStyle.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(222f, -24f), new Vector2(480f, 66f));
            var where = UiStyle.Text(header, subtitle, 32, UiStyle.TextDim, TextAnchor.MiddleLeft, true);
            UiStyle.Place(where.rectTransform, new Vector2(0f, 1f), new Vector2(224f, -92f), new Vector2(560f, 44f));

            // A thin life bar under the name, on the HUD bar's dark grey.
            var bar = UiStyle.Framed(header, "Life", UiStyle.FrameDark, 2f);
            UiStyle.Place(bar.rectTransform, new Vector2(0f, 1f), new Vector2(222f, -148f), new Vector2(430f, 20f));
            UiStyle.FillOf(bar).color = HudLayout.LifeBarBack;
            var fill = UiStyle.Image(UiStyle.FillOf(bar).transform, "Amount", UiStyle.BloodBright);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(lifeFraction), 1f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;

            // Her coins: the coin and the count in a framed box.
            var gold = UiStyle.Framed(header, "Gold", UiStyle.Panel, 3f);
            UiStyle.Place(gold.rectTransform, new Vector2(1f, 1f), new Vector2(-190f, -40f), new Vector2(250f, 96f));
            var coin = UiStyle.Image(gold.transform, "Coin", Color.white);
            coin.sprite = HudIcons.For(HudIcons.Kind.Coin);
            coin.preserveAspect = true;
            UiStyle.Place(coin.rectTransform, new Vector2(0f, 0.5f), new Vector2(34f, 0f), new Vector2(44f, 40f));
            coin.rectTransform.pivot = new Vector2(0f, 0.5f);
            var count = UiStyle.Text(gold.transform, Coins(current.Gold), 36, UiStyle.Gold, TextAnchor.MiddleCenter, true);
            count.rectTransform.anchorMin = Vector2.zero;
            count.rectTransform.anchorMax = Vector2.one;
            count.rectTransform.offsetMin = new Vector2(70f, 0f);
            count.rectTransform.offsetMax = new Vector2(-14f, 0f);

            var closeButton = UiStyle.Button(header, "Close", close, 34);
            UiStyle.Place((RectTransform)closeButton.transform, new Vector2(1f, 1f), new Vector2(-24f, -32f), new Vector2(144f, 112f));
            return header;
        }

        /// <summary>
        /// Makes <paramref name="root"/> (a full-screen rect) a menu page: opaque leather, the safe area, a layer for the
        /// header and title (filled by <see cref="Redraw"/>) and the body under them, returned, for the page's own rows.
        /// </summary>
        public static RectTransform Page(GameObject root, out RectTransform chrome)
        {
            var background = root.GetComponent<Image>();
            if (background == null)
                background = root.AddComponent<Image>();
            background.color = UiStyle.Backdrop;
            UiStyle.Leather(background);
            var opaque = background.color;
            opaque.a = 1f;
            background.color = opaque;
            background.raycastTarget = true;
            var safe = UiStyle.Rect(root.transform, "Safe Area");
            SafeArea.Fit(safe);
            chrome = UiStyle.Rect(safe, "Chrome");
            UiStyle.Stretch(chrome);
            var body = UiStyle.Rect(safe, "Body");
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(16f, 8f);
            body.offsetMax = new Vector2(-16f, -(HeaderHeight + TitleHeight));
            return body;
        }

        /// <summary>Builds the page's header and title afresh (her life and coins may have changed).</summary>
        public static void Redraw(RectTransform chrome, string title, Action close)
        {
            for (var i = chrome.childCount - 1; i >= 0; i--)
            {
                var child = chrome.GetChild(i);
                child.SetParent(null);
                UnityEngine.Object.Destroy(child.gameObject);
            }
            var health = UnityEngine.Object.FindAnyObjectByType<PlayerHealth>();
            Header(chrome, GameSession.Current, health != null ? health.Fraction : 1f, Where(), close);
            Title(chrome, title, HeaderHeight);
        }

        /// <summary>The coins as the concept writes them: thousands apart by a space ("1 890").</summary>
        public static string Coins(int gold) =>
            gold.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', ' ');

        /// <summary>Where she is, for the header: the depth, or the town.</summary>
        public static string Where()
        {
            var dungeon = UnityEngine.Object.FindAnyObjectByType<DungeonLevel>();
            return dungeon != null ? $"Depth {dungeon.Depth}" : "In town";
        }

        /// <summary>The page's title, centred, with a rule and a diamond either side, in a band
        /// <see cref="TitleHeight"/> tall whose top is <paramref name="top"/> below the parent's.</summary>
        public static RectTransform Title(Transform parent, string text, float top)
        {
            var band = UiStyle.Rect(parent, "Title");
            band.anchorMin = new Vector2(0f, 1f);
            band.anchorMax = new Vector2(1f, 1f);
            band.pivot = new Vector2(0.5f, 1f);
            band.offsetMin = new Vector2(0f, -top - TitleHeight);
            band.offsetMax = new Vector2(0f, -top);
            var label = UiStyle.Text(band, text, 64, UiStyle.Gold, TextAnchor.MiddleCenter, true);
            UiStyle.Stretch(label.rectTransform);
            var shadow = label.gameObject.AddComponent<Outline>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(2f, -2f);
            // The ornaments either side of the word, at a distance from it by its width.
            var half = label.preferredWidth / 2f + 34f;
            for (var side = -1; side <= 1; side += 2)
            {
                var diamond = Diamond(band, 18f, UiStyle.Gold);
                diamond.rectTransform.anchoredPosition = new Vector2(side * half, 0f);
                var rule = UiStyle.Image(band, "Rule", new Color(UiStyle.Gold.r, UiStyle.Gold.g, UiStyle.Gold.b, 0.55f));
                var rect = rule.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(side < 0 ? 1f : 0f, 0.5f);
                rect.anchoredPosition = new Vector2(side * (half + 18f), 0f);
                rect.sizeDelta = new Vector2(130f, 3f);
            }
            // The line under the header the concept draws, at the band's top.
            var line = UiStyle.Image(band, "Edge", new Color(UiStyle.Frame.r, UiStyle.Frame.g, UiStyle.Frame.b, 0.5f));
            line.rectTransform.anchorMin = new Vector2(0f, 1f);
            line.rectTransform.anchorMax = new Vector2(1f, 1f);
            line.rectTransform.pivot = new Vector2(0.5f, 1f);
            line.rectTransform.offsetMin = new Vector2(16f, -3f);
            line.rectTransform.offsetMax = new Vector2(-16f, 0f);
            return band;
        }

        /// <summary>A small diamond: a square turned 45 degrees, centred on its anchor.</summary>
        public static Image Diamond(Transform parent, float size, Color color)
        {
            var diamond = UiStyle.Image(parent, "Diamond", color);
            var rect = diamond.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            return diamond;
        }

        /// <summary>A menu icon in the concept's tan, <paramref name="size"/> across, centred on its anchor.</summary>
        public static Image Icon(Transform parent, MenuIcons.Kind kind, float size, Color? color = null)
        {
            var icon = UiStyle.Image(parent, kind + " Icon", color ?? MenuIcons.Tint);
            icon.sprite = MenuIcons.For(kind);
            icon.preserveAspect = true;
            var rect = icon.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            return icon;
        }
    }
}
