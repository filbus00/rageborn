using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// Bakes the enemies from Mixamo files (Docs/09-art-brief.md, 5.1 and 5.2, Route A), one folder each in
    /// Assets/_Project/Art/Models/Enemies/&lt;name&gt;: <c>&lt;name&gt;.fbx</c> (downloaded with skin; its own clip is the
    /// idle), <c>&lt;name&gt;_albedo.png</c> (its texture, when the re-export dropped it), and <c>&lt;name&gt;_run.fbx</c>,
    /// <c>_attack.fbx</c>, <c>_hit.fbx</c>, <c>_death.fbx</c> (without skin; <c>_idle.fbx</c> replaces the body's own
    /// clip if present). A rank look is its own folder (husk_champion). Each becomes a plain 8-direction character in
    /// Resources/Characters/&lt;name&gt;, at the brief's size for it, and the enemy definitions that name it
    /// (<see cref="EnemyDefinition.SpriteCharacter"/>) animate from it; see <see cref="EnemyAnimationSet"/>.
    /// </summary>
    public static class EnemyBakeSetup
    {
        const string Root = "Assets/_Project/Art/Models/Enemies";

        // The brief's frames and loops (5.1). The attack has room for a readable wind-up; the death is not held (the
        // engine burns the body away).
        static readonly (string name, int frames, bool loop)[] Animations =
        {
            ("idle", 12, true), ("run", 10, true), ("attack", 12, false), ("hit", 4, false), ("death", 12, false),
        };

        /// <summary>Heights in final pixels at 64 per unit (the brief, 5.2 and 15.3, halved on 2026-09-28); others 80.</summary>
        static readonly Dictionary<string, float> Heights = new Dictionary<string, float>
        {
            { "husk", 70f }, { "ghoul", 88f }, { "bandit_archer", 80f }, { "ash_wolf", 45f },
        };

        /// <summary>Cells wider than the default 128 px: the ghoul's slam swings its big arm past a 128 px cell (clipped on
        /// the first bake, 2026-09-29).</summary>
        static readonly Dictionary<string, int> CellSizes = new Dictionary<string, int> { { "ghoul", 192 } };

        /// <summary>The definitions each baked look is wired to when it exists: base looks to every rank of the type
        /// until the rank has its own.</summary>
        static readonly (string definition, string character, string fallback)[] Wiring =
        {
            ("Swarmer", "husk", null), ("SwarmerChampion", "husk_champion", "husk"), ("SwarmerElite", "husk_elite", "husk"),
            ("Ghoul", "ghoul", null), ("BanditArcher", "bandit_archer", null),
        };

        [MenuItem("Tools/ARPG/Sprite Bake/Bake Enemies")]
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
            WireDefinitions();
            Debug.Log(baked.Count == 0
                ? $"No enemy models in {Root}/<name>/<name>.fbx yet."
                : "Baked enemies: " + string.Join(", ", baked));
        }

        /// <summary>Writes the bake job for one enemy folder, or null when it holds no body.</summary>
        public static SpriteBakeJob SetUp(string folder)
        {
            var name = Path.GetFileName(folder);
            var bodyPath = $"{folder}/{name}.fbx";
            if (!File.Exists(bodyPath))
            {
                Debug.LogWarning($"{folder}: no {name}.fbx (the model with skin); skipped.");
                return null;
            }
            MixamoImport.ConfigureBody(bodyPath);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(bodyPath).OfType<Avatar>().FirstOrDefault();

            var jobPath = $"{folder}/{name}_job.asset";
            var job = AssetDatabase.LoadAssetAtPath<SpriteBakeJob>(jobPath);
            if (job == null)
            {
                job = ScriptableObject.CreateInstance<SpriteBakeJob>();
                AssetDatabase.CreateAsset(job, jobPath);
            }
            job.characterName = name;
            job.outputFolder = $"Assets/_Project/Resources/Characters/{name}";
            // The resolution of 2026-09-28 (09, 0.8): 64 pixels a unit, half the brief's first sizes.
            job.pixelsPerUnit = 64f;
            // A rank look is measured like its base type (husk_champion as husk); the engine scales ranks up itself.
            var baseName = Heights.Keys.FirstOrDefault(k => name == k || name.StartsWith(k + "_")) ?? name;
            job.cellSize = CellSizes.TryGetValue(baseName, out var cell) ? cell : 128;
            job.pivot = new Vector2(job.cellSize / 2f, 20f);
            // Measured on the rest pose's whole box, which the Wrathborn showed runs about 9 percent over the standing
            // height (185 asked, 170 stood), so ask that much more.
            job.targetHeightPixels = (Heights.TryGetValue(baseName, out var height) ? height : 80f) * 1.09f;
            job.supersample = 4;
            job.directions = 8;
            job.bodies.Clear();
            job.bodies.Add(new SpriteBakeJob.Body { look = "", model = AssetDatabase.LoadAssetAtPath<GameObject>(bodyPath) });
            job.pieces.Clear();

            var set = new SpriteBakeJob.GripSet { grip = "" };
            foreach (var (animation, frames, loop) in Animations)
            {
                var path = $"{folder}/{name}_{animation}.fbx";
                var clip = File.Exists(path)
                    ? MixamoImport.ConfigureAnimation(path, loop, avatar, animation, false)
                    : animation == "idle" ? MixamoImport.FirstClip(bodyPath) : null;
                if (clip == null)
                    continue;
                var window = loop ? new Vector2(0f, clip.length) : MixamoImport.ActionWindow(bodyPath, clip, animation == "death");
                set.clips.Add(new SpriteBakeJob.Clip
                {
                    name = animation, clip = clip, frames = frames, loop = loop,
                    start = window.x, end = window.y, playbackSeconds = window.y - window.x,
                });
            }
            job.grips.Clear();
            job.grips.Add(set);
            EditorUtility.SetDirty(job);
            AssetDatabase.SaveAssets();
            Debug.Log($"{name}: {string.Join(", ", set.clips.Select(c => c.name))}; missing: " +
                      string.Join(", ", Animations.Select(a => a.name).Where(a => set.clips.All(c => c.name != a))));
            return job;
        }

        /// <summary>Points each enemy definition at its baked look, or the base type's until the rank has its own.</summary>
        [MenuItem("Tools/ARPG/Sprite Bake/Wire Enemy Looks")]
        public static void WireDefinitions()
        {
            foreach (var (definitionName, character, fallback) in Wiring)
            {
                var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"Assets/_Project/Data/Enemies/{definitionName}.asset");
                if (definition == null)
                    continue;
                var chosen = Baked(character) ? character : fallback != null && Baked(fallback) ? fallback : "";
                if (definition.SpriteCharacter == chosen)
                    continue;
                definition.SetSpriteCharacter(chosen);
                EditorUtility.SetDirty(definition);
                Debug.Log($"{definitionName}: look '{chosen}'");
            }
            AssetDatabase.SaveAssets();
        }

        static bool Baked(string character) =>
            File.Exists($"Assets/_Project/Resources/Characters/{character}/{character}_idle.png");
    }
}
