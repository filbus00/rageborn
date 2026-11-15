using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// The Iron Gaol's art (2026-11-14): ArtSource/pixel/sixteen/gaol_art.py writes its pieces straight into
    /// Resources/Art (as forest_art.py does) and lists them in <see cref="ListPath"/>, one "Folder/name width height
    /// pivotX pivotY" a line, the pivot in pixels from the left and the bottom. This sets each listed sprite's import
    /// settings: 27 pixels a unit (<see cref="PixelArtImporter.RenderScaleUnit"/>), point filtered, uncompressed, its
    /// pivot from the list; the Dungeon pieces readable (props and floor marks are combined at load), and the floor
    /// picture a readable texture like the forest's ground. Run it after the script whenever a piece is new or its size
    /// changed.
    /// </summary>
    public static class GaolArtImport
    {
        public const string ListPath = "ArtSource/pixel/sixteen/gaol_art_sizes.txt";
        const string Root = "Assets/_Project/Resources/Art/";
        const string FloorPath = Root + "Ground/gaol_floor.png";

        [MenuItem("Tools/ARPG/Configure Gaol Art")]
        public static void Configure()
        {
            if (!File.Exists(ListPath))
            {
                Debug.LogWarning($"[ARPG] {ListPath} is missing: run python3 ArtSource/pixel/sixteen/gaol_art.py first.");
                return;
            }
            AssetDatabase.Refresh();
            var count = 0;
            foreach (var line in File.ReadAllLines(ListPath))
            {
                var parts = line.Trim().Split(' ');
                if (parts.Length < 5)
                    continue;
                var asset = Root + parts[0] + ".png";
                var importer = AssetImporter.GetAtPath(asset) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogWarning($"[ARPG] Gaol art: {asset} is not there.");
                    continue;
                }
                float width = float.Parse(parts[1], CultureInfo.InvariantCulture), height = float.Parse(parts[2], CultureInfo.InvariantCulture);
                var pivot = new Vector2(float.Parse(parts[3], CultureInfo.InvariantCulture) / width, float.Parse(parts[4], CultureInfo.InvariantCulture) / height);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PixelArtImporter.RenderScaleUnit;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.isReadable = parts[0].StartsWith("Dungeon/");
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
                count++;
            }

            // The floor: a plain readable texture, as the forest's grass and path.
            var floor = AssetImporter.GetAtPath(FloorPath) as TextureImporter;
            if (floor != null)
            {
                floor.textureType = TextureImporterType.Default;
                floor.isReadable = true;
                floor.filterMode = FilterMode.Point;
                floor.textureCompression = TextureImporterCompression.Uncompressed;
                floor.mipmapEnabled = false;
                floor.npotScale = TextureImporterNPOTScale.None;
                floor.wrapMode = TextureWrapMode.Repeat;
                floor.SaveAndReimport();
            }
            Debug.Log($"[ARPG] Gaol art: {count} pieces set to {PixelArtImporter.RenderScaleUnit} a unit{(floor != null ? ", the floor readable" : ", no floor picture")}.");
        }
    }
}
