using UnityEditor;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// The inventory's item pictures (Resources/UI/ItemArt, ArtSource/tools/ui/import_item_art.py, 2026-10-19) import
    /// as crisp sprites: point filtered, uncompressed, no mipmaps, full-rect meshes (a traced outline fails on iOS for
    /// unreadable textures), 24 pixels a backpack cell.
    /// </summary>
    public class ItemArtImportSettings : AssetPostprocessor
    {
        const string Folder = "Resources/UI/ItemArt/";

        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains(Folder))
                return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 24f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
        }
    }
}
