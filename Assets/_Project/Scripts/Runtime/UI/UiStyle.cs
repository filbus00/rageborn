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
        // Brighter than the flat rim it replaced: the frame's art (UiArt) shades it down, highlights at full.
        public static readonly Color Frame = new Color(0.62f, 0.52f, 0.4f, 1f);
        public static readonly Color FrameDark = new Color(0.02f, 0.015f, 0.015f, 1f);
        public static readonly Color Blood = new Color(0.55f, 0.06f, 0.05f, 1f);
        public static readonly Color BloodBright = new Color(0.8f, 0.12f, 0.09f, 1f);
        public static readonly Color Gold = new Color(0.86f, 0.72f, 0.48f, 1f);
        public static readonly Color TextMain = new Color(0.9f, 0.86f, 0.8f, 1f);
        public static readonly Color TextDim = new Color(0.62f, 0.57f, 0.52f, 1f);

        // The sheets' shared tokens (item sheet, skills, tree, settings, Forge, waypoints, hints).
        public static readonly Color Sheet = new Color(0.06f, 0.05f, 0.05f, 1f);
        public static readonly Color Dim = new Color(0f, 0f, 0f, 0.6f);
        public static readonly Color ButtonFill = new Color(0.13f, 0.1f, 0.095f, 1f);
        public static readonly Color RowFill = new Color(0.1f, 0.08f, 0.075f, 1f);
        public static readonly Color Selected = new Color(0.45f, 0.06f, 0.05f, 1f);
        public static readonly Color Chip = new Color(0.3f, 0.06f, 0.05f, 1f);
        public static readonly Color Locked = new Color(0.07f, 0.06f, 0.06f, 1f);

        /// <summary>The iron rim every button and row of the sheets carries.</summary>
        public static void Rim(GameObject go, float width = 3f)
        {
            if (go.GetComponent<Outline>() != null)
                return;
            // A raised plate under the row's or button's own colour (UiTextures, 2026-10-04).
            var image = go.GetComponent<Image>();
            if (image != null && image.sprite == null && UiTextures.ButtonPlate != null)
            {
                image.sprite = UiTextures.ButtonPlate;
                image.type = UnityEngine.UI.Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1.5f;
            }
            var outline = go.AddComponent<Outline>();
            outline.effectColor = Frame;
            outline.effectDistance = new Vector2(width, -width);
        }

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
            // The screens' and sheets' backgrounds get the leather (UiTextures, 2026-10-04).
            if (color == Sheet || color == Backdrop)
                Leather(image);
            return image;
        }

        /// <summary>A recessed square (the backpack's cells, empty slots): the inset texture under the image's colour.</summary>
        public static void Recessed(Image image)
        {
            if (image == null || UiTextures.Inset == null)
                return;
            image.sprite = UiTextures.Inset;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1.6f;
        }

        /// <summary>Tiles the dark tooled leather (<see cref="UiTextures.Backdrop"/>) under an image's colour.</summary>
        public static void Leather(Image image)
        {
            var leather = UiTextures.Backdrop;
            if (image == null || leather == null)
                return;
            image.sprite = leather;
            image.type = UnityEngine.UI.Image.Type.Tiled;
            image.pixelsPerUnitMultiplier = 0.5f;
            // Light enough for the leather's grain to show under the dark tint.
            image.color = new Color(0.2f, 0.16f, 0.14f, image.color.a);
        }

        /// <summary>
        /// A framed panel: a dark outer edge, a pixel-art iron rim (bevelled, riveted at the corners; tinted by its
        /// colour, which screens change for rarity and drop highlights) and a faintly textured fill inside it. The art is
        /// drawn in code (<see cref="UiArt"/>), each art pixel <paramref name="rim"/> times 0.75 UI units.
        /// </summary>
        public static Image Framed(Transform parent, string name, Color fill, float rim = 4f)
        {
            var outer = Image(parent, name, FrameDark);
            var edge = Image(outer.transform, "Rim", Frame);
            Stretch(edge.rectTransform, 2f);
            Image inner;
            if (UiTextures.Frame != null && UiTextures.Inset != null)
            {
                // The rendered metal rim (ArtSource/tools/ui/make_ui.py), rim * 4 units wide, around a recessed fill.
                outer.color = Color.clear;
                Stretch(edge.rectTransform, 0f);
                var width = rim * 3.4f;
                edge.sprite = UiTextures.Frame;
                edge.type = UnityEngine.UI.Image.Type.Sliced;
                edge.pixelsPerUnitMultiplier = UiTextures.FrameBorder / width;
                inner = Image(edge.transform, "Fill", fill);
                Stretch(inner.rectTransform, width - 1f);
                inner.sprite = UiTextures.Inset;
                inner.type = UnityEngine.UI.Image.Type.Sliced;
                inner.pixelsPerUnitMultiplier = UiTextures.InsetBorder / (width * 1.2f);
                return outer;
            }
            var scale = Mathf.Max(2f, rim * 0.75f);
            edge.sprite = UiArt.Frame;
            edge.type = UnityEngine.UI.Image.Type.Sliced;
            edge.pixelsPerUnitMultiplier = 1f / scale;
            inner = Image(edge.transform, "Fill", fill);
            Stretch(inner.rectTransform, UiArt.FrameBorder * scale);
            inner.sprite = UiArt.Fill;
            inner.type = UnityEngine.UI.Image.Type.Tiled;
            inner.pixelsPerUnitMultiplier = 1f / scale;
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
            // Titles and names stand off the leather with a soft drop shadow.
            if (serif)
            {
                var shadow = go.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
                shadow.effectDistance = new Vector2(2f, -2f);
            }
            return text;
        }

        /// <summary>A framed button with a centered label.</summary>
        public static Button Button(Transform parent, string label, Action onClick, int size = 32, Color? fill = null)
        {
            var frame = Framed(parent, label + " Button", fill ?? Panel, 3f);
            frame.raycastTarget = true;
            // A raised plate rather than the recessed fill of a panel.
            var plate = FillOf(frame);
            if (UiTextures.ButtonPlate != null)
            {
                plate.sprite = UiTextures.ButtonPlate;
                plate.type = UnityEngine.UI.Image.Type.Sliced;
                plate.color = Color.Lerp(fill ?? Panel, Color.white, 0.12f);
            }
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
            ItemSlot.Weapon => "Bow",
            ItemSlot.TwoHandWeapon => "Longbow",
            ItemSlot.Shield => "Quiver",
            ItemSlot.Ring2 => "Ring",
            ItemSlot.OffHand => "Off-hand",
            _ => slot.ToString(),
        };
    }

    /// <summary>
    /// The UI's pixel art, drawn in code (Docs/09, section 11, until hand-made art exists; 2026-10-04): a 9-sliced iron
    /// frame, bevelled light at the top left and dark at the bottom right, a rivet in each corner and a dark line inside,
    /// in grey so a rim's colour tints it; and a tiling fill of faint blocky mottling. Point filtered.
    /// </summary>
    public static class UiArt
    {
        /// <summary>The frame's border, in art pixels.</summary>
        public const int FrameBorder = 4;

        const int FrameSize = 12;
        const int FillSize = 16;

        static Sprite frame;
        static Sprite fill;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            frame = null;
            fill = null;
        }

        public static Sprite Frame => frame != null ? frame : frame = MakeFrame();

        public static Sprite Fill => fill != null ? fill : fill = MakeFill();

        /// <summary>The frame's grey value at an art pixel, or a negative number for clear (the middle). Pure.</summary>
        public static float FrameValue(int x, int y)
        {
            const int n = FrameSize;
            var edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(n - 1 - x, n - 1 - y));
            if (edge >= FrameBorder)
                return -1f;
            // Rivets two pixels in from each corner.
            var cx = x < n / 2 ? 1 : n - 2;
            var cy = y < n / 2 ? 1 : n - 2;
            if (x == cx && y == cy)
                return 1f;
            if (Mathf.Abs(x - cx) + Mathf.Abs(y - cy) == 1 && (x < cx || y > cy))
                return 0.55f;
            switch (edge)
            {
                case 0:
                    // The bevel: light on the top and left (y up), dark on the bottom and right.
                    return x == 0 || y == n - 1 ? 0.95f : 0.5f;
                case 3:
                    return 0.3f;
                default:
                    return (x + y) % 5 == 0 ? 0.7f : 0.78f;
            }
        }

        static Sprite MakeFrame()
        {
            const int n = FrameSize;
            var pixels = new Color32[n * n];
            for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++)
                {
                    var v = FrameValue(x, y);
                    var b = (byte)Mathf.RoundToInt(Mathf.Clamp01(v) * 255f);
                    pixels[y * n + x] = v < 0f ? new Color32(0, 0, 0, 0) : new Color32(b, b, b, 255);
                }
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "UI Frame", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(FrameBorder, FrameBorder, FrameBorder, FrameBorder));
        }

        static Sprite MakeFill()
        {
            const int n = FillSize;
            var pixels = new Color32[n * n];
            var random = new System.Random(17);
            // Two-pixel blocks, so the mottling reads as pixel art at the frame's scale.
            var blocks = new float[n / 2, n / 2];
            for (var by = 0; by < n / 2; by++)
                for (var bx = 0; bx < n / 2; bx++)
                    blocks[bx, by] = random.NextDouble() < 0.18 ? 0.9f : random.NextDouble() < 0.1 ? 1.08f : 1f;
            for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++)
                {
                    var b = (byte)Mathf.RoundToInt(Mathf.Clamp01(blocks[x / 2, y / 2] * 0.92f) * 255f);
                    pixels[y * n + x] = new Color32(b, b, b, 255);
                }
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "UI Fill", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }
    }

    /// <summary>
    /// The menus' rendered textures (the owner, 2026-10-04: "The menus look bad, revamp them"; made by
    /// ArtSource/tools/ui/make_ui.py into Resources/UI): a 9-sliced metal rim, a recessed fill, a raised button plate and
    /// tiling leather, all in grey for the screens to tint. Null when missing, and the code-drawn <see cref="UiArt"/>
    /// stands in.
    /// </summary>
    public static class UiTextures
    {
        public const float FrameBorder = 22f;
        public const float InsetBorder = 24f;
        const float ButtonBorder = 20f;

        static Sprite frame, inset, button, backdrop, glow;
        static bool loaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => loaded = false;

        public static Sprite Frame { get { Load(); return frame; } }
        public static Sprite Inset { get { Load(); return inset; } }
        public static Sprite ButtonPlate { get { Load(); return button; } }
        public static Sprite Backdrop { get { Load(); return backdrop; } }
        public static Sprite Glow { get { Load(); return glow; } }

        static void Load()
        {
            if (loaded)
                return;
            loaded = true;
            frame = Make("frame", FrameBorder);
            inset = Make("inset", InsetBorder);
            button = Make("button", ButtonBorder);
            backdrop = Make("backdrop", 0f);
            glow = Make("glow", 0f);
        }

        static Sprite Make(string name, float border)
        {
            var texture = Resources.Load<Texture2D>("UI/" + name);
            if (texture == null)
                return null;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }
    }
}
