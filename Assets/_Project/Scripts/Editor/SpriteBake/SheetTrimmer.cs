using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// Crops every cell of a baked sheet to the part any of its frames draws in (<see cref="SpriteBakeMath.UsedArea"/>),
    /// keeping each sprite's name, id and feet, and makes its sprites plain rectangles (FullRect). The bake does it to
    /// every sheet it writes; the menu does it to sheets already baked. The Wild Arrow's sheets were 857 MB of the
    /// 1.4 GB app (2026-10-06): on iOS a transparent pixel costs as much as a drawn one, a helm or belt draws in a
    /// fraction of its 80 px cells, and Tight sprite outlines made 236 MB of sprite data. Running it twice changes
    /// nothing.
    /// </summary>
    public static class SheetTrimmer
    {
        const string CharactersFolder = "Assets/_Project/Resources/Characters";
        const int Margin = 1;

        [MenuItem("Tools/ARPG/Sprite Bake/Trim Character Sheets")]
        public static void TrimAll()
        {
            var paths = Directory.GetFiles(CharactersFolder, "*.png", SearchOption.AllDirectories)
                .Select(p => p.Replace('\\', '/')).OrderBy(p => p).ToArray();
            long before = 0, after = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                for (var i = 0; i < paths.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Trim Character Sheets", paths[i], (float)i / paths.Length);
                    var (from, to) = Trim(paths[i]);
                    before += from;
                    after += to;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }
            Debug.Log($"Trim Character Sheets: {paths.Length} sheets, {before / 1e6:0} to {after / 1e6:0} million pixels");
        }

        /// <summary>Crops one sheet and reimports it. Returns its pixel count before and after.</summary>
        public static (long before, long after) Trim(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null || importer.spriteImportMode != SpriteImportMode.Multiple)
                return (0, 0);
            var provider = Provider(importer);
            var rects = provider.GetSpriteRects();
            if (rects.Length == 0)
                return (0, 0);
            var cellWidth = (int)rects[0].rect.width;
            var cellHeight = (int)rects[0].rect.height;
            if (rects.Any(r => (int)r.rect.width != cellWidth || (int)r.rect.height != cellHeight ||
                               (int)r.rect.x % cellWidth != 0 || (int)r.rect.y % cellHeight != 0))
            {
                Debug.LogWarning($"Trim: {path} is not a grid of equal cells; left as it is");
                return (0, 0);
            }

            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(File.ReadAllBytes(path));
            var width = source.width;
            var height = source.height;
            var pixels = source.GetPixels32();
            Object.DestroyImmediate(source);
            var before = (long)width * height;
            var area = SpriteBakeMath.UsedArea(pixels, width, height, cellWidth, cellHeight, Margin);
            if (area.width == cellWidth && area.height == cellHeight)
            {
                // Nothing to crop; still make the sprites plain rectangles.
                if (MakeFullRect(importer))
                    importer.SaveAndReimport();
                return (before, before);
            }

            var columns = width / cellWidth;
            var rows = height / cellHeight;
            var outWidth = columns * area.width;
            var outHeight = rows * area.height;
            var result = new Color32[outWidth * outHeight];
            for (var row = 0; row < rows; row++)
                for (var column = 0; column < columns; column++)
                    for (var y = 0; y < area.height; y++)
                    {
                        var from = (row * cellHeight + area.y + y) * width + column * cellWidth + area.x;
                        var to = (row * area.height + y) * outWidth + column * area.width;
                        System.Array.Copy(pixels, from, result, to, area.width);
                    }

            // The rects first, then the pixels, then one import: each sprite moves to its cropped cell, keeping its name
            // and id, with its pivot where the feet were.
            foreach (var rect in rects)
            {
                var column = (int)rect.rect.x / cellWidth;
                var row = (int)rect.rect.y / cellHeight;
                rect.pivot = SpriteBakeMath.TrimmedPivot(rect.pivot, cellWidth, cellHeight, area);
                rect.alignment = SpriteAlignment.Custom;
                rect.rect = new Rect(column * area.width, row * area.height, area.width, area.height);
            }
            provider.SetSpriteRects(rects);
            provider.Apply();
            MakeFullRect(importer);

            var texture = new Texture2D(outWidth, outHeight, TextureFormat.RGBA32, false);
            texture.SetPixels32(result);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            importer.SaveAndReimport();
            return (before, (long)outWidth * outHeight);
        }

        // The sprite mesh lives in the importer's settings. Whether it changed.
        static bool MakeFullRect(TextureImporter importer)
        {
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (settings.spriteMeshType == SpriteMeshType.FullRect)
                return false;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            return true;
        }

        static ISpriteEditorDataProvider Provider(TextureImporter importer)
        {
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            return provider;
        }
    }
}
