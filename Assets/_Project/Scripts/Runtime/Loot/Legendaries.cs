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
    /// anywhere, and twice as often from its home). The first eleven of the proposed 24, one or more per archetype; their
    /// fixed affixes come from the built pool until the rest of it exists (Docs/03 lists the intended ones). All numbers
    /// are tuning. Pure.
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

        static readonly LegendaryDefinition[] Table =
        {
            null,
            new LegendaryDefinition(LegendaryId.Splinterbough, "Splinterbough", ItemSlot.Weapon,
                new[] { AffixId.FlatWeaponDamage, AffixId.CriticalChance, AffixId.AttackSpeed },
                "Split Arrow's arrows fork on their first hit", LootSource.Elite, "Elites", "Splinterstorm"),
            new LegendaryDefinition(LegendaryId.QuiverOfEndlessSplinters, "Quiver of Endless Splinters", ItemSlot.Shield,
                new[] { AffixId.FlatWeaponDamage, AffixId.ExtraArrowChance, AffixId.AttackSpeed },
                "Every basic arrow forks on its first hit", LootSource.NormalEnemy, "Any enemy", "Splinterstorm"),
            new LegendaryDefinition(LegendaryId.Gallowsreach, "Gallowsreach", ItemSlot.TwoHandWeapon,
                new[] { AffixId.FlatWeaponDamage, AffixId.IncreasedDamage, AffixId.CriticalDamage },
                "Pierce Arrow gains 20% damage for each enemy it passes through, and its blood trail lies on the floor for 4 s, making enemies who walk through it bleed",
                LootSource.Boss, "The Cinder Warden", "Blood Hunter"),
            new LegendaryDefinition(LegendaryId.EmberTongue, "Ember-Tongue", ItemSlot.Weapon,
                new[] { AffixId.FlatWeaponDamage, AffixId.IncreasedDamage, AffixId.IgniteChance },
                "Basic arrows always ignite; Explosive Arrow deals 60% more to burning enemies", LootSource.Boss, "The Cinder Warden", "Pyre"),
            new LegendaryDefinition(LegendaryId.AshfallQuiver, "Ashfall Quiver", ItemSlot.Shield,
                new[] { AffixId.FlatWeaponDamage, AffixId.IgniteChance, AffixId.ExtraArrowChance },
                "Every fifth basic arrow is a free Explosive Arrow at 60%", LootSource.ZoneChest, "Chests", "Pyre"),
            new LegendaryDefinition(LegendaryId.HuntersPromise, "Hunter's Promise", ItemSlot.Weapon,
                new[] { AffixId.FlatWeaponDamage, AffixId.IncreasedDamage, AffixId.CriticalChance },
                "Homing Arrow looses 2 more arrows, and an arrow whose target dies seeks a new one once", LootSource.Elite, "Elites", "Seeker"),
            new LegendaryDefinition(LegendaryId.StillwaterYew, "Stillwater Yew", ItemSlot.TwoHandWeapon,
                new[] { AffixId.FlatWeaponDamage, AffixId.CriticalDamage, AffixId.CriticalChance },
                "At full Stillness the next basic arrow is a certain crit that pierces every enemy", LootSource.ZoneChest, "Chests", "Sniper"),
            new LegendaryDefinition(LegendaryId.WindSwornQuiver, "Wind-Sworn Quiver", ItemSlot.Shield,
                new[] { AffixId.Life, AffixId.AttackSpeed, AffixId.CriticalChance },
                "While Momentum is at its cap, every arrow pierces 2 more enemies", LootSource.Elite, "Elites", "Windrunner"),
            new LegendaryDefinition(LegendaryId.Galeheart, "Galeheart", ItemSlot.Weapon,
                new[] { AffixId.FlatWeaponDamage, AffixId.AttackSpeed, AffixId.CriticalDamage },
                "At full Momentum every basic shot looses 2 more arrows in a 20 degree fan", LootSource.ZoneChest, "Chests", "Windrunner"),
            new LegendaryDefinition(LegendaryId.MagpiesNest, "Magpie's Nest", ItemSlot.Shield,
                new[] { AffixId.FlatWeaponDamage, AffixId.Life, AffixId.ExtraArrowChance },
                "Arrows ricochet off walls once toward the nearest enemy; a ricochet deals 30% more", LootSource.ZoneChest, "Chests", "Splinterstorm, Seeker"),
            new LegendaryDefinition(LegendaryId.PackLeadersSignet, "Pack Leader's Signet", ItemSlot.Ring,
                new[] { AffixId.CriticalChance, AffixId.CriticalDamage, AffixId.FlatWeaponDamage },
                "The pet bites with your attack speed and critical chance; its kills give 5 Focus", LootSource.Champion, "Champions", "Beastmaster"),
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
