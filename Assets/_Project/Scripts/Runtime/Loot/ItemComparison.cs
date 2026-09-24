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
        public static string Name(Item item) => $"{item.Rarity} {item.Slot}";

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
                default: return id.ToString();
            }
        }

        public static bool IsPercent(AffixId id) =>
            id == AffixId.IncreasedDamage || id == AffixId.AttackSpeed || id == AffixId.CriticalChance ||
            id == AffixId.CriticalDamage || id == AffixId.CooldownReduction;

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
            if (candidate.Slot == ItemSlot.Weapon)
                lines.Add(new StatLine("Weapon damage", candidate.WeaponAverageDamage,
                    equipped != null ? equipped.WeaponAverageDamage : CombatFormulas.WeaponAverageDamage(0), false));
            else
                lines.Add(new StatLine("Base armor", candidate.ArmorValue, equipped != null ? equipped.ArmorValue : 0f, false));

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
