using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ARPG.Editor
{
    /// <summary>
    /// Renders a 3D character into the game's sprite sheets (Docs/09-art-brief.md, Route A and 4.5). For every grip,
    /// clip, layer (each body, each piece baked for that grip), direction and frame: the bodies are posed by the clip, the
    /// layer is rendered orthographically from the game's 30 degree camera at the job's supersampling, over black and over
    /// white to recover transparency (<see cref="SpriteBakeMath.Matte"/>), averaged down to the final cell and placed by
    /// direction row and frame column. The sheet is written as a PNG and imported as sliced sprites with the feet pivot at
    /// 128 pixels per unit.
    ///
    /// Layers: a body is rendered alone. A piece (helm, off-hand, weapon) is rendered with the first body present but
    /// drawing only depth, so the piece comes out cut wherever the body hides it; the game draws the body first and the
    /// pieces over it (<see cref="LayeredCharacterSprite"/>), with no draw-order table. Pieces are not cut by each other,
    /// so a weapon passing behind a helm shows over it; rare, and the helm varies anyway.
    ///
    /// The models are set up in a preview scene, so the open scenes are untouched. The project renders with the 2D
    /// renderer, which draws a 3D model flat and unshaded, so the bake adds a standard (Universal) renderer to the
    /// pipeline asset for the length of the bake and gives it to its camera, then takes it out again: the game's build
    /// does not carry it.
    /// </summary>
    public static class SpriteBaker
    {
        const string BakeRendererPath = "Assets/_Project/Settings/SpriteBakeRenderer.asset";

        [MenuItem("Tools/ARPG/Sprite Bake/Bake Selected Jobs")]
        static void BakeSelected()
        {
            var jobs = Selection.GetFiltered<SpriteBakeJob>(SelectionMode.Assets);
            if (jobs.Length == 0)
            {
                Debug.LogWarning("Select one or more Sprite Bake Job assets first.");
                return;
            }
            foreach (var job in jobs)
                Bake(job);
        }

        /// <summary>Bakes every grip, clip and layer of the job. Returns the written file paths.</summary>
        public static List<string> Bake(SpriteBakeJob job)
        {
            var written = new List<string>();
            if (job.bodies.Count == 0 || job.bodies.Any(b => b.model == null) || job.grips.All(g => g.clips.Count == 0))
            {
                Debug.LogError($"{job.name}: needs at least one body with a model and one clip.");
                return written;
            }

            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null)
            {
                Debug.LogError("The sprite bake needs the Universal Render Pipeline.");
                return written;
            }

            var rendererData = LoadOrCreateRendererData();
            var rendererIndex = AddRenderer(pipeline, rendererData);
            var scene = EditorSceneManager.NewPreviewScene();
            var size = job.cellSize * Mathf.Max(1, job.supersample);
            var target = new RenderTexture(new RenderTextureDescriptor(size, size, RenderTextureFormat.ARGBHalf, 24) { sRGB = false });
            var readback = new Texture2D(size, size, TextureFormat.RGBAHalf, false, true);
            var depthOnly = new Material(Shader.Find("ARPG/Depth Only"));
            var speeds = new Dictionary<string, float>();

            try
            {
                var stage = BuildStage(job, scene, rendererIndex);
                stage.Camera.targetTexture = target;
                // The first render after the renderer is added comes out unlit (the bake's first test cell did);
                // one thrown away warms it up.
                stage.Camera.Render();

                foreach (var gripSet in job.grips)
                    foreach (var clip in gripSet.clips)
                    {
                        if (clip.clip == null || string.IsNullOrEmpty(clip.name))
                            continue;
                        var windowEnd = clip.end > clip.start ? Mathf.Min(clip.end, clip.clip.length) : clip.clip.length;
                        var windowStart = Mathf.Clamp(clip.start, 0f, windowEnd);
                        var times = SpriteBakeMath.SampleTimes(windowEnd - windowStart, clip.frames, clip.loop, job.framesPerSecond);
                        for (var t = 0; t < times.Length; t++)
                            times[t] += windowStart;
                        if (clip.loop)
                            speeds[$"{(string.IsNullOrEmpty(gripSet.grip) ? "" : gripSet.grip + "_")}{clip.name}"] = MeasureGroundSpeed(stage.Bodies[0], clip.clip);

                        foreach (var layer in LayersFor(stage, gripSet.grip))
                        {
                            var sheet = new SheetWriter(job, job.SheetName(layer.Layer, layer.Look, gripSet.grip, clip.name), times.Length);
                            var poseIndex = layer.BodyIndex;
                            var hipsStart = Vector3.zero;
                            for (var row = 0; row < job.DirectionCount; row++)
                            {
                                stage.Turntable.rotation = Quaternion.LookRotation(SpriteBakeMath.Facing(row, job.DirectionCount), Vector3.up);
                                for (var frame = 0; frame < times.Length; frame++)
                                {
                                    var body = stage.Bodies[poseIndex];
                                    // A generic clip writes only the bones it animates; the rest would keep the last clip's
                                    // pose (the first test attack ran on frozen running legs). SampleAnimation poses humanoid
                                    // and generic clips alike; a PlayableGraph evaluated in the editor left a humanoid in its
                                    // bind pose (the Wrathborn's first bake). The root is put back after, so a clip that was
                                    // not downloaded in place still stays on the spot.
                                    body.RestPose.Restore();
                                    clip.clip.SampleAnimation(body.Root, times[frame]);
                                    body.Root.transform.localPosition = body.RootPosition;
                                    body.Root.transform.localRotation = Quaternion.identity;
                                    // A one-shot keeps its hips over the ground spot they start on. On a humanoid the
                                    // clip's travel rides on the hips, not the root: Ground Breaker leapt out of the top
                                    // of its cells and the death slid out of the side. Loops are recorded in place.
                                    if (!clip.loop && body.Hips != null)
                                    {
                                        var hips = body.Root.transform.parent.InverseTransformPoint(body.Hips.position);
                                        if (frame == 0)
                                            hipsStart = hips;
                                        var drift = hips - hipsStart;
                                        drift.y *= 1f - Mathf.Clamp01(clip.riseScale);
                                        body.Root.transform.localPosition = body.RootPosition - drift;
                                    }
                                    body.Skin();

                                    stage.Show(layer, depthOnly);
                                    sheet.Put(row, frame, RenderCell(stage.Camera, target, readback, job));
                                }
                            }
                            written.AddRange(sheet.Save());
                        }
                    }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                RemoveRenderer(pipeline, rendererData);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(readback);
                Object.DestroyImmediate(depthOnly);
            }

            written.Add(WriteTiming(job, speeds));
            Debug.Log($"{job.name}: baked {written.Count} file(s):\n" + string.Join("\n", written));
            return written;
        }

        /// <summary>
        /// Writes <c>&lt;character&gt;_timing.txt</c> beside the sheets: one line per grip and clip with a playback length,
        /// <c>&lt;grip&gt;_&lt;clip&gt; &lt;seconds&gt;</c> (just <c>&lt;clip&gt;</c> without a grip), which
        /// <see cref="LayeredCharacterSprite"/> reads so a sheet plays over its animation's real length.
        /// </summary>
        static string WriteTiming(SpriteBakeJob job, Dictionary<string, float> speeds)
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            var lines = new List<string>();
            foreach (var grip in job.grips)
                foreach (var clip in grip.clips)
                {
                    if (clip.clip == null)
                        continue;
                    var key = $"{(string.IsNullOrEmpty(grip.grip) ? "" : grip.grip + "_")}{clip.name}";
                    var seconds = clip.playbackSeconds > 0f ? clip.playbackSeconds : 0f;
                    speeds.TryGetValue(key, out var speed);
                    if (seconds <= 0f && speed <= 0f)
                        continue;
                    // A third number is the ground speed (units a second) the clip looks right at, for locomotion clips.
                    lines.Add($"{key} {seconds.ToString("0.###", culture)}" + (speed > 0.01f ? " " + speed.ToString("0.###", culture) : ""));
                }
            var folder = job.outputFolder.TrimEnd('/');
            Directory.CreateDirectory(folder);
            var path = $"{folder}/{job.characterName}_timing.txt";
            File.WriteAllLines(path, lines);
            AssetDatabase.ImportAsset(path);
            return path;
        }

        /// <summary>What one sheet shows: a body alone, or a piece hanging on the first body.</summary>
        sealed class LayerTarget
        {
            public AppearanceLayer Layer;
            public string Look;
            public int BodyIndex;
            public PieceInstance Piece;
        }

        sealed class BodyInstance
        {
            public GameObject Root;
            public Animator Animator;
            public Transform Hips;
            public Vector3 RootPosition;
            public Pose RestPose;
            public Renderer[] Renderers;
            public Material[][] Materials;
            public SkinProxy[] Skins;

            /// <summary>Brings every skinned mesh's stand-in to the current pose (see <see cref="SkinProxy"/>).</summary>
            public void Skin()
            {
                foreach (var skin in Skins)
                    skin.Update();
            }
        }

        /// <summary>
        /// A skinned mesh drawn as a plain mesh baked from its current pose. In the editor a skinned mesh is re-skinned
        /// once per editor frame, not per camera render, so rendering it straight after posing drew its rest shape carried
        /// rigidly by the hip bone (the Wrathborn's first real bake: a run with no running). The bake draws this stand-in
        /// instead and hides the skinned renderer.
        /// </summary>
        sealed class SkinProxy
        {
            readonly SkinnedMeshRenderer skinned;
            readonly Transform transform;
            readonly Mesh mesh = new Mesh();
            public readonly MeshRenderer Renderer;

            public SkinProxy(SkinnedMeshRenderer skinned, Scene scene)
            {
                this.skinned = skinned;
                var go = new GameObject(skinned.name + " (posed)", typeof(MeshFilter), typeof(MeshRenderer));
                SceneManager.MoveGameObjectToScene(go, scene);
                transform = go.transform;
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                Renderer = go.GetComponent<MeshRenderer>();
                Renderer.sharedMaterials = skinned.sharedMaterials;
                skinned.enabled = false;
                Update();
            }

            public void Update()
            {
                // Measured, not guessed: with useScale false and the renderer's position and rotation (no scale), the baked
                // vertices land on the bones at any body scale. The first version passed true, which came out 1.55 times
                // too big at the bake's scale of 0.645 and grown around the wrong point: the body drew about 200 px tall
                // instead of 170 and a held axe hung 25 cm off the hand it was fixed to.
                skinned.BakeMesh(mesh, false);
                transform.SetPositionAndRotation(skinned.transform.position, skinned.transform.rotation);
                transform.localScale = Vector3.one;
            }
        }

        sealed class PieceInstance
        {
            public SpriteBakeJob.Piece Definition;
            public Renderer[] Renderers;
        }

        sealed class Stage
        {
            public Camera Camera;
            public Transform Turntable;
            public readonly List<BodyInstance> Bodies = new List<BodyInstance>();
            public readonly List<PieceInstance> Pieces = new List<PieceInstance>();

            /// <summary>Only the layer's own meshes draw; for a piece, the first body draws depth only, to cut it.</summary>
            public void Show(LayerTarget layer, Material depthOnly)
            {
                for (var b = 0; b < Bodies.Count; b++)
                {
                    var body = Bodies[b];
                    var drawn = layer.Piece == null && b == layer.BodyIndex;
                    var occluder = layer.Piece != null && b == 0;
                    for (var r = 0; r < body.Renderers.Length; r++)
                    {
                        var renderer = body.Renderers[r];
                        renderer.enabled = drawn || occluder;
                        renderer.sharedMaterials = occluder
                            ? Enumerable.Repeat(depthOnly, body.Materials[r].Length).ToArray()
                            : body.Materials[r];
                    }
                }
                foreach (var piece in Pieces)
                    foreach (var renderer in piece.Renderers)
                        renderer.enabled = piece == layer.Piece;
            }
        }

        static IEnumerable<LayerTarget> LayersFor(Stage stage, string grip)
        {
            for (var b = 0; b < stage.Bodies.Count; b++)
                yield return new LayerTarget { Layer = AppearanceLayer.Body, Look = BodyLook(stage, b), BodyIndex = b };
            foreach (var piece in stage.Pieces)
            {
                var grips = piece.Definition.grips;
                if (grips.Count > 0 && !grips.Contains(grip))
                    continue;
                yield return new LayerTarget { Layer = piece.Definition.layer, Look = piece.Definition.look, BodyIndex = 0, Piece = piece };
            }
        }

        static string BodyLook(Stage stage, int index) => stage.Bodies[index].Root.name;

        /// <summary>Every transform's local position, rotation and scale under a model, to put back before each frame.</summary>
        sealed class Pose
        {
            readonly Transform[] transforms;
            readonly Vector3[] positions;
            readonly Quaternion[] rotations;
            readonly Vector3[] scales;

            public Pose(Transform root)
            {
                transforms = root.GetComponentsInChildren<Transform>(true).Where(t => t != root).ToArray();
                positions = transforms.Select(t => t.localPosition).ToArray();
                rotations = transforms.Select(t => t.localRotation).ToArray();
                scales = transforms.Select(t => t.localScale).ToArray();
            }

            public void Restore()
            {
                for (var i = 0; i < transforms.Length; i++)
                    transforms[i].SetLocalPositionAndRotation(positions[i], rotations[i]);
                for (var i = 0; i < transforms.Length; i++)
                    transforms[i].localScale = scales[i];
            }
        }

        // The bodies on a turntable at the origin, feet on the origin, all scaled and placed as the first (they share its
        // proportions); the pieces on the first body's bones; the camera at the game's angle with the origin on the pivot
        // pixel; lights that turn with the camera, not the model.
        static Stage BuildStage(SpriteBakeJob job, Scene scene, int rendererIndex)
        {
            var stage = new Stage();
            var turntable = new GameObject("Turntable");
            SceneManager.MoveGameObjectToScene(turntable, scene);
            stage.Turntable = turntable.transform;

            var scale = 1f;
            var offset = Vector3.zero;
            for (var b = 0; b < job.bodies.Count; b++)
            {
                var definition = job.bodies[b];
                var model = (GameObject)Object.Instantiate(definition.model);
                // The look id travels as the instance's name (empty for a plain character).
                model.name = definition.look ?? "";
                SceneManager.MoveGameObjectToScene(model, scene);
                model.transform.SetParent(turntable.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;

                var animator = model.GetComponent<Animator>();
                if (animator == null)
                    animator = model.AddComponent<Animator>();
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                if (b == 0)
                {
                    // Measured in the first body's rest pose (the A-pose of a rig-ready model), facing the camera (S), as
                    // most frames show it; the measure is the whole box, so a little generous.
                    var rest = MeasureBounds(model);
                    scale = SpriteBakeMath.ModelScale(SpriteBakeMath.ScreenHeight(rest.size.y, rest.size.z), job.targetHeightPixels);
                    model.transform.localScale *= scale;
                    rest = MeasureBounds(model);
                    offset = new Vector3(-rest.center.x, -rest.min.y, -rest.center.z);
                }
                else
                    model.transform.localScale *= scale;
                model.transform.localPosition = offset;

                // Taken before the pieces go on, so their own transforms are left alone.
                var restPose = new Pose(model.transform);
                var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(s => new SkinProxy(s, scene)).ToArray();
                var renderers = model.GetComponentsInChildren<MeshRenderer>(true).Cast<Renderer>()
                    .Concat(skins.Select(s => (Renderer)s.Renderer)).ToArray();
                stage.Bodies.Add(new BodyInstance
                {
                    Root = model,
                    Animator = animator,
                    Hips = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Hips) : null,
                    RootPosition = model.transform.localPosition,
                    RestPose = restPose,
                    Renderers = renderers,
                    Materials = renderers.Select(r => r.sharedMaterials).ToArray(),
                    Skins = skins,
                });
            }

            foreach (var piece in job.pieces)
            {
                if (piece.prefab == null)
                    continue;
                var holder = Holder(stage.Bodies[0], piece);
                if (holder == null)
                {
                    Debug.LogWarning($"{job.name}: no bone or path to hold {piece.look}; baked without it.");
                    continue;
                }
                var instance = (GameObject)Object.Instantiate(piece.prefab, holder, false);
                if (piece.autoGrip && stage.Bodies[0].Animator.isHuman)
                    PlaceInFist(stage.Bodies[0], piece, instance.transform, holder);
                else
                {
                    instance.transform.localPosition = piece.localPosition;
                    instance.transform.localRotation = Quaternion.Euler(piece.localEuler);
                    instance.transform.localScale = Vector3.one * piece.scale;
                }
                stage.Pieces.Add(new PieceInstance { Definition = piece, Renderers = instance.GetComponentsInChildren<Renderer>(true) });
            }

            var cameraObject = new GameObject("Bake Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.transform.rotation = Quaternion.Euler(SpriteBakeMath.ElevationDegrees, 0f, 0f);
            var center = SpriteBakeMath.CenterFromPivot(job.cellSize, job.pivot);
            var cameraTransform = cameraObject.transform;
            cameraTransform.position = cameraTransform.right * center.x + cameraTransform.up * center.y - cameraTransform.forward * 50f;

            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.scene = scene;
            camera.orthographic = true;
            camera.orthographicSize = SpriteBakeMath.OrthographicSize(job.cellSize);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.SetRenderer(rendererIndex);
            cameraData.renderPostProcessing = false;
            cameraData.renderShadows = false;
            cameraData.antialiasing = AntialiasingMode.None;
            stage.Camera = camera;

            // Key from the viewer's upper left (the brief, 0.2), a cool fill from the right, a rim from behind.
            AddLight(scene, "Key", new Vector3(0.55f, -0.75f, 0.55f), new Color(1f, 0.95f, 0.88f), 1.5f);
            AddLight(scene, "Fill", new Vector3(-0.8f, -0.25f, 0.3f), new Color(0.7f, 0.78f, 0.9f), 0.35f);
            AddLight(scene, "Rim", new Vector3(0f, -0.35f, -1f), new Color(0.9f, 0.85f, 0.8f), 0.6f);
            return stage;
        }

        /// <summary>
        /// Puts a weapon in a hand from the hand's bones in the rest pose: the haft across the palm (from the little
        /// finger toward the index, so the head is on the thumb side), the blade in line with the knuckles (the way the
        /// fingers point), the grip a little past the middle of the palm. The weapon keeps its real size: it is scaled like
        /// the body, whatever scale the hand bone carries.
        /// </summary>
        static void PlaceInFist(BodyInstance body, SpriteBakeJob.Piece piece, Transform weapon, Transform hand)
        {
            var animator = body.Animator;
            var left = piece.bone == HumanBodyBones.LeftHand;
            var index = animator.GetBoneTransform(left ? HumanBodyBones.LeftIndexProximal : HumanBodyBones.RightIndexProximal);
            var little = animator.GetBoneTransform(left ? HumanBodyBones.LeftLittleProximal : HumanBodyBones.RightLittleProximal);
            var middle = animator.GetBoneTransform(left ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal);
            if (index == null || little == null || middle == null)
            {
                Debug.LogWarning($"{piece.look}: the hand has no finger bones to grip with; placed at the hand.");
                weapon.localPosition = Vector3.zero;
                weapon.localRotation = Quaternion.identity;
                return;
            }
            var up = (index.position - little.position).normalized;
            var forward = Vector3.ProjectOnPlane(middle.position - hand.position, up).normalized;
            weapon.SetPositionAndRotation(Vector3.Lerp(hand.position, middle.position, 0.6f), Quaternion.LookRotation(forward, up));
            weapon.localScale = Vector3.one * (body.Root.transform.lossyScale.x / Mathf.Max(hand.lossyScale.x, 1e-6f)) * piece.scale;
        }

        static Transform Holder(BodyInstance body, SpriteBakeJob.Piece piece)
        {
            if (!string.IsNullOrEmpty(piece.transformPath))
                return body.Root.transform.Find(piece.transformPath);
            return body.Animator.isHuman ? body.Animator.GetBoneTransform(piece.bone) : null;
        }

        // The ground speed a loop looks right at, in game units: the planted foot's slide, sampled 60 times a second on the
        // scaled first body (so the units are the game's), facing the camera, then the body is put back in its rest pose.
        static float MeasureGroundSpeed(BodyInstance body, AnimationClip clip)
        {
            if (!body.Animator.isHuman)
                return 0f;
            var feet = new[] { body.Animator.GetBoneTransform(HumanBodyBones.LeftFoot), body.Animator.GetBoneTransform(HumanBodyBones.RightFoot) };
            if (feet[0] == null || feet[1] == null)
                return 0f;
            const float rate = 60f;
            var count = Mathf.Max(2, Mathf.RoundToInt(clip.length * rate)) + 1;
            var samples = new[] { new Vector3[count], new Vector3[count] };
            for (var i = 0; i < count; i++)
            {
                body.RestPose.Restore();
                clip.SampleAnimation(body.Root, clip.length * i / (count - 1));
                body.Root.transform.localPosition = body.RootPosition;
                body.Root.transform.localRotation = Quaternion.identity;
                samples[0][i] = feet[0].position;
                samples[1][i] = feet[1].position;
            }
            body.RestPose.Restore();
            // A foot within 3 cm (game units; the character stands about 1.3) of its lowest point is planted.
            return SpriteBakeMath.PlantedFootSpeed(samples, rate, 0.03f);
        }

        static void AddLight(Scene scene, string name, Vector3 direction, Color color, float intensity)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }

        // World bounds of every mesh in the model as it stands now. A skinned mesh is baked first, since its renderer's
        // bounds are only brought up to date by rendering.
        static Bounds MeasureBounds(GameObject model)
        {
            var bounds = new Bounds();
            var any = false;
            void Add(Vector3 point)
            {
                if (!any)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    any = true;
                }
                else
                    bounds.Encapsulate(point);
            }

            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    var mesh = new Mesh();
                    skinned.BakeMesh(mesh, false); // See SkinProxy.Update: false with position and rotation is exact.
                    var matrix = Matrix4x4.TRS(skinned.transform.position, skinned.transform.rotation, Vector3.one);
                    foreach (var vertex in mesh.vertices)
                        Add(matrix.MultiplyPoint3x4(vertex));
                    Object.DestroyImmediate(mesh);
                }
                else if (renderer is MeshRenderer)
                {
                    var b = renderer.bounds;
                    Add(b.min);
                    Add(b.max);
                }
            }
            return bounds;
        }

        static Color[] RenderCell(Camera camera, RenderTexture target, Texture2D readback, SpriteBakeJob job)
        {
            var overBlack = RenderOver(camera, target, readback, new Color(0f, 0f, 0f, 0f));
            var overWhite = RenderOver(camera, target, readback, new Color(1f, 1f, 1f, 0f));
            var matte = SpriteBakeMath.Matte(overBlack, overWhite);
            var cell = SpriteBakeMath.Downsample(matte, target.width, target.height, Mathf.Max(1, job.supersample));
            // Rendered and averaged in linear light; the PNG holds sRGB. (A gamma-space project renders gamma already.)
            if (QualitySettings.activeColorSpace != ColorSpace.Linear)
                return cell;
            for (var i = 0; i < cell.Length; i++)
            {
                var c = cell[i];
                var gamma = new Color(c.r, c.g, c.b).gamma;
                cell[i] = new Color(gamma.r, gamma.g, gamma.b, c.a);
            }
            return cell;
        }

        static Color[] RenderOver(Camera camera, RenderTexture target, Texture2D readback, Color background)
        {
            camera.backgroundColor = background;
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            readback.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0, false);
            readback.Apply(false);
            RenderTexture.active = previous;
            return readback.GetPixels();
        }

        /// <summary>
        /// One layer's sheet, filled cell by cell: rows are directions from the top (S first), columns frames; too big for
        /// one texture, a file per direction with its frames in rows from the top.
        /// </summary>
        sealed class SheetWriter
        {
            readonly SpriteBakeJob job;
            readonly string name;
            readonly int frames;
            readonly bool split;
            readonly int directions;
            readonly string[] codes;
            readonly Vector2Int grid;
            readonly Texture2D[] textures;
            readonly List<(string, Rect)>[] rects;

            public SheetWriter(SpriteBakeJob job, string name, int frames)
            {
                this.job = job;
                this.name = name;
                this.frames = frames;
                var cell = job.cellSize;
                directions = job.DirectionCount;
                codes = AppearanceRules.DirectionCodes(directions);
                split = SpriteBakeMath.NeedsSplit(frames, cell, directions);
                grid = split ? SpriteBakeMath.SplitGrid(frames, cell) : new Vector2Int(frames, directions);
                var count = split ? directions : 1;
                textures = new Texture2D[count];
                rects = new List<(string, Rect)>[count];
                for (var i = 0; i < count; i++)
                {
                    textures[i] = new Texture2D(grid.x * cell, grid.y * cell, TextureFormat.RGBA32, false);
                    textures[i].SetPixels(new Color[grid.x * cell * grid.y * cell]);
                    rects[i] = new List<(string, Rect)>();
                }
            }

            public void Put(int row, int frame, Color[] pixels)
            {
                var cell = job.cellSize;
                int file, x, y;
                if (split)
                {
                    file = row;
                    x = frame % grid.x * cell;
                    y = (grid.y - 1 - frame / grid.x) * cell;
                }
                else
                {
                    file = 0;
                    x = frame * cell;
                    y = (directions - 1 - row) * cell;
                }
                textures[file].SetPixels(x, y, cell, cell, pixels);
                rects[file].Add(($"{name}_{codes[row]}_{frame:00}", new Rect(x, y, cell, cell)));
            }

            public IEnumerable<string> Save()
            {
                var folder = job.outputFolder.TrimEnd('/');
                Directory.CreateDirectory(folder);
                var paths = new List<string>();
                for (var i = 0; i < textures.Length; i++)
                {
                    var path = split ? $"{folder}/{name}_{codes[i]}.png" : $"{folder}/{name}.png";
                    File.WriteAllBytes(path, textures[i].EncodeToPNG());
                    Object.DestroyImmediate(textures[i]);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                    ConfigureImporter(job, path, rects[i]);
                    paths.Add(path);
                }
                return paths;
            }
        }

        // Sliced sprites with the feet pivot, 128 pixels per unit. A rebake keeps each sprite's id by name, so animations
        // that already use the sprites keep working.
        static void ConfigureImporter(SpriteBakeJob job, string path, List<(string name, Rect rect)> rects)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = SpriteBakeMath.PixelsPerMeter;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = SpriteBakeMath.MaxSheetSize;
            importer.SaveAndReimport();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var existing = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
            var pivot = new Vector2(job.pivot.x / job.cellSize, job.pivot.y / job.cellSize);
            var spriteRects = rects.Select(r => new SpriteRect
            {
                name = r.name,
                rect = r.rect,
                alignment = SpriteAlignment.Custom,
                pivot = pivot,
                spriteID = existing.TryGetValue(r.name, out var id) ? id : GUID.Generate(),
            }).ToArray();
            provider.SetSpriteRects(spriteRects);
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            names?.SetNameFileIdPairs(spriteRects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        static UniversalRendererData LoadOrCreateRendererData()
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(BakeRendererPath);
            if (data != null)
                return data;
            Directory.CreateDirectory(Path.GetDirectoryName(BakeRendererPath));
            data = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(data, BakeRendererPath);
            AssetDatabase.SaveAssets();
            return data;
        }

        static int AddRenderer(UniversalRenderPipelineAsset pipeline, ScriptableRendererData data)
        {
            var serialized = new SerializedObject(pipeline);
            var list = serialized.FindProperty("m_RendererDataList");
            for (var i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == data)
                    return i;
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = data;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return list.arraySize - 1;
        }

        static void RemoveRenderer(UniversalRenderPipelineAsset pipeline, ScriptableRendererData data)
        {
            var serialized = new SerializedObject(pipeline);
            var list = serialized.FindProperty("m_RendererDataList");
            for (var i = list.arraySize - 1; i >= 0; i--)
            {
                var element = list.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue != data)
                    continue;
                element.objectReferenceValue = null;
                list.DeleteArrayElementAtIndex(i);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
