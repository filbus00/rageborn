using System.Collections.Generic;

namespace ARPG
{
    /// <summary>
    /// Plain item names (the owner, 2026-10-15: "Rename the weapons and armor to regular names, their uniqueness will be
    /// displayed by the color"): an item is called by what it looks like, its look picked by item level as the character
    /// shows it (<see cref="AppearanceRules.LookOf"/>), so a "Scale Mail" is the scale chest she wears. Jewellery has no
    /// look and is named by the armour's bands. Pure.
    /// </summary>
    public static class ItemNames
    {
        static readonly Dictionary<string, string> LookNames = new Dictionary<string, string>
        {
            { "ranger", "Ranger's Coat" }, { "brigand", "Brigandine" }, { "scale", "Scale Mail" }, { "plate", "Plate Armour" }, { "knight", "Knight's Plate" },
            { "hood", "Hood" }, { "mask", "Leather Mask" }, { "barbute", "Barbute" }, { "visored", "Visored Helm" }, { "horned", "Horned Helm" },
            { "wrapped", "Wrapped Boots" }, { "leather_boots", "Leather Boots" }, { "strapped", "Strapped Boots" }, { "sabatons", "Sabatons" }, { "spiked", "Spiked Greaves" },
            { "rope", "Rope Belt" }, { "pouch_belt", "Pouch Belt" }, { "studded", "Studded Belt" }, { "tassets", "Tassets" }, { "war_girdle", "War Girdle" },
            { "wraps", "Hand Wraps" }, { "gloves", "Leather Gloves" }, { "bracers", "Bracers" }, { "gauntlets", "Gauntlets" }, { "claws", "Clawed Gauntlets" },
            { "hunting_bow", "Hunting Bow" }, { "recurve_bow", "Recurve Bow" }, { "horn_bow", "Horn Bow" },
            { "yew_longbow", "Yew Longbow" }, { "war_bow", "War Bow" }, { "great_bow", "Great Bow" },
            { "hide_quiver", "Hide Quiver" }, { "studded_quiver", "Studded Quiver" }, { "bone_quiver", "Bone Quiver" },
        };

        static readonly string[] Rings = { "Iron Ring", "Copper Ring", "Silver Ring", "Gold Ring", "Jewelled Ring" };
        static readonly string[] Amulets = { "Bone Charm", "Copper Amulet", "Silver Amulet", "Gold Amulet", "Jewelled Amulet" };

        /// <summary>The looks an item kind is drawn in, in order of item level; jewellery has five names instead.</summary>
        public static int LookCount(ItemSlot kind) => Looks(kind)?.Length ?? (IsJewellery(kind) ? Rings.Length : 0);

        static bool IsJewellery(ItemSlot kind) => kind == ItemSlot.Ring || kind == ItemSlot.Ring2 || kind == ItemSlot.Amulet;

        static string[] Looks(ItemSlot kind) => kind switch
        {
            ItemSlot.Chest => AppearanceRules.ChestLooks,
            ItemSlot.Helm => AppearanceRules.HelmLooks,
            ItemSlot.Boots => AppearanceRules.BootLooks,
            ItemSlot.Belt => AppearanceRules.BeltLooks,
            ItemSlot.Gloves => AppearanceRules.GloveLooks,
            ItemSlot.Weapon => AppearanceRules.OneHandWeaponLooks,
            ItemSlot.TwoHandWeapon => AppearanceRules.TwoHandWeaponLooks,
            ItemSlot.Shield or ItemSlot.OffHand => AppearanceRules.ShieldLooks,
            _ => null,
        };

        /// <summary>Which of the kind's looks an item of this level has (0 the first).</summary>
        public static int LookIndex(ItemSlot kind, int itemLevel)
        {
            var count = LookCount(kind);
            if (count == 0)
                return 0;
            var tier = count == 5 ? AppearanceRules.ArmourTier(itemLevel) : AppearanceRules.Tier(itemLevel);
            return System.Math.Min(tier, count - 1);
        }

        /// <summary>The highest item level that still has a look (the top look has no ceiling: its lowest level).</summary>
        public static int LevelOfLook(ItemSlot kind, int index)
        {
            var five = LookCount(kind) == 5;
            int[] tops = five ? new[] { 3, 7, 11, 15, 16 } : new[] { 3, 6, 7 };
            return tops[System.Math.Max(0, System.Math.Min(index, tops.Length - 1))];
        }

        public static string LookName(ItemSlot kind, int index)
        {
            var looks = Looks(kind);
            if (looks != null)
            {
                var look = looks[System.Math.Max(0, System.Math.Min(index, looks.Length - 1))];
                return LookNames.TryGetValue(look, out var name) ? name : look;
            }
            if (IsJewellery(kind))
                return (kind == ItemSlot.Amulet ? Amulets : Rings)[System.Math.Max(0, System.Math.Min(index, Rings.Length - 1))];
            return ItemComparison.KindName(kind);
        }

        public static string BaseName(ItemSlot kind, int itemLevel) => LookName(kind, LookIndex(kind, itemLevel));
    }

    /// <summary>
    /// Blueprints (the owner, 2026-10-15: "Add rare drops of blueprints that get thought to a smith that can then build the
    /// weapons"; Thorin learns one for good): a rare drop for an item kind in the look of the level it dropped at. Carried
    /// home through the portal (or at the end of the run won), Thorin the smith learns it; he then forges that item, Rare,
    /// for coins, and it is the kit she starts every run wearing in its place after a death. A blueprint carried at death
    /// is lost with the rest. Numbers are Claude's, to review (Docs/08). Pure.
    /// </summary>
    public static class BlueprintRules
    {
        /// <summary>The kinds a blueprint can teach.</summary>
        public static readonly ItemSlot[] Kinds =
        {
            ItemSlot.Weapon, ItemSlot.TwoHandWeapon, ItemSlot.Shield, ItemSlot.Chest, ItemSlot.Helm, ItemSlot.Gloves,
            ItemSlot.Boots, ItemSlot.Belt, ItemSlot.Amulet, ItemSlot.Ring,
        };

        public static float DropChance(LootSource source) => source switch
        {
            LootSource.Champion => 0.012f,
            LootSource.Elite => 0.04f,
            LootSource.ZoneChest => 0.04f,
            LootSource.Boss => 0.35f,
            _ => 0.002f,
        };

        /// <summary>The key a blueprint is known by: its kind and look ("Chest/2").</summary>
        public static string Key(ItemSlot kind, int lookIndex) => kind + "/" + lookIndex;

        public static string Key(Item blueprint) => Key(blueprint.Slot, ItemNames.LookIndex(blueprint.Slot, blueprint.ItemLevel));

        public static bool TryParse(string key, out ItemSlot kind, out int lookIndex)
        {
            kind = ItemSlot.Weapon;
            lookIndex = 0;
            var slash = key?.IndexOf('/') ?? -1;
            return slash > 0 && System.Enum.TryParse(key.Substring(0, slash), out kind) && System.Enum.IsDefined(typeof(ItemSlot), kind) &&
                   int.TryParse(key.Substring(slash + 1), out lookIndex) && lookIndex >= 0 && lookIndex < ItemNames.LookCount(kind);
        }

        /// <summary>A blueprint for a kind not yet known in the look of this item level, or null when every kind's is known.</summary>
        public static Item Roll(int itemLevel, ICollection<string> known, System.Random random)
        {
            var open = new List<ItemSlot>();
            foreach (var kind in Kinds)
                if (!known.Contains(Key(kind, ItemNames.LookIndex(kind, itemLevel))))
                    open.Add(kind);
            if (open.Count == 0)
                return null;
            var pick = open[random.Next(open.Count)];
            return new Item(pick, ItemRarity.Rare, itemLevel, blueprint: true);
        }

        /// <summary>What Thorin asks to forge an item from a blueprint: more for the later looks.</summary>
        public static int ForgeCost(int lookIndex) => 300 + 400 * (lookIndex + 1) * (lookIndex + 1);

        /// <summary>The item level Thorin forges a look at: the top of its band, and for the last look the deepest
        /// level she has reached.</summary>
        public static int ForgeLevel(ItemSlot kind, int lookIndex, int deepestEnemyLevel)
        {
            var level = ItemNames.LevelOfLook(kind, lookIndex);
            return lookIndex >= ItemNames.LookCount(kind) - 1 ? System.Math.Max(level, deepestEnemyLevel) : level;
        }
    }
}
