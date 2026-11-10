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

        /// <summary>
        /// Art drawn at the game's rendered scale (2026-11-04, the owner: "redraw the art at the new scale"): 27 pixels a
        /// unit, as the world is rendered since the camera moved back (<see cref="RenderResolution.RenderedUnit"/> on the
        /// iPhone 11 and on 3x phones), so one of its pixels is one pixel on screen. Taken as drawn: no shrinking, no
        /// palette snap or outline (the drawing has its own), only hard alpha; it wins over a piece of the same name in
        /// <see cref="SourceFolder"/>. Made by the scripts in ArtSource/tools/pixel (Aseprite).
        /// </summary>
        public const string RenderScaleFolder = "ArtSource/pixel/dungeon27";

        /// <summary>The rendered scale's pixels a unit.</summary>
        public const int RenderScaleUnit = 27;
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
            // The forest's pieces drawn by forest_art.py are left as they are: not overwritten by an older source of the
            // same name, not removed (2026-11-10).
            var keep = DrawnElsewhere();
            foreach (var name in keep)
                written.Add($"{OutputFolder}/{name}.png");
            if (keep.Count > 0)
                report.AppendLine($"KEPT {keep.Count} pieces drawn by {DrawnListPath}: {string.Join(", ", keep)}").AppendLine();

            foreach (var path in Directory.GetFiles(SourceFolder, "*.png"))
            {
                var name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                if (keep.Contains(name))
                    continue;
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
                if (Doubled.Contains(name))
                {
                    pixels = DoubleUp(pixels, width, height, out width, out height);
                    notes.Add("doubled");
                }

                var output = $"{OutputFolder}/{name}.png";
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(output, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                written.Add(output);
                counts[(int)kind]++;

                report.Append($"{kind.ToString().PadRight(8)} {name}: {width} x {height}");
                if (notes.Count > 0)
                    report.Append("  (").Append(string.Join("; ", notes)).Append(')');
                if (notes.Exists(n => n.StartsWith("WARNING")))
                    problems++;
                report.AppendLine();
            }

            // Art drawn at the rendered scale, taken as drawn.
            var atRenderScale = new HashSet<string>();
            Directory.CreateDirectory(RenderScaleFolder);
            foreach (var path in Directory.GetFiles(RenderScaleFolder, "*.png"))
            {
                var name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                if (keep.Contains(name))
                    continue;
                if (ArtNames.Parse(name, out _, out _) == ArtKind.Unknown)
                {
                    report.AppendLine($"SKIPPED {Path.GetFileName(path)} (at the rendered scale): not a name the game reads.");
                    problems++;
                    continue;
                }
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(File.ReadAllBytes(path)))
                {
                    report.AppendLine($"SKIPPED {Path.GetFileName(path)} (at the rendered scale): not a readable PNG.");
                    problems++;
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
                report.AppendLine($"RENDERED {name}: {texture.width} x {texture.height} at {RenderScaleUnit} a unit");
                Object.DestroyImmediate(texture);
                written.Add(output);
                atRenderScale.Add(output);
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
            {
                // The kept pieces keep their own import settings too (the stumps' foot points were set by hand).
                if (keep.Contains(Path.GetFileNameWithoutExtension(asset)))
                    continue;
                Configure(asset, atRenderScale.Contains(asset) ? RenderScaleUnit : PixelArt.PixelsPerUnit);
            }

            report.AppendLine();
            report.AppendLine($"Imported {written.Count - keep.Count} (kept {keep.Count}): {counts[(int)ArtKind.Floor]} floors, {counts[(int)ArtKind.Decal]} decals, " +
                $"{counts[(int)ArtKind.Wall] + counts[(int)ArtKind.LowWall]} walls, {counts[(int)ArtKind.Prop]} props. Removed {removed}.");
            report.AppendLine($"Names the game reads: {ArtNames.Accepted()}");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, report.ToString());
            var summary = $"Pixel art: imported {written.Count - keep.Count}, kept {keep.Count}, removed {removed}, {problems} to check. See {ReportPath}.";
            if (problems > 0)
                Debug.LogWarning(summary + "\n" + report);
            else
                Debug.Log(summary);
        }

        /// <summary>
        /// Props shown twice as big as they come out (2026-11-10, the owner: the deer skull and the ribcage "are small and
        /// need to be bigger"; "update the import so the bones stay big"): their sources draw them a game pixel a pixel,
        /// finer than the 16x16 style's two.
        /// </summary>
        public static readonly HashSet<string> Doubled = new HashSet<string> { "prop_bone_pile_1", "prop_bone_pile_2" };

        /// <summary>
        /// A prop's drawing (<paramref name="width"/> by <paramref name="height"/>, rows from the bottom) cropped to what
        /// is drawn, each pixel made 2 x 2, centred across a canvas at least a cell wide and standing 6 pixels up, so
        /// it sits on the cell (the pivot is 10 up). Empty art comes back as it was.
        /// </summary>
        public static Color32[] DoubleUp(Color32[] pixels, int width, int height, out int newWidth, out int newHeight)
        {
            int x0 = width, y0 = height, x1 = -1, y1 = -1;
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                    if (pixels[y * width + x].a >= 128)
                    {
                        x0 = Mathf.Min(x0, x);
                        x1 = Mathf.Max(x1, x);
                        y0 = Mathf.Min(y0, y);
                        y1 = Mathf.Max(y1, y);
                    }
            if (x1 < 0)
            {
                newWidth = width;
                newHeight = height;
                return pixels;
            }
            int bw = (x1 - x0 + 1) * 2, bh = (y1 - y0 + 1) * 2;
            newWidth = Mathf.Max(ArtNames.Width, bw + 4);
            newWidth += newWidth % 2;
            newHeight = bh + 8;
            var output = new Color32[newWidth * newHeight];
            var left = (newWidth - bw) / 2;
            for (var y = 0; y < bh; y++)
                for (var x = 0; x < bw; x++)
                {
                    var c = pixels[(y0 + y / 2) * width + x0 + x / 2];
                    c.a = c.a >= 128 ? (byte)255 : (byte)0;
                    output[(6 + y) * newWidth + left + x] = c;
                }
            return output;
        }

        /// <summary>The list of pieces forest_art.py writes, one "Folder/name width height" a line.</summary>
        public const string DrawnListPath = "ArtSource/pixel/sixteen/forest_art_sizes.txt";

        /// <summary>
        /// The pieces in <see cref="OutputFolder"/> drawn by ArtSource/pixel/sixteen/forest_art.py (its Dungeon lines in
        /// <see cref="DrawnListPath"/>): the forest's 16x16 props and floor marks. Older sources of the same names sit in
        /// <see cref="SourceFolder"/> and <see cref="RenderScaleFolder"/>, so an import would have put them back over the
        /// forest's (found 2026-11-10). Empty when the list is missing.
        /// </summary>
        public static HashSet<string> DrawnElsewhere()
        {
            var names = new HashSet<string>();
            if (!File.Exists(DrawnListPath))
                return names;
            foreach (var line in File.ReadAllLines(DrawnListPath))
            {
                var entry = line.Trim().Split(' ')[0];
                if (entry.StartsWith("Dungeon/"))
                    names.Add(entry.Substring("Dungeon/".Length).ToLowerInvariant());
            }
            return names;
        }

        /// <summary>The processed pixels of one source image, and their size.</summary>
        static Color32[] Convert(Texture2D source, ArtKind kind, List<string> notes, out int width, out int height)
        {
            int sw = source.width, sh = source.height;
            var src = source.GetPixels32();
            width = ArtNames.Width;
            // A prop drawn at 4x may be several cells wide (2026-10-27, the owner: "the tents are comically small"): a
            // source a whole multiple of 4 cells across keeps its width.
            if (kind == ArtKind.Prop && sw > width * 4 && sw % (width * 4) == 0)
                width = sw / 4;
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
                if (height > ArtNames.Width * 3)
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
        static void Configure(string asset, int unit)
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
            importer.isReadable = true;
            importer.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            importer.GetSourceTextureWidthAndHeight(out _, out var height);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            // The cell's middle a quarter unit up from the bottom, at either scale.
            settings.spritePivot = new Vector2(0.5f, unit / 4f / Mathf.Max(1, height));
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
