using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// `Tools > ARPG > Import Pixel Art`: brings hand-made or generated dungeon art into the game (Docs/09, 0.9). Reads
    /// every PNG in <see cref="SourceFolder"/> (outside Assets, so Unity does not import the sources), named as
    /// <see cref="ArtNames"/> says, and for each: shrinks it to 40 pixels wide (art drawn at 2x, 4x or any size; a whole
    /// number shrink averages exact blocks), cuts a floor to its diamond, makes the alpha hard, snaps every colour to the
    /// <see cref="PixelArt"/> palette, outlines props, and writes it to Resources/Art/Dungeon as a point-filtered sprite at
    /// 40 a unit standing at (20, 10). <see cref="DungeonArt"/> then uses it in place of the code-drawn piece. Outputs
    /// whose source is gone are deleted (a Resources folder ships everything in it). Writes a report to
    /// Logs/PixelArtImport.txt. Safe to run again.
    /// </summary>
    public static class PixelArtImporter
    {
        public const string SourceFolder = "ArtSource/pixel/dungeon";
        public const string OutputFolder = "Assets/_Project/Resources/" + DungeonArt.ImportedFolder;
        const string ReportPath = "Logs/PixelArtImport.txt";

        [MenuItem("Tools/ARPG/Import Pixel Art")]
        public static void Import()
        {
            var report = new StringBuilder();
            report.AppendLine($"Pixel art import, {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            report.AppendLine($"From {SourceFolder} to {OutputFolder}");
            report.AppendLine();

            Directory.CreateDirectory(SourceFolder);
            Directory.CreateDirectory(OutputFolder);
            var written = new HashSet<string>();
            var counts = new int[System.Enum.GetValues(typeof(ArtKind)).Length];
            var problems = 0;

            foreach (var path in Directory.GetFiles(SourceFolder, "*.png"))
            {
                var name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                var kind = ArtNames.Parse(name, out _, out _);
                if (kind == ArtKind.Unknown)
                {
                    report.AppendLine($"SKIPPED {Path.GetFileName(path)}: not a name the game reads.");
                    problems++;
                    continue;
                }

                var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!source.LoadImage(File.ReadAllBytes(path)))
                {
                    report.AppendLine($"SKIPPED {Path.GetFileName(path)}: not a readable PNG.");
                    problems++;
                    Object.DestroyImmediate(source);
                    continue;
                }

                var notes = new List<string>();
                var pixels = Convert(source, kind, notes, out var width, out var height);
                Object.DestroyImmediate(source);

                var output = $"{OutputFolder}/{name}.png";
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(output, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                written.Add(output);
                counts[(int)kind]++;

                report.Append($"{kind,-8} {name}: {width} x {height}");
                if (notes.Count > 0)
                    report.Append("  (").Append(string.Join("; ", notes)).Append(')');
                if (notes.Exists(n => n.StartsWith("WARNING")))
                    problems++;
                report.AppendLine();
            }

            // Outputs whose source was removed or renamed.
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
                Configure(asset);

            report.AppendLine();
            report.AppendLine($"Imported {written.Count}: {counts[(int)ArtKind.Floor]} floors, {counts[(int)ArtKind.Decal]} decals, " +
                $"{counts[(int)ArtKind.Wall] + counts[(int)ArtKind.LowWall]} walls, {counts[(int)ArtKind.Prop]} props. Removed {removed}.");
            report.AppendLine($"Names the game reads: {ArtNames.Accepted()}");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, report.ToString());
            var summary = $"Pixel art: imported {written.Count}, removed {removed}, {problems} to check. See {ReportPath}.";
            if (problems > 0)
                Debug.LogWarning(summary + "\n" + report);
            else
                Debug.Log(summary);
        }

        /// <summary>The processed pixels of one source image, and their size.</summary>
        static Color32[] Convert(Texture2D source, ArtKind kind, List<string> notes, out int width, out int height)
        {
            int sw = source.width, sh = source.height;
            var src = source.GetPixels32();
            width = ArtNames.Width;
            var factor = sw / (float)width;
            if (Mathf.Abs(factor - Mathf.Round(factor)) > 0.001f)
                notes.Add($"{sw} px wide is not a whole multiple of {width}: shrunk by {factor:0.##}, edges may blur");
            else if (factor > 1f)
                notes.Add($"drawn at {factor:0}x");

            if (kind == ArtKind.Floor || kind == ArtKind.Decal)
            {
                height = ArtNames.FloorHeight;
                if (Mathf.Abs(sh * 2f - sw) > 1f)
                    notes.Add($"WARNING: {sw} x {sh} is not 2:1, stretched to {width} x {height}");
            }
            else
            {
                height = Mathf.Max(1, Mathf.RoundToInt(sh / factor));
                if (height < ArtNames.PivotY * 2)
                    notes.Add($"WARNING: only {height} px tall; the footprint alone is {ArtNames.PivotY * 2}");
                if (height > width * 3)
                    notes.Add($"WARNING: {height} px tall, more than 3 cells; check the size");
            }

            var pixels = sw == width && sh == height ? src : PixelArt.Downscale(src, sw, sh, width, height);
            var distance = PixelArt.PaletteDistance(pixels);
            if (distance > 25f)
                notes.Add($"WARNING: colours sit {distance:0} from the palette on average; expect a changed look");
            else if (distance > 0.5f)
                notes.Add($"palette distance {distance:0}");

            if (kind == ArtKind.Floor)
                PixelArt.CutDiamond(pixels, width);
            PixelArt.Process(pixels, width, height, kind == ArtKind.Prop);
            return pixels;
        }

        /// <summary>Import settings for one written sprite: 40 a unit, point filtered, uncompressed, readable (floors and
        /// decals are combined at load), standing on the cell's middle.</summary>
        static void Configure(string asset)
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
            importer.isReadable = true;
            importer.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            importer.GetSourceTextureWidthAndHeight(out _, out var height);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(0.5f, ArtNames.PivotY / (float)Mathf.Max(1, height));
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
