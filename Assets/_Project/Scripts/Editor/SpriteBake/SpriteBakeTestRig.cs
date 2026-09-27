using System.IO;
using UnityEditor;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// A stand-in to prove the sprite bake before real models exist: a mannequin of primitives with a generic rig (a red
    /// nose on the front of the face, the pauldron and a glowing lantern on its left side, so a render shows at once
    /// whether a direction or a hand is wrong), an axe for its right hand, three clips made in code (idle, run, a one-shot
    /// attack) and a job that bakes them. Everything goes to Assets/_Project/Art/SpriteBakeTest, which git ignores.
    /// </summary>
    public static class SpriteBakeTestRig
    {
        public const string Folder = "Assets/_Project/Art/SpriteBakeTest";

        [MenuItem("Tools/ARPG/Sprite Bake/Bake Test Mannequin")]
        public static void BakeTestMannequin() => SpriteBaker.Bake(CreateJob());

        public static SpriteBakeJob CreateJob()
        {
            Directory.CreateDirectory(Folder);
            var skin = Material("Skin", new Color(0.62f, 0.48f, 0.4f));
            var leather = Material("Leather", new Color(0.3f, 0.2f, 0.13f));
            var cloth = Material("Cloth", new Color(0.42f, 0.14f, 0.1f));
            var iron = Material("Iron", new Color(0.36f, 0.35f, 0.34f), 0.6f);
            var wood = Material("Wood", new Color(0.33f, 0.22f, 0.14f));
            var marker = Material("Nose", new Color(0.9f, 0.1f, 0.1f));
            var ember = Material("Ember", new Color(1f, 0.5f, 0.15f), 0f, new Color(2.5f, 1.1f, 0.3f));

            var mannequin = SavePrefab(BuildMannequin(skin, leather, cloth, iron, marker, ember), "Mannequin");
            var axe = SavePrefab(BuildAxe(wood, iron), "Axe");

            var job = AssetDatabase.LoadAssetAtPath<SpriteBakeJob>($"{Folder}/MannequinJob.asset");
            if (job == null)
            {
                job = ScriptableObject.CreateInstance<SpriteBakeJob>();
                AssetDatabase.CreateAsset(job, $"{Folder}/MannequinJob.asset");
            }
            job.characterName = "mannequin";
            job.model = mannequin;
            job.weapon = new SpriteBakeJob.Weapon { prefab = axe, transformPath = "Hips/Spine/ArmR/HandR" };
            job.clips.Clear();
            job.clips.Add(new SpriteBakeJob.Clip { name = "idle", clip = Idle(), frames = 12, loop = true });
            job.clips.Add(new SpriteBakeJob.Clip { name = "run", clip = Run(), frames = 10, loop = true });
            job.clips.Add(new SpriteBakeJob.Clip { name = "attack", clip = Attack(), frames = 10, loop = false });
            job.outputFolder = $"{Folder}/Output";
            EditorUtility.SetDirty(job);
            AssetDatabase.SaveAssets();
            return job;
        }

        // Faces +z; its right is +x. About 1.9 m tall, so the bake's height scaling is exercised.
        static GameObject BuildMannequin(Material skin, Material leather, Material cloth, Material iron, Material marker, Material ember)
        {
            var root = new GameObject("Mannequin");
            var hips = Node(root.transform, "Hips", new Vector3(0f, 0.95f, 0f));
            Part(hips, "Pelvis", PrimitiveType.Cube, Vector3.zero, new Vector3(0.36f, 0.2f, 0.22f), cloth);
            var spine = Node(hips, "Spine", new Vector3(0f, 0.1f, 0f));
            Part(spine, "Chest", PrimitiveType.Cube, new Vector3(0f, 0.3f, 0f), new Vector3(0.48f, 0.55f, 0.28f), leather);
            Part(spine, "Head", PrimitiveType.Sphere, new Vector3(0f, 0.78f, 0f), new Vector3(0.26f, 0.3f, 0.28f), skin);
            Part(spine, "Nose", PrimitiveType.Cube, new Vector3(0f, 0.78f, 0.15f), new Vector3(0.07f, 0.07f, 0.1f), marker);
            Part(spine, "Pauldron", PrimitiveType.Cube, new Vector3(-0.3f, 0.56f, 0f), new Vector3(0.22f, 0.14f, 0.32f), iron);
            foreach (var (side, x) in new[] { ("L", -0.3f), ("R", 0.3f) })
            {
                var arm = Node(spine, "Arm" + side, new Vector3(x, 0.5f, 0f));
                Part(arm, "UpperArm" + side, PrimitiveType.Cube, new Vector3(0f, -0.3f, 0f), new Vector3(0.13f, 0.6f, 0.13f), skin);
                Node(arm, "Hand" + side, new Vector3(0f, -0.62f, 0f));
                var leg = Node(hips, "Leg" + side, new Vector3(x * 0.4f, -0.06f, 0f));
                Part(leg, "Thigh" + side, PrimitiveType.Cube, new Vector3(0f, -0.44f, 0f), new Vector3(0.16f, 0.86f, 0.17f), cloth);
                Part(leg, "Foot" + side, PrimitiveType.Cube, new Vector3(0f, -0.86f, 0.06f), new Vector3(0.17f, 0.08f, 0.3f), leather);
            }
            Part(hips, "Lantern", PrimitiveType.Sphere, new Vector3(-0.25f, -0.06f, 0.06f), new Vector3(0.1f, 0.14f, 0.1f), ember);
            return root;
        }

        // Hangs from the hand along the arm, the blade facing forward.
        static GameObject BuildAxe(Material wood, Material iron)
        {
            var root = new GameObject("Axe");
            Part(root.transform, "Haft", PrimitiveType.Cube, new Vector3(0f, -0.12f, 0f), new Vector3(0.04f, 0.75f, 0.04f), wood);
            Part(root.transform, "Head", PrimitiveType.Cube, new Vector3(0f, -0.42f, 0.11f), new Vector3(0.03f, 0.22f, 0.2f), iron);
            return root;
        }

        static Transform Node(Transform parent, string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            return go.transform;
        }

        static void Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        static GameObject SavePrefab(GameObject instance, string name)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, $"{Folder}/{name}.prefab");
            Object.DestroyImmediate(instance);
            return prefab;
        }

        static Material Material(string name, Color color, float smoothness = 0.2f, Color emission = default)
        {
            var path = $"{Folder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            if (emission != default)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        // Generic clips, keyed as local rotations and positions on the mannequin's paths.
        static AnimationClip Idle() => SaveClip("Idle", 1.2f, (clip, t) =>
        {
            var breath = Mathf.Sin(t * Mathf.PI * 2f);
            Rotation(clip, "Hips/Spine", t, 1.2f, new Vector3(breath * 2f, 0f, 0f));
            Rotation(clip, "Hips/Spine/ArmL", t, 1.2f, new Vector3(0f, 0f, -6f + breath * 2f));
            Rotation(clip, "Hips/Spine/ArmR", t, 1.2f, new Vector3(0f, 0f, 6f - breath * 2f));
        });

        static AnimationClip Run() => SaveClip("Run", 0.8f, (clip, t) =>
        {
            var swing = Mathf.Sin(t * Mathf.PI * 2f);
            Rotation(clip, "Hips/Spine", t, 0.8f, new Vector3(12f, 0f, 0f));
            Rotation(clip, "Hips/Spine/ArmL", t, 0.8f, new Vector3(swing * 45f, 0f, -8f));
            Rotation(clip, "Hips/Spine/ArmR", t, 0.8f, new Vector3(-swing * 45f, 0f, 8f));
            Rotation(clip, "Hips/LegL", t, 0.8f, new Vector3(-swing * 40f, 0f, 0f));
            Rotation(clip, "Hips/LegR", t, 0.8f, new Vector3(swing * 40f, 0f, 0f));
            Position(clip, "Hips", t, 0.8f, new Vector3(0f, 0.95f - Mathf.Abs(swing) * 0.05f, 0f));
        });

        // The axe arm raised back, then swept across the body; the hit lands about halfway.
        static AnimationClip Attack() => SaveClip("Attack", 0.8f, (clip, t) =>
        {
            var windup = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.4f));
            var strike = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.4f) / 0.2f));
            var recover = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.6f) / 0.4f));
            var raise = Mathf.Lerp(Mathf.Lerp(0f, -150f, windup), -70f, strike);
            var across = Mathf.Lerp(0f, -80f, strike);
            Rotation(clip, "Hips/Spine/ArmR", t, 0.8f, Vector3.Lerp(new Vector3(raise, across, 10f), new Vector3(0f, 0f, 6f), recover));
            Rotation(clip, "Hips/Spine", t, 0.8f, Vector3.Lerp(new Vector3(0f, Mathf.Lerp(20f, -35f, strike), 0f), Vector3.zero, recover));
        });

        static AnimationClip SaveClip(string name, float length, System.Action<AnimationClip, float> key)
        {
            var path = $"{Folder}/{name}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }
            clip.ClearCurves();
            const int keys = 24;
            for (var i = 0; i <= keys; i++)
                key(clip, length * i / keys);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        static void Rotation(AnimationClip clip, string path, float time, float length, Vector3 euler)
        {
            var rotation = Quaternion.Euler(euler);
            AddKey(clip, path, "m_LocalRotation.x", time, rotation.x);
            AddKey(clip, path, "m_LocalRotation.y", time, rotation.y);
            AddKey(clip, path, "m_LocalRotation.z", time, rotation.z);
            AddKey(clip, path, "m_LocalRotation.w", time, rotation.w);
        }

        static void Position(AnimationClip clip, string path, float time, float length, Vector3 position)
        {
            AddKey(clip, path, "m_LocalPosition.x", time, position.x);
            AddKey(clip, path, "m_LocalPosition.y", time, position.y);
            AddKey(clip, path, "m_LocalPosition.z", time, position.z);
        }

        static void AddKey(AnimationClip clip, string path, string property, float time, float value)
        {
            var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), property);
            var curve = AnimationUtility.GetEditorCurve(clip, binding) ?? new AnimationCurve();
            curve.AddKey(time, value);
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }
    }
}
