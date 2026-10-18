using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The HUD's arrangement (the owner, 2026-09-28): the player's round portrait in the top-left corner, the life bar,
    /// experience bar, potion pips and the depth (2026-10-13, in place of the level on the portrait) to its right; the
    /// mini-map sits top right (<see cref="Minimap"/>), and under it (2026-10-18) the Bag and Settings icon buttons and
    /// her coins (<see cref="CoinCounter"/>). Arranges the scene's HUD objects at runtime inside the safe area,
    /// so the scenes need no rebuild, and keeps the portrait in the character's current gear.
    /// </summary>
    public class HudLayout : MonoBehaviour
    {
        public const float PortraitSize = 176f;
        const float Margin = 24f, Top = 12f, BarsLeft = Margin + PortraitSize + 20f, BarWidth = 400f;

        /// <summary>The icon buttons under the mini-map (its box is 220 tall at 12 from the top): square, a gap apart,
        /// against the right edge.</summary>
        public const float IconButtonSize = 132f;
        const float IconGap = 16f, RightMargin = 16f, ButtonsTop = 12f + 220f + 24f;

        CharacterPortrait portrait;
        GameSession session;

        /// <summary>Moves the HUD canvas's elements into place and adds the portrait. Safe to call once per scene.</summary>
        public static void Arrange(Transform hud)
        {
            if (hud == null || hud.GetComponent<HudLayout>() != null)
                return;
            var layout = hud.gameObject.AddComponent<HudLayout>();
            SafeArea.WrapChildren((RectTransform)hud);
            var area = hud.GetChild(hud.childCount - 1);

            layout.portrait = Portrait(area, new Vector2(Margin, -Top), PortraitSize, GameSession.Current.Equipment);
            layout.portrait.Root.SetAsFirstSibling();

            // A copy: the level badge is inserted among the children, which would make a live walk visit the level
            // text again and again (it hung the game at start).
            var children = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in area)
                children.Add(child);
            foreach (var child in children)
            {
                var rect = (RectTransform)child;
                if (child.GetComponent<LifeBar>() != null)
                    UiStyle.Place(rect, new Vector2(0f, 1f), new Vector2(BarsLeft, -Top - 30f), new Vector2(BarWidth, 36f));
                else if (child.GetComponent<ExperienceBar>() != null)
                    UiStyle.Place(rect, new Vector2(0f, 1f), new Vector2(BarsLeft, -Top - 74f), new Vector2(BarWidth, 14f));
                else if (child.GetComponent<PotionPips>() != null)
                    UiStyle.Place(rect, new Vector2(0f, 1f), new Vector2(BarsLeft, -Top - 100f), rect.sizeDelta);
                else if (child.name == "Bag Button")
                    StyleBag(rect, area);
                else if (child.name == "Level")
                    // The roguelite has no character levels to show (the owner, 2026-10-13: "remove the level
                    // indicator in the profile picture"); the depth is shown instead.
                    child.gameObject.SetActive(false);
            }
            DepthLabel.Create(area, new Vector2(BarsLeft, -Top - 138f));
            // The boons taken, as icons under the depth (2026-10-15).
            if (Features.Roguelite)
                BoonBar.Create(area, new Vector2(BarsLeft, -Top - 192f));

            layout.session = GameSession.Current;
            layout.session.Changed += layout.RefreshPortrait;
        }

        void OnDestroy()
        {
            if (session != null)
                session.Changed -= RefreshPortrait;
        }

        void RefreshPortrait() => portrait?.Refresh(session.Equipment);

        /// <summary>A round portrait: a blood-red ring and, masked inside it, the character's head and shoulders.</summary>
        public static CharacterPortrait Portrait(Transform parent, Vector2 topLeft, float size, EquipmentState equipment)
        {
            var ring = UiStyle.Disc(parent, "Portrait", UiStyle.Blood);
            UiStyle.Place(ring.rectTransform, new Vector2(0f, 1f), topLeft, new Vector2(size, size));
            var edge = UiStyle.Disc(ring.transform, "Edge", UiStyle.FrameDark);
            UiStyle.Place(edge.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size - 10f, size - 10f));
            var mask = UiStyle.Disc(edge.transform, "Face", new Color(0.16f, 0.07f, 0.06f, 1f), true);
            UiStyle.Place(mask.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size - 18f, size - 18f));
            // The baked cell is 128 px with the feet 20 px up and the head near the top: drawn at three times the
            // circle and lowered, the head and shoulders fill it (placed by eye on the simulator).
            var figure = UiStyle.Place(UiStyle.Rect(mask.transform, "Character"), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -size * 0.6f), new Vector2(size * 3f, size * 3f));
            var portrait = new CharacterPortrait(figure) { Root = ring.transform };
            portrait.Refresh(equipment);
            return portrait;
        }

        // The Bag button under the mini-map as a framed bag icon (the owner, 2026-10-18: "the bag button looks wierd.
        // Make an icon with a bag, an icon for settings and place them under the map"), Settings beside it on the
        // right, and her coins under them.
        static void StyleBag(RectTransform rect, Transform area)
        {
            var label = rect.GetComponentInChildren<Text>();
            if (label != null)
                label.gameObject.SetActive(false);
            IconButton(rect, HudIcons.Kind.Bag, 1);

            var settings = UiStyle.Rect(area, "Settings Button");
            settings.gameObject.AddComponent<Image>();
            settings.gameObject.AddComponent<Button>().onClick.AddListener(SettingsScreen.OpenPaused);
            IconButton(settings, HudIcons.Kind.Settings, 0);

            // Left of the two buttons, on their row: under them the development builds' frame readout would cover it.
            CoinCounter.Create(area, new Vector2(-RightMargin - 2f * (IconButtonSize + IconGap),
                -ButtonsTop - (IconButtonSize - CoinCounter.Height) / 2f));
        }

        // Places a button at its place from the right (0 the rightmost) and dresses it: an iron frame, the icon inside.
        static void IconButton(RectTransform rect, HudIcons.Kind kind, int fromRight)
        {
            UiStyle.Place(rect, new Vector2(1f, 1f), new Vector2(-RightMargin - fromRight * (IconButtonSize + IconGap), -ButtonsTop),
                new Vector2(IconButtonSize, IconButtonSize));
            var image = rect.GetComponent<Image>();
            if (image != null)
            {
                // The tap target only; the frame below draws the button.
                image.sprite = null;
                image.color = new Color(0f, 0f, 0f, 0f);
                image.raycastTarget = true;
            }
            var outline = rect.GetComponent<Outline>();
            if (outline != null)
                Object.Destroy(outline);
            var frame = UiStyle.Framed(rect, "Frame", UiStyle.Panel, 3f);
            UiStyle.Stretch(frame.rectTransform);
            frame.transform.SetAsFirstSibling();
            var icon = UiStyle.Image(rect, "Icon", Color.white);
            icon.sprite = HudIcons.For(kind);
            icon.preserveAspect = true;
            UiStyle.Stretch(icon.rectTransform, IconButtonSize * 0.16f);
        }
    }

    /// <summary>
    /// Her coins on the HUD (the owner, 2026-10-18: "Display the amount of coins somewhere on the hud. No need to open the
    /// inventory to see money"): a coin icon and the count, right-aligned left of the icon buttons. The text is rebuilt
    /// only when the count changes.
    /// </summary>
    public class CoinCounter : MonoBehaviour
    {
        public const float Height = 56f;

        Text text;
        int shown = -1;

        public static CoinCounter Create(Transform parent, Vector2 topRight)
        {
            var rect = UiStyle.Place(UiStyle.Rect(parent, "Coins"), new Vector2(1f, 1f), topRight, new Vector2(320f, Height));
            var counter = rect.gameObject.AddComponent<CoinCounter>();
            var icon = UiStyle.Image(rect, "Coin", Color.white);
            icon.sprite = HudIcons.For(HudIcons.Kind.Coin);
            icon.preserveAspect = true;
            UiStyle.Place(icon.rectTransform, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(54f, 48f));
            icon.rectTransform.pivot = new Vector2(1f, 0.5f);
            counter.text = UiStyle.Text(rect, "", 38, UiStyle.Gold, TextAnchor.MiddleRight);
            counter.text.font = UiStyle.Title;
            counter.text.raycastTarget = false;
            var textRect = counter.text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = new Vector2(-64f, 0f);
            var shadow = counter.text.gameObject.AddComponent<Outline>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return counter;
        }

        void LateUpdate()
        {
            var session = GameSession.Current;
            var gold = session != null ? session.Gold : 0;
            if (gold == shown)
                return;
            shown = gold;
            text.text = gold.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Which dungeon level she is on, under the potion pips (the owner, 2026-10-13: "Add a level indicator for which
    /// dungeon level the player is at"): "Depth 7", or "Town". Set once, since every depth is its own scene load.
    /// </summary>
    public class DepthLabel : MonoBehaviour
    {
        Text text;

        public static DepthLabel Create(Transform parent, Vector2 topLeft)
        {
            var rect = UiStyle.Place(UiStyle.Rect(parent, "Depth"), new Vector2(0f, 1f), topLeft, new Vector2(400f, 48f));
            var label = rect.gameObject.AddComponent<DepthLabel>();
            label.text = rect.gameObject.AddComponent<Text>();
            label.text.font = UiStyle.Title;
            label.text.fontSize = 34;
            label.text.color = UiStyle.Gold;
            label.text.alignment = TextAnchor.MiddleLeft;
            label.text.raycastTarget = false;
            var shadow = rect.gameObject.AddComponent<Outline>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return label;
        }

        void Start()
        {
            var level = FindAnyObjectByType<DungeonLevel>();
            text.text = level != null ? "Depth " + level.Depth : "Town";
        }
    }
}
