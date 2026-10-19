using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The item icons as sprites (<see cref="ItemIconArt"/>), made the first time each kind and rarity is asked for and
    /// kept: ten kinds and four rarities at most. Point filtered, so the pixels stay square when the UI scales them up.
    /// </summary>
    public static class ItemIcons
    {
        static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Cache.Clear();
            Art.Clear();
        }

        static readonly Dictionary<string, Sprite> Art = new Dictionary<string, Sprite>();

        /// <summary>An item's picture (<see cref="ItemArtNames"/>, the owner's pixel art, 2026-10-19), or the drawn icon of
        /// its kind (or the blueprint scroll) where there is none.</summary>
        public static Sprite For(Item item)
        {
            var art = Picture(ItemArtNames.For(item));
            if (art != null)
                return art;
            if (!item.IsBlueprint)
                return For(item.Slot, item.Rarity);
            const int key = -1;
            if (Cache.TryGetValue(key, out var cached) && cached != null)
                return cached;
            var pixels = ItemIconArt.DrawBlueprint(out var width, out var height);
            return Cache[key] = Make(pixels, width, height, "Icon Blueprint");
        }

        /// <summary>A named legendary's picture, or the drawn icon of its kind in its rarity (the Codex).</summary>
        public static Sprite ForLegendary(LegendaryDefinition legendary, ItemRarity rarity) =>
            Picture(ItemArtNames.ForLegendary(legendary.Id)) ?? For(legendary.Slot, rarity);

        /// <summary>A picture from Resources/UI/ItemArt, loaded once (null kept too, so a missing one is not asked again).
        /// </summary>
        public static Sprite Picture(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            if (Art.TryGetValue(name, out var sprite))
                return sprite;
            return Art[name] = Resources.Load<Sprite>(ItemArtNames.Folder + name);
        }

        static Sprite Make(Color32[] pixels, int width, int height, string name)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), ItemIconArt.PixelsPerCell, 0,
                SpriteMeshType.FullRect, Vector4.zero, false);
        }

        public static Sprite For(ItemSlot kind, ItemRarity rarity)
        {
            var key = (int)kind * 16 + (int)rarity;
            if (Cache.TryGetValue(key, out var sprite) && sprite != null)
                return sprite;

            var pixels = ItemIconArt.Draw(kind, rarity, out var width, out var height);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = $"Icon {kind} {rarity}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            // Full rect, no physics shape: the texture is no longer readable, and tracing its outline fails on iOS.
            sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), ItemIconArt.PixelsPerCell, 0,
                SpriteMeshType.FullRect, Vector4.zero, false);
            Cache[key] = sprite;
            return sprite;
        }
    }
}
