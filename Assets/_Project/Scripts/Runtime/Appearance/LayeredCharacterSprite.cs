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
    /// as its eight files. A sheet plays over its animation's real length when the character's timing file
    /// (Characters/&lt;character&gt;/&lt;character&gt;_timing, written by the bake) gives one, else at 12 frames a second.
    /// </summary>
    public class LayeredCharacterSprite : MonoBehaviour
    {
        public const float FramesPerSecond = 12f;
        const int LayerCount = 4;

        readonly SpriteRenderer[] renderers = new SpriteRenderer[LayerCount];
        // The sheets of the playing animation, looked up when the animation or the look changes, so a frame allocates
        // nothing.
        readonly CharacterSheet[] current = new CharacterSheet[LayerCount];
        // Seconds per animation from the character's timing file ("<grip>_<clip> <seconds> [<ground speed>]"), written by
        // the sprite bake, and the ground speed a locomotion clip was recorded at.
        readonly Dictionary<string, float> timing = new Dictionary<string, float>();
        readonly Dictionary<string, float> speeds = new Dictionary<string, float>();
        bool timingLoaded;
        float currentSeconds;
        readonly Dictionary<string, CharacterSheet> sheets = new Dictionary<string, CharacterSheet>();
        readonly List<SpriteRenderer> visible = new List<SpriteRenderer>(LayerCount);
        string character;
        CharacterAppearance appearance;
        string animation = "idle";
        bool loop = true;
        float time;
        float duration;
        int row;
        int directionCount = 8;

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

        /// <summary>The ground speed (units a second) a locomotion animation was recorded at, or 0 when unknown.</summary>
        public float RecordedSpeed(string animationName)
        {
            PlaybackSeconds(animationName, null);
            return speeds.TryGetValue(AppearanceRules.GripCode(appearance.Grip) + "_" + animationName, out var found) ||
                   speeds.TryGetValue(AppearanceRules.GripCode(CharacterGrip.OneHand) + "_" + animationName, out found) ||
                   speeds.TryGetValue(animationName, out found) ? found : 0f;
        }

        /// <summary>How fast a loop plays: 1 as recorded, 2 twice as fast. Set every frame for locomotion.</summary>
        public float Rate { get; set; } = 1f;

        /// <summary>The sheet row shown (0 south, then clockwise seen from above).</summary>
        public int Row => row;

        /// <summary>Shows a row directly (a turn is played in the row it starts from).</summary>
        public void FaceRow(int value) => row = ((value % directionCount) + directionCount) % directionCount;

        /// <summary>How many directions the character's sheets have (8 or 16), known once a sheet is loaded.</summary>
        public int DirectionCount => directionCount;

        /// <summary>A one-shot's natural length in seconds, or 0 when the body has no such animation.</summary>
        public float LengthOf(string animationName)
        {
            var sheet = SheetFor(AppearanceLayer.Body, animationName);
            return sheet != null ? PlaybackSeconds(animationName, sheet) : 0f;
        }

        public void SetAppearance(CharacterAppearance next)
        {
            if (next.Equals(appearance) && sheets.Count > 0)
                return;
            appearance = next;
            // Only the sheets of looks no longer worn are let go; the body's and every unchanged layer's stay loaded, so
            // equipping a bow loads the bow's sheets and nothing else. Equipping used to drop every sheet, call
            // Resources.UnloadUnusedAssets and reload all of them, which froze the game 1.2 to 2.2 s in the editor on
            // every visible item (the owner, 2026-10-03). The dropped sheets' textures are freed by the next scene change.
            PruneSheets();
            Resolve();
            Apply();
            Preload();
        }

        readonly List<string> pruned = new List<string>();

        void PruneSheets()
        {
            pruned.Clear();
            foreach (var name in sheets.Keys)
                if (!IsWorn(name))
                    pruned.Add(name);
            foreach (var name in pruned)
                sheets.Remove(name);
        }

        // Whether a cached sheet belongs to a look now worn on its layer, or to that layer's fallback look.
        bool IsWorn(string sheetName)
        {
            for (var i = 0; i < LayerCount; i++)
            {
                var layer = (AppearanceLayer)i;
                var code = $"{character}_{AppearanceRules.LayerCode(layer)}_";
                if (!sheetName.StartsWith(code))
                    continue;
                var look = appearance.LookOf(layer);
                var fallback = AppearanceRules.FallbackLook(layer, appearance.Grip);
                return (look != null && sheetName.StartsWith(code + look + "_")) ||
                       (look != null && fallback != null && sheetName.StartsWith(code + fallback + "_"));
            }
            return false;
        }

        // Every animation the player can play, so all their sheets load with the look (on arriving and on equipping,
        // behind a fade or the paused Bag) rather than the first time each plays: a sheet loading mid-fight took about
        // 50 ms in the editor and froze the game on the phone when a big pack was engaged (the owner, 2026-09-29). The
        // list is the character's timing file, which the bake writes with every clip it baked; the retired Wrathborn's
        // names are the fallback when there is none.
        static readonly string[] Actions = { "attack", "hew", "hurl_axe", "ground_breaker" };
        static readonly string[] Others = { "idle", "run", "run_back", "bull_rush", "hit", "death" };
        static readonly string[] Legs =
        {
            "", LocomotionRules.MovingSuffix, LocomotionRules.MovingBackSuffix, LocomotionRules.MovingRightSuffix,
            LocomotionRules.MovingLeftSuffix,
        };

        readonly List<string> baked = new List<string>();

        void Preload()
        {
            EnsureTiming();
            if (baked.Count == 0)
            {
                foreach (var name in Others)
                    baked.Add(name);
                foreach (var action in Actions)
                    foreach (var legs in Legs)
                        baked.Add(action + legs);
            }
            for (var layer = 0; layer < LayerCount; layer++)
                foreach (var name in baked)
                    SheetFor((AppearanceLayer)layer, name);
        }

        void EnsureTiming()
        {
            if (timingLoaded)
                return;
            timingLoaded = true;
            CharacterSheets.LoadTiming(character, timing, speeds);
            // Keys are "<grip>_<clip>" (or a bare clip): the clips baked, once each.
            foreach (var key in timing.Keys)
            {
                var underscore = key.IndexOf('_');
                var clip = underscore > 0 && key.Substring(0, underscore).Length <= 6 && IsGripCode(key.Substring(0, underscore))
                    ? key.Substring(underscore + 1) : key;
                if (!baked.Contains(clip))
                    baked.Add(clip);
            }
        }

        static bool IsGripCode(string code) => code == "1h" || code == "2h" || code == "dual" || code == "shield";

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

        /// <summary>
        /// Swaps a one-shot for another at the same point of its progress and with the same duration, such as a swing
        /// that goes on from standing to running (its <c>_move</c> variant) when the character sets off mid-swing.
        /// </summary>
        public void Switch(string animationName)
        {
            var progress = Progress;
            animation = animationName;
            loop = false;
            Resolve();
            time = progress * (duration > 0f ? duration : currentSeconds);
            Apply();
        }

        /// <summary>How far a one-shot has played, 0 to 1 (0 for a loop).</summary>
        public float Progress
        {
            get
            {
                if (loop)
                    return 0f;
                var length = duration > 0f ? duration : currentSeconds;
                return length > 0f ? Mathf.Clamp01(time / length) : 1f;
            }
        }

        /// <summary>Keeps a looping animation's place when it is already playing (idle to idle), else starts it.</summary>
        public void Loop(string animationName)
        {
            if (animation == animationName && loop)
                return;
            Play(animationName, true);
        }

        /// <summary>Whether a one-shot has shown its last frame.</summary>
        public bool Finished => !loop && time >= (duration > 0f ? duration : currentSeconds);

        public void Face(Vector2 groundDirection)
        {
            if (groundDirection.sqrMagnitude > 1e-6f)
                row = AppearanceRules.DirectionRow(groundDirection, directionCount);
        }

        void Update()
        {
            time += Time.deltaTime * (loop ? Rate : 1f);
            Apply();
        }

        void Resolve()
        {
            for (var i = 0; i < LayerCount; i++)
                current[i] = SheetFor((AppearanceLayer)i, animation);
            currentSeconds = PlaybackSeconds(animation, current[0]);
            CurrentRecordedSpeed = RecordedSpeed(animation);
        }

        /// <summary>The playing animation's <see cref="RecordedSpeed"/>, looked up when it started.</summary>
        public float CurrentRecordedSpeed { get; private set; }

        /// <summary>How long the animation plays: its real length from the timing file, else its frames at 12 a second.</summary>
        float PlaybackSeconds(string animationName, CharacterSheet sheet)
        {
            EnsureTiming();
            if (timing.TryGetValue(AppearanceRules.GripCode(appearance.Grip) + "_" + animationName, out var found) ||
                timing.TryGetValue(AppearanceRules.GripCode(CharacterGrip.OneHand) + "_" + animationName, out found) ||
                timing.TryGetValue(animationName, out found))
                return found;
            return sheet != null ? sheet.Frames / FramesPerSecond : 0f;
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
            var length = currentSeconds > 0f ? currentSeconds : frames / FramesPerSecond;
            if (loop)
                return Mathf.FloorToInt(time / length * frames) % frames;
            var progress = time / (duration > 0f ? duration : length);
            return Mathf.Clamp(Mathf.FloorToInt(progress * frames), 0, frames - 1);
        }

        CharacterSheet SheetFor(AppearanceLayer layer, string animationName)
        {
            var look = appearance.LookOf(layer);
            if (string.IsNullOrEmpty(look) || string.IsNullOrEmpty(character))
                return null;
            // While the art is being made not every look exists yet: a missing look shows the grip's reference look (the
            // models that exist), so the character is never drawn without a body or with an empty hand it should not
            // have; a missing helm stays off. A grip bakes only the clips it has its own of (the shield's run, the
            // two-hander's run and swing, dual wield's combo) and, for the rest, just the off-hand over the one-handed
            // clip: body and weapon then fall back to the one-handed sheets, which move the same.
            var grip = appearance.Grip;
            var sheet = LoadLook(layer, look, grip, animationName);
            if (sheet == null && layer != AppearanceLayer.OffHand && grip != CharacterGrip.OneHand)
                sheet = LoadLook(layer, look, CharacterGrip.OneHand, animationName);
            return sheet;
        }

        CharacterSheet LoadLook(AppearanceLayer layer, string look, CharacterGrip grip, string animationName)
        {
            var sheet = Baked(layer, look, grip) ? LoadCached(AppearanceRules.SheetName(character, layer, look, grip, animationName)) : null;
            var fallback = AppearanceRules.FallbackLook(layer, grip);
            if (sheet == null && fallback != null && fallback != look && Baked(layer, fallback, grip))
                sheet = LoadCached(AppearanceRules.SheetName(character, layer, fallback, grip, animationName));
            return sheet;
        }

        // Whether any sheet of a look exists (its idle or its attack), asked once per look: a look not modelled yet (a
        // recurve bow while only the hunting bow is) skips straight to the fallback instead of searching for each of its
        // animations, 17 file names apiece, on every equip.
        readonly Dictionary<string, bool> lookBaked = new Dictionary<string, bool>();

        bool Baked(AppearanceLayer layer, string look, CharacterGrip grip)
        {
            var key = AppearanceRules.SheetName(character, layer, look, grip, "");
            if (lookBaked.TryGetValue(key, out var known))
                return known;
            var found = LoadCached(key + "idle") != null || LoadCached(key + "attack") != null;
            lookBaked[key] = found;
            return found;
        }

        // Sheets that do not exist, remembered for good (nothing to free), so a missing one is searched for once.
        readonly HashSet<string> absent = new HashSet<string>();

        CharacterSheet LoadCached(string name)
        {
            if (sheets.TryGetValue(name, out var cached))
                return cached;
            if (absent.Contains(name))
                return null;
            var sheet = CharacterSheets.Load($"Characters/{character}/{name}");
            if (sheet == null)
            {
                absent.Add(name);
                return null;
            }
            sheets[name] = sheet;
            if (sheet != null && sheet.Rows.Length != directionCount)
            {
                // A row index means a different direction at another count: keep the same heading.
                row = row * sheet.Rows.Length / directionCount;
                directionCount = sheet.Rows.Length;
            }
            return sheet;
        }
    }
}
