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
    /// are deleted. Writes a report to Logs/WorldArtImport.txt. Safe to run again. Pieces drawn by hand at the rendered
    /// scale (2026-11-04, the trees first) go in <see cref="RenderScaleFolder"/>: taken as drawn at
    /// <see cref="PixelArtImporter.RenderScaleUnit"/> a unit (hard alpha only), standing a quarter unit up from their
    /// bottom like the props, and winning over a rendered piece of the same name.
    /// </summary>
    public static class WorldArtImporter
    {
        public const string SourceFolder = "ArtSource/pixel/world";
        public const string OutputFolder = "Assets/_Project/Resources/" + WorldArt.Folder;
        public const string RenderScaleFolder = "ArtSource/pixel/world27";
        const string ReportPath = "Logs/WorldArtImport.txt";
        const int Shrink = 4;

        // Lying on the floor: no outline.
        // Chasm cells (2026-10-20) join into one drop: an outline would draw a grid over it.
        // Steps too (2026-10-20): three cells make one flight, and outlined each read as broken.
        static readonly HashSet<string> Flat = new HashSet<string>
        {
            "crypt_chasm", "crypt_chasm_x", "crypt_chasm_y", "crypt_chasm_xy",
            "crypt_stairs_px", "crypt_stairs_nx", "crypt_stairs_py", "crypt_stairs_ny",
        };

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
            var atRenderScale = new HashSet<string>();
            Directory.CreateDirectory(RenderScaleFolder);
            foreach (var path in Directory.GetFiles(RenderScaleFolder, "*.png"))
                atRenderScale.Add(Path.GetFileNameWithoutExtension(path).ToLowerInvariant());

            foreach (var item in manifest.items)
            {
                if (atRenderScale.Contains(item.name))
                    continue;
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

            // Pieces drawn at the rendered scale, taken as drawn. Those listed in its pivots.json (arrows, thrown things,
            // particles, 2026-11-05) take their pivot from it, in pixels from the left and the bottom, and stay readable:
            // the game turns them a pixel at a time (TurnedArt).
            var unit = PixelArtImporter.RenderScaleUnit;
            var units = new Dictionary<string, int>();
            var ownPivots = new Dictionary<string, Vector2>();
            var pivotsPath = Path.Combine(RenderScaleFolder, "pivots.json");
            if (File.Exists(pivotsPath))
                foreach (var p in JsonUtility.FromJson<PivotList>(File.ReadAllText(pivotsPath)).items)
                    ownPivots[p.name.ToLowerInvariant()] = new Vector2(p.x, p.y);
            var readable = new HashSet<string>();
            foreach (var name in atRenderScale)
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(File.ReadAllBytes(Path.Combine(RenderScaleFolder, name + ".png"))))
                {
                    report.AppendLine($"SKIPPED {name}.png (at the rendered scale): not a readable PNG.");
                    Object.DestroyImmediate(texture);
                    continue;
                }
                var pixels = texture.GetPixels32();
                for (var i = 0; i < pixels.Length; i++)
                    pixels[i].a = pixels[i].a >= 128 ? (byte)255 : (byte)0;
                texture.SetPixels32(pixels);
                texture.Apply();
                var output = $"{OutputFolder}/{name}.png";
                File.WriteAllBytes(output, texture.EncodeToPNG());
                written.Add(output);
                if (ownPivots.TryGetValue(name, out var own))
                {
                    pivots[output] = new Vector2(own.x / texture.width, own.y / texture.height);
                    readable.Add(output);
                }
                else
                    pivots[output] = new Vector2(0.5f, unit / 4f / Mathf.Max(1, texture.height));
                units[output] = unit;
                report.AppendLine($"RENDERED {name}: {texture.width} x {texture.height} at {unit} a unit");
                Object.DestroyImmediate(texture);
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
                Configure(asset, pivots[asset], units.TryGetValue(asset, out var u) ? u : PixelArt.PixelsPerUnit, readable.Contains(asset));

            report.AppendLine($"Imported {written.Count}, removed {removed}.");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, report.ToString());
            Debug.Log($"World art: imported {written.Count}, removed {removed}. See {ReportPath}.");
        }

        [System.Serializable]
        class PivotList
        {
            public PivotEntry[] items = new PivotEntry[0];
        }

        [System.Serializable]
        class PivotEntry
        {
            public string name;
            public float x;
            public float y;
        }

        static void Configure(string asset, Vector2 pivot, int unit, bool readable = false)
        {
            var importer = AssetImporter.GetAtPath(asset) as TextureImporter;
            if (importer == null)
                return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = unit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = readable;
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
