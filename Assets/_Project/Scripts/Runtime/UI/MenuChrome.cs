using System;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// What every full-screen menu shares: one short header. Since 2026-11-11 (the owner: "They feel very poorly put
    /// together ... Reimagine it all") it is the page's own name in gold, under it the archer's name and where she is,
    /// her coins in a framed box and Close; her portrait, the fixed "Wild Arrow", the life bar and the separate title
    /// band between ornaments (the 2026-10-19 concept) are gone, so a page starts 172 units higher. Used by the Bag
    /// (<see cref="InventoryScreen"/>), <see cref="SettingsScreen"/>, the Codex and the town's pages.
    /// </summary>
    public static class MenuChrome
    {
        /// <summary>The header's height. No title band follows it (<see cref="TitleHeight"/> stays for the layouts
        /// that add the two).</summary>
        public const float HeaderHeight = 136f, TitleHeight = 0f;

        /// <summary>The header across the top of <paramref name="parent"/>, on an opaque band.</summary>
        public static RectTransform Header(Transform parent, GameSession current, string title, string subtitle, Action close)
        {
            var header = UiStyle.Place(UiStyle.Rect(parent, "Header"), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, HeaderHeight));
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            // An opaque band, so the HUD under the menu does not show through (2026-10-17).
            var band = UiStyle.Image(header, "Band", UiStyle.Sheet);
            UiStyle.Stretch(band.rectTransform);
            UiStyle.Leather(band);
            band.color = new Color(0.12f, 0.1f, 0.09f, 1f);

            var name = UiStyle.Text(header, title, 56, UiStyle.Gold, TextAnchor.MiddleLeft, true);
            UiStyle.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(26f, -12f), new Vector2(560f, 66f));
            var where = UiStyle.Text(header, subtitle, 30, UiStyle.TextDim, TextAnchor.MiddleLeft, true);
            UiStyle.Place(where.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -80f), new Vector2(700f, 42f));

            // Her coins: the coin and the count in a framed box.
            var gold = UiStyle.Framed(header, "Gold", UiStyle.Panel, 3f);
            UiStyle.Place(gold.rectTransform, new Vector2(1f, 1f), new Vector2(-184f, -22f), new Vector2(250f, 92f));
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
            UiStyle.Place((RectTransform)closeButton.transform, new Vector2(1f, 1f), new Vector2(-24f, -22f), new Vector2(144f, 92f));

            // The line under the header.
            var line = UiStyle.Image(header, "Edge", new Color(UiStyle.Frame.r, UiStyle.Frame.g, UiStyle.Frame.b, 0.5f));
            line.rectTransform.anchorMin = Vector2.zero;
            line.rectTransform.anchorMax = new Vector2(1f, 0f);
            line.rectTransform.pivot = new Vector2(0.5f, 0f);
            line.rectTransform.offsetMin = new Vector2(16f, 0f);
            line.rectTransform.offsetMax = new Vector2(-16f, 3f);
            return header;
        }

        /// <summary>Who she is and where, for the header: "Name · the level", or the company.</summary>
        public static string WhoAndWhere(GameSession current)
        {
            var who = current != null && current.Company != null && !string.IsNullOrEmpty(current.Company.Mercenary) ? current.Company.Mercenary : "Wild Arrow";
            return $"{who}   ·   {Where()}";
        }

        /// <summary>
        /// Makes <paramref name="root"/> (a full-screen rect) a menu page: opaque leather, the safe area, a layer for the
        /// header (filled by <see cref="Redraw"/>) and the body under them, returned, for the page's own rows.
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
            Header(chrome, GameSession.Current, title, WhoAndWhere(GameSession.Current), close);
        }

        /// <summary>The coins as the concept writes them: thousands apart by a space ("1 890").</summary>
        public static string Coins(int gold) =>
            gold.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', ' ');

        /// <summary>Where she is, for the header: the depth, or the town.</summary>
        public static string Where()
        {
            var dungeon = UnityEngine.Object.FindAnyObjectByType<DungeonLevel>();
            return dungeon != null ? ContractRules.LevelName(dungeon.Depth) : Features.CompanyMenu ? "At the company" : "In town";
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
