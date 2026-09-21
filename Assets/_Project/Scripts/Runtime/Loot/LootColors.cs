using UnityEngine;

namespace ARPG
{
    /// <summary>How loot looks by rarity. The colors are the rarity colors from Docs/06-ui-ux.md.</summary>
    public static class LootColors
    {
        public static readonly Color Gold = new Color32(240, 200, 64, 255);

        /// <summary>Common 9A9A9A, Magic 4A7BD4, Rare E0C040, Legendary E07A20.</summary>
        public static Color Of(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return new Color32(0x9A, 0x9A, 0x9A, 255);
                case ItemRarity.Magic: return new Color32(0x4A, 0x7B, 0xD4, 255);
                case ItemRarity.Rare: return new Color32(0xE0, 0xC0, 0x40, 255);
                default: return new Color32(0xE0, 0x7A, 0x20, 255);
            }
        }

        /// <summary>
        /// Height of the loot beam in world units. Better items reach higher so they stand out in a crowd
        /// (Docs/01-core-gameplay.md). Tuning values.
        /// </summary>
        public static float BeamHeight(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return 1.0f;
                case ItemRarity.Magic: return 1.6f;
                case ItemRarity.Rare: return 2.4f;
                default: return 3.6f;
            }
        }
    }
}
