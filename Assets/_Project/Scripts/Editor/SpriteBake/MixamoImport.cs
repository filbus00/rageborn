using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// Setting Mixamo downloads up for the sprite bake, shared by the Wrathborn's setup and the enemies': a body with
    /// skin gets a humanoid rig of its own and its texture (Mixamo's re-export drops it), an animation file gets a
    /// humanoid rig copied from a body and its clip kept in place, and a one-shot's action window is found from how
    /// much the body moves (Mixamo clips carry long stances around the action).
    /// </summary>
    internal static class MixamoImport
    {
        // How much the body moves over a clip, 60 samples a second: the distance the hands, feet, head and hips travel
        // between samples, relative to the hips' start, on the body model at its own size.
        internal static Vector2 ActionWindow(string bodyPath, AnimationClip clip, bool keepEnd)
        {
            var body = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(bodyPath));
            try
            {
                var animator = body.GetComponent<Animator>();
                var bones = new[]
                {
                    HumanBodyBones.LeftHand, HumanBodyBones.RightHand, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot,
                    HumanBodyBones.Head, HumanBodyBones.Hips,
                }.Select(animator.GetBoneTransform).Where(t => t != null).ToArray();
                const float rate = 60f;
                var count = Mathf.Max(2, Mathf.RoundToInt(clip.length * rate));
                var motion = new float[count];
                var previous = new Vector3[bones.Length];
                for (var i = 0; i <= count; i++)
                {
                    clip.SampleAnimation(body, clip.length * i / count);
                    for (var b = 0; b < bones.Length; b++)
                    {
                        var position = body.transform.InverseTransformPoint(bones[b].position);
                        if (i > 0)
                            motion[i - 1] += (position - previous[b]).magnitude;
                        previous[b] = position;
                    }
                }
                return SpriteBakeMath.ActiveWindow(motion, rate, clip.length, 0.2f, keepEnd);
            }
            finally
            {
                Object.DestroyImmediate(body);
            }
        }

        internal static void ConfigureBody(string path)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            // The body's own clip is the idle it was downloaded with, which loops.
            KeepClipsInPlace(importer, true);
            importer.SaveAndReimport();

            ApplyTexture(path, importer);
        }

        // Mixamo's re-export does not carry the texture into Unity (the first bake came out white), so the texture taken
        // from the image-to-3D GLB (<model>_albedo.png, ArtSource/tools/extract_textures.py) goes on a material of our
        // own that replaces the model's. Mixamo keeps the mesh and its UVs, so the texture fits.
        internal static void ApplyTexture(string path, ModelImporter importer)
        {
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

        // Sets a Mixamo animation file up as a humanoid clip in place, and returns the clip named clipName: the file's own
        // clip, or with mirrored a second, mirrored copy of it added to the same file.
        internal static AnimationClip ConfigureAnimation(string path, bool loop, Avatar avatar, string clipName, bool mirrored)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = avatar;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            var clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations.ToList() : importer.defaultClipAnimations.ToList();
            var own = clips[0];
            if (!mirrored)
                own.name = clipName;
            SetInPlace(own, loop);
            clips[0] = own;
            if (mirrored)
            {
                clips.RemoveAll(c => c.name == clipName);
                var copy = importer.defaultClipAnimations[0];
                copy.name = clipName;
                copy.mirror = true;
                SetInPlace(copy, loop);
                clips.Add(copy);
            }
            importer.clipAnimations = clips.ToArray();
            importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => c.name == clipName);
        }

        internal static void SetInPlace(ModelImporterClipAnimation clip, bool loop)
        {
            clip.loopTime = loop;
            clip.lockRootRotation = true;
            clip.lockRootHeightY = true;
            // A loop's travel goes to the root, which the bake puts back each frame, so a run downloaded without In Place
            // stays on its spot (the shield's and the two-hander's runs, 2026-09-29, ran out of their cells). One-shots
            // keep it in the pose: the bake holds their hips over their first spot itself.
            clip.lockRootPositionXZ = !loop;
            clip.keepOriginalOrientation = true;
            clip.keepOriginalPositionY = true;
            // Based on the centre of mass for a loop: a Mixamo rig carries its travel on the hips, not on its root, so
            // "Original" extracted nothing and the run still left its cell.
            clip.keepOriginalPositionXZ = !loop;
        }

        internal static void KeepClipsInPlace(ModelImporter importer, bool loop)
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

        internal static AnimationClip FirstClip(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
    }
}
