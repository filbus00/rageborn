using System.Collections.Generic;

namespace ARPG
{
    /// <summary>One stat of an item next to the same stat of the item it would replace.</summary>
    public readonly struct StatLine
    {
        public StatLine(string label, float candidate, float equipped, bool percent)
        {
            Label = label;
            Candidate = candidate;
            Equipped = equipped;
            Percent = percent;
        }

        public string Label { get; }
        public float Candidate { get; }
        public float Equipped { get; }

        /// <summary>Shown with a percent sign.</summary>
        public bool Percent { get; }

        /// <summary>Every stat so far is better when higher, so this is simply the sign of the difference.</summary>
        public int Direction => Candidate > Equipped + 0.0001f ? 1 : Candidate < Equipped - 0.0001f ? -1 : 0;
    }

    /// <summary>
    /// The words and numbers of an item tooltip (Docs/03-itemization.md, item tooltips): its name, its base stat and
    /// affixes, and the comparison strip against the equipped item. Pure, so the layout code only places text.
    /// </summary>
    public static class ItemComparison
    {
        /// <summary>A named legendary's name, else its rarity and kind ("Magic Bow").</summary>
        public static string Name(Item item) => item.LegendaryDefinition != null ? item.LegendaryDefinition.Name : $"{item.Rarity} {KindName(item.Slot)}";

        /// <summary>An item kind's name for players: bows only (Docs/03, 2026-09-30), so Weapon is a short bow, the
        /// two-handed kind a longbow and the shield kind a quiver.</summary>
        public static string KindName(ItemSlot kind) => kind switch
        {
            ItemSlot.Weapon => "Bow",
            ItemSlot.TwoHandWeapon => "Longbow",
            ItemSlot.Shield => "Quiver",
            ItemSlot.Ring2 => "Ring",
            ItemSlot.OffHand => "Off-hand",
            _ => kind.ToString(),
        };

        public static string AffixLabel(AffixId id)
        {
            switch (id)
            {
                case AffixId.FlatWeaponDamage: return "Flat damage";
                case AffixId.IncreasedDamage: return "Increased damage";
                case AffixId.Life: return "Life";
                case AffixId.Armor: return "Armor";
                case AffixId.AttackSpeed: return "Attack speed";
                case AffixId.CriticalChance: return "Critical chance";
                case AffixId.CriticalDamage: return "Critical damage";
                case AffixId.LifeOnHit: return "Life on hit";
                case AffixId.CooldownReduction: return "Cooldown reduction";
                case AffixId.MovementSpeed: return "Movement speed";
                case AffixId.DodgeChance: return "Dodge chance";
                case AffixId.ExtraArrowChance: return "Extra arrow chance";
                case AffixId.IgniteChance: return "Ignite chance";
                case AffixId.ChillChance: return "Chill chance";
                case AffixId.ExtraPierce: return "Pierce on every arrow";
                case AffixId.ForkChance: return "Fork chance";
                case AffixId.BurnDamage: return "Burn damage";
                case AffixId.DamagePerPierce: return "Damage per enemy pierced";
                case AffixId.FarDamage: return "Damage beyond 6 units";
                case AffixId.NearDamage: return "Damage within 3 units";
                case AffixId.ArrowSpeed: return "Arrow speed";
                case AffixId.PetDamage: return "Pet damage";
                case AffixId.PetLife: return "Pet life";
                case AffixId.ExtraSkillArrow: return "Arrows to Split and Homing Arrow";
                default: return id.ToString();
            }
        }

        public static bool IsPercent(AffixId id) =>
            id == AffixId.IncreasedDamage || id == AffixId.AttackSpeed || id == AffixId.CriticalChance ||
            id == AffixId.CriticalDamage || id == AffixId.CooldownReduction || id == AffixId.MovementSpeed ||
            id == AffixId.DodgeChance || id == AffixId.ExtraArrowChance || id == AffixId.IgniteChance || id == AffixId.ChillChance ||
            id == AffixId.ForkChance || id == AffixId.BurnDamage || id == AffixId.DamagePerPierce || id == AffixId.FarDamage ||
            id == AffixId.NearDamage || id == AffixId.ArrowSpeed || id == AffixId.PetDamage || id == AffixId.PetLife;

        /// <summary>The docs' tier dots: five dots, filled for how good the tier is (T1 all five, T5 one).</summary>
        public static string TierDots(int tier)
        {
            var filled = UnityEngine.Mathf.Clamp(AffixTable.WorstTier + 1 - tier, 1, AffixTable.WorstTier);
            return new string('●', filled) + new string('○', AffixTable.WorstTier - filled);
        }

        public static string FormatValue(float value, bool percent) => percent ? $"{value:0.#}%" : $"{value:0.#}";

        /// <summary>
        /// The comparison strip: the base stat, then every affix stat either item has, each with the candidate's and the
        /// equipped item's value (0 where an item lacks it). <paramref name="equipped"/> may be null for an empty slot.
        /// </summary>
        public static List<StatLine> Lines(Item candidate, Item equipped)
        {
            var lines = new List<StatLine>();
            if (candidate.IsWeapon)
                lines.Add(new StatLine("Weapon damage", candidate.WeaponAverageDamage,
                    equipped != null ? equipped.WeaponAverageDamage : CombatFormulas.WeaponAverageDamage(0), false));
            else if (Item.ArmorShare(candidate.Slot) > 0f)
                lines.Add(new StatLine("Base armor", candidate.ArmorValue, equipped != null ? equipped.ArmorValue : 0f, false));
            if (candidate.Slot == ItemSlot.Shield)
                lines.Add(new StatLine("Quiver attack speed", candidate.QuiverAttackSpeedPercent, equipped != null ? equipped.QuiverAttackSpeedPercent : 0f, true));
            // Amulets and rings have no base stat: only their affixes.

            foreach (AffixId id in System.Enum.GetValues(typeof(AffixId)))
            {
                var mine = candidate.AffixSum(id);
                var theirs = equipped != null ? equipped.AffixSum(id) : 0f;
                if (mine != 0f || theirs != 0f)
                    lines.Add(new StatLine(AffixLabel(id), mine, theirs, IsPercent(id)));
            }
            return lines;
        }
    }
}
