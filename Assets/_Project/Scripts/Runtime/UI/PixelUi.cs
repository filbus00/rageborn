using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The pixel-art menus (2026-11-05, the owner: "remake the menues" after two concept pictures,
    /// ArtSource/ui/concepts): a canvas measured in menu pixels, shown at a whole number of screen pixels each
    /// (<see cref="Scale"/>: 2 on the iPhone 11, 3 on the wider phones), the drawn frames, icons and portraits of
    /// Resources/UI/Pixel (ArtSource/tools/ui/menu_art.lua, PixelUiImport) and the two pixel fonts (Title, Body;
    /// pixel_fonts.lua). Dark green and old gold, after the concepts.
    /// </summary>
    public static class PixelUi
    {
        public static readonly Color Gold = new Color32(222, 178, 88, 255);
        public static readonly Color GoldDim = new Color32(168, 128, 52, 255);
        public static readonly Color Text = new Color32(196, 196, 184, 255);
        public static readonly Color TextDim = new Color32(132, 136, 124, 255);
        public static readonly Color Red = new Color32(204, 52, 44, 255);
        public static readonly Color Shade = new Color32(6, 8, 6, 255);

        /// <summary>The narrowest the menu is drawn, in menu pixels: the concept's width.</summary>
        public const float DesignWidth = 384f;

        static Font title, titleBig, body, title3, title6, body3;
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            title = titleBig = body = title3 = title6 = body3 = null;
            sprites.Clear();
            circles.Clear();
        }

        public static Font TitleFont => title != null ? title : title = Resources.Load<Font>("UI/Pixel/Fonts/title");
        public static Font TitleBigFont => titleBig != null ? titleBig : titleBig = Resources.Load<Font>("UI/Pixel/Fonts/title_x2");
        public static Font BodyFont => body != null ? body : body = Resources.Load<Font>("UI/Pixel/Fonts/body");

        // The same at three and six canvas units a pixel, for the menus laid out 1170 units across (PixelMenu).
        public static Font TitleFont3 => title3 != null ? title3 : title3 = Resources.Load<Font>("UI/Pixel/Fonts/title_x3");
        public static Font TitleFont6 => title6 != null ? title6 : title6 = Resources.Load<Font>("UI/Pixel/Fonts/title_x6");
        public static Font BodyFont3 => body3 != null ? body3 : body3 = Resources.Load<Font>("UI/Pixel/Fonts/body_x3");

        /// <summary>Whether the drawn menu art is in the build (else the menus fall back to their old look).</summary>
        public static bool Available => TitleFont != null && BodyFont != null && Sprite("frame_panel_b4") != null;

        /// <summary>Screen pixels a menu pixel: the most whole pixels that still fit the concept's width.</summary>
        public static int Scale(float screenWidth) => Mathf.Max(1, Mathf.FloorToInt(screenWidth / DesignWidth));

        static readonly Dictionary<int, Sprite> circles = new Dictionary<int, Sprite>();

        /// <summary>A white disc <paramref name="diameter"/> pixels across with hard, stepped edges (the HUD's round
        /// portrait, 2026-11-08), point filtered, made once a size.</summary>
        public static Sprite Circle(int diameter)
        {
            diameter = Mathf.Max(2, diameter);
            if (circles.TryGetValue(diameter, out var circle) && circle != null)
                return circle;
            var pixels = new Color32[diameter * diameter];
            var r = diameter / 2f;
            for (var y = 0; y < diameter; y++)
                for (var x = 0; x < diameter; x++)
                {
                    float dx = x + 0.5f - r, dy = y + 0.5f - r;
                    pixels[x + y * diameter] = dx * dx + dy * dy <= r * r ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            var texture = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false)
            {
                name = "Pixel Circle " + diameter, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return circles[diameter] = UnityEngine.Sprite.Create(texture, new Rect(0, 0, diameter, diameter), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, Vector4.zero, false);
        }

        public static Sprite Sprite(string name)
        {
            if (!sprites.TryGetValue(name, out var sprite))
                sprites[name] = sprite = Resources.Load<Sprite>("UI/Pixel/" + name);
            return sprite;
        }

        /// <summary>A canvas measured in menu pixels, pixel perfect.</summary>
        public static Canvas MakeCanvas(Transform parent, string name, int order)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            canvas.pixelPerfect = true;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = Scale(Screen.width);
            return canvas;
        }

        /// <summary>A drawn frame stretched over a rect by slicing (its corners and edges kept at their pixels).</summary>
        public static Image Frame(Transform parent, string name, string frame, bool raycast = false)
        {
            var image = UiStyle.Image(parent, name, Color.white, raycast);
            image.sprite = Sprite(frame);
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            return image;
        }

        /// <summary>A drawn picture at its own size.</summary>
        public static Image Picture(Transform parent, string name, string sprite)
        {
            var image = UiStyle.Image(parent, name, Color.white);
            image.sprite = Sprite(sprite);
            if (image.sprite != null)
                image.rectTransform.sizeDelta = image.sprite.rect.size;
            return image;
        }

        /// <summary>A picture tiled over a rect (the backdrop).</summary>
        public static Image Tiled(Transform parent, string name, string sprite)
        {
            var image = UiStyle.Image(parent, name, Color.white);
            image.sprite = Sprite(sprite);
            image.type = UnityEngine.UI.Image.Type.Tiled;
            image.pixelsPerUnitMultiplier = 1f;
            return image;
        }

        /// <summary>Text in a pixel font, with a one-pixel shadow down and right.</summary>
        public static Text Label(Transform parent, string content, Font font, Color color, TextAnchor alignment = TextAnchor.UpperLeft,
            bool shadow = true)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = font != null ? font.fontSize : 9;
            text.color = color;
            text.alignment = alignment;
            text.text = content;
            text.supportRichText = true;
            text.lineSpacing = 1f;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            if (shadow)
            {
                var s = go.AddComponent<Shadow>();
                s.effectColor = Shade;
                s.effectDistance = new Vector2(1f, -1f);
            }
            return text;
        }

        /// <summary>A horizontal gold rule with a diamond in its middle.</summary>
        public static RectTransform Rule(Transform parent, string name, bool diamond = true)
        {
            var rule = UiStyle.Rect(parent, name);
            var line = Tiled(rule, "Line", "rule");
            var lr = line.rectTransform;
            lr.anchorMin = new Vector2(0f, 0.5f);
            lr.anchorMax = new Vector2(1f, 0.5f);
            lr.sizeDelta = new Vector2(0f, 1f);
            lr.anchoredPosition = Vector2.zero;
            if (diamond)
                UiStyle.Place(Picture(rule, "Diamond", "rule_diamond").rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5f, 5f));
            return rule;
        }

        /// <summary>Puts a rect at a place in its parent, in menu pixels, from the parent's top left.</summary>
        public static RectTransform At(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        /// <summary>Stretches a rect across its parent's width at a height from its top, inset at the sides.</summary>
        public static RectTransform Across(RectTransform rect, float y, float height, float left, float right)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -y - height);
            rect.offsetMax = new Vector2(-right, -y);
            return rect;
        }
    }
}
