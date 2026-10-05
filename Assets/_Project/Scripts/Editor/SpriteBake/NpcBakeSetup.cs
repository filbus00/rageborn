using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// Bakes the town NPCs (the owner, 2026-10-03) from Mixamo files, one folder each in Assets/_Project/Art/Models/NPCs/
    /// &lt;name&gt;: <c>&lt;name&gt;.fbx</c>, downloaded with skin in its idle, and <c>&lt;name&gt;_albedo.png</c>, its texture.
    /// Each becomes an 8-direction idle in Resources/Characters/&lt;name&gt; at the Wild Arrow's size, which
    /// <see cref="NpcFigure"/> plays (merchant, pet_vendor, and the newcomers: stash_keeper, healer, gambler, trainer). Tools > ARPG > Sprite Bake > Bake NPCs.
    /// </summary>
    public static class NpcBakeSetup
    {
        const string Root = "Assets/_Project/Art/Models/NPCs";

        /// <summary>The newcomers in town (2026-10-05) idle in another NPC's clip.</summary>
        static readonly Dictionary<string, string> IdleFrom = new Dictionary<string, string>
        {
            { "stash_keeper", "merchant" }, { "healer", "merchant" }, { "gambler", "merchant" }, { "trainer", "merchant" },
        };

        [MenuItem("Tools/ARPG/Sprite Bake/Bake NPCs")]
        public static void BakeAll()
        {
            if (!Directory.Exists(Root))
            {
                Directory.CreateDirectory(Root);
                AssetDatabase.Refresh();
            }
            var baked = new List<string>();
            foreach (var folder in Directory.GetDirectories(Root).Select(d => d.Replace('\\', '/')).OrderBy(d => d))
            {
                var job = SetUp(folder);
                if (job == null)
                    continue;
                SpriteBaker.Bake(job);
                baked.Add(job.characterName);
            }
            Debug.Log(baked.Count == 0 ? $"No NPC models in {Root}/<name>/<name>.fbx yet." : "Baked NPCs: " + string.Join(", ", baked));
        }

        static SpriteBakeJob SetUp(string folder)
        {
            var name = Path.GetFileName(folder);
            var bodyPath = $"{folder}/{name}.fbx";
            if (!File.Exists(bodyPath))
            {
                Debug.LogWarning($"{folder}: no {name}.fbx (the model with skin, in its idle); skipped.");
                return null;
            }
            MixamoImport.ConfigureBody(bodyPath);
            // A body built in Blender on a borrowed rig (the newcomers, ArtSource/tools/props/undead.py) has no idle of
            // its own: it stands in the merchant's, retargeted by the humanoid.
            var clip = IdleFrom.TryGetValue(name, out var donor)
                ? MixamoImport.FirstClip($"{Root}/{donor}/{donor}.fbx")
                : MixamoImport.FirstClip(bodyPath);
            if (clip == null)
            {
                Debug.LogWarning($"{bodyPath}: no animation in it; download it from Mixamo in an idle.");
                return null;
            }

            var jobPath = $"{folder}/{name}_job.asset";
            var job = AssetDatabase.LoadAssetAtPath<SpriteBakeJob>(jobPath);
            if (job == null)
            {
                job = ScriptableObject.CreateInstance<SpriteBakeJob>();
                AssetDatabase.CreateAsset(job, jobPath);
            }
            job.characterName = name;
            job.outputFolder = $"Assets/_Project/Resources/Characters/{name}";
            // The Wild Arrow's numbers (WildArrowBakeSetup), so the townsfolk stand as tall as she does.
            const float scale = PixelArt.PixelsPerUnit / 64f;
            job.pixelsPerUnit = PixelArt.PixelsPerUnit;
            job.pixelArt = true;
            job.cellSize = Mathf.RoundToInt(128 * scale);
            job.pivot = new Vector2(job.cellSize / 2f, Mathf.Round(20f * scale));
            job.targetHeightPixels = 92.5f * scale;
            job.supersample = 4;
            job.directions = 8;
            job.bodies.Clear();
            job.bodies.Add(new SpriteBakeJob.Body { look = "", model = AssetDatabase.LoadAssetAtPath<GameObject>(bodyPath) });
            job.pieces.Clear();
            var set = new SpriteBakeJob.GripSet { grip = "" };
            set.clips.Add(new SpriteBakeJob.Clip
            {
                name = "idle", clip = clip, frames = 24, loop = true, start = 0f, end = clip.length, playbackSeconds = clip.length,
            });
            job.grips.Clear();
            job.grips.Add(set);
            EditorUtility.SetDirty(job);
            AssetDatabase.SaveAssets();
            return job;
        }
    }
}
