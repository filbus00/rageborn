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
        // No turn clips: both leaned the body into the turn, which looked wrong (the owner, 2026-09-28).
        static readonly (string name, int frames, bool loop)[] Animations =
        {
            ("idle", 24, true), ("run", 10, true), ("run_back", 10, true),
            ("attack", 10, false), ("hew", 12, false), ("hurl_axe", 10, false),
            ("bull_rush", 8, true), ("ground_breaker", 14, false), ("hit", 4, false), ("death", 16, false),
        };

        static readonly string[] Grips = { "1h", "dual", "shield", "2h" };

        // The grips with something in the off-hand, which get its layer baked over the clips they borrow.
        static readonly string[] GripsWithOffHand = { "dual", "shield" };

        // The strafes (Mixamo's Jog Strafe Right and Left, in place) are only the legs of moving actions, resolved in
        // AddMovingActions, not baked as sheets of their own.

        // The body the pieces hang on and whose rest pose sets the scale: the approved model sheet's look.
        const string ReferenceBody = "leather";

        /// <summary>
        /// Helms, weapons and off-hands whose models exist. A one-handed weapon is <c>wrathborn_weapon_&lt;look&gt;.fbx</c>
        /// (made by ArtSource/tools/prepare_weapon.py: grip at the origin, haft up, real size), held in the right hand for
        /// the one-handed, dual and shield grips, placed from the finger bones.
        /// </summary>
        static SpriteBakeJob.Piece[] Pieces()
        {
            var pieces = new System.Collections.Generic.List<SpriteBakeJob.Piece>();
            foreach (var look in AppearanceRules.OneHandWeaponLooks)
            {
                var held = HeldWeapon(look);
                if (held == null)
                    continue;
                var piece = new SpriteBakeJob.Piece
                {
                    layer = AppearanceLayer.Weapon,
                    look = look,
                    prefab = held,
                    bone = HumanBodyBones.RightHand,
                    autoGrip = true,
                };
                piece.grips.AddRange(new[] { "1h", "dual", "shield" });
                pieces.Add(piece);
                // The same axe in the left fist for dual wield (the off-hand layer).
                var offHand = new SpriteBakeJob.Piece
                {
                    layer = AppearanceLayer.OffHand, look = look, prefab = held, bone = HumanBodyBones.LeftHand, autoGrip = true,
                };
                offHand.grips.Add("dual");
                pieces.Add(offHand);
            }

            // The two-hander: no two-handed model exists yet, so the bearded axe half as big again stands in for the
            // great axe (Docs/03's first two-handed look).
            var greatAxe = HeldWeapon("bearded_axe");
            if (greatAxe != null)
            {
                var piece = new SpriteBakeJob.Piece
                {
                    layer = AppearanceLayer.Weapon, look = AppearanceRules.TwoHandWeaponLooks[0], prefab = greatAxe,
                    bone = HumanBodyBones.RightHand, autoGrip = true, scale = 1.5f,
                };
                piece.grips.Add("2h");
                pieces.Add(piece);
            }

            foreach (var look in AppearanceRules.ShieldLooks)
            {
                var held = HeldShield(look);
                if (held == null)
                    continue;
                var piece = new SpriteBakeJob.Piece
                {
                    layer = AppearanceLayer.OffHand, look = look, prefab = held, bone = HumanBodyBones.LeftHand, autoGrip = true,
                };
                piece.grips.Add("shield");
                pieces.Add(piece);
            }
            return pieces.ToArray();
        }

        /// <summary>Which way a held shield's face points in the fist's frame: +1 along the fist's +X, -1 against it.
        /// Set by looking at a bake: the face must point away from the body.</summary>
        const float ShieldFaceSide = -1f;

        // A shield model (wrathborn_shield_<look>.fbx, textured from <...>_albedo.png) made into a prefab to hold: its disc
        // turned into the fist's up-and-forward plane (the plane PlaceInFist lays a haft and blade in), its face (the side
        // the boss stands out on) toward ShieldFaceSide, and set a hand's breadth out from the grip so the fist holds its back.
        static GameObject HeldShield(string look)
        {
            var path = $"{Folder}/{Character}_shield_{look}.fbx";
            if (!File.Exists(path))
                return null;
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.SaveAndReimport();
            MixamoImport.ApplyTexture(path, importer);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var points = new System.Collections.Generic.List<Vector3>();
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
            {
                var toRoot = model.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                foreach (var vertex in filter.sharedMesh.vertices)
                    points.Add(toRoot.MultiplyPoint3x4(vertex));
            }
            if (points.Count == 0)
                return null;
            var min = points[0];
            var max = points[0];
            foreach (var p in points)
            {
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
            var center = (min + max) / 2f;
            var size = max - min;
            // The disc's normal is its thinnest axis; its face is the side the boss sticks out further on.
            var axis = size.x <= size.y && size.x <= size.z ? Vector3.right : size.y <= size.z ? Vector3.up : Vector3.forward;
            var front = Vector3.Dot(max - center, axis) >= Vector3.Dot(center - min, axis) ? axis : -axis;
            var farthest = 0f;
            foreach (var p in points)
                farthest = Mathf.Max(farthest, Vector3.Dot(p - center, front));
            var nearest = 0f;
            foreach (var p in points)
                nearest = Mathf.Min(nearest, Vector3.Dot(p - center, front));
            if (Mathf.Abs(nearest) > farthest)
                front = -front;

            var root = new GameObject($"{Character}_shield_{look}_held");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.transform.SetParent(root.transform, false);
            var face = Vector3.right * ShieldFaceSide;
            instance.transform.localRotation = Quaternion.FromToRotation(front, face);
            instance.transform.localPosition = face * 0.07f - instance.transform.localRotation * center;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Folder}/{Character}_shield_{look}_held.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        // The weapon model textured and turned so its blade faces +Z (which side the blade is on is read from the mesh: the
        // blade holds most of the mass off the haft), saved as a prefab to hold.
        static GameObject HeldWeapon(string look)
        {
            var path = $"{Folder}/{Character}_weapon_{look}.fbx";
            if (!File.Exists(path))
                return null;
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.SaveAndReimport();
            MixamoImport.ApplyTexture(path, importer);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var centroid = Vector3.zero;
            var count = 0;
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
            {
                var toRoot = model.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                foreach (var vertex in filter.sharedMesh.vertices)
                {
                    centroid += toRoot.MultiplyPoint3x4(vertex);
                    count++;
                }
            }
            var blade = count > 0 ? new Vector3(centroid.x / count, 0f, centroid.z / count) : Vector3.forward;
            if (blade.sqrMagnitude < 1e-8f)
                blade = Vector3.forward;

            var root = new GameObject($"{Character}_weapon_{look}_held");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.transform.SetParent(root.transform, false);
            instance.transform.localRotation = Quaternion.FromToRotation(blade.normalized, Vector3.forward);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Folder}/{Character}_weapon_{look}_held.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

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
                MixamoImport.ConfigureBody(body);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(bodies[0]).OfType<Avatar>().FirstOrDefault();

            var job = AssetDatabase.LoadAssetAtPath<SpriteBakeJob>(JobPath);
            if (job == null)
            {
                job = ScriptableObject.CreateInstance<SpriteBakeJob>();
                AssetDatabase.CreateAsset(job, JobPath);
            }
            job.characterName = Character;
            job.outputFolder = $"Assets/_Project/Resources/Characters/{Character}";
            // Half resolution, drawn at the same size (the owner, 2026-09-27: "the game is quite high res, it can drop
            // more in resolution. It can be the same as d2"): 64 pixels per unit, so every number below is half the
            // brief's. He stands about 85 px, close to Diablo 2's heroes, and his sheets take a quarter of the memory.
            job.pixelsPerUnit = 64f;
            job.cellSize = 128;
            job.pivot = new Vector2(64f, 20f);
            // The height is measured on the rest pose's whole box, arms out in the A-pose and all; 170 gave 155 px
            // standing at full resolution, so 185 gave the brief's 170. Halved.
            job.targetHeightPixels = 92.5f;
            job.supersample = 4;
            // 16 directions, as Diablo 2 gave its heroes, so the facing is never more than 11 degrees off the path (the
            // user's request of 2026-09-27; 8 left up to 22).
            job.directions = 16;
            job.bodies.Clear();
            foreach (var body in bodies)
                job.bodies.Add(new SpriteBakeJob.Body { look = Look(body), model = AssetDatabase.LoadAssetAtPath<GameObject>(body) });
            job.pieces.Clear();
            job.pieces.AddRange(Pieces());

            job.grips.Clear();
            // Every clip each grip plays: its own file, or the one-handed one it borrows (Docs/03's grips; the owner's
            // Mixamo downloads of 2026-09-28 give the shield a run, the two-hander a run and a swing, dual wield a combo).
            // A borrowed clip is baked only as the off-hand layer, since the body and weapon moving the same way are the
            // one-handed sheets the game falls back to; a grip with no off-hand (the two-hander) borrows nothing.
            var resolved = new System.Collections.Generic.Dictionary<string, (AnimationClip clip, bool own)>();
            (AnimationClip clip, bool own) Resolve(string grip, string name, bool loop)
            {
                var key = grip + "/" + name;
                if (resolved.TryGetValue(key, out var found))
                    return found;
                var own = ClipFor(grip, name, loop, avatar, bodies[0]);
                var result = own != null ? (own, true)
                    : grip != Grips[0] ? (Resolve(Grips[0], name, loop).clip, false) : ((AnimationClip)null, false);
                resolved[key] = result;
                return result;
            }

            foreach (var grip in Grips)
            {
                var set = new SpriteBakeJob.GripSet { grip = grip };
                var hasOffHand = GripsWithOffHand.Contains(grip);
                foreach (var (name, frames, loop) in Animations)
                {
                    var (clip, own) = Resolve(grip, name, loop);
                    if (clip == null || (!own && !hasOffHand))
                        continue;
                    // A one-shot is sampled only where its action is (Mixamo's Hew was 5.2 s with the swing in about two
                    // seconds of it; its 12 frames went mostly on the stance around it). A loop keeps its whole cycle.
                    // Played over the sampled span's real length: Mixamo clips run from 0.7 s (a run) to 10 s (an idle).
                    var window = loop ? new Vector2(0f, clip.length) : MixamoImport.ActionWindow(bodies[0], clip, name == "death");
                    set.clips.Add(new SpriteBakeJob.Clip
                    {
                        name = name, clip = clip, frames = frames, loop = loop,
                        start = window.x, end = window.y, playbackSeconds = window.y - window.x,
                        // Ground Breaker leaps about a body height, above the top of its cells.
                        riseScale = name == "ground_breaker" ? 0.4f : 1f,
                        offHandOnly = !own,
                    });
                }
                AddMovingActions(set, grip, hasOffHand, Resolve, job.grips.Count > 0 ? job.grips[0] : set);
                if (set.clips.Count > 0)
                    job.grips.Add(set);
            }

            EditorUtility.SetDirty(job);
            AssetDatabase.SaveAssets();
            Debug.Log($"Wrathborn bake job: {job.bodies.Count} bod(ies) ({string.Join(", ", job.bodies.Select(b => b.look))}), " +
                      string.Join("; ", job.grips.Select(g => $"{g.grip}: {string.Join(", ", g.clips.Select(c => c.name))}")));
            return job;
        }

        // The actions that play while the character runs, and how long the game plays each (PlayerSpriteAnimator: the
        // basic attack fitted to the attack interval, 1.4 a second before attack speed; a skill at most 1.2 s).
        static readonly (string name, float seconds)[] MovingActions =
        {
            ("attack", 1f / 1.4f), ("hew", 1.2f), ("hurl_axe", 1.2f), ("ground_breaker", 1.2f),
        };

        /// <summary>The frame rate of a moving action's sheet: the legs cycle about twice a second under it, and 10 to 12
        /// frames a second made them stutter where the run's own sheet plays about 20.</summary>
        const float MovingFramesPerSecond = 20f;

        // Adds <name>_move for each action: the run's hips and legs under the action's torso (the owner, 2026-09-27: "the
        // player will be moving most of the time. The various animations need to play even when the character is moving"),
        // and <name>_move_back over the backward run, for a blow at an enemy behind the way the character runs (the
        // character always faces what it strikes; the owner, 2026-09-28).
        static void AddMovingActions(SpriteBakeJob.GripSet set, string grip, bool hasOffHand,
            System.Func<string, string, bool, (AnimationClip clip, bool own)> resolve, SpriteBakeJob.GripSet reference)
        {
            var legSets = new[]
            {
                ("run", LocomotionRules.MovingSuffix), ("run_back", LocomotionRules.MovingBackSuffix),
                ("strafe_right", LocomotionRules.MovingRightSuffix), ("strafe_left", LocomotionRules.MovingLeftSuffix),
            };
            foreach (var (legsName, suffix) in legSets)
            {
                var legs = resolve(grip, legsName, true);
                if (legs.clip == null)
                    continue;
                foreach (var (name, seconds) in MovingActions)
                {
                    // Where the swing sits in its clip: this grip's entry, or the one-handed one it borrows.
                    var action = set.clips.FirstOrDefault(c => c.name == name) ?? reference.clips.FirstOrDefault(c => c.name == name);
                    var actionClip = resolve(grip, name, false);
                    if (actionClip.clip == null)
                        continue;
                    // The grip's own when either half is (its run under a borrowed swing, or its swing over borrowed legs).
                    var own = actionClip.own || legs.own;
                    if (!own && !hasOffHand)
                        continue;
                    var start = action != null ? action.start : 0f;
                    var end = action != null ? action.end : actionClip.clip.length;
                    var length = action != null ? action.playbackSeconds : actionClip.clip.length;
                    var play = Mathf.Min(seconds, length);
                    set.clips.Add(new SpriteBakeJob.Clip
                    {
                        name = name + suffix, clip = actionClip.clip, loop = false,
                        frames = Mathf.Max(action != null ? action.frames : 10, Mathf.RoundToInt(play * MovingFramesPerSecond) + 1),
                        start = start, end = end, playbackSeconds = play,
                        legs = legs.clip, legsGroundSpeed = 4.4f,
                        offHandOnly = !own,
                    });
                }
            }
        }

        static string Look(string bodyPath) => Path.GetFileNameWithoutExtension(bodyPath).Substring($"{Character}_body_".Length);

        // The animation file for a grip, or for the reference grip's idle the body's own clip. A left-hand variant
        // (<name> ending in _left) with no file of its own is the _right file mirrored: Unity mirrors a humanoid clip's
        // motion, not the model, so the weapon stays in the right hand (the user's suggestion, 2026-09-27).
        static AnimationClip ClipFor(string grip, string name, bool loop, Avatar avatar, string referenceBody)
        {
            var path = $"{Folder}/{Character}_{grip}_{name}.fbx";
            if (File.Exists(path))
                return MixamoImport.ConfigureAnimation(path, loop, avatar, name, false);
            if (name.EndsWith("_left"))
            {
                var right = $"{Folder}/{Character}_{grip}_{name.Substring(0, name.Length - 5)}_right.fbx";
                if (File.Exists(right))
                    return MixamoImport.ConfigureAnimation(right, loop, avatar, name, true);
            }
            // The body was downloaded with its idle, which serves the one-handed grip until each grip has its own.
            if (name == "idle" && grip == "1h")
                return MixamoImport.FirstClip(referenceBody);
            return null;
        }

        [MenuItem("Tools/ARPG/Sprite Bake/Bake Wrathborn")]
        public static void Bake()
        {
            var job = SetUp();
            if (job != null)
                SpriteBaker.Bake(job);
        }
    }
}
