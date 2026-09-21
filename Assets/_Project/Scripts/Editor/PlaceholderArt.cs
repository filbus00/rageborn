using System.IO;
using UnityEditor;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// Generates simple placeholder sprites so scenes are playable before real art exists.
    /// Every generator returns a texture that ImportSprite writes to disk and imports.
    /// </summary>
    public static class PlaceholderArt
    {
        /// <summary>A vertical capsule with a darker outline.</summary>
        public static Texture2D Capsule(int width, int height, Color32 fill)
        {
            var outline = Darken(fill, 0.6f);
            var radius = width * 0.5f;
            var top = new Vector2(radius, height - radius);
            var bottom = new Vector2(radius, radius);
            const float outlineWidth = 4f;

            return Generate(width, height, (x, y) =>
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                var onSegment = new Vector2(radius, Mathf.Clamp(p.y, bottom.y, top.y));
                var distance = Vector2.Distance(p, onSegment);

                var coverage = Mathf.Clamp01(radius - distance + 0.5f);
                var color = distance > radius - outlineWidth ? outline : fill;
                return new Color32(color.r, color.g, color.b, (byte)(coverage * 255f));
            });
        }

        /// <summary>A soft-edged black ellipse, used as a ground shadow.</summary>
        public static Texture2D ShadowEllipse(int width, int height, float opacity)
        {
            return Generate(width, height, (x, y) =>
            {
                var dx = (x + 0.5f - width * 0.5f) / (width * 0.5f);
                var dy = (y + 0.5f - height * 0.5f) / (height * 0.5f);
                var distance = Mathf.Sqrt(dx * dx + dy * dy);

                var alpha = opacity * Mathf.Clamp01((1f - distance) * 4f);
                return new Color32(0, 0, 0, (byte)(alpha * 255f));
            });
        }

        /// <summary>A white ring of the given thickness in pixels, tinted at runtime.</summary>
        public static Texture2D Ring(int size, float thickness)
        {
            var radius = size * 0.5f;
            return Generate(size, size, (x, y) =>
            {
                var distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                var outer = Mathf.Clamp01(radius - distance + 0.5f);
                var inner = Mathf.Clamp01(distance - (radius - thickness) + 0.5f);
                return new Color32(255, 255, 255, (byte)(outer * inner * 255f));
            });
        }

        /// <summary>
        /// A white pie slice that points along +x and spans the given angle, filling the square. Brighter toward the
        /// rim so a sweep reads as a slash. Tinted at runtime.
        /// </summary>
        public static Texture2D Wedge(int size, float arcDegrees)
        {
            var radius = size * 0.5f;
            var halfArc = arcDegrees * 0.5f;

            return Generate(size, size, (x, y) =>
            {
                var dx = x + 0.5f - radius;
                var dy = y + 0.5f - radius;
                var distance = Mathf.Sqrt(dx * dx + dy * dy);
                if (distance > radius || Mathf.Abs(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg) > halfArc)
                    return new Color32(255, 255, 255, 0);

                var rim = Mathf.Clamp01((distance / radius - 0.55f) / 0.45f);
                return new Color32(255, 255, 255, (byte)(255f * (0.25f + 0.75f * rim)));
            });
        }

        /// <summary>
        /// A stairwell seen from above: a 2:1 diamond made of nested diamonds that alternate between two tones,
        /// so it reads as steps leading down. The same shape as a ground tile, so it sits on a cell.
        /// </summary>
        public static Texture2D Stairs(int width, int height, Color32 dark, Color32 light)
        {
            var outline = Darken(dark, 0.5f);
            return Generate(width, height, (x, y) =>
            {
                var dx = Mathf.Abs(x + 0.5f - width * 0.5f) / (width * 0.5f);
                var dy = Mathf.Abs(y + 0.5f - height * 0.5f) / (height * 0.5f);
                var distance = dx + dy;
                if (distance > 1f)
                    return new Color32(0, 0, 0, 0);
                if (distance > 0.94f)
                    return outline;

                // Four steps from the rim inward.
                return (int)(distance * 4f) % 2 == 0 ? light : dark;
            });
        }

        /// <summary>A white filled circle, tinted at runtime.</summary>
        public static Texture2D Disc(int size)
        {
            var radius = size * 0.5f;
            return Generate(size, size, (x, y) =>
            {
                var distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                return new Color32(255, 255, 255, (byte)(Mathf.Clamp01(radius - distance + 0.5f) * 255f));
            });
        }

        /// <summary>
        /// An isometric wall block: a 2:1 diamond footprint across the full width, a top face and two side faces.
        /// The center of the footprint sits at 0.5 of the width horizontally and a quarter of the footprint width
        /// vertically, so import it with that custom pivot and it stands on its cell.
        /// </summary>
        public static Texture2D WallBlock(int width, int height, Color32 top, Color32 left, Color32 right)
        {
            var half = width * 0.5f;
            var footprintHalfHeight = width * 0.25f;
            var wallHeight = height - width * 0.5f;
            var topCenterY = footprintHalfHeight + wallHeight;
            var outlineColor = Darken(left, 0.55f);
            const float outline = 2f;

            return Generate(width, height, (x, y) =>
            {
                var px = x + 0.5f;
                var py = y + 0.5f;
                var slope = footprintHalfHeight - Mathf.Abs(px - half) * (footprintHalfHeight / half);
                var lower = slope;
                var upper = topCenterY + slope;

                if (py < lower || py > upper)
                    return new Color32(0, 0, 0, 0);

                // Distance from the center of the top face, normalized so 1 is its outline.
                var topDistance = Mathf.Abs(px - half) / half + Mathf.Abs(py - topCenterY) / footprintHalfHeight;
                if (topDistance <= 1f)
                    return topDistance > 0.93f ? outlineColor : top;

                var isEdge = py - lower < outline || px < outline || px > width - outline || Mathf.Abs(px - half) < 1f;
                if (isEdge)
                    return outlineColor;
                return px < half ? left : right;
            });
        }

        /// <summary>Writes the texture as a PNG, imports it as a sprite and returns the sprite.</summary>
        /// <param name="customPivot">Used when <paramref name="alignment"/> is Custom, as a fraction of the sprite size.</param>
        public static Sprite ImportSprite(
            string path, Texture2D texture, int pixelsPerUnit, SpriteAlignment alignment, FilterMode filter,
            Vector2? customPivot = null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = filter;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)alignment;
            if (alignment == SpriteAlignment.Custom)
                settings.spritePivot = customPivot ?? new Vector2(0.5f, 0.5f);
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Texture2D Generate(int width, int height, System.Func<int, int, Color32> pixel)
        {
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                    pixels[x + y * width] = pixel(x, y);

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            return texture;
        }

        static Color32 Darken(Color32 color, float factor) =>
            new Color32((byte)(color.r * factor), (byte)(color.g * factor), (byte)(color.b * factor), 255);
    }
}
