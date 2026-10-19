namespace ARPG
{
    /// <summary>
    /// Which painted picture an item shows in the Bag (the owner's Gemini pixel art, 2026-10-19): a named legendary its
    /// own, a blueprint the scroll, any other item the look of its kind at its level (as she wears it,
    /// <see cref="ItemNames.LookIndex"/>), jewellery by the same five tiers. The pictures are in Resources/UI/ItemArt,
    /// made by ArtSource/tools/ui/import_item_art.py; <see cref="ItemIcons"/> falls back to the drawn icon where one is
    /// missing (the belts, the Quiver of the Hollow Hound). Pure, tested.
    /// </summary>
    public static class ItemArtNames
    {
        public const string Folder = "UI/ItemArt/";

        static readonly string[] Rings = { "iron_ring", "copper_ring", "silver_ring", "gold_ring", "jewelled_ring" };
        static readonly string[] Amulets = { "bone_charm", "copper_amulet", "silver_amulet", "gold_amulet", "jewelled_amulet" };

        public static string For(Item item)
        {
            if (item == null)
                return null;
            if (item.IsBlueprint)
                return "blueprint";
            if (item.Legendary != LegendaryId.None)
                return ForLegendary(item.Legendary);
            return ForLook(item.Slot, ItemNames.LookIndex(item.Slot, item.ItemLevel));
        }

        /// <summary>A named legendary's picture: its worn look's name, or its own for the two with none.</summary>
        public static string ForLegendary(LegendaryId id) => id switch
        {
            LegendaryId.PackLeadersSignet => "pack_leaders_signet",
            LegendaryId.EyeOfTheStorm => "eye_of_the_storm",
            _ => AppearanceRules.LegendaryLook(id),
        };

        /// <summary>The picture of a kind's look (0 the first).</summary>
        public static string ForLook(ItemSlot kind, int index)
        {
            string[] looks = kind switch
            {
                ItemSlot.Chest => AppearanceRules.ChestLooks,
                ItemSlot.Helm => AppearanceRules.HelmLooks,
                ItemSlot.Boots => AppearanceRules.BootLooks,
                ItemSlot.Belt => AppearanceRules.BeltLooks,
                ItemSlot.Gloves => AppearanceRules.GloveLooks,
                ItemSlot.Weapon => AppearanceRules.OneHandWeaponLooks,
                ItemSlot.TwoHandWeapon => AppearanceRules.TwoHandWeaponLooks,
                ItemSlot.Shield or ItemSlot.OffHand => AppearanceRules.ShieldLooks,
                ItemSlot.Ring or ItemSlot.Ring2 => Rings,
                ItemSlot.Amulet => Amulets,
                _ => null,
            };
            return looks == null ? null : looks[System.Math.Max(0, System.Math.Min(index, looks.Length - 1))];
        }
    }
}
