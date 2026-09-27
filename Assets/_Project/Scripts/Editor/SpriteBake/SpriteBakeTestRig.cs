using System.IO;
using UnityEditor;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// A stand-in to prove the sprite bake and the layered character before real models exist: a mannequin of primitives
    /// with a generic rig (a red nose on the front of the face and a glowing lantern on its left side, so a render shows at
    /// once whether a direction or a hand is wrong) in four body looks (bare, padded, leather with a pauldron, mail),
    /// three helms, three one-handed axes, a two-handed great axe, an off-hand hatchet and a shield, with an idle, a run and
    /// an attack made in code for each grip (1h, dual, shield, 2h). The sheets go to
    /// Assets/_Project/Art/SpriteBakeTest/Resources/Characters/mannequin, so the game can show them
    /// (<see cref="PlayerSpriteAnimator.Character"/> = "mannequin"). The folder is git-ignored; delete it with
    /// Delete Test Mannequin before a build, since a Resources folder goes into the build.
    /// </summary>
    public static class SpriteBakeTestRig
    {
        public const string Folder = "Assets/_Project/Art/SpriteBakeTest";
        public const string Character = "mannequin";

        const string RightHand = "Hips/Spine/ArmR/HandR";
        const string LeftHand = "Hips/Spine/ArmL/HandL";
        const string LeftArm = "Hips/Spine/ArmL";
        const string Spine = "Hips/Spine";

        [MenuItem("Tools/ARPG/Sprite Bake/Bake Test Mannequin")]
        public static void BakeTestMannequin() => SpriteBaker.Bake(CreateJob());

        [MenuItem("Tools/ARPG/Sprite Bake/Delete Test Mannequin")]
        public static void DeleteTestMannequin()
        {
            AssetDatabase.DeleteAsset(Folder);
            Debug.Log($"Deleted {Folder}.");
        }

        public static SpriteBakeJob CreateJob()
        {
            Directory.CreateDirectory(Folder);
            var skin = Material("Skin", new Color(0.62f, 0.48f, 0.4f));
            var linen = Material("Linen", new Color(0.55f, 0.5f, 0.42f));
            var padded = Material("Padded", new Color(0.62f, 0.55f, 0.38f));
            var leather = Material("Leather", new Color(0.3f, 0.2f, 0.13f));
            var mail = Material("Mail", new Color(0.42f, 0.42f, 0.44f), 0.7f);
            var cloth = Material("Cloth", new Color(0.42f, 0.14f, 0.1f));
            var iron = Material("Iron", new Color(0.36f, 0.35f, 0.34f), 0.6f);
            var gold = Material("Brass", new Color(0.6f, 0.45f, 0.15f), 0.6f);
            var wood = Material("Wood", new Color(0.33f, 0.22f, 0.14f));
            var marker = Material("Nose", new Color(0.9f, 0.1f, 0.1f));
            var ember = Material("Ember", new Color(1f, 0.5f, 0.15f), 0f, new Color(2.5f, 1.1f, 0.3f));

            var job = AssetDatabase.LoadAssetAtPath<SpriteBakeJob>($"{Folder}/MannequinJob.asset");
            if (job == null)
            {
                job = ScriptableObject.CreateInstance<SpriteBakeJob>();
                AssetDatabase.CreateAsset(job, $"{Folder}/MannequinJob.asset");
            }
            job.characterName = Character;
            job.bodies.Clear();
            // The first body is the reference the pieces hang on; the leather look, as for the Wrathborn.
            job.bodies.Add(Body("leather", BuildMannequin(skin, leather, cloth, iron, marker, ember, 0.48f, true)));
            job.bodies.Add(Body("bare", BuildMannequin(skin, linen, cloth, iron, marker, ember, 0.44f, false)));
            job.bodies.Add(Body("padded", BuildMannequin(skin, padded, cloth, iron, marker, ember, 0.5f, false)));
            job.bodies.Add(Body("mail", BuildMannequin(skin, mail, mail, iron, marker, ember, 0.52f, true)));

            job.pieces.Clear();
            var headHeight = new Vector3(0f, 0.9f, 0f);
            job.pieces.Add(Piece(AppearanceLayer.Helm, "cap", Helm("Cap", iron, 0.3f, 0.12f, false), Spine, headHeight));
            job.pieces.Add(Piece(AppearanceLayer.Helm, "nasal", Helm("Nasal", iron, 0.3f, 0.2f, true), Spine, headHeight));
            job.pieces.Add(Piece(AppearanceLayer.Helm, "great", Helm("Great", gold, 0.34f, 0.36f, false), Spine, headHeight + new Vector3(0f, -0.1f, 0f)));
            string[] oneHandGrips = { "1h", "dual", "shield" };
            job.pieces.Add(Piece(AppearanceLayer.Weapon, "hatchet", Axe("Hatchet", wood, iron, 0.5f, 0.14f), RightHand, Vector3.zero, oneHandGrips));
            job.pieces.Add(Piece(AppearanceLayer.Weapon, "bearded_axe", Axe("BeardedAxe", wood, iron, 0.75f, 0.22f), RightHand, Vector3.zero, oneHandGrips));
            job.pieces.Add(Piece(AppearanceLayer.Weapon, "war_axe", Axe("WarAxe", wood, gold, 0.85f, 0.3f), RightHand, Vector3.zero, oneHandGrips));
            job.pieces.Add(Piece(AppearanceLayer.Weapon, "great_axe", Axe("GreatAxe", wood, iron, 1.3f, 0.4f), RightHand, new Vector3(0f, 0.3f, 0f), "2h"));
            job.pieces.Add(Piece(AppearanceLayer.OffHand, "hatchet", Axe("Hatchet", wood, iron, 0.5f, 0.14f), LeftHand, Vector3.zero, "dual"));
            job.pieces.Add(Piece(AppearanceLayer.OffHand, "buckler", Shield(wood, iron), LeftArm, new Vector3(-0.1f, -0.45f, 0.05f), "shield"));

            job.grips.Clear();
            foreach (var grip in new[] { "1h", "dual", "shield", "2h" })
                job.grips.Add(new SpriteBakeJob.GripSet
                {
                    grip = grip,
                    clips =
                    {
                        new SpriteBakeJob.Clip { name = "idle", clip = Idle(grip), frames = 12, loop = true },
                        new SpriteBakeJob.Clip { name = "run", clip = Run(grip), frames = 10, loop = true },
                        new SpriteBakeJob.Clip { name = "attack", clip = Attack(grip), frames = 10, loop = false },
                    },
                });

            // Half the brief's supersampling: a test, many layers.
            job.supersample = 2;
            job.outputFolder = $"{Folder}/Resources/Characters/{Character}";
            EditorUtility.SetDirty(job);
            AssetDatabase.SaveAssets();
            return job;
        }

        static SpriteBakeJob.Body Body(string look, GameObject instance) =>
            new SpriteBakeJob.Body { look = look, model = SavePrefab(instance, "Body_" + look) };

        static SpriteBakeJob.Piece Piece(AppearanceLayer layer, string look, GameObject instance, string path, Vector3 position, params string[] grips)
        {
            var piece = new SpriteBakeJob.Piece
            {
                layer = layer,
                look = look,
                prefab = SavePrefab(instance, $"{layer}_{look}"),
                transformPath = path,
                localPosition = position,
            };
            piece.grips.AddRange(grips);
            return piece;
        }

        // Faces +z; its right is +x. About 1.9 m tall, so the bake's height scaling is exercised. The chest's width and
        // material make the body looks tell apart; the leather and mail looks have a pauldron on the left shoulder.
        static GameObject BuildMannequin(Material skin, Material chest, Material legs, Material iron, Material marker, Material ember, float chestWidth, bool pauldron)
        {
            var root = new GameObject("Mannequin");
            var hips = Node(root.transform, "Hips", new Vector3(0f, 0.95f, 0f));
            Part(hips, "Pelvis", PrimitiveType.Cube, Vector3.zero, new Vector3(0.36f, 0.2f, 0.22f), legs);
            var spine = Node(hips, "Spine", new Vector3(0f, 0.1f, 0f));
            Part(spine, "Chest", PrimitiveType.Cube, new Vector3(0f, 0.3f, 0f), new Vector3(chestWidth, 0.55f, 0.28f), chest);
            Part(spine, "Head", PrimitiveType.Sphere, new Vector3(0f, 0.78f, 0f), new Vector3(0.26f, 0.3f, 0.28f), skin);
            Part(spine, "Nose", PrimitiveType.Cube, new Vector3(0f, 0.78f, 0.15f), new Vector3(0.07f, 0.07f, 0.1f), marker);
            if (pauldron)
                Part(spine, "Pauldron", PrimitiveType.Cube, new Vector3(-0.3f, 0.56f, 0f), new Vector3(0.22f, 0.14f, 0.32f), iron);
            foreach (var (side, x) in new[] { ("L", -0.3f), ("R", 0.3f) })
            {
                var arm = Node(spine, "Arm" + side, new Vector3(x, 0.5f, 0f));
                Part(arm, "UpperArm" + side, PrimitiveType.Cube, new Vector3(0f, -0.3f, 0f), new Vector3(0.13f, 0.6f, 0.13f), skin);
                Node(arm, "Hand" + side, new Vector3(0f, -0.62f, 0f));
                var leg = Node(hips, "Leg" + side, new Vector3(x * 0.4f, -0.06f, 0f));
                Part(leg, "Thigh" + side, PrimitiveType.Cube, new Vector3(0f, -0.44f, 0f), new Vector3(0.16f, 0.86f, 0.17f), legs);
                Part(leg, "Foot" + side, PrimitiveType.Cube, new Vector3(0f, -0.86f, 0.06f), new Vector3(0.17f, 0.08f, 0.3f), iron);
            }
            Part(hips, "Lantern", PrimitiveType.Sphere, new Vector3(-0.25f, -0.06f, 0.06f), new Vector3(0.1f, 0.14f, 0.1f), ember);
            return root;
        }

        // Sits on the head (placed at the head's height on the spine); the nasal has a guard down the face.
        static GameObject Helm(string name, Material metal, float width, float height, bool nasal)
        {
            var root = new GameObject(name);
            Part(root.transform, "Dome", PrimitiveType.Cylinder, Vector3.zero, new Vector3(width, height * 0.5f, width * 1.05f), metal);
            if (nasal)
                Part(root.transform, "Guard", PrimitiveType.Cube, new Vector3(0f, -0.1f, 0.15f), new Vector3(0.04f, 0.14f, 0.03f), metal);
            return root;
        }

        // Hangs from the hand along the arm, the blade facing forward.
        static GameObject Axe(string name, Material wood, Material metal, float length, float blade)
        {
            var root = new GameObject(name);
            Part(root.transform, "Haft", PrimitiveType.Cube, new Vector3(0f, 0.25f - length * 0.5f, 0f), new Vector3(0.04f, length, 0.04f), wood);
            Part(root.transform, "Head", PrimitiveType.Cube, new Vector3(0f, 0.3f - length, blade * 0.5f), new Vector3(0.03f, blade, blade), metal);
            return root;
        }

        // Strapped to the outside of the left forearm, facing out.
        static GameObject Shield(Material wood, Material iron)
        {
            var root = new GameObject("Buckler");
            Part(root.transform, "Board", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.45f, 0.02f, 0.45f), wood);
            Part(root.transform, "Boss", PrimitiveType.Sphere, new Vector3(0f, 0.03f, 0f), new Vector3(0.1f, 0.06f, 0.1f), iron);
            root.transform.GetChild(0).localRotation = Quaternion.Euler(0f, 0f, 90f);
            root.transform.GetChild(1).localPosition = new Vector3(-0.03f, 0f, 0f);
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
        // The left arm by grip: at the side (1h), swinging like the right (dual), the shield held forward (shield), or on
        // the haft beside the right hand (2h).
        static Vector3 LeftArmPose(string grip, float swing) => grip switch
        {
            "shield" => new Vector3(-55f, 0f, -20f),
            "2h" => new Vector3(-35f, 0f, 25f),
            _ => new Vector3(swing, 0f, -8f),
        };

        static Vector3 RightArmRest(string grip) => grip == "2h" ? new Vector3(-35f, 0f, -25f) : new Vector3(0f, 0f, 6f);

        // Generic clips per grip, keyed as local rotations and positions on the mannequin's paths.
        static AnimationClip Idle(string grip) => SaveClip($"Idle_{grip}", 1.2f, (clip, t) =>
        {
            var breath = Mathf.Sin(t / 1.2f * Mathf.PI * 2f);
            Rotation(clip, Spine, new Vector3(breath * 2f, 0f, 0f), t);
            Rotation(clip, LeftArm, LeftArmPose(grip, breath * 2f), t);
            Rotation(clip, "Hips/Spine/ArmR", RightArmRest(grip) + new Vector3(0f, 0f, -breath * 2f), t);
        });

        static AnimationClip Run(string grip) => SaveClip($"Run_{grip}", 0.8f, (clip, t) =>
        {
            var swing = Mathf.Sin(t / 0.8f * Mathf.PI * 2f);
            Rotation(clip, Spine, new Vector3(12f, 0f, 0f), t);
            Rotation(clip, LeftArm, LeftArmPose(grip, swing * 45f), t);
            Rotation(clip, "Hips/Spine/ArmR", grip == "2h" ? RightArmRest(grip) : new Vector3(-swing * 45f, 0f, 8f), t);
            Rotation(clip, "Hips/LegL", new Vector3(-swing * 40f, 0f, 0f), t);
            Rotation(clip, "Hips/LegR", new Vector3(swing * 40f, 0f, 0f), t);
            Position(clip, "Hips", new Vector3(0f, 0.95f - Mathf.Abs(swing) * 0.05f, 0f), t);
        });

        // The weapon arm raised back, then swept across the body; the hit lands about halfway. A two-handed swing brings
        // both arms; dual wield follows with the left.
        static AnimationClip Attack(string grip) => SaveClip($"Attack_{grip}", 0.8f, (clip, t) =>
        {
            var windup = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.4f));
            var strike = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.4f) / 0.2f));
            var recover = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.6f) / 0.4f));
            var raise = Mathf.Lerp(Mathf.Lerp(0f, -150f, windup), -70f, strike);
            var across = Mathf.Lerp(0f, -80f, strike);
            var right = Vector3.Lerp(new Vector3(raise, across, 10f), RightArmRest(grip), recover);
            Rotation(clip, "Hips/Spine/ArmR", right, t);
            var left = grip switch
            {
                "2h" => new Vector3(right.x, -right.y, -right.z),
                "dual" => Vector3.Lerp(new Vector3(Mathf.Lerp(0f, -70f, windup), Mathf.Lerp(0f, 60f, strike), -10f), LeftArmPose(grip, 0f), recover),
                _ => LeftArmPose(grip, 0f),
            };
            Rotation(clip, LeftArm, left, t);
            Rotation(clip, Spine, Vector3.Lerp(new Vector3(0f, Mathf.Lerp(20f, -35f, strike), 0f), Vector3.zero, recover), t);
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

        static void Rotation(AnimationClip clip, string path, Vector3 euler, float time)
        {
            var rotation = Quaternion.Euler(euler);
            AddKey(clip, path, "m_LocalRotation.x", time, rotation.x);
            AddKey(clip, path, "m_LocalRotation.y", time, rotation.y);
            AddKey(clip, path, "m_LocalRotation.z", time, rotation.z);
            AddKey(clip, path, "m_LocalRotation.w", time, rotation.w);
        }

        static void Position(AnimationClip clip, string path, Vector3 position, float time)
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
