using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The HUD's arrangement (the owner, 2026-09-28): the player's round portrait in the top-left corner with the level
    /// on it, the life bar, experience bar and potion pips to its right, and the Bag button under the portrait; the
    /// mini-map sits top right (<see cref="Minimap"/>). Arranges the scene's HUD objects at runtime inside the safe area,
    /// so the scenes need no rebuild, and keeps the portrait in the character's current gear.
    /// </summary>
    public class HudLayout : MonoBehaviour
    {
        public const float PortraitSize = 176f;
        const float Margin = 24f, Top = 12f, BarsLeft = Margin + PortraitSize + 20f, BarWidth = 400f;

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
                    StyleBag(rect);
                else if (child.name == "Level")
                    StyleLevel(area, rect);
            }

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

        // The Bag button under the portrait, in the dark iron style.
        static void StyleBag(RectTransform rect)
        {
            UiStyle.Place(rect, new Vector2(0f, 1f), new Vector2(Margin, -Top - PortraitSize - 16f), new Vector2(PortraitSize, 100f));
            var image = rect.GetComponent<Image>();
            if (image != null)
                image.color = UiStyle.Panel;
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = UiStyle.Frame;
            outline.effectDistance = new Vector2(3f, -3f);
            var label = rect.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.font = UiStyle.Title;
                label.color = UiStyle.Gold;
                label.fontSize = 36;
            }
        }

        // The level number on a small dark disc at the bottom of the portrait.
        static void StyleLevel(Transform area, RectTransform rect)
        {
            var badge = UiStyle.Disc(area, "Level Badge", UiStyle.FrameDark);
            UiStyle.Place(badge.rectTransform, new Vector2(0f, 1f), new Vector2(Margin + PortraitSize / 2f - 32f, -Top - PortraitSize + 44f), new Vector2(64f, 64f));
            badge.transform.SetSiblingIndex(rect.GetSiblingIndex());
            UiStyle.Place(rect, new Vector2(0f, 1f), new Vector2(Margin + PortraitSize / 2f - 40f, -Top - PortraitSize + 44f), new Vector2(80f, 64f));
            var text = rect.GetComponent<Text>();
            if (text != null)
            {
                text.alignment = TextAnchor.MiddleCenter;
                text.font = UiStyle.Title;
                text.fontSize = 34;
                text.color = UiStyle.Gold;
            }
        }
    }
}
