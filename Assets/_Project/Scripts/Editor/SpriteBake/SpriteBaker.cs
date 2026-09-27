using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ARPG.Editor
{
    /// <summary>
    /// Renders a 3D character into the game's sprite sheets (Docs/09-art-brief.md, Route A): for every clip of a
    /// <see cref="SpriteBakeJob"/>, every direction and every frame, an orthographic render from the game's 30 degree
    /// camera at the job's supersampling, over black and over white to recover transparency (<see cref="SpriteBakeMath.Matte"/>),
    /// averaged down to the final cell, placed by direction row and frame column, written as a PNG and imported as
    /// sliced sprites with the feet pivot at 128 pixels per unit.
    ///
    /// The model is set up in a preview scene, so the open scenes are untouched. The project renders with the 2D
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

        /// <summary>Bakes every clip of the job. Returns the written file paths.</summary>
        public static List<string> Bake(SpriteBakeJob job)
        {
            var written = new List<string>();
            if (job.model == null || job.clips.Count == 0)
            {
                Debug.LogError($"{job.name}: needs a model and at least one clip.");
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
            PlayableGraph graph = default;
            var size = job.cellSize * Mathf.Max(1, job.supersample);
            var target = new RenderTexture(new RenderTextureDescriptor(size, size, RenderTextureFormat.ARGBHalf, 24) { sRGB = false });
            var readback = new Texture2D(size, size, TextureFormat.RGBAHalf, false, true);

            try
            {
                var setup = BuildStage(job, scene, rendererIndex);
                setup.Camera.targetTexture = target;
                // The first render after the renderer is added comes out unlit (the bake's first test cell did);
                // one thrown away warms it up.
                setup.Camera.Render();

                foreach (var clip in job.clips)
                {
                    if (clip.clip == null || string.IsNullOrEmpty(clip.name))
                        continue;

                    var times = SpriteBakeMath.SampleTimes(clip.clip.length, clip.frames, clip.loop, job.framesPerSecond);
                    var cells = new Color[SpriteBakeMath.DirectionCount, times.Length][];

                    var clipPlayable = AnimationPlayableUtilities.PlayClip(setup.Animator, clip.clip, out graph);
                    clipPlayable.SetApplyFootIK(false);
                    graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

                    for (var row = 0; row < SpriteBakeMath.DirectionCount; row++)
                    {
                        setup.Turntable.rotation = Quaternion.LookRotation(SpriteBakeMath.Facing(row), Vector3.up);
                        for (var frame = 0; frame < times.Length; frame++)
                        {
                            // A generic clip writes only the bones it animates; the rest would keep the last clip's pose
                            // (the first test attack ran on frozen running legs).
                            setup.RestPose.Restore();
                            clipPlayable.SetTime(times[frame]);
                            clipPlayable.SetTime(times[frame]); // Twice, so no root motion delta is carried from the last sample.
                            graph.Evaluate(0f);
                            cells[row, frame] = RenderCell(setup.Camera, target, readback, job);
                        }
                    }
                    graph.Destroy();

                    written.AddRange(WriteSheets(job, clip.name, cells, times.Length));
                }
            }
            finally
            {
                if (graph.IsValid())
                    graph.Destroy();
                EditorSceneManager.ClosePreviewScene(scene);
                RemoveRenderer(pipeline, rendererData);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(readback);
            }

            Debug.Log($"{job.name}: baked {written.Count} sheet(s):\n" + string.Join("\n", written));
            return written;
        }

        struct Stage
        {
            public Camera Camera;
            public Animator Animator;
            public Transform Turntable;
            public Pose RestPose;
        }

        /// <summary>Every transform's local position, rotation and scale under the model, to put back before each frame.</summary>
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

        // The model on a turntable at the origin, feet on the origin, scaled to the job's height; the weapon in its hand;
        // the camera at the game's angle with the origin on the pivot pixel; lights that turn with the camera, not the model.
        static Stage BuildStage(SpriteBakeJob job, Scene scene, int rendererIndex)
        {
            var turntable = new GameObject("Turntable");
            SceneManager.MoveGameObjectToScene(turntable, scene);

            var model = (GameObject)Object.Instantiate(job.model);
            model.name = job.model.name;
            SceneManager.MoveGameObjectToScene(model, scene);
            model.transform.SetParent(turntable.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            var animator = model.GetComponent<Animator>();
            if (animator == null)
                animator = model.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Measured in the model's rest pose (the A-pose of a rig-ready model), before the weapon is added.
            var rest = MeasureBounds(model);
            // Facing the camera (S), as most frames show it; the measure is the whole box, so a little generous.
            var scale = SpriteBakeMath.ModelScale(SpriteBakeMath.ScreenHeight(rest.size.y, rest.size.z), job.targetHeightPixels);
            model.transform.localScale *= scale;
            rest = MeasureBounds(model);
            model.transform.localPosition = new Vector3(-rest.center.x, -rest.min.y, -rest.center.z);

            // Taken before the weapon goes on, so the weapon's own transforms are left alone.
            var restPose = new Pose(model.transform);
            AttachWeapon(job, model, animator);

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

            // Key from the viewer's upper left (the brief, 0.2), a cool fill from the right, a rim from behind.
            AddLight(scene, "Key", new Vector3(0.55f, -0.75f, 0.55f), new Color(1f, 0.95f, 0.88f), 1.5f);
            AddLight(scene, "Fill", new Vector3(-0.8f, -0.25f, 0.3f), new Color(0.7f, 0.78f, 0.9f), 0.35f);
            AddLight(scene, "Rim", new Vector3(0f, -0.35f, -1f), new Color(0.9f, 0.85f, 0.8f), 0.6f);

            return new Stage { Camera = camera, Animator = animator, Turntable = turntable.transform, RestPose = restPose };
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

        static void AttachWeapon(SpriteBakeJob job, GameObject model, Animator animator)
        {
            var weapon = job.weapon;
            if (weapon == null || weapon.prefab == null)
                return;
            Transform holder = null;
            if (!string.IsNullOrEmpty(weapon.transformPath))
                holder = model.transform.Find(weapon.transformPath);
            else if (animator.isHuman)
                holder = animator.GetBoneTransform(weapon.bone);
            if (holder == null)
            {
                Debug.LogWarning($"{job.name}: no bone or path to hold the weapon; baked without it.");
                return;
            }
            var instance = (GameObject)Object.Instantiate(weapon.prefab, holder, false);
            instance.transform.localPosition = weapon.localPosition;
            instance.transform.localRotation = Quaternion.Euler(weapon.localEuler);
            instance.transform.localScale = Vector3.one * weapon.scale;
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
                    skinned.BakeMesh(mesh, true);
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

        static IEnumerable<string> WriteSheets(SpriteBakeJob job, string clipName, Color[,][] cells, int frames)
        {
            var cell = job.cellSize;
            var folder = job.outputFolder.TrimEnd('/');
            Directory.CreateDirectory(folder);
            var paths = new List<string>();

            if (!SpriteBakeMath.NeedsSplit(frames, cell))
            {
                // One sheet: rows are directions from the top (S first), columns are frames.
                var sheet = NewSheet(frames * cell, SpriteBakeMath.DirectionCount * cell);
                var rects = new List<(string, Rect)>();
                for (var row = 0; row < SpriteBakeMath.DirectionCount; row++)
                    for (var frame = 0; frame < frames; frame++)
                    {
                        var x = frame * cell;
                        var y = (SpriteBakeMath.DirectionCount - 1 - row) * cell;
                        sheet.SetPixels(x, y, cell, cell, cells[row, frame]);
                        rects.Add(($"{job.characterName}_{clipName}_{SpriteBakeMath.DirectionCodes[row]}_{frame:00}", new Rect(x, y, cell, cell)));
                    }
                paths.Add(Save(job, sheet, $"{folder}/{job.characterName}_{clipName}.png", rects));
                return paths;
            }

            // Too big for one texture: a file per direction, frames in rows from the top.
            var grid = SpriteBakeMath.SplitGrid(frames, cell);
            for (var row = 0; row < SpriteBakeMath.DirectionCount; row++)
            {
                var code = SpriteBakeMath.DirectionCodes[row];
                var sheet = NewSheet(grid.x * cell, grid.y * cell);
                var rects = new List<(string, Rect)>();
                for (var frame = 0; frame < frames; frame++)
                {
                    var x = frame % grid.x * cell;
                    var y = (grid.y - 1 - frame / grid.x) * cell;
                    sheet.SetPixels(x, y, cell, cell, cells[row, frame]);
                    rects.Add(($"{job.characterName}_{clipName}_{code}_{frame:00}", new Rect(x, y, cell, cell)));
                }
                paths.Add(Save(job, sheet, $"{folder}/{job.characterName}_{clipName}_{code}.png", rects));
            }
            return paths;
        }

        static Texture2D NewSheet(int width, int height)
        {
            var sheet = new Texture2D(width, height, TextureFormat.RGBA32, false);
            sheet.SetPixels(new Color[width * height]);
            return sheet;
        }

        static string Save(SpriteBakeJob job, Texture2D sheet, string path, List<(string name, Rect rect)> rects)
        {
            File.WriteAllBytes(path, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            ConfigureImporter(job, path, rects);
            return path;
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
