using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// Every menu in the company menu's pixel look (2026-11-05, the owner: "remake all", every menu but the HUD): put on a
    /// menu's canvas, it shows the canvas at a whole number of screen pixels for every three units (a menu pixel; the
    /// screens were laid out for 1170 units across, the concept's 390 pixels), and each frame turns what the screen's
    /// builders made into the pixel look (<see cref="PixelUi"/>): text into the pixel fonts (a title font for titles and
    /// buttons, the body font for the rest; size and bold tags dropped, the fonts having one size each), the rendered
    /// metal rims and plates into the drawn gold frames, the leather into the dark green backdrop, and the old palette's
    /// browns and reds into the concept's greens and red. The screens' own code is unchanged.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PixelMenu : MonoBehaviour
    {
        /// <summary>Canvas units a menu pixel.</summary>
        public const float Unit = 3f;

        Canvas canvas;
        CanvasScaler scaler;
        int screenWidth;
        readonly List<Text> texts = new List<Text>(256);
        readonly List<Image> images = new List<Image>(256);
        readonly List<Shadow> effects = new List<Shadow>(128);
        readonly HashSet<Object> done = new HashSet<Object>();

        /// <summary>Gives a menu's canvas the pixel look, when the drawn menu art is in the build.</summary>
        public static void Attach(GameObject canvasObject)
        {
            if (canvasObject != null && PixelUi.Available && canvasObject.GetComponent<PixelMenu>() == null)
                canvasObject.AddComponent<PixelMenu>();
        }

        void Awake()
        {
            canvas = GetComponent<Canvas>();
            scaler = GetComponent<CanvasScaler>();
            if (canvas != null)
                canvas.pixelPerfect = true;
            Fit();
        }

        void Fit()
        {
            screenWidth = Screen.width;
            if (scaler == null)
                return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = PixelUi.Scale(Screen.width) / Unit;
        }

        void LateUpdate()
        {
            if (canvas == null || !canvas.enabled)
                return;
            if (Screen.width != screenWidth)
                Fit();
            Restyle();
        }

        /// <summary>Turns what is new on the canvas into the pixel look (what was turned before is passed over).</summary>
        public void Restyle()
        {
            // What was turned and destroyed since (a sheet's rows are made anew each time it draws) is let go now and then.
            if (done.Count > 4000)
                done.Clear();
            GetComponentsInChildren(false, texts);
            foreach (var text in texts)
                Turn(text);
            GetComponentsInChildren(false, images);
            foreach (var image in images)
                Turn(image);
            GetComponentsInChildren(false, effects);
            foreach (var effect in effects)
                Turn(effect);
        }

        // --- Text ----------------------------------------------------------------------------------------------------

        static readonly Regex Tags = new Regex(@"</?(size|b|i)(=[^>]*)?>", RegexOptions.Compiled);

        void Turn(Text text)
        {
            // Tags the pixel fonts cannot show, stripped whenever the text changes.
            var content = text.text;
            if (content != null && content.IndexOf('<') >= 0 && Tags.IsMatch(content))
                text.text = Tags.Replace(content, "");
            if (IsPixel(text.font))
                return;
            var size = text.fontSize;
            var titled = size >= 40 || (text.fontStyle != FontStyle.Normal && size >= 34) || text.font == UiStyle.Title && size >= 36;
            var font = !titled ? PixelUi.BodyFont3 : size >= 90 ? PixelUi.TitleFont6 : PixelUi.TitleFont3;
            if (font == null)
                return;
            text.font = font;
            text.fontSize = font.fontSize;
            text.fontStyle = FontStyle.Normal;
            text.lineSpacing = 1f;
            text.resizeTextForBestFit = false;
        }

        static bool IsPixel(Font font) =>
            font != null && (font == PixelUi.BodyFont3 || font == PixelUi.TitleFont3 || font == PixelUi.TitleFont6 ||
                             font == PixelUi.BodyFont || font == PixelUi.TitleFont || font == PixelUi.TitleBigFont);

        // A text's shadow or a rim's outline: shadows a menu pixel down and right; a rim's outline becomes a drawn frame.
        void Turn(Shadow effect)
        {
            if (!done.Add(effect))
                return;
            if (effect.GetComponent<Text>() != null)
            {
                effect.effectDistance = new Vector2(Unit, -Unit);
                effect.effectColor = PixelUi.Shade;
                return;
            }
            if (effect is Outline && effect.GetComponent<Image>() != null)
            {
                effect.enabled = false;
                Rim(effect.transform, "frame_card_b3");
            }
        }

        // A drawn frame's border over a rect (its centre left clear), ignored by layouts, first among its children.
        static void Rim(Transform parent, string frame)
        {
            if (parent.Find("Pixel Rim") != null)
                return;
            var rim = PixelUi.Frame(parent, "Pixel Rim", frame);
            rim.fillCenter = false;
            rim.pixelsPerUnitMultiplier = 1f / Unit;
            rim.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UiStyle.Stretch(rim.rectTransform);
            rim.transform.SetAsFirstSibling();
        }

        // --- Images --------------------------------------------------------------------------------------------------

        void Turn(Image image)
        {
            if (!done.Add(image))
                return;
            var sprite = image.sprite;
            if (sprite != null && (sprite == UiTextures.Frame || sprite == UiArt.Frame))
            {
                // A framed panel's rim: the drawn gold frame.
                image.sprite = PixelUi.Sprite("frame_panel_b4");
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f / Unit;
            }
            else if (sprite != null && (sprite == UiTextures.Inset || sprite == UiArt.Fill))
            {
                // A panel's fill is flat; a recessed cell (the backpack's) a dark inset box.
                if (image.name == "Fill")
                    image.sprite = null;
                else
                {
                    image.sprite = PixelUi.Sprite("frame_inset_b3");
                    image.type = Image.Type.Sliced;
                    image.pixelsPerUnitMultiplier = 1f / Unit;
                }
            }
            else if (sprite != null && sprite == UiTextures.ButtonPlate)
            {
                image.sprite = null;
                Rim(image.transform, "frame_card_b3");
            }
            else if (sprite != null && sprite == UiTextures.Backdrop)
            {
                image.sprite = PixelUi.Sprite("backdrop");
                image.type = Image.Type.Tiled;
                image.pixelsPerUnitMultiplier = 1f / Unit;
                image.color = new Color(1f, 1f, 1f, image.color.a);
                return;
            }
            image.color = Remap(image.color);
        }

        // The old palette's browns and reds, and the concept's greens and red for them.
        static readonly (Color from, Color to)[] Palette =
        {
            (UiStyle.Backdrop, new Color32(14, 22, 17, 250)),
            (UiStyle.Panel, new Color32(20, 32, 24, 255)),
            (UiStyle.Sheet, new Color32(14, 22, 17, 255)),
            (UiStyle.ButtonFill, new Color32(27, 42, 31, 255)),
            (UiStyle.RowFill, new Color32(22, 35, 26, 255)),
            (UiStyle.EmptySlotFill, new Color32(16, 26, 19, 255)),
            (UiStyle.SlotFill, new Color32(70, 14, 16, 255)),
            (UiStyle.Selected, new Color32(112, 22, 24, 255)),
            (UiStyle.Chip, new Color32(70, 14, 16, 255)),
            (UiStyle.Locked, new Color32(12, 18, 14, 255)),
            (UiStyle.FrameDark, new Color32(8, 12, 9, 255)),
            (SheetKit.CardFill, new Color32(22, 35, 26, 245)),
            (SheetKit.ReadyFill, new Color32(40, 64, 40, 255)),
            (SheetKit.DoneFill, new Color32(16, 24, 19, 255)),
            (SheetKit.Accent, new Color32(168, 128, 52, 255)),
            (SheetKit.CardEdge, new Color32(92, 68, 26, 255)),
        };

        static Color Remap(Color color)
        {
            foreach (var (from, to) in Palette)
                if (Same(color, from))
                    return new Color(to.r, to.g, to.b, color.a);
            // Any other dark warm fill (a screen's own brown) turns the concept's dark green at its brightness.
            var light = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            if (light < 0.25f && color.r >= color.g && color.r >= color.b && color.r - color.b > 0.008f && color.r - color.g < 0.12f)
                return new Color(light * 0.62f, light, light * 0.72f, color.a);
            return color;
        }

        static bool Same(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.004f && Mathf.Abs(a.g - b.g) < 0.004f && Mathf.Abs(a.b - b.b) < 0.004f;
    }
}
