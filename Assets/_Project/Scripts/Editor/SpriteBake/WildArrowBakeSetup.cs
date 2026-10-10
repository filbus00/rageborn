using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// The Wild Arrow's bake. Her own model (the owner's Meshy model rigged on Mixamo, 2026-10-03) when
    /// <c>Art/Models/WildArrow/wild_arrow.fbx</c> exists: the body with its skin (Unarmed Idle 01), textured from
    /// wild_arrow_albedo.png, and the Pro Longbow Pack's clips as wild_arrow_&lt;name&gt;.fbx (<see cref="OwnAnimations"/>);
    /// her shots while moving play over the pack's aiming walk, since she walks while she fires (FiringPace).
    /// Otherwise the stand-in bake (Docs/09-art-brief.md 4.6; the owner's choice of 2026-09-30: "wrathborn with the
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
        const string OwnBodyPath = Folder + "/" + Character + ".fbx";

        // Her own clips: the file suffix in her folder, the brief's frame counts (4.4).
        static readonly (string name, int frames, bool loop)[] OwnAnimations =
        {
            ("idle", 24, true),
            ("run", 10, true),
            // Her walk when moving slowly (the owner, 2026-10-10): the pack's Standing Walk Forward, left out until it is there.
            ("walk", 16, true),
            ("run_back", 10, true),
            ("attack", 10, false),
            ("overdraw", 12, false),
            ("recoil", 10, false),
            ("hit", 4, false),
            ("death", 16, false),
            // The dodge's dive (the owner, 2026-10-10): the pack's Standing Dive Forward, whole, left out until it is there.
            ("dive", 12, false),
        };

        // The actions that get moving variants, over her aiming walk (forward, back and the two sides).
        static readonly string[] OwnMovingActions = { "attack", "overdraw", "recoil" };
        static readonly (string file, string suffix)[] OwnLegs =
        {
            ("walk_aim_forward", LocomotionRules.MovingSuffix),
            ("walk_aim_back", LocomotionRules.MovingBackSuffix),
            ("walk_aim_right", LocomotionRules.MovingRightSuffix),
            ("walk_aim_left", LocomotionRules.MovingLeftSuffix),
        };

        // Her walking pace while she fires (PlayerController's base speed of 4 at FiringPace.MoveMultiplier).
        const float WalkGroundSpeed = 4f * FiringPace.MoveMultiplier;

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
            foreach (var look in AppearanceRules.OneHandWeaponLooks.Concat(AppearanceRules.TwoHandWeaponLooks)
                         .Concat(AppearanceRules.LegendaryLooksOf(AppearanceLayer.Weapon)))
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
            foreach (var look in AppearanceRules.ShieldLooks.Concat(AppearanceRules.LegendaryLooksOf(AppearanceLayer.OffHand)))
            {
                var held = Held($"{Folder}/{Character}_offhand_{look}.fbx");
                if (held == null)
                    continue;
                // Across the upper back, fletchings up over the right shoulder, in the chest bone's frame (metres, +Z
                // forward). The quiver's centre at 0.21 behind the bone clears her back (its own radius is about 0.1).
                var piece = new SpriteBakeJob.Piece
                {
                    layer = AppearanceLayer.OffHand, look = look, prefab = held, bone = HumanBodyBones.UpperChest,
                    localPosition = new Vector3(0.05f, 0.05f, -0.21f), localEuler = new Vector3(0f, 0f, -25f),
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
            var own = File.Exists(OwnBodyPath);
            var bodyPath = own ? OwnBodyPath : BodyPath;
            if (!File.Exists(bodyPath))
            {
                Debug.LogError($"[ARPG] The Wild Arrow's body is missing: {bodyPath}.");
                return null;
            }
            MixamoImport.ConfigureBody(bodyPath);
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
            if (own && File.Exists(GearBodyPath("bare")))
                AddGearBodies(job);
            else
                // The look stays "leather", the name the game's sheets are found by, for either body.
                job.bodies.Add(new SpriteBakeJob.Body { look = "leather", model = AssetDatabase.LoadAssetAtPath<GameObject>(bodyPath) });
            job.pieces.Clear();
            job.pieces.AddRange(Pieces());
            // The pieces' offsets are in the bones of her original rig; the gear bodies are a re-export whose bones are
            // scaled and turned differently (on them the quiver came out huge, a square of texture over her).
            if (job.bodies.Count > 0 && job.bodies[0].model != null && File.Exists(OwnBodyPath) && job.bodies[0].model.name != Character)
                RemapPieces(job, AssetDatabase.LoadAssetAtPath<GameObject>(OwnBodyPath), job.bodies[0].model);

            var set = own ? OwnClips(bodyPath) : StandInClips();
            job.grips.Clear();
            if (set.clips.Count > 0)
                job.grips.Add(set);
            EditorUtility.SetDirty(job);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ARPG] Wild Arrow bake job ({(own ? "her own model" : "the stand-in")}): {string.Join(", ", set.clips.Select(c => c.name))}.");
            return job;
        }

        // Her worn gear (2026-10-04, ArtSource/tools/props/wild_arrow_gear.py): her body bare and in each chest look, and
        // each helm on her rig alone, all exported through one path so they share scale and placement, and one texture.
        // Bare comes first: the bake measures and places every body as the first, and cuts pieces and helms with it.
        const string GearAlbedoPath = Folder + "/" + Character + "_gear_albedo.png";

        static string GearBodyPath(string look) => $"{Folder}/{Character}_body_{look}.fbx";

        // Unity's import strips bones no vertex uses ("optimize bones"), and a model whose mesh is only on her hands (the
        // gloves) lost her legs with it, so no humanoid could be made and the first bake came out unanimated (2026-10-04).
        // Every gear model keeps all her bones.
        static void KeepAllBones(string path)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            if (importer == null || !importer.optimizeBones)
                return;
            importer.optimizeBones = false;
            importer.SaveAndReimport();
        }

        // The bone map Unity worked out while the legs were stripped stays stale (36 of 46 bones, no upper legs): a gear
        // model that is still no humanoid takes the bare body's map, the same rig's names.
        static void TakeBareBodyMap(string path)
        {
            var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            if (avatar != null && avatar.isValid && avatar.isHuman)
                return;
            var bare = (ModelImporter)AssetImporter.GetAtPath(GearBodyPath(AppearanceRules.BareBody));
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            if (bare == null || importer == null)
                return;
            importer.humanDescription = bare.humanDescription;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
            Debug.Log($"[ARPG] {Path.GetFileName(path)}: the bare body's bone map.");
        }

        static void AddGearBodies(SpriteBakeJob job)
        {
            foreach (var look in new[] { AppearanceRules.BareBody }.Concat(AppearanceRules.ChestLooks)
                         .Concat(AppearanceRules.LegendaryLooksOf(AppearanceLayer.Body)))
            {
                var path = GearBodyPath(look);
                if (!File.Exists(path))
                    continue;
                KeepAllBones(path);
                MixamoImport.ConfigureBody(path, GearAlbedoPath);
                job.bodies.Add(new SpriteBakeJob.Body { look = look, model = AssetDatabase.LoadAssetAtPath<GameObject>(path) });
            }
            // Each worn layer: wild_arrow_<layer code>_<look>.fbx, her rig with only that gear on it.
            foreach (var (layer, looks) in new[]
                     {
                         (AppearanceLayer.Boots, AppearanceRules.BootLooks), (AppearanceLayer.Belt, AppearanceRules.BeltLooks),
                         (AppearanceLayer.Gloves, AppearanceRules.GloveLooks), (AppearanceLayer.Helm, AppearanceRules.HelmLooks),
                     })
                foreach (var look in looks.Concat(AppearanceRules.LegendaryLooksOf(layer)))
                {
                    var path = $"{Folder}/{Character}_{AppearanceRules.LayerCode(layer)}_{look}.fbx";
                    if (!File.Exists(path))
                        continue;
                    KeepAllBones(path);
                    // A model with its own texture beside it (the owner's Meshy hood, 2026-10-07) keeps it.
                    MixamoImport.ConfigureBody(path, File.Exists(path.Replace(".fbx", "_albedo.png")) ? null : GearAlbedoPath);
                    TakeBareBodyMap(path);
                    job.bodies.Add(new SpriteBakeJob.Body { look = look, model = AssetDatabase.LoadAssetAtPath<GameObject>(path), layer = layer });
                }
        }

        /// <summary>
        /// Moves each piece placed by offsets (not gripped) from its bone on one rig to the same bone of another: placed on
        /// the first rig's bone in its rest pose, the piece's pose relative to the model is read and written back as offsets
        /// in the second rig's bone. Both rigs are her, at rest, so the piece lands in the same place on her.
        /// </summary>
        static void RemapPieces(SpriteBakeJob job, GameObject from, GameObject to)
        {
            var a = (GameObject)Object.Instantiate(from);
            var b = (GameObject)Object.Instantiate(to);
            try
            {
                var animA = a.GetComponent<Animator>();
                var animB = b.GetComponent<Animator>();
                if (animA == null || animB == null || !animA.isHuman || !animB.isHuman)
                    return;
                // Both models the same height, at the origin.
                var heightA = Measure(a);
                var heightB = Measure(b);
                if (heightA > 0f && heightB > 0f)
                    b.transform.localScale *= heightA / heightB;
                foreach (var piece in job.pieces)
                {
                    if (piece.autoGrip)
                        continue;
                    var boneA = animA.GetBoneTransform(piece.bone);
                    var boneB = animB.GetBoneTransform(piece.bone);
                    if (boneA == null || boneB == null)
                        continue;
                    var position = boneA.TransformPoint(piece.localPosition);
                    var rotation = boneA.rotation * Quaternion.Euler(piece.localEuler);
                    var size = boneA.lossyScale.x * piece.scale;
                    piece.localPosition = boneB.InverseTransformPoint(position);
                    piece.localEuler = (Quaternion.Inverse(boneB.rotation) * rotation).eulerAngles;
                    // Both models at one height, so the bone-local offsets and scale carry over whatever the bake's scale.
                    piece.scale = size / Mathf.Max(1e-6f, boneB.lossyScale.x);
                    Debug.Log($"[ARPG] {piece.look}: on the gear rig at {piece.localPosition}, {piece.localEuler}, scale {piece.scale:0.###}");
                }
            }
            finally
            {
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
            }
        }

        static float Measure(GameObject model)
        {
            var bounds = new Bounds();
            var any = false;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                if (!any)
                    bounds = renderer.bounds;
                else
                    bounds.Encapsulate(renderer.bounds);
                any = true;
            }
            return any ? bounds.size.y : 0f;
        }

        // Her own model and the longbow pack's clips, configured as humanoid clips on her avatar.
        static SpriteBakeJob.GripSet OwnClips(string bodyPath)
        {
            var avatar = AssetDatabase.LoadAllAssetsAtPath(bodyPath).OfType<Avatar>().FirstOrDefault();
            var set = new SpriteBakeJob.GripSet { grip = Grip };
            var actions = new System.Collections.Generic.Dictionary<string, SpriteBakeJob.Clip>();
            foreach (var (name, frames, loop) in OwnAnimations)
            {
                var path = $"{Folder}/{Character}_{name}.fbx";
                var clip = File.Exists(path) ? MixamoImport.ConfigureAnimation(path, loop, avatar, name, false) : null;
                if (clip == null)
                {
                    Debug.LogWarning($"[ARPG] No clip for the Wild Arrow's {name} in {path}; left out.");
                    continue;
                }
                var window = loop || name == "dive" ? new Vector2(0f, clip.length) : MixamoImport.ActionWindow(bodyPath, clip, name == "death");
                var entry = new SpriteBakeJob.Clip
                {
                    name = name, clip = clip, frames = frames, loop = loop,
                    start = window.x, end = window.y, playbackSeconds = window.y - window.x,
                };
                set.clips.Add(entry);
                actions[name] = entry;
            }

            foreach (var (file, suffix) in OwnLegs)
            {
                var path = $"{Folder}/{Character}_{file}.fbx";
                var legs = File.Exists(path) ? MixamoImport.ConfigureAnimation(path, true, avatar, file, false) : null;
                if (legs == null)
                {
                    Debug.LogWarning($"[ARPG] No legs for the Wild Arrow's moving shots in {path}; left out.");
                    continue;
                }
                foreach (var action in OwnMovingActions)
                    if (actions.TryGetValue(action, out var shot))
                        set.clips.Add(MovingVariant(shot, action + suffix, legs, WalkGroundSpeed));
            }
            return set;
        }

        // The stand-in: the Wrathborn's leather body with the bandit archer's clips and running legs.
        static SpriteBakeJob.GripSet StandInClips()
        {
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
                    if (legs != null)
                        set.clips.Add(MovingVariant(shot, "attack" + suffix, legs, 4.4f));
                }
            return set;
        }

        // An action's torso over a walk or run's hips and legs, played over at most the basic shot's interval.
        static SpriteBakeJob.Clip MovingVariant(SpriteBakeJob.Clip shot, string name, AnimationClip legs, float groundSpeed)
        {
            var play = Mathf.Min(ShotSeconds, shot.playbackSeconds);
            return new SpriteBakeJob.Clip
            {
                name = name, clip = shot.clip, loop = false,
                frames = Mathf.Max(shot.frames, Mathf.RoundToInt(play * MovingFramesPerSecond) + 1),
                start = shot.start, end = shot.end, playbackSeconds = play,
                legs = legs, legsGroundSpeed = groundSpeed,
            };
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
