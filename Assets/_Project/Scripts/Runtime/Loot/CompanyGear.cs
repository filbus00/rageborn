using System.Collections.Generic;

namespace ARPG
{
    /// <summary>The upgrade materials (2026-11-26), one for each keeper who makes gear.</summary>
    public enum CompanyMaterial
    {
        Iron,       // scrap iron, for Thorin's armour
        Heartwood,  // for the fletcher's bows and quivers
        Gems,       // rough gems, for the jeweller's rings, amulet and trinkets
    }

    /// <summary>The three keepers who make the company's gear.</summary>
    public enum GearKeeper
    {
        Thorin,
        Fletcher,
        Jeweller,
    }

    /// <summary>A trinket (found, never made, 2026-11-26): its name, what it does, and its two affixes at their full
    /// strength.</summary>
    public sealed class TrinketDesign
    {
        public TrinketDesign(string id, string name, string line, AffixId first, float firstValue, AffixId second, float secondValue)
        {
            Id = id;
            Name = name;
            Line = line;
            Affixes = new[] { first, second };
            Values = new[] { firstValue, secondValue };
        }

        public string Id { get; }
        public string Name { get; }
        public string Line { get; }
        public AffixId[] Affixes { get; }
        public float[] Values { get; }
    }

    /// <summary>
    /// Trinkets (2026-11-26, the owner: "make it so that trinkets are found. Make them much more powerful and make the very
    /// rare. More rare than legendary items"): worn in their own slot, never made by a keeper, dropped by kills and chests
    /// far more rarely than legendaries, <see cref="ItemRarity.Mythic"/>. Each is one of the designs below, its two
    /// affixes several times what gear rolls, each rolled between 85 and 100 percent of its full strength. Numbers are
    /// Claude's, to review (Docs/08). Pure.
    /// </summary>
    public static class TrinketRules
    {
        public static readonly TrinketDesign[] Designs =
        {
            new TrinketDesign("wolf_tooth", "Wolf's Tooth", "The wolf bites far harder and lasts far longer.", AffixId.PetDamage, 80f, AffixId.PetLife, 80f),
            new TrinketDesign("ember_charm", "Ember Charm", "Arrows set fires that burn twice as hot.", AffixId.IgniteChance, 30f, AffixId.BurnDamage, 100f),
            new TrinketDesign("frost_bead", "Frost Bead", "Arrows chill often and fly much faster.", AffixId.ChillChance, 30f, AffixId.ArrowSpeed, 60f),
            new TrinketDesign("hare_foot", "Hare's Foot", "Much quicker feet, and blows miss her often.", AffixId.MovementSpeed, 25f, AffixId.DodgeChance, 12f),
            new TrinketDesign("hawk_feather", "Hawk Feather", "Criticals come often, and far shots hit hard.", AffixId.CriticalChance, 15f, AffixId.FarDamage, 60f),
            new TrinketDesign("blood_knot", "Blood Knot", "Every hit heals her, and criticals tear.", AffixId.LifeOnHit, 12f, AffixId.CriticalDamage, 80f),
            new TrinketDesign("huntsmans_knot", "Huntsman's Knot", "She draws faster and looses more arrows.", AffixId.AttackSpeed, 25f, AffixId.ExtraArrowChance, 30f),
            new TrinketDesign("thorn_sigil", "Thorn Sigil", "Every arrow pierces once more, and harder each time.", AffixId.ExtraPierce, 1f, AffixId.DamagePerPierce, 30f),
        };

        public static TrinketDesign Get(string id)
        {
            foreach (var design in Designs)
                if (design.Id == id)
                    return design;
            return null;
        }

        /// <summary>A trinket's design, by its first affix (every design's first is its own), or null.</summary>
        public static TrinketDesign Of(Item item)
        {
            if (item == null || item.Slot != ItemSlot.Trinket || item.Affixes.Count == 0)
                return null;
            foreach (var design in Designs)
                if (design.Affixes[0] == item.Affixes[0].Id)
                    return design;
            return null;
        }

        /// <summary>The chance a kill or a chest drops a trinket: well under a legendary's (bosses drop one about one time
        /// in twenty-five).</summary>
        public static float DropChance(LootSource source) => source switch
        {
            LootSource.Champion => 0.0004f,
            LootSource.Elite => 0.002f,
            LootSource.ZoneChest => 0.003f,
            LootSource.Boss => 0.04f,
            _ => 0.00003f,
        };

        /// <summary>A trinket of a design, its values rolled between 85 and 100 percent of full.</summary>
        public static Item Make(TrinketDesign design, int itemLevel, System.Random random)
        {
            var affixes = new AffixRoll[design.Affixes.Length];
            for (var i = 0; i < affixes.Length; i++)
            {
                var full = design.Values[i];
                var value = AffixTable.Get(design.Affixes[i]).Fixed ? full : full * (0.85f + 0.15f * (float)random.NextDouble());
                affixes[i] = new AffixRoll(design.Affixes[i], AffixTable.BestTier, value >= 10f ? (float)System.Math.Round(value) : (float)System.Math.Round(value, 1));
            }
            return new Item(ItemSlot.Trinket, ItemRarity.Mythic, itemLevel, affixes);
        }

        /// <summary>A kill's or a chest's trinket, or null (most of the time).</summary>
        public static Item Roll(LootSource source, int itemLevel, System.Random random)
        {
            if (random.NextDouble() >= DropChance(source))
                return null;
            return Make(Designs[random.Next(Designs.Length)], itemLevel, random);
        }
    }

    /// <summary>
    /// The company's gear (2026-11-26, the owner: "Remake the gear building system. Make it so that thorin furnishes the
    /// player with armor, make a jeweler that makes rings, amulets and special trinkets and a fletcher that makes quivers
    /// and bows. Make it so that they all offer "standard" company gear but that the gear, by picking up upgrade materials
    /// in the game can be permanently upgraded"; in place of the blueprints). Every kind of gear has one company piece with
    /// a rank, 1 to <see cref="MaxRank"/>, kept for good. A piece is always the same item for its kind and rank (no
    /// rolling): its item level and rarity rise with the rank, and so do its fixed affixes, in the order the kind lists
    /// them. Every archer starts a run in the company's kit, and a rank raised in camp is issued at once. Ranks are raised
    /// with the keeper's material, picked up in the levels, and coins. Numbers are Claude's, to review (Docs/08). Pure.
    /// </summary>
    public static class CompanyGearRules
    {
        public const int MaxRank = 10;

        /// <summary>The kinds each keeper makes, in the order his page shows them.</summary>
        public static ItemSlot[] KindsOf(GearKeeper keeper) => keeper switch
        {
            GearKeeper.Thorin => ThorinKinds,
            GearKeeper.Fletcher => FletcherKinds,
            _ => JewellerKinds,
        };

        static readonly ItemSlot[] ThorinKinds = { ItemSlot.Chest, ItemSlot.Helm, ItemSlot.Gloves, ItemSlot.Boots, ItemSlot.Belt };
        static readonly ItemSlot[] FletcherKinds = { ItemSlot.Weapon, ItemSlot.TwoHandWeapon, ItemSlot.Shield };
        static readonly ItemSlot[] JewellerKinds = { ItemSlot.Ring, ItemSlot.Amulet };

        /// <summary>Every kind with a company piece.</summary>
        public static readonly ItemSlot[] Kinds =
        {
            ItemSlot.Weapon, ItemSlot.TwoHandWeapon, ItemSlot.Shield, ItemSlot.Chest, ItemSlot.Helm, ItemSlot.Gloves,
            ItemSlot.Boots, ItemSlot.Belt, ItemSlot.Ring, ItemSlot.Amulet,
        };

        public static GearKeeper KeeperOf(ItemSlot kind) => kind switch
        {
            ItemSlot.Weapon or ItemSlot.TwoHandWeapon or ItemSlot.Shield or ItemSlot.OffHand => GearKeeper.Fletcher,
            ItemSlot.Ring or ItemSlot.Ring2 or ItemSlot.Amulet or ItemSlot.Trinket => GearKeeper.Jeweller,
            _ => GearKeeper.Thorin,
        };

        public static CompanyMaterial MaterialOf(GearKeeper keeper) => keeper switch
        {
            GearKeeper.Thorin => CompanyMaterial.Iron,
            GearKeeper.Fletcher => CompanyMaterial.Heartwood,
            _ => CompanyMaterial.Gems,
        };

        public static CompanyMaterial MaterialOf(ItemSlot kind) => MaterialOf(KeeperOf(kind));

        public static string MaterialName(CompanyMaterial material) => material switch
        {
            CompanyMaterial.Iron => "Scrap iron",
            CompanyMaterial.Heartwood => "Heartwood",
            _ => "Rough gems",
        };

        /// <summary>A kind as the keepers name it ("Short bow", "Longbow", "Quiver", ...).</summary>
        public static string KindName(ItemSlot kind) => kind switch
        {
            ItemSlot.Weapon => "Short bow",
            ItemSlot.TwoHandWeapon => "Longbow",
            ItemSlot.Shield => "Quiver",
            ItemSlot.Ring or ItemSlot.Ring2 => "Rings",
            _ => kind.ToString(),
        };

        // --- What a piece is ------------------------------------------------------------------------------------------

        static readonly int[] Levels = { 1, 3, 6, 9, 12, 16, 20, 24, 28, 33 };
        // No Common pieces (2026-11-26, the owner: "Remove common quality items"): rank 1 has an affix.
        static readonly int[] AffixCounts = { 1, 1, 2, 3, 3, 4, 4, 5, 5, 6 };

        public static int Clamp(int rank) => rank < 1 ? 1 : rank > MaxRank ? MaxRank : rank;

        /// <summary>The item level of a rank: from 1 to the last depth's enemy level.</summary>
        public static int LevelAt(int rank) => Levels[Clamp(rank) - 1];

        /// <summary>How many of its kind's affixes a rank carries.</summary>
        public static int AffixCount(int rank) => AffixCounts[Clamp(rank) - 1];

        public static ItemRarity RarityAt(int rank)
        {
            var count = AffixCount(rank);
            return count == 0 ? ItemRarity.Common : count <= 2 ? ItemRarity.Magic : ItemRarity.Rare;
        }

        /// <summary>Where in each affix's range a rank's value sits: a fifth of its best at rank 1, three quarters at the top.</summary>
        public static float Share(int rank) => 0.2f + 0.55f * (Clamp(rank) - 1) / (MaxRank - 1);

        /// <summary>The affixes a kind's company piece takes, first to last as its rank rises.</summary>
        public static AffixId[] AffixesOf(ItemSlot kind) => kind switch
        {
            ItemSlot.Weapon => new[] { AffixId.FlatWeaponDamage, AffixId.AttackSpeed, AffixId.CriticalChance, AffixId.CriticalDamage, AffixId.NearDamage, AffixId.IgniteChance },
            ItemSlot.TwoHandWeapon => new[] { AffixId.FlatWeaponDamage, AffixId.CriticalDamage, AffixId.FarDamage, AffixId.CriticalChance, AffixId.DamagePerPierce, AffixId.ForkChance },
            ItemSlot.Shield => new[] { AffixId.ExtraArrowChance, AffixId.AttackSpeed, AffixId.ArrowSpeed, AffixId.FlatWeaponDamage, AffixId.Life, AffixId.ExtraPierce },
            ItemSlot.Chest => new[] { AffixId.Life, AffixId.Armor, AffixId.PetLife },
            ItemSlot.Helm => new[] { AffixId.Life, AffixId.Armor, AffixId.CooldownReduction, AffixId.FarDamage },
            ItemSlot.Gloves => new[] { AffixId.AttackSpeed, AffixId.Armor, AffixId.CriticalChance, AffixId.IncreasedDamage, AffixId.LifeOnHit },
            ItemSlot.Boots => new[] { AffixId.MovementSpeed, AffixId.Armor, AffixId.Life, AffixId.DodgeChance },
            ItemSlot.Belt => new[] { AffixId.Life, AffixId.DodgeChance, AffixId.PetDamage, AffixId.PetLife },
            ItemSlot.Ring or ItemSlot.Ring2 => new[] { AffixId.CriticalChance, AffixId.AttackSpeed, AffixId.CooldownReduction, AffixId.CriticalDamage, AffixId.LifeOnHit },
            ItemSlot.Amulet => new[] { AffixId.IncreasedDamage, AffixId.CriticalDamage, AffixId.CooldownReduction, AffixId.CriticalChance, AffixId.BurnDamage },
            _ => System.Array.Empty<AffixId>(),
        };

        /// <summary>A trinket's design (<see cref="TrinketRules.Of"/>).</summary>
        public static TrinketDesign TrinketOf(Item item) => TrinketRules.Of(item);

        /// <summary>The company's piece of a kind at a rank: always the same item. (Trinkets are found, never made: none.)</summary>
        public static Item Make(ItemSlot kind, int rank)
        {
            if (kind == ItemSlot.Ring2)
                kind = ItemSlot.Ring;
            rank = Clamp(rank);
            var level = LevelAt(rank);
            var pool = AffixesOf(kind);
            var count = System.Math.Min(AffixCount(rank), pool.Length);
            var affixes = new List<AffixRoll>(count);
            for (var i = 0; i < pool.Length && affixes.Count < count; i++)
            {
                var id = pool[i];
                if (level < AffixTable.Get(id).MinItemLevel)
                    continue;
                affixes.Add(new AffixRoll(id, TierAt(rank), Value(id, rank)));
            }
            return new Item(kind, RarityAt(rank), level, affixes.ToArray(), issue: rank);
        }

        /// <summary>An affix's value on a company piece of this rank (rounded as players read it).</summary>
        public static float Value(AffixId id, int rank)
        {
            var definition = AffixTable.Get(id);
            if (definition.Fixed)
                return definition.T1Min;
            var level = AffixRoller.ScalesWithLevel(id) ? AffixRoller.LevelScale(LevelAt(rank)) : 1f;
            var value = (definition.T1Min + definition.T1Max) * 0.5f * Share(rank) * level;
            return value >= 10f ? (float)System.Math.Round(value) : (float)System.Math.Round(value, 1);
        }

        // The tier a rank's affixes are shown at: 5 at the bottom to 2 at the top.
        static int TierAt(int rank) => System.Math.Max(2, AffixTable.WorstTier - (Clamp(rank) - 1) / 3);

        /// <summary>The company's name for a piece: its look's name and "+2" for rank 3.</summary>
        public static string Name(Item item)
        {
            var design = TrinketRules.Of(item);
            var name = design != null ? design.Name : ItemNames.BaseName(item.Slot, item.ItemLevel);
            return item.Issue > 1 ? $"{name} +{item.Issue - 1}" : name;
        }

        // --- Raising a rank -------------------------------------------------------------------------------------------

        /// <summary>The material it takes to raise a piece from this rank to the next.</summary>
        public static int MaterialCost(int rank) => Clamp(rank) + 1;

        /// <summary>The coins it takes to raise a piece from this rank to the next.</summary>
        public static int CoinCost(int rank) => 80 * Clamp(rank) * Clamp(rank);

        // --- Materials in the levels ----------------------------------------------------------------------------------

        /// <summary>The chance a kill or a chest drops material.</summary>
        public static float DropChance(LootSource source) => source switch
        {
            LootSource.Champion => 0.3f,
            LootSource.Elite => 0.6f,
            LootSource.ZoneChest => 1f,
            LootSource.Boss => 1f,
            _ => 0.025f,
        };

        /// <summary>How much a drop is: more from chests and bosses, and one more for each contract below the first.</summary>
        public static int DropAmount(LootSource source, int depth)
        {
            var deeper = System.Math.Max(0, (System.Math.Max(1, depth) - 1) / 12);
            var amount = source == LootSource.Boss ? 6 : source == LootSource.ZoneChest ? 2 : 1;
            return amount + deeper;
        }

        /// <summary>Which material a drop is: in the share of the pieces each keeper makes (5 armour, 3 bows and quivers,
        /// 2 jewellery).</summary>
        public static CompanyMaterial PickMaterial(System.Random random)
        {
            var roll = random.Next(10);
            return roll < 5 ? CompanyMaterial.Iron : roll < 8 ? CompanyMaterial.Heartwood : CompanyMaterial.Gems;
        }

        /// <summary>A kill's or a chest's material, or false for none.</summary>
        public static bool RollDrop(LootSource source, int depth, System.Random random, out CompanyMaterial material, out int amount)
        {
            material = CompanyMaterial.Iron;
            amount = 0;
            if (random.NextDouble() >= DropChance(source))
                return false;
            material = PickMaterial(random);
            amount = DropAmount(source, depth);
            return true;
        }

        // --- Recycling (2026-11-26, the owner: "Make it so that the vendors can recycle dropped gear for upgrade materials") --

        /// <summary>What a keeper gives for a found piece he takes apart: his own material, more for a better rarity and a
        /// deeper item. Company pieces and blueprints give nothing.</summary>
        public static int RecycleYield(Item item)
        {
            if (item == null || item.IsBlueprint || item.IsCompanyIssue)
                return 0;
            var basis = item.Rarity switch
            {
                ItemRarity.Common => 1,
                ItemRarity.Magic => 1,
                ItemRarity.Rare => 3,
                ItemRarity.Legendary => 8,
                _ => 20,
            };
            return basis + item.ItemLevel / 12;
        }

        /// <summary>An old save's rank for a kind (format 25): from the best blueprint Thorin knew, two ranks a look.</summary>
        public static int RankFromBlueprint(int bestLook) => bestLook < 0 ? 1 : Clamp(1 + 2 * bestLook);
    }
}
