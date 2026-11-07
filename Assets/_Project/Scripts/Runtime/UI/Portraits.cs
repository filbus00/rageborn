using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>The marks a face can carry, any of them together.</summary>
    [Flags]
    public enum FaceMarks
    {
        None = 0,
        WarPaint = 1,
        Scar = 2,
        Freckles = 4,
        ChinMark = 8,
    }

    /// <summary>One archer's face: a base portrait and what is changed on it.</summary>
    public struct PortraitLook
    {
        public int Base;
        public Color Skin, Hair, Eyes, Paint;
        public FaceMarks Marks;

        /// <summary>Which way the scar runs (true: down to the right) and across which cheek.</summary>
        public bool ScarDown, ScarLeft;
    }

    /// <summary>Where things are on a base face (ArtSource/ui/faces/&lt;base&gt;.json): pixels from the top left as x, y
    /// pairs.</summary>
    [Serializable]
    public sealed class PortraitBase
    {
        public int[] eyes = new int[0];
        public int[] cheeks = new int[0];
        public int[] brow = new int[0];
        public int[] chin = new int[0];
    }

    /// <summary>
    /// The archers' faces (2026-11-07, the owner: "now all the amazon archers have the same portrait, I want it to auto
    /// generate different looks ... so that no amazon is identical", then "Bases + variations"): each archer's look comes
    /// from her name, so the same archer always has the same face and the save needs nothing new. A base portrait (ten,
    /// the owner's Gemini faces: hair styles, face shapes, ages) and on it a skin, a hair colour, eye colour and marks
    /// (war paint, a scar, freckles, a mark on the chin). Her armour keeps its colours (the owner, 2026-11-07: "do not
    /// change the characters armor color"). The palettes are Claude's, to review (Docs/08). Pure.
    /// </summary>
    public static class PortraitLooks
    {
        static readonly Color[] Skins =
        {
            new Color(0.66f, 0.49f, 0.39f), new Color(0.58f, 0.40f, 0.29f), new Color(0.50f, 0.34f, 0.22f),
            new Color(0.43f, 0.28f, 0.17f), new Color(0.34f, 0.21f, 0.12f), new Color(0.24f, 0.14f, 0.08f),
        };

        static readonly Color[] Hairs =
        {
            new Color(0.07f, 0.06f, 0.06f), new Color(0.07f, 0.07f, 0.09f), new Color(0.16f, 0.10f, 0.06f),
            new Color(0.26f, 0.14f, 0.07f), new Color(0.38f, 0.13f, 0.05f), new Color(0.55f, 0.23f, 0.08f),
            new Color(0.62f, 0.48f, 0.26f), new Color(0.52f, 0.47f, 0.37f), new Color(0.58f, 0.57f, 0.56f),
        };

        static readonly Color[] EyeColours =
        {
            new Color(0.25f, 0.14f, 0.07f), new Color(0.38f, 0.30f, 0.12f), new Color(0.22f, 0.42f, 0.22f),
            new Color(0.24f, 0.40f, 0.62f), new Color(0.45f, 0.48f, 0.52f), new Color(0.62f, 0.42f, 0.12f),
        };

        static readonly Color[] Paints =
        {
            new Color(0.62f, 0.16f, 0.10f), new Color(0.16f, 0.30f, 0.62f), new Color(0.86f, 0.84f, 0.78f),
            new Color(0.06f, 0.06f, 0.07f),
        };

        /// <summary>A name's number, the same in every session and on every device (FNV-1a).</summary>
        public static int Hash(string name)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var c in name ?? "")
                {
                    hash ^= c;
                    hash *= 16777619u;
                }
                return (int)(hash & 0x7FFFFFFF);
            }
        }

        /// <summary>The look of the archer with this name, from <paramref name="bases"/> base faces.</summary>
        public static PortraitLook For(string name, int bases)
        {
            var random = new System.Random(Hash(name));
            var look = new PortraitLook
            {
                Base = random.Next(Math.Max(1, bases)),
                Skin = Skins[random.Next(Skins.Length)],
                Hair = Hairs[random.Next(Hairs.Length)],
                Eyes = EyeColours[random.Next(EyeColours.Length)],
                Paint = Paints[random.Next(Paints.Length)],
                ScarDown = random.Next(2) == 0,
                ScarLeft = random.Next(2) == 0,
            };
            // Each mark on its own: about two archers in three carry at least one.
            if (random.NextDouble() < 0.3)
                look.Marks |= FaceMarks.WarPaint;
            if (random.NextDouble() < 0.2)
                look.Marks |= FaceMarks.Scar;
            if (random.NextDouble() < 0.25)
                look.Marks |= FaceMarks.Freckles;
            if (random.NextDouble() < 0.15)
                look.Marks |= FaceMarks.ChinMark;
            return look;
        }
    }

    /// <summary>
    /// Paints a look onto a base face. The base's regions (red: 1 skin, 2 hair, 3 iris; 4, the cloak, is left as it is;
    /// green: how much the recolouring takes) come from ArtSource/tools/ui/face_regions.py. A region keeps its shading: each pixel's lightness
    /// against the region's average sets how light the new colour is there. Pixels top row first. Pure.
    /// </summary>
    public static class PortraitPainter
    {
        public const int Skin = 1, Hair = 2, Iris = 3;

        public static Color32[] Paint(Color32[] pixels, Color32[] regions, int width, int height, PortraitBase spot, PortraitLook look)
        {
            var result = (Color32[])pixels.Clone();
            var average = new float[4];
            var counts = new int[4];
            for (var i = 0; i < pixels.Length; i++)
            {
                var region = regions[i].r;
                if (region <= 0 || region > Iris)
                    continue;
                average[region] += Lightness(pixels[i]);
                counts[region]++;
            }
            for (var k = 1; k <= Iris; k++)
                average[k] = counts[k] > 0 ? Mathf.Max(0.02f, average[k] / counts[k]) : 1f;

            for (var i = 0; i < pixels.Length; i++)
            {
                var region = regions[i].r;
                if (region <= 0 || region > Iris)
                    continue;
                var target = region == Skin ? look.Skin : region == Hair ? look.Hair : look.Eyes;
                // Toward light targets the shading is eased, so a blonde's dark strands do not go grey.
                var relative = Lightness(pixels[i]) / average[region];
                // Capped: a lit strand three times the hair's average lightness glared on red and blonde hair.
                var shade = Mathf.Min(1.4f,Mathf.Pow(relative, Lightness(target) > 0.4f ? 0.75f : 1f));
                var painted = new Color(target.r * shade, target.g * shade, target.b * shade);
                var take = regions[i].g / 255f;
                result[i] = Color.Lerp(pixels[i], painted, take);
            }

            if ((look.Marks & FaceMarks.WarPaint) != 0)
                for (var k = 0; k + 1 < spot.cheeks.Length; k += 2)
                    for (var stripe = 0; stripe < 2; stripe++)
                        Line(result, regions, width, height, spot.cheeks[k] - 3, spot.cheeks[k + 1] - 2 + stripe * 3,
                            spot.cheeks[k] + 3, spot.cheeks[k + 1] - 2 + stripe * 3, look.Paint, 0.85f);
            if ((look.Marks & FaceMarks.Freckles) != 0)
                for (var k = 0; k + 1 < spot.cheeks.Length; k += 2)
                    foreach (var (dx, dy) in FreckleSpots)
                        Dot(result, regions, width, height, spot.cheeks[k] + dx, spot.cheeks[k + 1] + dy, Darker(look.Skin, 0.62f), 0.7f);
            if ((look.Marks & FaceMarks.Scar) != 0 && spot.cheeks.Length >= 4)
            {
                var k = look.ScarLeft ? 0 : 2;
                var x = spot.cheeks[k];
                var y = spot.cheeks[k + 1];
                var down = look.ScarDown ? 1 : -1;
                Line(result, regions, width, height, x - 3, y - 4 * down, x + 3, y + 4 * down, Lighter(look.Skin, 1.45f), 0.9f);
            }
            if ((look.Marks & FaceMarks.ChinMark) != 0 && spot.chin.Length >= 2)
                for (var dx = -2; dx <= 2; dx += 2)
                    Line(result, regions, width, height, spot.chin[0] + dx, spot.chin[1] - 1, spot.chin[0] + dx, spot.chin[1] + 1, look.Paint, 0.8f);
            return result;
        }

        static readonly (int, int)[] FreckleSpots = { (-3, 0), (-1, 1), (1, -1), (2, 1), (-2, -1), (3, 0) };

        static float Lightness(Color32 c) => (c.r * 0.3f + c.g * 0.55f + c.b * 0.15f) / 255f;

        static float Lightness(Color c) => c.r * 0.3f + c.g * 0.55f + c.b * 0.15f;

        static Color Darker(Color c, float f) => new Color(c.r * f, c.g * f, c.b * f);

        static Color Lighter(Color c, float f) => new Color(Mathf.Min(1f, c.r * f), Mathf.Min(1f, c.g * f), Mathf.Min(1f, c.b * f));

        // A mark only on skin, keeping some of the shading under it.
        static void Dot(Color32[] pixels, Color32[] regions, int width, int height, int x, int y, Color colour, float strength)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
                return;
            var i = y * width + x;
            if (regions[i].r != Skin)
                return;
            var under = (Color)pixels[i];
            var shade = Mathf.Clamp(Lightness(pixels[i]) / Mathf.Max(0.05f, Lightness(colour)), 0.6f, 1.2f);
            pixels[i] = Color.Lerp(under, new Color(colour.r * shade, colour.g * shade, colour.b * shade), strength);
        }

        static void Line(Color32[] pixels, Color32[] regions, int width, int height, int x0, int y0, int x1, int y1, Color colour, float strength)
        {
            var steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            for (var s = 0; s <= steps; s++)
            {
                var t = steps == 0 ? 0f : s / (float)steps;
                Dot(pixels, regions, width, height, Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), colour, strength);
            }
        }
    }

    /// <summary>
    /// The archers' portraits as sprites: the base faces from Resources/UI/Faces (&lt;base&gt;.png, its regions and its
    /// .json, imported readable by PixelUiImport), painted once per name and kept.
    /// </summary>
    public static class PortraitFaces
    {
        sealed class Face
        {
            public Color32[] Pixels, Regions;
            public int Width, Height;
            public PortraitBase Spots;
        }

        static readonly Dictionary<string, Sprite> Painted = new Dictionary<string, Sprite>();
        static List<Face> faces;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Painted.Clear();
            faces = null;
        }

        /// <summary>The portrait of the archer with this name, or null when there are no base faces.</summary>
        public static Sprite For(string name)
        {
            name ??= "";
            if (Painted.TryGetValue(name, out var sprite) && sprite != null)
                return sprite;
            var all = Faces();
            if (all.Count == 0)
                return null;
            var look = PortraitLooks.For(name, all.Count);
            var face = all[look.Base];
            var pixels = PortraitPainter.Paint(face.Pixels, face.Regions, face.Width, face.Height, face.Spots, look);
            var texture = new Texture2D(face.Width, face.Height, TextureFormat.RGBA32, false)
            {
                name = "Portrait " + name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(Flip(pixels, face.Width, face.Height));
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0, 0, face.Width, face.Height), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, Vector4.zero, false);
            return Painted[name] = sprite;
        }

        static List<Face> Faces()
        {
            if (faces != null)
                return faces;
            faces = new List<Face>();
            var specs = Resources.LoadAll<TextAsset>("UI/Faces");
            Array.Sort(specs, (a, b) => string.CompareOrdinal(a.name, b.name));
            foreach (var spec in specs)
            {
                var picture = Resources.Load<Texture2D>("UI/Faces/" + spec.name);
                var regions = Resources.Load<Texture2D>("UI/Faces/" + spec.name + "_regions");
                if (picture == null || regions == null || !picture.isReadable || !regions.isReadable ||
                    picture.width != regions.width || picture.height != regions.height)
                {
                    Debug.LogWarning($"[ARPG] Face {spec.name}: its picture or regions are missing or not readable.");
                    continue;
                }
                faces.Add(new Face
                {
                    Width = picture.width,
                    Height = picture.height,
                    Pixels = Flip(picture.GetPixels32(), picture.width, picture.height),
                    Regions = Flip(regions.GetPixels32(), regions.width, regions.height),
                    Spots = JsonUtility.FromJson<PortraitBase>(spec.text) ?? new PortraitBase(),
                });
            }
            return faces;
        }

        // Unity's rows run from the bottom; the painter's from the top.
        static Color32[] Flip(Color32[] pixels, int width, int height)
        {
            var flipped = new Color32[pixels.Length];
            for (var y = 0; y < height; y++)
                Array.Copy(pixels, y * width, flipped, (height - 1 - y) * width, width);
            return flipped;
        }
    }
}
