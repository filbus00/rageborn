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
        static readonly AppearanceLayer[] Layers = { AppearanceLayer.Body, AppearanceLayer.Helm, AppearanceLayer.Weapon };

        readonly Image[] images = new Image[Layers.Length];

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
            var fallback = AppearanceRules.FallbackLook(layer);
            if (sprite == null && fallback != null && fallback != look)
                sprite = Load(layer, fallback, grip);
            return sprite;
        }

        static Sprite Load(AppearanceLayer layer, string look, CharacterGrip grip)
        {
            if (string.IsNullOrEmpty(look))
                return null;
            var name = AppearanceRules.SheetName(PlayerSpriteAnimator.DefaultCharacter, layer, look, grip, "idle");
            var sheet = CharacterSheets.Load($"Characters/{PlayerSpriteAnimator.DefaultCharacter}/{name}");
            return sheet != null && sheet.Rows.Length > 0 && sheet.Frames > 0 ? sheet.Rows[0][0] : null;
        }
    }
}
