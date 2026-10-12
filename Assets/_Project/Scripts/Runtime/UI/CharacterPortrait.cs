using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// Draws the player's character in the UI from its baked sheets, wearing what is equipped (body, boots, belt, gloves,
    /// helm, quiver and bow stacked like <see cref="LayeredCharacterSprite"/>). The layers are drawn at their true size
    /// into a small canvas (a render texture of art pixels), each placed by its own feet point, and the canvas is shown
    /// scaled up by a whole number with no smoothing, so she stays crisp pixel art.
    /// Before 2026-10-06 each layer was a UI image stretched over one shared rect: the sheets' cells were all the same
    /// size then, but since <c>SheetTrimmer</c> crops every sheet to what it draws, a cropped belt or helm was
    /// stretched over the whole character, and the 8 times upscale was smoothed and blurry.
    /// <see cref="Animated"/> plays the idle (the Bag's figure); the HUD shows a still frame.
    /// The Bag's figure is sharp (2026-10-12, the owner: "very pixelated and it is hard to see what the character is
    /// wearing"): it draws the portrait bake (Resources/Characters/wild_arrow_portrait: each look as one still frame
    /// facing the viewer, smooth, at six times the game's size) when every layer has one, scaled smoothly to fit.
    /// </summary>
    public sealed class CharacterPortrait
    {
        static readonly AppearanceLayer[] Layers =
        {
            AppearanceLayer.Body, AppearanceLayer.Boots, AppearanceLayer.Belt, AppearanceLayer.Gloves, AppearanceLayer.Helm,
            AppearanceLayer.OffHand, AppearanceLayer.Weapon,
        };

        /// <summary>The canvas, in art pixels: the baked cell (80 px, her feet 12 up from its bottom, mid-width), so
        /// everything the bake drew fits and the Bag's figure shows her at 8 times.</summary>
        public const int CanvasSize = 80;

        /// <summary>The portrait bake's scale over the game's sheets (WildArrowBakeSetup.BakePortrait).</summary>
        const int SharpTimes = 6;
        const string SharpCharacter = "wild_arrow_portrait";

        readonly int canvasSize;
        readonly Vector2 feet;
        readonly bool sharp;

        /// <summary>Which way she faces: straight at the viewer (2026-10-11, the owner: "facing front"). The ground's axes
        /// run diagonally on screen, so that is ground (1, -1), row 14 of 16; the old fixed row 2 had her turned away to
        /// the left.</summary>
        static readonly Vector2 Facing = new Vector2(1f, -1f);

        readonly RawImage image;
        readonly RenderTexture canvas;
        readonly CharacterSheet[] sheets = new CharacterSheet[Layers.Length];
        CharacterAppearance shown;
        bool hasShown;
        int frame = -1;

        // Sheets already found, by name, shared by every portrait: loading a sheet reads all its sprites.
        static readonly Dictionary<string, CharacterSheet> Cache = new Dictionary<string, CharacterSheet>();
        static Material drawMaterial;

        /// <summary>The outermost object of the portrait's frame, when it has one.</summary>
        public Transform Root { get; set; }

        /// <summary>Plays the idle (unscaled time, so it runs while the Bag pauses the game).</summary>
        public bool Animated { get; set; }

        /// <param name="wantSharp">Draw the high-resolution portrait bake when it exists (the Bag's figure).</param>
        public CharacterPortrait(RectTransform parent, bool wantSharp = false)
        {
            sharp = wantSharp && Resources.Load<TextAsset>($"Characters/{SharpCharacter}/{SharpCharacter}_timing") != null;
            var times = sharp ? SharpTimes : 1;
            canvasSize = CanvasSize * times;
            feet = new Vector2(canvasSize / 2f, 12f * times);
            canvas = new RenderTexture(canvasSize, canvasSize, 0, RenderTextureFormat.ARGB32)
            {
                name = "Character Portrait", filterMode = sharp ? FilterMode.Bilinear : FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
            };
            canvas.Create();
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(RawImage), typeof(Driver));
            go.transform.SetParent(parent, false);
            image = go.GetComponent<RawImage>();
            image.texture = canvas;
            image.raycastTarget = false;
            // The largest whole multiple of the canvas that fits, centred, so every art pixel is the same size.
            var size = parent.rect.size;
            if (size.x <= 0f || size.y <= 0f)
                size = parent.sizeDelta;
            // The sharp portrait is scaled smoothly to fill the frame instead.
            var scale = sharp ? Mathf.Min(size.x, size.y) / canvasSize : Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(size.x, size.y) / CanvasSize));
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(canvasSize * scale, canvasSize * scale);
            rect.anchoredPosition = Vector2.zero;
            go.GetComponent<Driver>().Owner = this;
        }

        /// <summary>Shows the character in its current gear.</summary>
        public void Refresh(EquipmentState equipment)
        {
            var appearance = AppearanceRules.For(equipment);
            // GameSession.Changed fires on every kill and gold pickup; reloading the sheets each time froze the game for
            // half a second when a big pack died (the owner, 2026-09-29). Only a changed look reloads.
            if (hasShown && appearance.Equals(shown))
                return;
            shown = appearance;
            hasShown = true;
            for (var i = 0; i < Layers.Length; i++)
                sheets[i] = Sheet(Layers[i], appearance.LookOf(Layers[i]), appearance.Grip, sharp ? SharpCharacter : PlayerSpriteAnimator.DefaultCharacter);
            frame = -1;
            Draw(0);
        }

        void Tick()
        {
            if (!Animated || sheets[0] == null)
                return;
            var frames = sheets[0].Frames;
            var next = frames > 0 ? Mathf.FloorToInt(Time.unscaledTime * LayeredCharacterSprite.FramesPerSecond * 0.75f) % frames : 0;
            if (next != frame)
                Draw(next);
        }

        void Draw(int index)
        {
            frame = index;
            var previous = RenderTexture.active;
            RenderTexture.active = canvas;
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, canvasSize, canvasSize, 0);
            GL.Clear(true, true, Color.clear);
            if (drawMaterial == null)
                drawMaterial = new Material(Shader.Find("UI/Default"));
            for (var i = 0; i < Layers.Length; i++)
            {
                var sheet = sheets[i];
                if (sheet == null || sheet.Rows.Length == 0 || sheet.Frames == 0)
                    continue;
                var row = sheet.Rows[Mathf.Clamp(AppearanceRules.DirectionRow(Facing, sheet.Rows.Length), 0, sheet.Rows.Length - 1)];
                var sprite = row[index % row.Length];
                if (sprite == null)
                    continue;
                // At true size, the sprite's pivot (its feet) on the canvas's feet point; pixel matrix y runs down.
                var r = sprite.textureRect;
                var pivot = sprite.pivot;
                var left = Mathf.Round(feet.x - pivot.x);
                var bottom = Mathf.Round(feet.y - pivot.y);
                var texture = sprite.texture;
                var source = new Rect(r.x / texture.width, r.y / texture.height, r.width / texture.width, r.height / texture.height);
                // White: the default colour is half grey, which the shader multiplies in (half as bright, half see-through).
                Graphics.DrawTexture(new Rect(left, canvasSize - bottom - r.height, r.width, r.height), texture, source, 0, 0, 0, 0, Color.white, drawMaterial);
            }
            GL.PopMatrix();
            RenderTexture.active = previous;
        }

        void Release()
        {
            if (canvas != null)
                canvas.Release();
            Object.Destroy(canvas);
        }

        // A layer's idle sheet, or the layer's fallback look while its own is not baked.
        static CharacterSheet Sheet(AppearanceLayer layer, string look, CharacterGrip grip, string character)
        {
            var sheet = Load(layer, look, grip, character);
            var fallback = AppearanceRules.FallbackLook(layer, grip);
            if (sheet == null && fallback != null && fallback != look)
                sheet = Load(layer, fallback, grip, character);
            // A grip's idle may be only its off-hand: body and weapon then come from the one-handed sheets.
            if (sheet == null && layer != AppearanceLayer.OffHand && grip != CharacterGrip.OneHand)
                return Sheet(layer, look, CharacterGrip.OneHand, character);
            return sheet;
        }

        static CharacterSheet Load(AppearanceLayer layer, string look, CharacterGrip grip, string character)
        {
            if (string.IsNullOrEmpty(look))
                return null;
            var name = AppearanceRules.SheetName(character, layer, look, grip, "idle");
            if (Cache.TryGetValue(name, out var cached))
                return cached;
            var sheet = CharacterSheets.Load($"Characters/{character}/{name}");
            Cache[name] = sheet;
            return sheet;
        }

        // Ticks the idle and frees the canvas with the UI that shows it.
        sealed class Driver : MonoBehaviour
        {
            public CharacterPortrait Owner;

            void Update() => Owner?.Tick();

            void OnDestroy() => Owner?.Release();
        }
    }
}
