using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The Wrathborn's attributes (Docs/02, Attributes; decided 2026-09-27, Q6: they grow by themselves, per level from
    /// level 2, 8 Vitality, 2 Might, 2 Agility and 1 Will; Paragon points, not built, will be the only free choice).
    /// Effects per point from Docs/02's table: Might +0.5 percent melee and area damage and +1 armor; Agility +0.4
    /// percent attack speed, +0.2 percent critical chance, +0.15 percent dodge; Will +0.3 percent cooldown reduction and,
    /// for a Rage class, +0.5 percent Rage gained instead of Focus regeneration (Docs/02's proposal). Vitality's life is
    /// the built stand-in (<see cref="CombatFormulas.CharacterLife"/>); its life regeneration is left out: at 0.1
    /// percent a second per point it would heal 4.8 percent of max life a second by act 1's end (an open question).
    /// The diminishing returns in Docs/02 are not built. Pure.
    /// </summary>
    public readonly struct CharacterAttributes
    {
        public const float DamagePerMight = 0.005f;
        public const float ArmorPerMight = 1f;
        public const float AttackSpeedPerAgility = 0.004f;
        public const float CritChancePerAgility = 0.2f;
        public const float DodgePerAgility = 0.0015f;
        public const float RageGainPerWill = 0.005f;
        public const float CooldownReductionPerWill = 0.003f;

        public readonly int Vitality, Might, Agility, Will;

        CharacterAttributes(int vitality, int might, int agility, int will)
        {
            Vitality = vitality;
            Might = might;
            Agility = agility;
            Will = will;
        }

        /// <summary>A character's attributes at a level: the class's growth from level 2 (Q6).</summary>
        public static CharacterAttributes At(int level)
        {
            var levels = Mathf.Max(0, level - 1);
            return new CharacterAttributes((int)CombatFormulas.VitalityPerLevel * levels, 2 * levels, 2 * levels, levels);
        }

        public float IncreasedDamage => Might * DamagePerMight;
        public float Armor => Might * ArmorPerMight;
        public float AttackSpeed => Agility * AttackSpeedPerAgility;

        /// <summary>In percent, like the gear's.</summary>
        public float CriticalChance => Agility * CritChancePerAgility;
        public float Dodge => Agility * DodgePerAgility;
        public float RageGain => Will * RageGainPerWill;
        public float CooldownReduction => Will * CooldownReductionPerWill;
    }
}
