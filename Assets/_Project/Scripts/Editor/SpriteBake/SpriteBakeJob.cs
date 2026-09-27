using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// One character to bake into sprite sheets (Docs/09-art-brief.md, Route A): the model, its animations, an optional
    /// weapon and the sheet rules. Create one with Assets > Create > ARPG > Sprite Bake Job, fill it, then run
    /// Tools > ARPG > Sprite Bake > Bake Selected Job. See <see cref="SpriteBaker"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "ARPG/Sprite Bake Job", fileName = "SpriteBakeJob")]
    public class SpriteBakeJob : ScriptableObject
    {
        [Serializable]
        public class Clip
        {
            [Tooltip("The animation's name in the file names: idle, run, attack, hew and so on (the brief's lists).")]
            public string name = "idle";

            public AnimationClip clip;

            [Tooltip("Frames to render; 0 takes the clip's length at the job's frame rate.")]
            public int frames;

            [Tooltip("A loop spreads its frames over the clip without repeating the first at the end.")]
            public bool loop = true;
        }

        [Serializable]
        public class Weapon
        {
            [Tooltip("A model or prefab to hold, such as the axe. Empty for none.")]
            public GameObject prefab;

            [Tooltip("For a humanoid rig: the bone that holds it.")]
            public HumanBodyBones bone = HumanBodyBones.RightHand;

            [Tooltip("For a rig without an avatar: the path of the holding transform under the model, such as Hips/ArmR/HandR. Wins over the bone when set.")]
            public string transformPath = "";

            public Vector3 localPosition;
            public Vector3 localEuler;
            public float scale = 1f;
        }

        [Tooltip("File name prefix, lower case: wrathborn, husk, cinder_warden.")]
        public string characterName = "character";

        [Tooltip("The model (FBX or prefab). A humanoid clip needs the model's Animator avatar; a generic clip binds by transform paths.")]
        public GameObject model;

        public Weapon weapon = new Weapon();

        public List<Clip> clips = new List<Clip>();

        [Tooltip("Cell size in final pixels: 256 for characters and enemies, 512 for bosses.")]
        public int cellSize = 256;

        [Tooltip("Where the feet sit in every cell, in final pixels from the bottom left.")]
        public Vector2 pivot = new Vector2(128f, 40f);

        [Tooltip("How tall the character stands in final pixels (the brief: the Wrathborn about 170). 0 keeps the model's own size, 1 m across the screen being 128 px.")]
        public float targetHeightPixels = 170f;

        [Tooltip("Renders at this many times the final size and averages down (the brief: 4).")]
        public int supersample = 4;

        public float framesPerSecond = 12f;

        [Tooltip("Where the sheets are written, under Assets so they import as sprites.")]
        public string outputFolder = "Assets/_Project/Art/Characters";
    }
}
