using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// Remembers what each baked sheet was made from, so a bake renders only the sheets whose inputs changed (2026-10-11,
    /// the owner: "why does rebaking take so long"; a full Wild Arrow bake was over 30 minutes, nearly all of it sheets
    /// that came out the same). A sheet's key is a hash of: the bake code (every script in this folder and the depth
    /// shader), the job's settings, the first body (it sets the scale, holds the pieces and hides what is behind them),
    /// the layer's own model or piece, and the clip, each with every file it depends on (meshes, textures, materials,
    /// import settings). The bake code counts by its version (<see cref="SpriteBaker.BakeVersion"/>), not its text. A sheet is skipped when its key is unchanged and the files it wrote are still there. A new
    /// clip has no key yet, so adding an animation bakes only it.
    /// Kept in Library/SpriteBakeCache (per machine, not committed): a fresh checkout bakes everything once.
    /// </summary>
    public sealed class SpriteBakeCache
    {
        [Serializable]
        sealed class Entry
        {
            public string sheet;
            public string key;
            public List<string> files = new List<string>();
        }

        [Serializable]
        sealed class Store
        {
            public List<Entry> entries = new List<Entry>();
        }

        const string Folder = "Library/SpriteBakeCache";

        readonly string path;
        readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
        readonly Dictionary<string, string> fileHashes = new Dictionary<string, string>();
        readonly string baseKey;

        public SpriteBakeCache(SpriteBakeJob job)
        {
            path = $"{Folder}/{job.characterName}.json";
            if (File.Exists(path))
                foreach (var entry in JsonUtility.FromJson<Store>(File.ReadAllText(path)).entries)
                    entries[entry.sheet] = entry;
            baseKey = CodeVersion() + "|" + Settings(job) + "|" + Body(job.bodies[0]);
        }

        /// <summary>The key of one sheet: the base, the layer (its body or piece) and the clip.</summary>
        public string Key(SpriteBakeJob job, string grip, SpriteBakeJob.Clip clip, int bodyIndex, SpriteBakeJob.Piece piece)
        {
            var text = new StringBuilder(baseKey);
            text.Append('|').Append(grip);
            text.Append('|').Append(piece != null ? Piece(piece) : Body(job.bodies[bodyIndex]));
            text.Append('|').Append(Strip(JsonUtility.ToJson(clip)));
            text.Append('|').Append(Asset(clip.clip)).Append('|').Append(Asset(clip.legs));
            return Hash(text.ToString());
        }

        /// <summary>Whether a sheet with this key was baked before and its files are still there.</summary>
        public bool IsFresh(string sheet, string key) =>
            entries.TryGetValue(sheet, out var entry) && entry.key == key && entry.files.All(File.Exists);

        public void Record(string sheet, string key, IEnumerable<string> files) =>
            entries[sheet] = new Entry { sheet = sheet, key = key, files = files.ToList() };

        public void Save()
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(path, JsonUtility.ToJson(new Store { entries = entries.Values.OrderBy(e => e.sheet).ToList() }));
        }

        /// <summary>Forgets every sheet of a character, so the next bake renders all of them.</summary>
        public static void Clear(string characterName)
        {
            var file = $"{Folder}/{characterName}.json";
            if (File.Exists(file))
                File.Delete(file);
        }

        string Body(SpriteBakeJob.Body body) => Strip(JsonUtility.ToJson(body)) + "|" + Asset(body.model);

        string Piece(SpriteBakeJob.Piece piece) => Strip(JsonUtility.ToJson(piece)) + "|" + Asset(piece.prefab);

        // The job's own settings (cell size, supersampling, frame rate, directions, scale, output folder...), not its lists.
        static string Settings(SpriteBakeJob job)
        {
            var text = new StringBuilder();
            foreach (var field in typeof(SpriteBakeJob).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (typeof(System.Collections.IList).IsAssignableFrom(field.FieldType))
                    continue;
                // A setting added later counts only when it is used, so its default leaves the sheets already baked
                // fresh (onlyRow, the portrait's one row, 2026-10-12).
                if (field.Name == nameof(SpriteBakeJob.onlyRow) && job.onlyRow < 0)
                    continue;
                text.Append(field.Name).Append('=').Append(Convert.ToString(field.GetValue(job), System.Globalization.CultureInfo.InvariantCulture)).Append(';');
            }
            return text.ToString();
        }

        // An asset and everything it depends on, by content: its path and name, and every dependency's file and import
        // settings.
        string Asset(UnityEngine.Object asset)
        {
            if (asset == null)
                return "none";
            var assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath))
                return asset.name;
            var text = new StringBuilder(assetPath).Append(':').Append(asset.name);
            foreach (var dependency in AssetDatabase.GetDependencies(assetPath, true).OrderBy(p => p, StringComparer.Ordinal))
                text.Append('|').Append(FileHash(dependency)).Append(FileHash(dependency + ".meta"));
            return text.ToString();
        }

        string FileHash(string file)
        {
            if (fileHashes.TryGetValue(file, out var known))
                return known;
            var hash = File.Exists(file) ? Hash(File.ReadAllBytes(file)) : "-";
            fileHashes[file] = hash;
            return hash;
        }

        // How sheets are drawn: the bake's version (SpriteBaker.BakeVersion, bumped by hand when a change alters the pixels)
        // and the depth shader. Until 2026-10-12 a hash of every bake script, which redrew everything on any edit.
        static string CodeVersion()
        {
            var text = new StringBuilder("v").Append(SpriteBaker.BakeVersion);
            foreach (var guid in AssetDatabase.FindAssets("DepthOnly t:Shader"))
            {
                var shader = AssetDatabase.GUIDToAssetPath(guid);
                text.Append(Hash(File.ReadAllBytes(shader)));
            }
            return Hash(text.ToString());
        }

        // Object references serialize as instance ids, which change between sessions; the assets are hashed apart.
        static string Strip(string json) => Regex.Replace(json, "\"instanceID\":-?\\d+", "\"instanceID\":0");

        static string Hash(string text) => Hash(Encoding.UTF8.GetBytes(text));

        static string Hash(byte[] bytes)
        {
            using var md5 = MD5.Create();
            return BitConverter.ToString(md5.ComputeHash(bytes)).Replace("-", "");
        }
    }
}
