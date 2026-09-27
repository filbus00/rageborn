using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// Sets up the Wrathborn's sprite bake from the Mixamo files in Assets/_Project/Art/Models/Wrathborn
    /// (Docs/09-art-brief.md, 4.4 and 4.5): each body (<c>wrathborn_body_&lt;look&gt;.fbx</c>, downloaded with skin) gets a
    /// humanoid rig of its own; each animation file (<c>wrathborn_&lt;grip&gt;_&lt;animation&gt;.fbx</c>, without skin) gets a
    /// humanoid rig copied from the first body, its clip kept in place (root rotation, height and position baked into the
    /// pose) and looping where the brief says so. Then it writes the bake job: every body, the pieces in
    /// <see cref="Pieces"/>, and a clip set per grip with the brief's frame counts. The body file's own clip (the idle it
    /// was downloaded with) is used for that grip's idle. Idempotent; run it again after adding files.
    /// </summary>
    public static class WrathbornBakeSetup
    {
        const string Folder = "Assets/_Project/Art/Models/Wrathborn";
        const string JobPath = Folder + "/WrathbornJob.asset";
        const string Character = "wrathborn";

        // The brief's frame counts and loops (section 4.4); the idle has twice the brief's, since Mixamo's idles are long.
        static readonly (string name, int frames, bool loop)[] Animations =
        {
            ("idle", 24, true), ("run", 10, true), ("attack", 10, false), ("hew", 12, false), ("hurl_axe", 10, false),
            ("bull_rush", 8, true), ("ground_breaker", 14, false), ("hit", 4, false), ("death", 16, false),
        };

        static readonly string[] Grips = { "1h", "dual", "shield", "2h" };

        // The body the pieces hang on and whose rest pose sets the scale: the approved model sheet's look.
        const string ReferenceBody = "leather";

        /// <summary>Helms, weapons and off-hands, once their models exist (none yet: the axe is still to be made).</summary>
        static SpriteBakeJob.Piece[] Pieces => new SpriteBakeJob.Piece[0];

        [MenuItem("Tools/ARPG/Sprite Bake/Set Up Wrathborn")]
        public static SpriteBakeJob SetUp()
        {
            var bodies = Directory.GetFiles(Folder, $"{Character}_body_*.fbx")
                .Select(p => p.Replace('\\', '/'))
                .OrderBy(p => Look(p) == ReferenceBody ? 0 : 1).ThenBy(p => p)
                .ToList();
            if (bodies.Count == 0)
            {
                Debug.LogError($"No {Character}_body_<look>.fbx in {Folder}.");
                return null;
            }

            foreach (var body in bodies)
                ConfigureBody(body);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(bodies[0]).OfType<Avatar>().FirstOrDefault();

            var job = AssetDatabase.LoadAssetAtPath<SpriteBakeJob>(JobPath);
            if (job == null)
            {
                job = ScriptableObject.CreateInstance<SpriteBakeJob>();
                AssetDatabase.CreateAsset(job, JobPath);
            }
            job.characterName = Character;
            job.outputFolder = $"Assets/_Project/Resources/Characters/{Character}";
            job.cellSize = 256;
            job.pivot = new Vector2(128f, 40f);
            job.targetHeightPixels = 170f;
            job.supersample = 4;
            job.bodies.Clear();
            foreach (var body in bodies)
                job.bodies.Add(new SpriteBakeJob.Body { look = Look(body), model = AssetDatabase.LoadAssetAtPath<GameObject>(body) });
            job.pieces.Clear();
            job.pieces.AddRange(Pieces);

            job.grips.Clear();
            foreach (var grip in Grips)
            {
                var set = new SpriteBakeJob.GripSet { grip = grip };
                foreach (var (name, frames, loop) in Animations)
                {
                    var clip = ClipFor(grip, name, loop, avatar, bodies[0]);
                    // Played over the animation's real length: Mixamo clips run from 0.7 s (a run) to 10 s (an idle).
                    if (clip != null)
                        set.clips.Add(new SpriteBakeJob.Clip { name = name, clip = clip, frames = frames, loop = loop, playbackSeconds = clip.length });
                }
                if (set.clips.Count > 0)
                    job.grips.Add(set);
            }

            EditorUtility.SetDirty(job);
            AssetDatabase.SaveAssets();
            Debug.Log($"Wrathborn bake job: {job.bodies.Count} bod(ies) ({string.Join(", ", job.bodies.Select(b => b.look))}), " +
                      string.Join("; ", job.grips.Select(g => $"{g.grip}: {string.Join(", ", g.clips.Select(c => c.name))}")));
            return job;
        }

        static string Look(string bodyPath) => Path.GetFileNameWithoutExtension(bodyPath).Substring($"{Character}_body_".Length);

        static void ConfigureBody(string path)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            // The body's own clip is the idle it was downloaded with, which loops.
            KeepClipsInPlace(importer, true);
            importer.SaveAndReimport();

            // Mixamo's re-export does not carry the texture into Unity (the first bake came out white), so the texture
            // taken from the image-to-3D GLB (<body>_albedo.png, ArtSource/tools/extract_textures.py) goes on a material
            // of our own that replaces the model's. Mixamo keeps the mesh and its UVs, so the texture fits.
            var albedoPath = path.Replace(".fbx", "_albedo.png");
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            if (albedo == null)
            {
                Debug.LogWarning($"{path}: no {albedoPath}; the body bakes untextured.");
                return;
            }
            var materialPath = path.Replace(".fbx", ".mat");
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.SetTexture("_BaseMap", albedo);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(material);
            foreach (var source in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().Select(m => m.name).Distinct().ToList())
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), source), material);
            importer.SaveAndReimport();
        }

        // The animation file for a grip, or for the reference grip's idle the body's own clip.
        static AnimationClip ClipFor(string grip, string name, bool loop, Avatar avatar, string referenceBody)
        {
            var path = $"{Folder}/{Character}_{grip}_{name}.fbx";
            if (File.Exists(path))
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = avatar;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                KeepClipsInPlace(importer, loop);
                importer.SaveAndReimport();
                return FirstClip(path);
            }
            // The body was downloaded with its idle, which serves the one-handed grip until each grip has its own.
            if (name == "idle" && grip == "1h")
                return FirstClip(referenceBody);
            return null;
        }

        static void KeepClipsInPlace(ModelImporter importer, bool loop)
        {
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.loopTime = loop;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
            }
            importer.clipAnimations = clips;
        }

        static AnimationClip FirstClip(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));

        [MenuItem("Tools/ARPG/Sprite Bake/Bake Wrathborn")]
        public static void Bake()
        {
            var job = SetUp();
            if (job != null)
                SpriteBaker.Bake(job);
        }
    }
}
