using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// The Wild Arrow's stand-in bake (Docs/09-art-brief.md 4.6; the owner's choice of 2026-09-30: "wrathborn with the
    /// moveset of the bandit archer for now"): the Wrathborn's leather body posed with the bandit archer's Mixamo clips
    /// (Standing Idle from its skinned file, Run Forward, Draw Arrow, the hit and the death), and the Wrathborn's
    /// backward run and strafes as the legs under the moving shot. Both rigs are Mixamo humanoids, so the archer's clips
    /// play on the Wrathborn's body through the humanoid avatar; their import settings are left as the enemy bake set
    /// them. One grip ("1h", the bow's; <see cref="AppearanceRules.For"/>), no bow or quiver piece until one is modelled
    /// (the hand draws empty, as the Wrathborn once swung before his axe). The job's numbers are the Wrathborn's (40
    /// pixels a unit, 16 directions, the same cell and height), so the character is the same size. Writes the sheets to
    /// Resources/Characters/wild_arrow, where <see cref="PlayerSpriteAnimator.DefaultCharacter"/> finds them. Tools >
    /// ARPG > Sprite Bake > Set Up Wild Arrow / Bake Wild Arrow. Idempotent.
    /// </summary>
    public static class WildArrowBakeSetup
    {
        const string Folder = "Assets/_Project/Art/Models/WildArrow";
        const string JobPath = Folder + "/WildArrowJob.asset";
        const string Character = PlayerSpriteAnimator.WildArrowCharacter;
        const string BodyPath = "Assets/_Project/Art/Models/Wrathborn/wrathborn_body_leather.fbx";
        const string ArcherFolder = "Assets/_Project/Art/Models/Enemies/bandit_archer";
        const string WrathbornFolder = "Assets/_Project/Art/Models/Wrathborn";
        const string Grip = "1h";

        // The brief's frame counts (4.4), and where each clip comes from: the archer's file, or the Wrathborn's (legs only).
        static readonly (string name, int frames, bool loop, string path)[] Animations =
        {
            ("idle", 24, true, ArcherFolder + "/bandit_archer.fbx"),
            ("run", 10, true, ArcherFolder + "/bandit_archer_run.fbx"),
            ("run_back", 10, true, WrathbornFolder + "/wrathborn_1h_run_back.fbx"),
            ("attack", 10, false, ArcherFolder + "/bandit_archer_attack.fbx"),
            ("hit", 4, false, ArcherFolder + "/bandit_archer_hit.fbx"),
            ("death", 16, false, ArcherFolder + "/bandit_archer_death.fbx"),
        };

        // The legs of the moving shot, as for the Wrathborn: forward, backward and the two strafes.
        static readonly (string path, string suffix)[] Legs =
        {
            (ArcherFolder + "/bandit_archer_run.fbx", LocomotionRules.MovingSuffix),
            (WrathbornFolder + "/wrathborn_1h_run_back.fbx", LocomotionRules.MovingBackSuffix),
            (WrathbornFolder + "/wrathborn_1h_strafe_right.fbx", LocomotionRules.MovingRightSuffix),
            (WrathbornFolder + "/wrathborn_1h_strafe_left.fbx", LocomotionRules.MovingLeftSuffix),
        };

        // The basic shot plays over the attack interval (1.4 a second); every skill borrows the same draw (Docs/09 4.6).
        const float ShotSeconds = 1f / 1.4f;
        const float MovingFramesPerSecond = 20f;

        // The bow and quiver models (Docs/09 4.6; the owner's starting kit of 2026-10-01), when their files exist in the
        // Wild Arrow's folder: wild_arrow_weapon_<look>.fbx, a bow prepared by ArtSource/tools/prepare_weapon.py with its
        // grip (the middle) at the origin and standing up, held in the left fist; wild_arrow_offhand_<look>.fbx, a quiver,
        // on the upper back. Each textured from <file>_albedo.png beside it. Without the files, no piece: the hand draws
        // empty and the back is bare.
        static SpriteBakeJob.Piece[] Pieces()
        {
            var pieces = new System.Collections.Generic.List<SpriteBakeJob.Piece>();
            foreach (var look in AppearanceRules.OneHandWeaponLooks.Concat(AppearanceRules.TwoHandWeaponLooks))
            {
                var held = Held($"{Folder}/{Character}_weapon_{look}.fbx");
                if (held == null)
                    continue;
                var piece = new SpriteBakeJob.Piece
                {
                    layer = AppearanceLayer.Weapon, look = look, prefab = held, bone = HumanBodyBones.LeftHand, autoGrip = true,
                };
                piece.grips.Add(Grip);
                pieces.Add(piece);
            }
            foreach (var look in AppearanceRules.ShieldLooks)
            {
                var held = Held($"{Folder}/{Character}_offhand_{look}.fbx");
                if (held == null)
                    continue;
                // Across the upper back, fletchings up over the right shoulder. A first guess in the chest bone's frame,
                // to be checked on the first bake's sheets (a piece's place is easiest judged on a rendered frame).
                var piece = new SpriteBakeJob.Piece
                {
                    layer = AppearanceLayer.OffHand, look = look, prefab = held, bone = HumanBodyBones.UpperChest,
                    localPosition = new Vector3(0.05f, 0.05f, -0.16f), localEuler = new Vector3(0f, 0f, -25f),
                };
                piece.grips.Add(Grip);
                pieces.Add(piece);
            }
            return pieces.ToArray();
        }

        // A rigid model with no rig, textured, wrapped in a prefab to hold.
        static GameObject Held(string path)
        {
            if (!File.Exists(path))
                return null;
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.SaveAndReimport();
            MixamoImport.ApplyTexture(path, importer);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var name = Path.GetFileNameWithoutExtension(path) + "_held";
            var root = new GameObject(name);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.transform.SetParent(root.transform, false);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Folder}/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        [MenuItem("Tools/ARPG/Sprite Bake/Set Up Wild Arrow")]
        public static SpriteBakeJob SetUp()
        {
            if (!File.Exists(BodyPath))
            {
                Debug.LogError($"[ARPG] The stand-in body is missing: {BodyPath}.");
                return null;
            }
            MixamoImport.ConfigureBody(BodyPath);
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/_Project/Art/Models", "WildArrow");

            var job = AssetDatabase.LoadAssetAtPath<SpriteBakeJob>(JobPath);
            if (job == null)
            {
                job = ScriptableObject.CreateInstance<SpriteBakeJob>();
                AssetDatabase.CreateAsset(job, JobPath);
            }
            job.characterName = Character;
            job.outputFolder = $"Assets/_Project/Resources/Characters/{Character}";
            // The Wrathborn's numbers (see WrathbornBakeSetup): 40 pixels a unit, pixel art, the same cell, pivot and height.
            const float scale = PixelArt.PixelsPerUnit / 64f;
            job.pixelsPerUnit = PixelArt.PixelsPerUnit;
            job.pixelArt = true;
            job.cellSize = Mathf.RoundToInt(128 * scale);
            job.pivot = new Vector2(job.cellSize / 2f, Mathf.Round(20f * scale));
            job.targetHeightPixels = 92.5f * scale;
            job.supersample = 4;
            job.directions = 16;
            job.bodies.Clear();
            job.bodies.Add(new SpriteBakeJob.Body { look = "leather", model = AssetDatabase.LoadAssetAtPath<GameObject>(BodyPath) });
            job.pieces.Clear();
            job.pieces.AddRange(Pieces());

            var set = new SpriteBakeJob.GripSet { grip = Grip };
            SpriteBakeJob.Clip shot = null;
            foreach (var (name, frames, loop, path) in Animations)
            {
                var clip = File.Exists(path) ? MixamoImport.FirstClip(path) : null;
                if (clip == null)
                {
                    Debug.LogWarning($"[ARPG] No clip for the Wild Arrow's {name} in {path}; left out.");
                    continue;
                }
                // A one-shot is sampled only where its action is, as for the Wrathborn and the enemies.
                var window = loop ? new Vector2(0f, clip.length) : MixamoImport.ActionWindow(BodyPath, clip, name == "death");
                var entry = new SpriteBakeJob.Clip
                {
                    name = name, clip = clip, frames = frames, loop = loop,
                    start = window.x, end = window.y, playbackSeconds = window.y - window.x,
                };
                set.clips.Add(entry);
                if (name == "attack")
                    shot = entry;
            }

            // The shot over running legs, for every way the legs can go relative to the aim (LocomotionRules.LegsFor).
            if (shot != null)
                foreach (var (path, suffix) in Legs)
                {
                    var legs = File.Exists(path) ? MixamoImport.FirstClip(path) : null;
                    if (legs == null)
                        continue;
                    var play = Mathf.Min(ShotSeconds, shot.playbackSeconds);
                    set.clips.Add(new SpriteBakeJob.Clip
                    {
                        name = "attack" + suffix, clip = shot.clip, loop = false,
                        frames = Mathf.Max(shot.frames, Mathf.RoundToInt(play * MovingFramesPerSecond) + 1),
                        start = shot.start, end = shot.end, playbackSeconds = play,
                        legs = legs, legsGroundSpeed = 4.4f,
                    });
                }

            job.grips.Clear();
            if (set.clips.Count > 0)
                job.grips.Add(set);
            EditorUtility.SetDirty(job);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ARPG] Wild Arrow bake job: the leather body with {string.Join(", ", set.clips.Select(c => c.name))}.");
            return job;
        }

        [MenuItem("Tools/ARPG/Sprite Bake/Bake Wild Arrow")]
        public static void Bake()
        {
            var job = SetUp();
            if (job != null)
                SpriteBaker.Bake(job);
        }
    }
}
