using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// One character to bake into sprite sheets (Docs/09-art-brief.md, Route A and 4.5). A character is one or more
    /// bodies (the whole figure in one chest armour look, all sharing a skeleton's proportions) and optional pieces
    /// (helms, weapons, off-hands: rigid models attached to a bone of the first body), animated by a clip set per grip.
    /// Every body and piece becomes its own layer sheet per grip and clip, which the game stacks
    /// (<see cref="LayeredCharacterSprite"/>). A plain character (an enemy: one body, no look name, no pieces, no grip name)
    /// is baked as <c>&lt;name&gt;_&lt;clip&gt;</c>. Create one with Assets > Create > ARPG > Sprite Bake Job, then run
    /// Tools > ARPG > Sprite Bake > Bake Selected Jobs. See <see cref="SpriteBaker"/>.
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

            [Tooltip("How long the game plays the sheet, in seconds; 0 plays its frames at 12 per second. Written to the character's timing file.")]
            public float playbackSeconds;

            [Tooltip("The part of the clip to sample, in seconds: from start to end (an end of 0 is the clip's end). Mixamo clips often carry a long stance before and after the action.")]
            public float start;
            public float end;

            [Tooltip("How much of the hips' rise a one-shot keeps, 0 to 1: a leap taller than the cell is flattened to fit.")]
            public float riseScale = 1f;

            [Tooltip("A moving variant of an action: the hips and legs come from this loop (the run) while the spine, arms and head come from the clip, so the character can swing while it runs. Empty for a normal clip.")]
            public AnimationClip legs;

            [Tooltip("The ground speed (game units a second) the legs of a moving variant run at: the loop plays at this over its recorded speed.")]
            public float legsGroundSpeed = 4.4f;
        }

        [Serializable]
        public class Body
        {
            [Tooltip("The look id in the file names (bare, padded, leather, mail); empty for a plain character.")]
            public string look = "";

            [Tooltip("The rigged model (FBX or prefab). A humanoid clip needs its Animator avatar; a generic clip binds by transform paths.")]
            public GameObject model;
        }

        [Serializable]
        public class Piece
        {
            public AppearanceLayer layer = AppearanceLayer.Weapon;

            [Tooltip("The look id in the file names: cap, bearded_axe, buckler and so on.")]
            public string look = "";

            [Tooltip("The rigid model or prefab.")]
            public GameObject prefab;

            [Tooltip("For a humanoid rig: the bone that holds it.")]
            public HumanBodyBones bone = HumanBodyBones.RightHand;

            [Tooltip("For a rig without an avatar: the path of the holding transform under the body, such as Hips/Spine/ArmR/HandR. Wins over the bone when set.")]
            public string transformPath = "";

            [Tooltip("For a weapon on a humanoid hand: place it from the finger bones instead of the offsets below. The prefab must have its grip at the origin, the haft along +Y with the head up and the blade toward +Z, in the body's units (metres).")]
            public bool autoGrip;

            public Vector3 localPosition;
            public Vector3 localEuler;
            public float scale = 1f;

            [Tooltip("The grips this piece is baked for (1h, dual, shield, 2h); empty for every grip.")]
            public List<string> grips = new List<string>();
        }

        [Serializable]
        public class GripSet
        {
            [Tooltip("The grip's name in the file names (1h, dual, shield, 2h); empty for a plain character.")]
            public string grip = "";

            public List<Clip> clips = new List<Clip>();
        }

        [Tooltip("File name prefix, lower case: wrathborn, husk, cinder_warden.")]
        public string characterName = "character";

        [Tooltip("The bodies. The first is the reference: its proportions set the scale, the pieces hang on its bones, and it hides what is behind it when a piece is baked.")]
        public List<Body> bodies = new List<Body>();

        public List<Piece> pieces = new List<Piece>();

        public List<GripSet> grips = new List<GripSet>();

        [Tooltip("Final pixels per game unit. 128 is full resolution (a tile's 1 m diagonal is 128 px); 64 bakes at half the resolution, drawn at the same size in the game (the Wrathborn since 2026-09-27: the owner allowed Diablo 2's resolution). Cell size, pivot and height are in these final pixels.")]
        public float pixelsPerUnit = SpriteBakeMath.PixelsPerMeter;

        [Tooltip("Cell size in final pixels: 256 for characters and enemies, 512 for bosses (at 128 pixels per unit).")]
        public int cellSize = 256;

        [Tooltip("Where the feet sit in every cell, in final pixels from the bottom left.")]
        public Vector2 pivot = new Vector2(128f, 40f);

        [Tooltip("How tall the character stands in final pixels (the brief: the Wrathborn about 170). 0 keeps the model's own size, 1 m across the screen being 128 px.")]
        public float targetHeightPixels = 170f;

        [Tooltip("Renders at this many times the final size and averages down (the brief: 4).")]
        public int supersample = 4;

        public float framesPerSecond = 12f;

        [Tooltip("Directions per animation: 8 (enemies), or 16 (the player, as Diablo 2 gave its heroes).")]
        public int directions = 8;

        public int DirectionCount => directions == 16 ? 16 : 8;

        [Tooltip("Where the sheets are written. The game loads a character's sheets from Resources/Characters/<name>.")]
        public string outputFolder = "Assets/_Project/Resources/Characters/character";

        /// <summary>Whether the sheets carry layer and look names (several bodies, a named look, or any piece).</summary>
        public bool Layered => pieces.Count > 0 || bodies.Count > 1 || (bodies.Count == 1 && !string.IsNullOrEmpty(bodies[0].look));

        /// <summary>
        /// A sheet's name: character, then for a layered character the layer and look, then the grip if named, then the
        /// clip. Matches <see cref="AppearanceRules.SheetName"/> for a layered character with grips.
        /// </summary>
        public string SheetName(AppearanceLayer layer, string look, string grip, string clip)
        {
            var name = characterName;
            if (Layered)
                name += "_" + AppearanceRules.LayerCode(layer) + "_" + look;
            if (!string.IsNullOrEmpty(grip))
                name += "_" + grip;
            return name + "_" + clip;
        }
    }
}
