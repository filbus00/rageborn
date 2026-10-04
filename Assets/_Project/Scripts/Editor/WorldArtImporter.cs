using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// `Tools > ARPG > Import World Art`: brings in the pieces larger than one cell that `ArtSource/tools/props`
    /// renders in Blender (stairs, chests, waypoints, the ritual circle, the town's buildings and dressing). Reads every
    /// PNG in <see cref="SourceFolder"/> with its size and pivot from manifest.json (render pixels, 4 times the game's),
    /// shrinks it by 4, makes the alpha hard, snaps it to the <see cref="PixelArt"/> palette, outlines it (not the flat
    /// pieces in <see cref="Flat"/>), and writes it to Resources/Art/World as a point-filtered sprite at 40 a unit with
    /// its pivot at the middle of its footprint. <see cref="WorldArt"/> loads them by name. Outputs whose source is gone
    /// are deleted. Writes a report to Logs/WorldArtImport.txt. Safe to run again.
    /// </summary>
    public static class WorldArtImporter
    {
        public const string SourceFolder = "ArtSource/pixel/world";
        public const string OutputFolder = "Assets/_Project/Resources/" + WorldArt.Folder;
        const string ReportPath = "Logs/WorldArtImport.txt";
        const int Shrink = 4;

        // Lying on the floor: no outline, which would draw a black ring around a painted circle.
        static readonly HashSet<string> Flat = new HashSet<string> { "ritual_circle" };

        [System.Serializable]
        class Item
        {
            public string name;
            public int width;
            public int height;
            public int pivotX;
            public int pivotY;
        }

        [System.Serializable]
        class Manifest
        {
            public List<Item> items = new List<Item>();
        }

        [MenuItem("Tools/ARPG/Import World Art")]
        public static void Import()
        {
            var report = new StringBuilder();
            report.AppendLine($"World art import, {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            Directory.CreateDirectory(OutputFolder);
            var manifestPath = Path.Combine(SourceFolder, "manifest.json");
            var manifest = File.Exists(manifestPath) ? JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath)) : new Manifest();
            var pivots = new Dictionary<string, Vector2>();
            var written = new HashSet<string>();

            foreach (var item in manifest.items)
            {
                var path = Path.Combine(SourceFolder, item.name + ".png");
                if (!File.Exists(path))
                {
                    report.AppendLine($"MISSING {item.name}.png");
                    continue;
                }
                var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                source.LoadImage(File.ReadAllBytes(path));
                int sw = source.width, sh = source.height;
                int w = sw / Shrink, h = sh / Shrink;
                var pixels = PixelArt.Downscale(source.GetPixels32(), sw, sh, w, h);
                Object.DestroyImmediate(source);
                PixelArt.Process(pixels, w, h, !Flat.Contains(item.name));

                var output = $"{OutputFolder}/{item.name}.png";
                var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(output, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                written.Add(output);
                pivots[output] = new Vector2(item.pivotX / (float)sw, item.pivotY / (float)sh);
                report.AppendLine($"{item.name}: {w} x {h}, pivot ({item.pivotX / Shrink}, {item.pivotY / Shrink})");
            }

            var removed = 0;
            foreach (var old in Directory.GetFiles(OutputFolder, "*.png"))
            {
                var asset = old.Replace('\\', '/');
                if (written.Contains(asset))
                    continue;
                AssetDatabase.DeleteAsset(asset);
                report.AppendLine($"REMOVED {Path.GetFileName(asset)}: no source any more.");
                removed++;
            }

            AssetDatabase.Refresh();
            foreach (var asset in written)
                Configure(asset, pivots[asset]);

            report.AppendLine($"Imported {written.Count}, removed {removed}.");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, report.ToString());
            Debug.Log($"World art: imported {written.Count}, removed {removed}. See {ReportPath}.");
        }

        static void Configure(string asset, Vector2 pivot)
        {
            var importer = AssetImporter.GetAtPath(asset) as TextureImporter;
            if (importer == null)
                return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelArt.PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            importer.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
