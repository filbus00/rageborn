using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// Draws the player's character in the UI from its baked sheets (the idle's first frame facing the camera, body,
    /// helm and weapon stacked like <see cref="LayeredCharacterSprite"/>), wearing what is equipped. Stands in for the
    /// painted portrait until the UI art exists (Docs/09). The images share one rect, since every layer's cell has the
    /// same size and pivot.
    /// </summary>
    public sealed class CharacterPortrait
    {
        static readonly AppearanceLayer[] Layers = { AppearanceLayer.Body, AppearanceLayer.Helm, AppearanceLayer.OffHand, AppearanceLayer.Weapon };

        readonly Image[] images = new Image[Layers.Length];
        CharacterAppearance shown;
        bool hasShown;

        // Frames already found, by sheet and grip, shared by every portrait: loading a sheet reads all its sprites.
        static readonly System.Collections.Generic.Dictionary<string, Sprite> Cache = new System.Collections.Generic.Dictionary<string, Sprite>();

        /// <summary>The outermost object of the portrait's frame, when it has one.</summary>
        public Transform Root { get; set; }

        public CharacterPortrait(RectTransform parent)
        {
            for (var i = 0; i < Layers.Length; i++)
            {
                images[i] = UiStyle.Image(parent, Layers[i] + " Layer", Color.white);
                UiStyle.Stretch(images[i].rectTransform);
                images[i].preserveAspect = true;
            }
        }

        /// <summary>Shows the character in its current gear.</summary>
        public void Refresh(EquipmentState equipment)
        {
            var appearance = AppearanceRules.For(equipment);
            // GameSession.Changed fires on every kill and gold pickup; reloading four sheets each time froze the game for
            // half a second when a big pack died (the owner, 2026-09-29). Only a changed look reloads.
            if (hasShown && appearance.Equals(shown))
                return;
            shown = appearance;
            hasShown = true;
            for (var i = 0; i < Layers.Length; i++)
            {
                var sprite = Frame(Layers[i], appearance.LookOf(Layers[i]), appearance.Grip);
                images[i].sprite = sprite;
                images[i].enabled = sprite != null;
            }
        }

        // The south-facing first idle frame of a layer, or the layer's fallback look while its own is not baked.
        static Sprite Frame(AppearanceLayer layer, string look, CharacterGrip grip)
        {
            var sprite = Load(layer, look, grip);
            var fallback = AppearanceRules.FallbackLook(layer, grip);
            if (sprite == null && fallback != null && fallback != look)
                sprite = Load(layer, fallback, grip);
            // A grip's idle may be only its off-hand: body and weapon then come from the one-handed sheets.
            if (sprite == null && layer != AppearanceLayer.OffHand && grip != CharacterGrip.OneHand)
                return Frame(layer, look, CharacterGrip.OneHand);
            return sprite;
        }

        static Sprite Load(AppearanceLayer layer, string look, CharacterGrip grip)
        {
            if (string.IsNullOrEmpty(look))
                return null;
            var name = AppearanceRules.SheetName(PlayerSpriteAnimator.DefaultCharacter, layer, look, grip, "idle");
            if (Cache.TryGetValue(name, out var cached))
                return cached;
            var sheet = CharacterSheets.Load($"Characters/{PlayerSpriteAnimator.DefaultCharacter}/{name}");
            var sprite = sheet != null && sheet.Rows.Length > 0 && sheet.Frames > 0 ? sheet.Rows[0][0] : null;
            Cache[name] = sprite;
            return sprite;
        }
    }
}
