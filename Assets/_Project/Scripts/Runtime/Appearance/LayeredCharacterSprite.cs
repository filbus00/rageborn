using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ARPG
{
    /// <summary>
    /// Draws a character from its baked layer sheets (Docs/09-art-brief.md, 4.5): body, helm, off-hand and weapon, each a
    /// sprite renderer stacked in that order under one sorting group, so the stack sorts against other characters as one.
    /// The sprite bake cut every layer where the layers under it hide it, so stacking needs no per-direction draw order.
    ///
    /// Sheets load from Resources (Characters/&lt;character&gt;/&lt;sheet name&gt;, see
    /// <see cref="AppearanceRules.SheetName"/>) when a look is first shown, and a changed look lets go of the old sheets,
    /// so only the equipped looks are in memory (the brief, decision 5). A sheet split per direction (over 4096 px) loads
    /// as its eight files. Frames are 12 per second, as baked.
    /// </summary>
    public class LayeredCharacterSprite : MonoBehaviour
    {
        public const float FramesPerSecond = 12f;
        const int LayerCount = 4;
        static readonly string[] DirectionCodes = { "s", "sw", "w", "nw", "n", "ne", "e", "se" };

        sealed class Sheet
        {
            public Sprite[][] Rows;
            public int Frames;
        }

        readonly SpriteRenderer[] renderers = new SpriteRenderer[LayerCount];
        // The sheets of the playing animation, looked up when the animation or the look changes, so a frame allocates
        // nothing.
        readonly Sheet[] current = new Sheet[LayerCount];
        readonly Dictionary<string, Sheet> sheets = new Dictionary<string, Sheet>();
        readonly List<SpriteRenderer> visible = new List<SpriteRenderer>(LayerCount);
        string character;
        CharacterAppearance appearance;
        string animation = "idle";
        bool loop = true;
        float time;
        float duration;
        int row;

        /// <summary>Builds the stack under a character's root, on the Entities sorting layer.</summary>
        public static LayeredCharacterSprite Create(Transform parent, string character)
        {
            var go = new GameObject("Sprite Layers", typeof(SortingGroup));
            go.transform.SetParent(parent, false);
            var group = go.GetComponent<SortingGroup>();
            group.sortingLayerName = GameSortingLayers.Entities;
            var sprite = go.AddComponent<LayeredCharacterSprite>();
            sprite.character = character;
            for (var i = 0; i < LayerCount; i++)
            {
                var layer = new GameObject(((AppearanceLayer)i).ToString(), typeof(SpriteRenderer));
                layer.transform.SetParent(go.transform, false);
                var renderer = layer.GetComponent<SpriteRenderer>();
                renderer.sortingLayerName = GameSortingLayers.Entities;
                renderer.sortingOrder = i;
                sprite.renderers[i] = renderer;
            }
            return sprite;
        }

        public string Character => character;

        public string Animation => animation;

        public CharacterGrip Grip => appearance.Grip;

        /// <summary>The renderers showing a sprite now, for the hit flash.</summary>
        public IReadOnlyList<SpriteRenderer> VisibleRenderers
        {
            get
            {
                visible.Clear();
                foreach (var renderer in renderers)
                    if (renderer.enabled && renderer.sprite != null)
                        visible.Add(renderer);
                return visible;
            }
        }

        public SpriteRenderer[] Renderers => renderers;

        /// <summary>Whether the body has this animation in the current look and grip.</summary>
        public bool Has(string animationName) => SheetFor(AppearanceLayer.Body, animationName) != null;

        /// <summary>A one-shot's natural length in seconds, or 0 when the body has no such animation.</summary>
        public float LengthOf(string animationName)
        {
            var sheet = SheetFor(AppearanceLayer.Body, animationName);
            return sheet != null ? sheet.Frames / FramesPerSecond : 0f;
        }

        public void SetAppearance(CharacterAppearance next)
        {
            if (next.Equals(appearance) && sheets.Count > 0)
                return;
            appearance = next;
            // Let go of every sheet of a look no longer worn; the next frames load what is needed.
            if (sheets.Count > 0)
            {
                sheets.Clear();
                Resources.UnloadUnusedAssets();
            }
            Resolve();
            Apply();
        }

        /// <summary>Plays an animation from its start. A one-shot with a duration is stretched or squeezed to fit it (an
        /// attack fitted to the attack rate); 0 plays it at 12 frames per second.</summary>
        public void Play(string animationName, bool looping, float seconds = 0f)
        {
            animation = animationName;
            loop = looping;
            duration = seconds;
            time = 0f;
            Resolve();
            Apply();
        }

        /// <summary>Keeps a looping animation's place when it is already playing (idle to idle), else starts it.</summary>
        public void Loop(string animationName)
        {
            if (animation == animationName && loop)
                return;
            Play(animationName, true);
        }

        /// <summary>Whether a one-shot has shown its last frame.</summary>
        public bool Finished => !loop && time >= (duration > 0f ? duration : current[0] != null ? current[0].Frames / FramesPerSecond : 0f);

        public void Face(Vector2 groundDirection)
        {
            if (groundDirection.sqrMagnitude > 1e-6f)
                row = AppearanceRules.DirectionRow(groundDirection);
        }

        void Update()
        {
            time += Time.deltaTime;
            Apply();
        }

        void Resolve()
        {
            for (var i = 0; i < LayerCount; i++)
                current[i] = SheetFor((AppearanceLayer)i, animation);
        }

        void Apply()
        {
            for (var i = 0; i < LayerCount; i++)
            {
                var sheet = current[i];
                var renderer = renderers[i];
                if (sheet == null)
                {
                    renderer.enabled = false;
                    continue;
                }
                renderer.enabled = true;
                renderer.sprite = sheet.Rows[row][FrameOf(sheet.Frames)];
            }
        }

        int FrameOf(int frames)
        {
            if (frames <= 1)
                return 0;
            if (loop)
                return Mathf.FloorToInt(time * FramesPerSecond) % frames;
            var progress = duration > 0f ? time / duration : time * FramesPerSecond / frames;
            return Mathf.Clamp(Mathf.FloorToInt(progress * frames), 0, frames - 1);
        }

        Sheet SheetFor(AppearanceLayer layer, string animationName)
        {
            var look = appearance.LookOf(layer);
            if (string.IsNullOrEmpty(look) || string.IsNullOrEmpty(character))
                return null;
            var name = AppearanceRules.SheetName(character, layer, look, appearance.Grip, animationName);
            if (sheets.TryGetValue(name, out var cached))
                return cached;
            var sheet = Load($"Characters/{character}/{name}");
            sheets[name] = sheet;
            return sheet;
        }

        // A whole sheet, or its eight per-direction files. Frames are found by their names' last two parts: the
        // direction code and the frame number. Null when the sheet does not exist.
        static Sheet Load(string path)
        {
            var sprites = new List<Sprite>(Resources.LoadAll<Sprite>(path));
            if (sprites.Count == 0)
                foreach (var code in DirectionCodes)
                    sprites.AddRange(Resources.LoadAll<Sprite>($"{path}_{code}"));
            if (sprites.Count == 0)
                return null;

            var frames = 0;
            var parsed = new List<(int row, int frame, Sprite sprite)>(sprites.Count);
            foreach (var sprite in sprites)
            {
                var parts = sprite.name.Split('_');
                if (parts.Length < 2 || !int.TryParse(parts[parts.Length - 1], out var frame))
                    continue;
                var direction = System.Array.IndexOf(DirectionCodes, parts[parts.Length - 2]);
                if (direction < 0)
                    continue;
                parsed.Add((direction, frame, sprite));
                frames = Mathf.Max(frames, frame + 1);
            }
            if (frames == 0)
                return null;

            var sheet = new Sheet { Frames = frames, Rows = new Sprite[DirectionCodes.Length][] };
            for (var i = 0; i < sheet.Rows.Length; i++)
                sheet.Rows[i] = new Sprite[frames];
            foreach (var (direction, frame, sprite) in parsed)
                sheet.Rows[direction][frame] = sprite;
            // A missing frame shows its neighbour rather than nothing.
            foreach (var rowFrames in sheet.Rows)
                for (var f = 0; f < frames; f++)
                    if (rowFrames[f] == null)
                        rowFrames[f] = f > 0 ? rowFrames[f - 1] : System.Array.Find(rowFrames, s => s != null);
            return sheet;
        }
    }
}
