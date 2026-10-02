using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The backpack and paper doll icons, drawn in code as pixel art until the UI art exists (Docs/09): one per kind of
    /// item, the size of its block in the backpack (<see cref="ItemSize"/>, <see cref="PixelsPerCell"/> art pixels a
    /// cell), in the palette with the dark outline (<see cref="PixelArt"/>). A Magic or better item gets a gem or band
    /// in its rarity's colour (drawn after the palette snap, so the rarity colours stay exact); a Common has none; a
    /// Legendary's outline is ember rather than black.
    /// Rows run from the bottom, as a texture's do. Pure.
    /// </summary>
    public static class ItemIconArt
    {
        public const int PixelsPerCell = 12;

        // Materials, picked from the palette's ramps.
        static readonly Color32 Wood = new Color32(132, 94, 60, 255);
        static readonly Color32 WoodDark = new Color32(72, 47, 30, 255);
        static readonly Color32 WoodLight = new Color32(168, 128, 86, 255);
        static readonly Color32 Leather = new Color32(100, 68, 43, 255);
        static readonly Color32 LeatherDark = new Color32(48, 31, 21, 255);
        static readonly Color32 LeatherLight = new Color32(132, 94, 60, 255);
        static readonly Color32 Cloth = new Color32(94, 108, 124, 255);
        static readonly Color32 ClothDark = new Color32(62, 74, 90, 255);
        static readonly Color32 Metal = new Color32(170, 164, 154, 255);
        static readonly Color32 MetalDark = new Color32(102, 96, 91, 255);
        static readonly Color32 Bone = new Color32(232, 224, 198, 255);
        static readonly Color32 Feather = new Color32(192, 62, 40, 255);
        static readonly Color32 FeatherDark = new Color32(108, 24, 20, 255);
        static readonly Color32 LegendaryOutline = new Color32(148, 70, 18, 255);

        /// <summary>Draws the icon for an item kind and rarity. Width and height are its block's cells times
        /// <see cref="PixelsPerCell"/>.</summary>
        public static Color32[] Draw(ItemSlot kind, ItemRarity rarity, out int width, out int height)
        {
            var cells = ItemSize.Of(kind);
            width = cells.x * PixelsPerCell;
            height = cells.y * PixelsPerCell;
            var canvas = new Canvas(width, height);
            switch (kind)
            {
                case ItemSlot.Weapon: Bow(canvas, false); break;
                case ItemSlot.TwoHandWeapon: Bow(canvas, true); break;
                case ItemSlot.Shield:
                case ItemSlot.OffHand: Quiver(canvas); break;
                case ItemSlot.Chest: Chest(canvas); break;
                case ItemSlot.Helm: Helm(canvas); break;
                case ItemSlot.Gloves: Gloves(canvas); break;
                case ItemSlot.Boots: Boots(canvas); break;
                case ItemSlot.Belt: Belt(canvas); break;
                case ItemSlot.Amulet: Amulet(canvas); break;
                default: Ring(canvas); break;
            }
            PixelArt.Process(canvas.Pixels, width, height, true);
            if (rarity != ItemRarity.Common)
                Accent(canvas, kind, LootColors.Of(rarity), rarity == ItemRarity.Legendary);
            return canvas.Pixels;
        }

        // --- Kinds ----------------------------------------------------------------------------------------------------

        // A curved limb on the left, the string straight on the right, a leather grip in the middle. The longbow fills
        // the block's height, darker and deeper; the short bow is shorter and lighter.
        static void Bow(Canvas c, bool longbow)
        {
            var top = c.Height - 2;
            var bottom = longbow ? 1 : c.Height / 6;
            var topEnd = longbow ? top : c.Height - c.Height / 6;
            var stringX = c.Width - 6;
            var depth = longbow ? c.Width - 11 : c.Width - 12;
            var wood = longbow ? WoodDark : Wood;
            for (var y = bottom; y <= topEnd; y++)
            {
                var t = (y - bottom) / (float)(topEnd - bottom);
                var x = Mathf.RoundToInt(stringX - 1 - depth * Mathf.Sin(t * Mathf.PI));
                var thick = t < 0.08f || t > 0.92f ? 1 : t < 0.2f || t > 0.8f ? 2 : 3;
                for (var i = 0; i < thick; i++)
                    c.Set(x + i, y, i == thick - 1 && thick > 1 ? WoodLight : wood);
            }
            c.Line(stringX, bottom, stringX, topEnd, Bone);
            // The grip, where the limb is deepest.
            var mid = (bottom + topEnd) / 2;
            var gripX = Mathf.RoundToInt(stringX - 1 - depth);
            c.Rect(gripX - 1, mid - 3, 4, 7, Leather);
            c.Rect(gripX - 1, mid - 1, 4, 1, LeatherDark);
            // Nocks at the tips.
            c.Set(stringX - 1, bottom, MetalDark);
            c.Set(stringX - 1, topEnd, MetalDark);
            if (longbow)
                c.Rect(gripX - 1, mid + 4, 3, 1, Bone);
        }

        // A leather tube leaning a little, with fletchings showing over its mouth.
        static void Quiver(Canvas c)
        {
            var h = c.Height;
            // The tube: a slanted band from the bottom to two thirds up.
            for (var y = 2; y < h * 2 / 3 + 2; y++)
            {
                var lean = (y - 2) / 6;
                var left = 6 + lean;
                c.Rect(left, y, 9, 1, Leather);
                c.Set(left, y, LeatherDark);
                c.Set(left + 8, y, LeatherLight);
            }
            var mouth = h * 2 / 3 + 2;
            var mouthLeft = 6 + (mouth - 2) / 6;
            c.Rect(mouthLeft - 1, mouth - 2, 11, 2, LeatherDark);
            c.Rect(mouthLeft, 4, 9, 2, LeatherDark);
            // Three arrows' shafts and fletchings.
            for (var i = 0; i < 3; i++)
            {
                var x = mouthLeft + 2 + i * 3;
                c.Line(x, mouth, x + 1, h - 3, WoodLight);
                c.Rect(x - 1, h - 6 + (i % 2), 3, 3, i == 1 ? FeatherDark : Feather);
            }
        }

        // A tunic: shoulders, short sleeves, a body narrowing to the waist and a laced collar.
        static void Chest(Canvas c)
        {
            var w = c.Width;
            var h = c.Height;
            var top = h - 3;
            c.Rect(2, top - 10, w - 4, 8, Leather);        // shoulders and sleeves
            c.Rect(5, 2, w - 10, top - 4, Leather);        // body
            c.Rect(5, 2, 2, top - 4, LeatherDark);
            c.Rect(w - 7, 2, 2, top - 4, LeatherLight);
            c.Rect(2, top - 10, 3, 2, LeatherDark);        // cuffs
            c.Rect(w - 5, top - 10, 3, 2, LeatherDark);
            c.Rect(w / 2 - 3, top - 2, 6, 2, LeatherDark); // collar
            c.Line(w / 2, top - 3, w / 2, top - 10, Bone); // lacing
            c.Rect(5, 6, w - 10, 2, LeatherDark);          // belt line
        }

        // A hood: a rounded cap with a dark opening for the face.
        static void Helm(Canvas c)
        {
            var w = c.Width;
            var h = c.Height;
            c.Disc(w / 2f - 0.5f, h / 2f, w / 2f - 2f, Cloth);
            c.Rect(3, 2, w - 6, h / 2 - 2, Cloth);
            c.Disc(w / 2f - 0.5f, h / 2f - 2f, w / 4f, ClothDark);
            c.Rect(w / 2 - 4, 2, 8, h / 2 - 3, ClothDark);
            c.Line(4, h / 2 + 3, w / 2 - 2, h - 3, Metal);
        }

        // A glove pointing up: the cuff, the palm, four fingers and the thumb out to the side.
        static void Gloves(Canvas c)
        {
            var w = c.Width;
            var h = c.Height;
            c.Rect(6, 2, 11, 6, LeatherDark);   // cuff
            c.Rect(6, 8, 11, 7, Leather);       // palm
            for (var i = 0; i < 4; i++)
                c.Rect(6 + i * 3, 15, 2, i == 1 || i == 2 ? 6 : 5, Leather);
            c.Rect(3, 9, 3, 5, Leather);        // thumb
            c.Rect(6, 4, 11, 1, Metal);         // cuff stud row
            c.Set(w - 7, h - 4, LeatherLight);
        }

        // A boot from the side: the shaft up, the foot pointing left, a sole and a turned cuff.
        static void Boots(Canvas c)
        {
            var w = c.Width;
            var h = c.Height;
            c.Rect(10, 6, 9, h - 9, Leather);   // shaft
            c.Rect(3, 3, 16, 6, Leather);       // foot
            c.Rect(2, 2, 18, 2, LeatherDark);   // sole
            c.Rect(9, h - 6, 11, 3, LeatherLight);
            c.Rect(17, 6, 2, h - 12, LeatherDark);
            c.Set(5, 6, LeatherLight);
            c.Set(w - 4, 2, LeatherDark);
        }

        // A strap across with a buckle near the middle.
        static void Belt(Canvas c)
        {
            var w = c.Width;
            c.Rect(1, 3, w - 2, 6, Leather);
            c.Rect(1, 3, w - 2, 1, LeatherDark);
            c.Rect(1, 8, w - 2, 1, LeatherLight);
            c.Rect(w / 2 - 3, 2, 6, 8, Metal);
            c.Rect(w / 2 - 1, 4, 2, 4, LeatherDark);
            for (var x = w / 2 + 6; x < w - 2; x += 3)
                c.Set(x, 5, LeatherDark);
        }

        // A chain hanging in a V with a pendant at the bottom.
        static void Amulet(Canvas c)
        {
            var w = c.Width;
            var h = c.Height;
            c.Line(1, h - 2, w / 2 - 1, 5, Metal);
            c.Line(w - 2, h - 2, w / 2, 5, Metal);
            c.Disc(w / 2f - 0.5f, 3.5f, 2.5f, MetalDark);
        }

        // A band seen from the front.
        static void Ring(Canvas c)
        {
            var w = c.Width;
            var h = c.Height;
            c.Band(w / 2f - 0.5f, h / 2f - 1f, 4.5f, 2.8f, Metal);
            c.Rect(w / 2 - 2, h - 4, 4, 2, MetalDark);
        }

        // --- Rarity ---------------------------------------------------------------------------------------------------

        static void Accent(Canvas c, ItemSlot kind, Color32 color, bool legendary)
        {
            var w = c.Width;
            var h = c.Height;
            switch (kind)
            {
                case ItemSlot.Weapon:
                case ItemSlot.TwoHandWeapon:
                    // A gem set in the grip (where Bow puts it: the limb's deepest point).
                    var gripX = kind == ItemSlot.TwoHandWeapon ? 4 : 5;
                    c.Rect(gripX, h / 2 - 1, 2, 2, color);
                    break;
                case ItemSlot.Shield:
                case ItemSlot.OffHand:
                    c.Rect(8, h / 3, 8, 2, color);
                    break;
                case ItemSlot.Chest:
                    c.Rect(w / 2 - 1, h / 2, 2, 2, color);
                    c.Rect(5, 6, w - 10, 1, color);
                    break;
                case ItemSlot.Helm:
                    c.Rect(w / 2 - 1, h - 6, 2, 2, color);
                    break;
                case ItemSlot.Gloves:
                    c.Rect(9, 10, 4, 3, color);
                    break;
                case ItemSlot.Boots:
                    c.Rect(10, h - 5, 9, 1, color);
                    break;
                case ItemSlot.Belt:
                    c.Rect(w / 2 - 1, 5, 2, 2, color);
                    break;
                case ItemSlot.Amulet:
                    c.Rect(w / 2 - 1, 2, 2, 3, color);
                    break;
                default:
                    c.Rect(w / 2 - 2, h - 4, 4, 2, color);
                    break;
            }
            // A legendary's outline glows ember instead of black.
            if (!legendary)
                return;
            for (var i = 0; i < c.Pixels.Length; i++)
            {
                var p = c.Pixels[i];
                if (p.a > 0 && p.r == PixelArt.Outline.r && p.g == PixelArt.Outline.g && p.b == PixelArt.Outline.b)
                    c.Pixels[i] = LegendaryOutline;
            }
        }

        /// <summary>A pixel buffer with a few drawing primitives; writes outside it are dropped.</summary>
        sealed class Canvas
        {
            public readonly int Width;
            public readonly int Height;
            public readonly Color32[] Pixels;

            public Canvas(int width, int height)
            {
                Width = width;
                Height = height;
                Pixels = new Color32[width * height];
            }

            public void Set(int x, int y, Color32 color)
            {
                // A one-pixel margin is kept clear for the outline.
                if (x < 1 || y < 1 || x >= Width - 1 || y >= Height - 1)
                    return;
                Pixels[x + y * Width] = color;
            }

            public void Rect(int x, int y, int w, int h, Color32 color)
            {
                for (var dy = 0; dy < h; dy++)
                    for (var dx = 0; dx < w; dx++)
                        Set(x + dx, y + dy, color);
            }

            public void Line(int x0, int y0, int x1, int y1, Color32 color)
            {
                int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0);
                int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
                var error = dx + dy;
                while (true)
                {
                    Set(x0, y0, color);
                    if (x0 == x1 && y0 == y1)
                        return;
                    var e2 = 2 * error;
                    if (e2 >= dy)
                    {
                        error += dy;
                        x0 += sx;
                    }
                    if (e2 <= dx)
                    {
                        error += dx;
                        y0 += sy;
                    }
                }
            }

            public void Disc(float cx, float cy, float radius, Color32 color) => Band(cx, cy, radius, -1f, color);

            /// <summary>Pixels whose centres lie between the inner and outer radius.</summary>
            public void Band(float cx, float cy, float outer, float inner, Color32 color)
            {
                for (var y = 0; y < Height; y++)
                    for (var x = 0; x < Width; x++)
                    {
                        float ddx = x - cx, ddy = y - cy;
                        var d = Mathf.Sqrt(ddx * ddx + ddy * ddy);
                        if (d <= outer && d > inner)
                            Set(x, y, color);
                    }
            }
        }
    }
}
