using System.Collections.Generic;

namespace ARPG
{
    /// <summary>
    /// The named legendaries (Docs/03, "The Wild Arrow's loot depth", the owner's decisions of 2026-10-01). None is a
    /// plain item, or a Legendary rolled where no named one fits (below its minimum item level). New ones go at the end:
    /// saves store names, but tests and tools read the order.
    /// </summary>
    public enum LegendaryId
    {
        None,
        Splinterbough,
        QuiverOfEndlessSplinters,
        Gallowsreach,
        EmberTongue,
        AshfallQuiver,
        HuntersPromise,
        StillwaterYew,
        WindSwornQuiver,
        Galeheart,
        MagpiesNest,
        PackLeadersSignet,
        WidowsDraw,
        TheLongSilence,
        QuiverOfTheHollowHound,
        FalconersHood,
        CrownOfTheUnblinkingEye,
        HideOfTheRunningStag,
        CinderStitchedJerkin,
        FletchersFingers,
        BloodlettersGrips,
        WindrunnerTreads,
        StalkersTreads,
        BandolierOfManyHeads,
        EyeOfTheStorm,
    }

    /// <summary>One named legendary: its kind, the affixes it always carries, its power in words, where it drops twice
    /// as often (its home) and the lowest item level it drops at.</summary>
    public sealed class LegendaryDefinition
    {
        public LegendaryDefinition(LegendaryId id, string name, ItemSlot slot, AffixId[] fixedAffixes, string power,
            LootSource home, string homeText, string archetype)
        {
            Id = id;
            Name = name;
            Slot = slot;
            FixedAffixes = fixedAffixes;
            Power = power;
            Home = home;
            HomeText = homeText;
            Archetype = archetype;
        }

        public LegendaryId Id { get; }
        public string Name { get; }
        public ItemSlot Slot { get; }
        public IReadOnlyList<AffixId> FixedAffixes { get; }
        public string Power { get; }
        public LootSource Home { get; }
        public string HomeText { get; }
        public string Archetype { get; }
    }

    /// <summary>
    /// The legendary table and how one is chosen for a drop (Docs/03, decided 2026-10-01: any legendary can drop
    /// anywhere, and twice as often from its home). All 24 of Docs/03's table, with the fixed affixes it lists where they are
    /// built and can roll on the slot (the rest stand in from the older pool). All numbers are tuning. Pure.
    /// </summary>
    public static class Legendaries
    {
        /// <summary>Named legendaries drop from this item level (Docs/03 proposed 8 and 20; lower so act 1 shows them).</summary>
        public const int MinItemLevel = 3;

        /// <summary>A legendary at its home is this much likelier than elsewhere.</summary>
        public const float HomeWeight = 2f;

        // The powers' numbers, read where each power lands.
        public const float ForkShare = 0.5f;
        public const float ForkDegrees = 45f;
        public const float GallowsPerPierce = 0.2f;
        public const float GallowsBleedPerSecond = 0.4f;
        public const float GallowsPoolSeconds = 4f;
        public const float EmberTongueBurstBonus = 0.6f;
        public const int AshfallEvery = 5;
        public const float AshfallShare = 0.6f;
        public const int HuntersPromiseArrows = 2;
        public const int WindSwornPierce = 2;
        public const int GaleheartArrows = 2;
        public const float GaleheartFanDegrees = 20f;
        public const float MagpieRicochetBonus = 0.3f;
        public const float PackLeaderFocusOnKill = 5f;
        public const float WidowBleedPerSecond = 0.3f;
        public const float WidowBleedSeconds = 3f;
        public const float WidowSpreadRadius = 3f;
        public const float LongSilenceReach = 2f;
        public const float LongSilenceFar = 7f;
        public const float LongSilenceBonus = 0.3f;
        public const float HoundMarkSeconds = 4f;
        public const float HoundMarkBonus = 0.25f;
        public const float FalconerCooldownCut = 2f;
        public const float FalconerFocusPerHit = 2f;
        public const int StagMomentumCap = 3;
        public const float StagAttackSpeedPerMomentum = 0.02f;
        public const float CinderGroundSeconds = 3f;
        public const float CinderGroundPerSecond = 0.6f;
        public const float CinderGroundSlow = 0.2f;
        public const int FletcherArrows = 3;
        public const float FletcherFocusCut = 10f;
        public const float BloodletterBonus = 0.2f;
        public const float BloodletterExtendSeconds = 1f;
        public const float WindrunnerSeconds = 3f;
        public const float StalkerBonus = 0.4f;
        public const int BandolierArrows = 1;
        public const float BandolierExtraArrowFactor = 2f;
        public const float StormRadius = 5f;
        public const int StormEnemies = 3;
        public const float StormRegenBonus = 0.5f;
        public const float StormLowFocus = 30f;
        public const float StormCostCut = 0.25f;

        static readonly LegendaryDefinition[] Table =
        {
            null,
            new LegendaryDefinition(LegendaryId.Splinterbough, "Splinterbough", ItemSlot.Weapon,
                new[] { AffixId.FlatWeaponDamage, AffixId.CriticalChance, AffixId.ForkChance, AffixId.ArrowSpeed },
                "Split Arrow's arrows fork on their first hit", LootSource.Elite, "Elites", "Splinterstorm"),
            new LegendaryDefinition(LegendaryId.QuiverOfEndlessSplinters, "Quiver of Endless Splinters", ItemSlot.Shield,
                new[] { AffixId.FlatWeaponDamage, AffixId.ExtraArrowChance, AffixId.ForkChance, AffixId.AttackSpeed },
                "Every basic arrow forks on its first hit", LootSource.NormalEnemy, "Any enemy", "Splinterstorm"),
            new LegendaryDefinition(LegendaryId.Gallowsreach, "Gallowsreach", ItemSlot.TwoHandWeapon,
                new[] { AffixId.FlatWeaponDamage, AffixId.IncreasedDamage, AffixId.DamagePerPierce, AffixId.CriticalDamage },
                "Pierce Arrow gains 20% damage for each enemy it passes through, and its blood trail lies on the floor for 4 s, making enemies who walk through it bleed",
                LootSource.Boss, "The Cinder Warden", "Blood Hunter"),
            new LegendaryDefinition(LegendaryId.EmberTongue, "Ember-Tongue", ItemSlot.Weapon,
                new[] { AffixId.FlatWeaponDamage, AffixId.IncreasedDamage, AffixId.IgniteChance, AffixId.BurnDamage },
                "Basic arrows always ignite; Explosive Arrow deals 60% more to burning enemies", LootSource.Boss, "The Cinder Warden", "Pyre"),
            new LegendaryDefinition(LegendaryId.AshfallQuiver, "Ashfall Quiver", ItemSlot.Shield,
                new[] { AffixId.FlatWeaponDamage, AffixId.IgniteChance, AffixId.ExtraArrowChance },
                "Every fifth basic arrow is a free Explosive Arrow at 60%", LootSource.ZoneChest, "Chests", "Pyre"),
            new LegendaryDefinition(LegendaryId.HuntersPromise, "Hunter's Promise", ItemSlot.Weapon,
                new[] { AffixId.FlatWeaponDamage, AffixId.IncreasedDamage, AffixId.CriticalChance },
                "Homing Arrow looses 2 more arrows, and an arrow whose target dies seeks a new one once", LootSource.Elite, "Elites", "Seeker"),
            new LegendaryDefinition(LegendaryId.StillwaterYew, "Stillwater Yew", ItemSlot.TwoHandWeapon,
                new[] { AffixId.FlatWeaponDamage, AffixId.CriticalDamage, AffixId.CriticalChance, AffixId.FarDamage },
                "At full Stillness the next basic arrow is a certain crit that pierces every enemy", LootSource.ZoneChest, "Chests", "Sniper"),
            new LegendaryDefinition(LegendaryId.WindSwornQuiver, "Wind-Sworn Quiver", ItemSlot.Shield,
                new[] { AffixId.Life, AffixId.ExtraPierce, AffixId.AttackSpeed, AffixId.CriticalChance },
                "While Momentum is at its cap, every arrow pierces 2 more enemies", LootSource.Elite, "Elites", "Windrunner"),
            new LegendaryDefinition(LegendaryId.Galeheart, "Galeheart", ItemSlot.Weapon,
                new[] { AffixId.FlatWeaponDamage, AffixId.AttackSpeed, AffixId.CriticalDamage },
                "At full Momentum every basic shot looses 2 more arrows in a 20 degree fan", LootSource.ZoneChest, "Chests", "Windrunner"),
            new LegendaryDefinition(LegendaryId.MagpiesNest, "Magpie's Nest", ItemSlot.Shield,
                new[] { AffixId.FlatWeaponDamage, AffixId.Life, AffixId.ArrowSpeed, AffixId.ExtraArrowChance },
                "Arrows ricochet off walls once toward the nearest enemy; a ricochet deals 30% more", LootSource.ZoneChest, "Chests", "Splinterstorm, Seeker"),
            new LegendaryDefinition(LegendaryId.PackLeadersSignet, "Pack Leader's Signet", ItemSlot.Ring,
                new[] { AffixId.PetDamage, AffixId.CriticalChance, AffixId.CriticalDamage },
                "The pet bites with your attack speed and critical chance; its kills give 5 Focus", LootSource.Champion, "Champions", "Beastmaster"),
            new LegendaryDefinition(LegendaryId.WidowsDraw, "The Widow's Draw", ItemSlot.Weapon,
                new[] { AffixId.FlatWeaponDamage, AffixId.CriticalChance, AffixId.CriticalDamage },
                "A basic arrow that crits makes its target bleed; a bleeding enemy hit by a basic arrow passes its bleed to the nearest enemy within 3",
                LootSource.Elite, "Elites", "Blood Hunter"),
            new LegendaryDefinition(LegendaryId.TheLongSilence, "The Long Silence", ItemSlot.TwoHandWeapon,
                new[] { AffixId.FlatWeaponDamage, AffixId.IncreasedDamage, AffixId.FarDamage },
                "Reach plus 2; enemies further than 7 away take 30% more from arrows", LootSource.Boss, "The Cinder Warden", "Sniper"),
            new LegendaryDefinition(LegendaryId.QuiverOfTheHollowHound, "Quiver of the Hollow Hound", ItemSlot.Shield,
                new[] { AffixId.Life, AffixId.ExtraArrowChance, AffixId.AttackSpeed },
                "Enemies the pet bites are marked for 4 s; arrows deal 25% more to marked enemies, and Homing Arrow seeks them first",
                LootSource.Champion, "Champions", "Beastmaster"),
            new LegendaryDefinition(LegendaryId.FalconersHood, "Falconer's Hood", ItemSlot.Helm,
                new[] { AffixId.Life, AffixId.Armor, AffixId.CooldownReduction },
                "Homing Arrow's cooldown is 2 s shorter, and each homing hit gives 2 Focus", LootSource.Elite, "Elites", "Seeker"),
            new LegendaryDefinition(LegendaryId.CrownOfTheUnblinkingEye, "Crown of the Unblinking Eye", ItemSlot.Helm,
                new[] { AffixId.Armor, AffixId.Life, AffixId.FarDamage },
                "A Pierce Arrow that crits resets its own cooldown, once per cast", LootSource.Boss, "The Cinder Warden", "Sniper"),
            new LegendaryDefinition(LegendaryId.HideOfTheRunningStag, "Hide of the Running Stag", ItemSlot.Chest,
                new[] { AffixId.Life, AffixId.Armor },
                "Momentum cap plus 3; each Momentum stack also gives 2% attack speed", LootSource.ZoneChest, "Chests", "Windrunner"),
            new LegendaryDefinition(LegendaryId.CinderStitchedJerkin, "Cinder-Stitched Jerkin", ItemSlot.Chest,
                new[] { AffixId.Armor, AffixId.Life },
                "Explosive Arrow leaves burning ground for 3 s (60% a second) that slows enemies 20%", LootSource.Boss, "The Cinder Warden", "Pyre"),
            new LegendaryDefinition(LegendaryId.FletchersFingers, "Fletcher's Fingers", ItemSlot.Gloves,
                new[] { AffixId.FlatWeaponDamage, AffixId.CriticalChance, AffixId.ExtraSkillArrow, AffixId.AttackSpeed },
                "Split Arrow looses 3 more arrows and costs 10 less Focus", LootSource.NormalEnemy, "Any enemy", "Splinterstorm"),
            new LegendaryDefinition(LegendaryId.BloodlettersGrips, "Bloodletter's Grips", ItemSlot.Gloves,
                new[] { AffixId.LifeOnHit, AffixId.CriticalChance, AffixId.IncreasedDamage },
                "Bleeding enemies take 20% more from arrows; crits on them add 1 s to the bleed", LootSource.Champion, "Champions", "Blood Hunter"),
            new LegendaryDefinition(LegendaryId.WindrunnerTreads, "Windrunner Treads", ItemSlot.Boots,
                new[] { AffixId.MovementSpeed, AffixId.Life, AffixId.DodgeChance },
                "After 3 s of moving without stopping, the next skill costs no Focus", LootSource.NormalEnemy, "Any enemy", "Windrunner"),
            new LegendaryDefinition(LegendaryId.StalkersTreads, "Stalker's Treads", ItemSlot.Boots,
                new[] { AffixId.MovementSpeed, AffixId.Armor, AffixId.Life },
                "At full Stillness, Pierce Arrow and Explosive Arrow deal 40% more", LootSource.ZoneChest, "Chests", "Sniper, Pyre"),
            new LegendaryDefinition(LegendaryId.BandolierOfManyHeads, "Bandolier of Many Heads", ItemSlot.Belt,
                new[] { AffixId.Life, AffixId.PetLife, AffixId.DodgeChance },
                "Split Arrow and Homing Arrow loose 1 more arrow; the quiver's extra arrow chance is doubled", LootSource.Elite, "Elites", "Splinterstorm, Seeker"),
            new LegendaryDefinition(LegendaryId.EyeOfTheStorm, "Eye of the Storm", ItemSlot.Amulet,
                new[] { AffixId.IncreasedDamage, AffixId.CooldownReduction, AffixId.CriticalChance },
                "Focus regenerates 50% faster with 3 or more enemies within 5; skills cost 25% less below 30 Focus",
                LootSource.NormalEnemy, "Any enemy", "Any"),
        };

        /// <summary>The definition, or null for <see cref="LegendaryId.None"/> or an unknown id.</summary>
        public static LegendaryDefinition Get(LegendaryId id) => (int)id > 0 && (int)id < Table.Length ? Table[(int)id] : null;

        /// <summary>Every named legendary, in table order.</summary>
        public static IEnumerable<LegendaryDefinition> All
        {
            get
            {
                for (var i = 1; i < Table.Length; i++)
                    yield return Table[i];
            }
        }

        /// <summary>
        /// Picks a named legendary for a drop from <paramref name="source"/> at an item level: every one is possible from
        /// <see cref="MinItemLevel"/>, those at home <see cref="HomeWeight"/> times as likely. None below the minimum.
        /// </summary>
        public static LegendaryId Pick(LootSource source, int itemLevel, System.Random random)
        {
            if (itemLevel < MinItemLevel)
                return LegendaryId.None;
            var total = 0f;
            for (var i = 1; i < Table.Length; i++)
                total += Weight(Table[i], source);
            var roll = (float)random.NextDouble() * total;
            for (var i = 1; i < Table.Length; i++)
            {
                roll -= Weight(Table[i], source);
                if (roll < 0f)
                    return Table[i].Id;
            }
            return Table[Table.Length - 1].Id;
        }

        public static float Weight(LegendaryDefinition legendary, LootSource source) => legendary.Home == source ? HomeWeight : 1f;

        /// <summary>
        /// A named legendary's affixes: its fixed ones, each at the best tier the item level has unlocked with a rolled
        /// value, then up to two of <paramref name="rolled"/> (the generic roll) that it does not already carry.
        /// </summary>
        public static AffixRoll[] Affixes(LegendaryDefinition legendary, int itemLevel, AffixRoll[] rolled, System.Random random)
        {
            var tier = AffixTable.BestUnlockedTier(itemLevel);
            var affixes = new List<AffixRoll>(legendary.FixedAffixes.Count + 2);
            foreach (var id in legendary.FixedAffixes)
                affixes.Add(new AffixRoll(id, tier, AffixRoller.RollValue(id, tier, random)));
            var extra = 0;
            if (rolled != null)
                foreach (var roll in rolled)
                {
                    if (extra >= 2 || legendary.FixedAffixes.Contains(roll.Id) || !AffixTable.CanRollOn(roll.Id, legendary.Slot))
                        continue;
                    affixes.Add(roll);
                    extra++;
                }
            return affixes.ToArray();
        }

        static bool Contains(this IReadOnlyList<AffixId> list, AffixId id)
        {
            for (var i = 0; i < list.Count; i++)
                if (list[i] == id)
                    return true;
            return false;
        }
    }
}
