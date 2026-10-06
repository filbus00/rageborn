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
            { "husk", 70f }, { "ghoul", 88f }, { "bandit_archer", 80f }, { "ash_wolf", 60f }, { "cinder_warden", 92f }, { "skeleton", 72f }, { "cultist", 82f },
            // The rest of act 1 (2026-10-05). The pyre keeper is measured with its brazier pole, which stands well over
            // its head, so it asks more to stand as tall as a cultist.
            { "cutthroat", 74f }, { "ember_acolyte", 80f }, { "pyre_keeper", 104f }, { "carrion_bloat", 84f },
            // The deep levels and their bosses (2026-10-05); staffs and halos count in the measured box.
            { "drowned", 72f }, { "harpooner", 96f }, { "drowned_watchman", 92f }, { "skeleton_knight", 88f },
            { "grave_priest", 96f }, { "hollowed", 72f }, { "void_wraith", 76f }, { "rift_caller", 98f },
            { "tidewife", 100f }, { "saint_marrow", 112f }, { "first_watchman", 100f },
            // The pets (2026-10-06, ArtSource/tools/props/pets.py): the wolf a little over the Ash Wolf (seen from behind it is narrow), the raven measured with
            // its wings spread.
            { "pet_wolf", 68f }, { "pet_boar", 58f }, { "pet_raven", 34f },
        };

        /// <summary>Cells wider than the default 128 px: the ghoul's slam swings its big arm past a 128 px cell (clipped on
        /// the first bake, 2026-09-29).</summary>
        static readonly Dictionary<string, int> CellSizes = new Dictionary<string, int> { { "ghoul", 192 }, { "cinder_warden", 192 }, { "carrion_bloat", 192 },
            { "drowned_watchman", 192 }, { "skeleton_knight", 192 }, { "tidewife", 192 }, { "saint_marrow", 192 }, { "first_watchman", 192 } };

        /// <summary>Bodies whose clips are copies of another enemy's (the Cinder Warden moves as the ghoul).</summary>
        static readonly Dictionary<string, string> ClipAvatarFrom = new Dictionary<string, string>
        {
            { "cinder_warden", "ghoul" }, { "skeleton", "husk" }, { "cultist", "bandit_archer" },
            { "cutthroat", "husk" }, { "ember_acolyte", "bandit_archer" }, { "pyre_keeper", "bandit_archer" }, { "carrion_bloat", "ghoul" },
            { "drowned", "husk" }, { "harpooner", "bandit_archer" }, { "drowned_watchman", "ghoul" }, { "skeleton_knight", "ghoul" },
            { "grave_priest", "bandit_archer" }, { "hollowed", "husk" }, { "void_wraith", "husk" }, { "rift_caller", "bandit_archer" },
            { "tidewife", "ghoul" }, { "saint_marrow", "ghoul" }, { "first_watchman", "ghoul" },
        };

        /// <summary>The definitions each baked look is wired to when it exists: base looks to every rank of the type
        /// until the rank has its own.</summary>
        static readonly (string definition, string character, string fallback)[] Wiring =
        {
            ("Swarmer", "husk", null), ("SwarmerChampion", "husk_champion", "husk"), ("SwarmerElite", "husk_elite", "husk"),
            ("Ghoul", "ghoul", null), ("BanditArcher", "bandit_archer", null),
            // The act boss (2026-10-04): built in Blender on the ghoul's rig, ArtSource/tools/props/boss.py.
            ("CinderWarden", "cinder_warden", null),
            // Act 1's undead (2026-10-04, ArtSource/tools/props/undead.py): the skeleton on the husk's rig, the cultist on
            // the archer's.
            ("Skeleton", "skeleton", null), ("Cultist", "cultist", null),
            // The rest of act 1 (2026-10-05): undead.py builds four on existing rigs, wolf.py the wolf on a rig of its own.
            ("AshWolf", "ash_wolf", null), ("Cutthroat", "cutthroat", null), ("EmberAcolyte", "ember_acolyte", null),
            ("PyreKeeper", "pyre_keeper", null), ("CarrionBloat", "carrion_bloat", null),
            // The deep levels and their bosses (2026-10-05, undead.py).
            ("Drowned", "drowned", null), ("Harpooner", "harpooner", null), ("DrownedWatchman", "drowned_watchman", null),
            ("SkeletonKnight", "skeleton_knight", null), ("GravePriest", "grave_priest", null), ("Hollowed", "hollowed", null),
            ("VoidWraith", "void_wraith", null), ("RiftCaller", "rift_caller", null),
            ("Tidewife", "tidewife", null), ("SaintMarrow", "saint_marrow", null), ("FirstWatchman", "first_watchman", null),
        };

        /// <summary>Bodies on a rig of their own (no humanoid): every clip is in the body's own file, named as the
        /// animation (wolf.py exports idle, run, attack, hit and death as takes).</summary>
        static readonly HashSet<string> GenericBodies = new HashSet<string> { "ash_wolf", "pet_wolf", "pet_boar", "pet_raven" };

        const string PetsRoot = "Assets/_Project/Art/Models/Pets";

        /// <summary>The pets (ArtSource/tools/props/pets.py), baked like the enemies on their own rigs, to
        /// Resources/Characters/pet_&lt;kind&gt;, which <see cref="PetController"/> plays.</summary>
        [MenuItem("Tools/ARPG/Sprite Bake/Bake Pets")]
        public static void BakePets()
        {
            var baked = new List<string>();
            foreach (var folder in Directory.GetDirectories(PetsRoot).Select(d => d.Replace('\\', '/')).OrderBy(d => d))
            {
                var job = SetUp(folder);
                if (job == null)
                    continue;
                SpriteBaker.Bake(job);
                baked.Add(job.characterName);
            }
            Debug.Log("Baked pets: " + string.Join(", ", baked));
        }

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
            if (GenericBodies.Contains(name))
                return SetUpGeneric(folder, name, bodyPath);
            // A body built on another enemy's rig keeps all its bones (Unity strips unweighted ones and the avatar then
            // fails; the skeleton knight, 2026-10-05). A bone map saved by a failed import stays stale: delete the FBX's
            // .meta to start it over.
            if (ClipAvatarFrom.ContainsKey(name))
                KeepAllBones(bodyPath);
            MixamoImport.ConfigureBody(bodyPath);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(bodyPath).OfType<Avatar>().FirstOrDefault();
            // A body built in Blender on another enemy's rig plays that enemy's clips: they keep that enemy's avatar
            // (their hierarchy is its), and the humanoid retargets them onto this body.
            if (!ClipAvatarFrom.TryGetValue(name, out var donor))
                donor = null;
            else
                avatar = AssetDatabase.LoadAllAssetsAtPath($"{Root}/{donor}/{donor}.fbx").OfType<Avatar>().FirstOrDefault() ?? avatar;

            var job = JobFor(folder, name, bodyPath);

            var set = new SpriteBakeJob.GripSet { grip = "" };
            foreach (var (animation, frames, loop) in Animations)
            {
                var path = $"{folder}/{name}_{animation}.fbx";
                var idleFrom = donor != null ? $"{Root}/{donor}/{donor}.fbx" : bodyPath;
                var clip = File.Exists(path)
                    ? MixamoImport.ConfigureAnimation(path, loop, avatar, animation, false)
                    : animation == "idle" ? MixamoImport.FirstClip(idleFrom) : null;
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

        static void KeepAllBones(string path)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            if (importer == null || !importer.optimizeBones)
                return;
            importer.optimizeBones = false;
            importer.SaveAndReimport();
        }

        /// <summary>The bake job for an enemy folder, its size and cell set from the tables above, its body loaded.</summary>
        static SpriteBakeJob JobFor(string folder, string name, string bodyPath)
        {
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
            // Pixel art at the game's 40 pixels a unit (2026-09-30), five eighths of the 64 of 2026-09-28: cells, pivot and
            // heights scale with it.
            const float scale = PixelArt.PixelsPerUnit / 64f;
            job.pixelsPerUnit = PixelArt.PixelsPerUnit;
            job.pixelArt = true;
            // A rank look is measured like its base type (husk_champion as husk); the engine scales ranks up itself.
            var baseName = Heights.Keys.FirstOrDefault(k => name == k || name.StartsWith(k + "_")) ?? name;
            // Drawn at its in-game size (the rank's or boss's VisualScale, 2026-10-04): stretched in the game, a 1.6 times
            // larger sprite had uneven pixels. EnemyAnimationSet reads the scale back from the timing file.
            var bakedScale = BakedScaleOf(name);
            job.bakedScale = bakedScale;
            job.cellSize = Mathf.RoundToInt((CellSizes.TryGetValue(baseName, out var cell) ? cell : 128) * scale * bakedScale);
            job.pivot = new Vector2(job.cellSize / 2f, Mathf.Round(20f * scale));
            // Measured on the rest pose's whole box, which the Wrathborn showed runs about 9 percent over the standing
            // height (185 asked, 170 stood), so ask that much more.
            job.targetHeightPixels = (Heights.TryGetValue(baseName, out var height) ? height : 80f) * 1.09f * scale * bakedScale;
            job.supersample = 4;
            job.directions = 8;
            job.bodies.Clear();
            job.bodies.Add(new SpriteBakeJob.Body { look = "", model = AssetDatabase.LoadAssetAtPath<GameObject>(bodyPath) });
            job.pieces.Clear();
            return job;
        }

        /// <summary>A body on its own generic rig, its clips in its own file (the Ash Wolf, ArtSource/tools/props/wolf.py).</summary>
        static SpriteBakeJob SetUpGeneric(string folder, string name, string bodyPath)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(bodyPath);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            var takes = importer.defaultClipAnimations;
            foreach (var take in takes)
            {
                var loop = Animations.Any(a => a.loop && TakeIs(take.name, a.name));
                take.loopTime = loop;
                take.name = Animations.Select(a => a.name).FirstOrDefault(a => TakeIs(take.name, a)) ?? take.name;
            }
            importer.clipAnimations = takes;
            importer.SaveAndReimport();
            MixamoImport.ApplyTexture(bodyPath, importer);

            var job = JobFor(folder, name, bodyPath);
            var clips = AssetDatabase.LoadAllAssetsAtPath(bodyPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToList();
            var set = new SpriteBakeJob.GripSet { grip = "" };
            foreach (var (animation, frames, loop) in Animations)
            {
                var clip = clips.FirstOrDefault(c => c.name == animation);
                if (clip == null)
                    continue;
                set.clips.Add(new SpriteBakeJob.Clip
                {
                    name = animation, clip = clip, frames = frames, loop = loop,
                    start = 0f, end = clip.length, playbackSeconds = clip.length,
                });
            }
            job.grips.Clear();
            job.grips.Add(set);
            EditorUtility.SetDirty(job);
            AssetDatabase.SaveAssets();
            Debug.Log($"{name} (generic): {string.Join(", ", set.clips.Select(c => c.name))}");
            return job;
        }

        // A take is named as its action ("idle") or with the armature in front ("Armature|idle").
        static bool TakeIs(string take, string animation) => take == animation || take.EndsWith("|" + animation);

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

        /// <summary>The VisualScale of the definition this look is wired to (1 when none): the size it is drawn at.</summary>
        static float BakedScaleOf(string character)
        {
            foreach (var (definitionName, wired, _) in Wiring)
            {
                if (wired != character)
                    continue;
                var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"Assets/_Project/Data/Enemies/{definitionName}.asset");
                if (definition != null)
                    return Mathf.Max(1f, definition.VisualScale);
            }
            return 1f;
        }

        static bool Baked(string character) =>
            File.Exists($"Assets/_Project/Resources/Characters/{character}/{character}_idle.png");
    }
}
