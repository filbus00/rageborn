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

        /// <summary>Writes the texture as a PNG, imports it as a sprite and returns the sprite.</summary>
        public static Sprite ImportSprite(
            string path, Texture2D texture, int pixelsPerUnit, SpriteAlignment alignment, FilterMode filter)
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
