using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// The menus' pixel art (Resources/UI/Pixel, 2026-11-05: ArtSource/tools/ui/menu_art.lua, pixel_fonts.lua and the
    /// portraits cut from the owner's concept pictures) imports as crisp sprites at one pixel a canvas unit: point
    /// filtered, uncompressed, no mipmaps, full-rect meshes; a frame's slicing border comes from its name (frame_panel_b4).
    /// <see cref="BuildFonts"/> makes the Unity fonts of the two font atlases.
    /// </summary>
    public class PixelUiImport : AssetPostprocessor
    {
        const string Folder = "Resources/UI/Pixel/";
        const string Source = "ArtSource/ui/pixel";
        const string Target = "Assets/_Project/Resources/UI/Pixel";

        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains(Folder))
                return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            var border = Regex.Match(Path.GetFileNameWithoutExtension(assetPath), @"_b(\d+)$");
            var b = border.Success ? int.Parse(border.Groups[1].Value) : 0;
            importer.spriteBorder = new Vector4(b, b, b, b);
        }

        /// <summary>Copies the drawn menu art from ArtSource/ui/pixel into Resources/UI/Pixel and builds the fonts.</summary>
        [MenuItem("Tools/ARPG/Import Menu Art")]
        public static void Import()
        {
            Directory.CreateDirectory(Target);
            foreach (var path in Directory.GetFiles(Source))
            {
                var ext = Path.GetExtension(path);
                if (ext != ".png" && ext != ".json")
                    continue;
                File.Copy(path, Path.Combine(Target, Path.GetFileName(path)), true);
            }
            AssetDatabase.Refresh();
            BuildFonts();
        }

        /// <summary>
        /// The pixel fonts (UI/Pixel/Fonts): each atlas in its sizes, a glyph pixel this many canvas units, as custom
        /// fonts whose characters are the atlas's glyphs, their tops at the ascent.
        /// </summary>
        public static void BuildFonts()
        {
            Directory.CreateDirectory(Target + "/Fonts");
            foreach (var (atlas, scale, name) in new[] { ("title", 1, "title"), ("title", 2, "title_x2"), ("body", 1, "body") })
                BuildFont(atlas, scale, name);
            AssetDatabase.SaveAssets();
            Debug.Log("[ARPG] Pixel fonts built.");
        }

        [System.Serializable]
        class FontData
        {
            public int cell, ascent, space, atlasHeight;
            public Glyph[] glyphs;
        }

        [System.Serializable]
        class Glyph
        {
            public int code, x, y, w, advance;
        }

        static void BuildFont(string atlasName, int s, string name)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Target}/font_{atlasName}.png");
            var data = JsonUtility.FromJson<FontData>(File.ReadAllText($"{Target}/font_{atlasName}.json"));
            float W = texture.width, H = texture.height;
            var characters = new List<CharacterInfo>();
            foreach (var g in data.glyphs)
            {
                // The atlas counts rows from the top; UVs from the bottom.
                float u0 = g.x / W, u1 = (g.x + g.w) / W, v1 = 1f - g.y / H, v0 = 1f - (g.y + data.cell) / H;
                characters.Add(new CharacterInfo
                {
                    index = g.code,
                    uvBottomLeft = new Vector2(u0, v0),
                    uvBottomRight = new Vector2(u1, v0),
                    uvTopLeft = new Vector2(u0, v1),
                    uvTopRight = new Vector2(u1, v1),
                    minX = 0,
                    maxX = g.w * s,
                    minY = (data.ascent - data.cell) * s,
                    maxY = data.ascent * s,
                    advance = g.advance * s,
                });
            }
            characters.Add(new CharacterInfo { index = ' ', advance = data.space * s });
            var materialPath = $"{Target}/Fonts/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("UI/Default"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.mainTexture = texture;
            EditorUtility.SetDirty(material);
            var fontPath = $"{Target}/Fonts/{name}.fontsettings";
            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null)
            {
                font = new Font(name);
                AssetDatabase.CreateAsset(font, fontPath);
            }
            font.material = material;
            font.characterInfo = characters.ToArray();
            var serialized = new SerializedObject(font);
            serialized.FindProperty("m_FontSize").floatValue = data.cell * s;
            serialized.FindProperty("m_LineSpacing").floatValue = (data.cell + (atlasName == "title" ? 4 : 3)) * s;
            serialized.FindProperty("m_Ascent").floatValue = data.ascent * s;
            var descent = serialized.FindProperty("m_Descent");
            if (descent != null)
                descent.floatValue = (data.ascent - data.cell) * s;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(font);
        }
    }
}
