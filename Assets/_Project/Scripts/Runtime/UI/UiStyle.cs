using System;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The look of the game's screens until the UI art exists (Docs/09, section 11), after the owner's reference of
    /// 2026-09-28: near-black panels framed in dark iron, blood-red accents, old-gold serif titles. Colors, the fonts and
    /// the small builders every code-built screen shares.
    /// </summary>
    public static class UiStyle
    {
        public static readonly Color Backdrop = new Color(0.05f, 0.04f, 0.04f, 0.98f);
        public static readonly Color Panel = new Color(0.09f, 0.075f, 0.07f, 1f);
        public static readonly Color SlotFill = new Color(0.17f, 0.055f, 0.05f, 1f);
        public static readonly Color EmptySlotFill = new Color(0.1f, 0.08f, 0.075f, 1f);
        public static readonly Color Frame = new Color(0.32f, 0.27f, 0.24f, 1f);
        public static readonly Color FrameDark = new Color(0.02f, 0.015f, 0.015f, 1f);
        public static readonly Color Blood = new Color(0.55f, 0.06f, 0.05f, 1f);
        public static readonly Color BloodBright = new Color(0.8f, 0.12f, 0.09f, 1f);
        public static readonly Color Gold = new Color(0.86f, 0.72f, 0.48f, 1f);
        public static readonly Color TextMain = new Color(0.9f, 0.86f, 0.8f, 1f);
        public static readonly Color TextDim = new Color(0.62f, 0.57f, 0.52f, 1f);

        static Font body;
        static Font title;
        static Sprite circle;

        /// <summary>The plain font for numbers and labels.</summary>
        public static Font Body => body != null ? body : body = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>A serif from the device for titles and names (Georgia on iOS and the Mac), else the plain font.</summary>
        public static Font Title
        {
            get
            {
                if (title != null)
                    return title;
                try
                {
                    title = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Baskerville", "Times New Roman" }, 40);
                }
                catch (Exception)
                {
                    title = null;
                }
                return title != null ? title : title = Body;
            }
        }

        /// <summary>A white disc for round masks and badges.</summary>
        public static Sprite Circle
        {
            get
            {
                if (circle != null)
                    return circle;
                const int size = 128;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "UI Circle" };
                var pixels = new Color32[size * size];
                var r = size / 2f;
                for (var y = 0; y < size; y++)
                    for (var x = 0; x < size; x++)
                    {
                        var d = Mathf.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r));
                        var a = Mathf.Clamp01(r - d);
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                texture.SetPixels32(pixels);
                texture.Apply();
                return circle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Anchors a rect to a corner (or edge) of its parent, by pivot and anchor (0 to 1 each), at an offset.</summary>
        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        public static Image Image(Transform parent, string name, Color color, bool raycast = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        /// <summary>A framed panel: a dark outer edge, a lighter iron rim and the fill inside it.</summary>
        public static Image Framed(Transform parent, string name, Color fill, float rim = 4f)
        {
            var outer = Image(parent, name, FrameDark);
            var edge = Image(outer.transform, "Rim", Frame);
            Stretch(edge.rectTransform, 2f);
            var inner = Image(edge.transform, "Fill", fill);
            Stretch(inner.rectTransform, rim);
            return outer;
        }

        /// <summary>The fill image of a <see cref="Framed"/> panel, where its content goes.</summary>
        public static Image FillOf(Image framed) => framed.transform.GetChild(0).GetChild(0).GetComponent<Image>();

        public static Text Text(Transform parent, string content, int size, Color color, TextAnchor alignment = TextAnchor.MiddleLeft,
            bool serif = false, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = serif ? Title : Body;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = content;
            return text;
        }

        /// <summary>A framed button with a centered label.</summary>
        public static Button Button(Transform parent, string label, Action onClick, int size = 32, Color? fill = null)
        {
            var frame = Framed(parent, label + " Button", fill ?? Panel, 3f);
            frame.raycastTarget = true;
            var button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = FillOf(frame);
            button.onClick.AddListener(() => onClick());
            var text = Text(frame.transform, label, size, TextMain, TextAnchor.MiddleCenter, true);
            Stretch(text.rectTransform, 8f);
            return button;
        }

        /// <summary>A circle of a color, optionally masking its children to the circle.</summary>
        public static Image Disc(Transform parent, string name, Color color, bool mask = false)
        {
            var image = Image(parent, name, color);
            image.sprite = Circle;
            if (mask)
                image.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            return image;
        }

        /// <summary>A short name for an item kind, standing in for its icon until the icons exist (Docs/09).</summary>
        public static string SlotLabel(ItemSlot slot) => slot switch
        {
            ItemSlot.Weapon => "Axe",
            ItemSlot.Ring2 => "Ring",
            _ => slot.ToString(),
        };
    }
}
