using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// Characters drawn as pixel art rather than baked from models (2026-11-05, the pet wolf from the owner's strip:
    /// ArtSource/tools/pets/strip_frame.lua and wolf_sheets.lua): each folder of ArtSource/pixel/characters holds a sheet
    /// a clip (<c>&lt;name&gt;_&lt;clip&gt;.png</c>, rows s, sw, w, nw, n, ne, e, se from the top, a column a frame) and
    /// sheet.json (the cell, the pivot, each clip's frames and seconds). Copied to Resources/Characters/&lt;name&gt; and
    /// sliced as the sprite bake slices (<c>&lt;name&gt;_&lt;clip&gt;_&lt;direction&gt;_&lt;frame&gt;</c>), at the screen's 27
    /// a unit, point filtered, with a timing file, so <see cref="EnemyAnimationSet"/> plays them as it plays a bake.
    /// Sheets of the character in Resources that the folder does not have are removed (a bake's dash, say).
    /// </summary>
    public static class DrawnCharacterImport
    {
        const string Source = "ArtSource/pixel/characters";
        const float PixelsPerUnit = 27f;
        static readonly string[] Directions = { "s", "sw", "w", "nw", "n", "ne", "e", "se" };

        [System.Serializable]
        class Sheet
        {
            public int cellWidth, cellHeight, pivotX, pivotY, directions;
            public Clip[] animations;

            /// <summary>The size the art is drawn at over the definition's (a boss drawn at its VisualScale, 2026-11-07), so
            /// the game does not stretch it; 0 when drawn at 1.</summary>
            public float bakedScale;
        }

        [System.Serializable]
        class Clip
        {
            public string name;
            public int frames;
            public float seconds;
            public float speed;

            /// <summary>The timing file's key when it is not the clip's name (the drawn Wild Arrow's body sheets,
            /// "body_ranger_1h_idle", time as her baked "1h_idle").</summary>
            public string key;
        }

        /// <summary>Whether a character is drawn here, so the sprite bake leaves it alone.</summary>
        public static bool IsDrawn(string name) => File.Exists($"{Source}/{name}/sheet.json");

        [MenuItem("Tools/ARPG/Import Drawn Characters")]
        public static void ImportAll()
        {
            if (!Directory.Exists(Source))
                return;
            foreach (var folder in Directory.GetDirectories(Source))
                Import(Path.GetFileName(folder));
            AssetDatabase.SaveAssets();
        }

        public static void Import(string name)
        {
            var json = $"{Source}/{name}/sheet.json";
            if (!File.Exists(json))
                return;
            var sheet = JsonUtility.FromJson<Sheet>(File.ReadAllText(json));
            var target = $"Assets/_Project/Resources/Characters/{name}";
            Directory.CreateDirectory(target);
            var written = new HashSet<string>();
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            var timing = new List<string>();
            var timed = new HashSet<string>();
            foreach (var clip in sheet.animations)
            {
                var path = $"{target}/{name}_{clip.name}.png";
                File.Copy($"{Source}/{name}/{name}_{clip.name}.png", path, true);
                written.Add(path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                Slice(path, name, clip, sheet);
                var key = string.IsNullOrEmpty(clip.key) ? clip.name : clip.key;
                if (timed.Add(key))
                    timing.Add($"{key} {clip.seconds.ToString("0.###", culture)}" + (clip.speed > 0f ? " " + clip.speed.ToString("0.###", culture) : ""));
            }
            if (sheet.bakedScale > 0f)
                timing.Add("scale " + sheet.bakedScale.ToString("0.###", culture));
            File.WriteAllLines($"{target}/{name}_timing.txt", timing);
            AssetDatabase.ImportAsset($"{target}/{name}_timing.txt");
            foreach (var old in Directory.GetFiles(target, "*.png").Select(p => p.Replace('\\', '/')))
                if (!written.Contains(old))
                {
                    AssetDatabase.DeleteAsset(old);
                    Debug.Log($"[ARPG] {name}: removed {Path.GetFileName(old)} (not drawn).");
                }
            Debug.Log($"[ARPG] {name}: {sheet.animations.Length} drawn sheets imported.");
        }

        static void Slice(string path, string name, Clip clip, Sheet sheet)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var existing = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
            var rows = Directions.Length;
            var pivot = new Vector2(sheet.pivotX / (float)sheet.cellWidth, sheet.pivotY / (float)sheet.cellHeight);
            var rects = new List<SpriteRect>();
            for (var row = 0; row < rows; row++)
                for (var frame = 0; frame < clip.frames; frame++)
                {
                    var spriteName = $"{name}_{clip.name}_{Directions[row]}_{frame:00}";
                    rects.Add(new SpriteRect
                    {
                        name = spriteName,
                        // Rows from the top in the sheet; Unity counts from the bottom.
                        rect = new Rect(frame * sheet.cellWidth, (rows - 1 - row) * sheet.cellHeight, sheet.cellWidth, sheet.cellHeight),
                        alignment = SpriteAlignment.Custom,
                        pivot = pivot,
                        spriteID = existing.TryGetValue(spriteName, out var id) ? id : GUID.Generate(),
                    });
                }
            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }
    }
}
