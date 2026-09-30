using System;
using UnityEngine;

namespace ARPG
{
    /// <summary>The Wild Arrow's five attributes (Docs/02, decided 2026-09-30), in the order they are saved and shown.
    /// New ones go at the end.</summary>
    public enum Attribute
    {
        Strength,
        Agility,
        Vitality,
        Speed,
        Focus,
    }

    /// <summary>
    /// The stat points a character has spent (Docs/02, decided 2026-09-30: "No skill trees, add stat points to spend on
    /// attributes"): 5 points a level from level 2, spent one at a time on any attribute, never refunded (no respec
    /// for now). Unspent points are not stored; they follow from the level. Lives in <see cref="GameSession"/> and is
    /// saved. Pure.
    /// </summary>
    public sealed class AttributePoints
    {
        public const int PointsPerLevel = 5;

        static readonly int Count = Enum.GetValues(typeof(Attribute)).Length;

        readonly int[] spent = new int[Count];

        public event Action Changed;

        /// <summary>Points earned by a level: 5 a level from 2, so 295 at 60.</summary>
        public static int Earned(int level) => PointsPerLevel * Mathf.Max(0, level - 1);

        public int Spent(Attribute attribute) => spent[(int)attribute];

        public int TotalSpent
        {
            get
            {
                var total = 0;
                for (var i = 0; i < spent.Length; i++)
                    total += spent[i];
                return total;
            }
        }

        public int Available(int level) => Mathf.Max(0, Earned(level) - TotalSpent);

        /// <summary>Spends one point on an attribute if one is free at this level.</summary>
        public bool Spend(Attribute attribute, int level)
        {
            if (Available(level) <= 0)
                return false;
            spent[(int)attribute]++;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Spent points by attribute, in <see cref="Attribute"/> order, for the save.</summary>
        public int[] ToArray() => (int[])spent.Clone();

        /// <summary>Loads saved points; a missing or negative entry counts as none.</summary>
        public void Restore(System.Collections.Generic.IReadOnlyList<int> saved)
        {
            for (var i = 0; i < spent.Length; i++)
                spent[i] = saved != null && i < saved.Count ? Mathf.Max(0, saved[i]) : 0;
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// What the spent points give (Docs/02, the Wild Arrow's attributes, the owner's split of 2026-09-30, numbers
    /// proposed): Strength +0.5 percent damage on every arrow and +1 armor; Agility +0.2 percent critical chance, +0.2
    /// percent dodge and +1 percent critical damage; Vitality +8 life; Speed +0.4 percent attack speed and +0.2 percent
    /// move speed; Focus +0.5 percent Focus regeneration, +0.3 percent cooldown reduction and +1 max Focus. Every
    /// attribute shows as 10 plus the points spent, and only the spent points give bonuses (the 10 are the class's base).
    /// Vitality's life comes on top of the class's own life by level (<see cref="CombatFormulas.CharacterLife"/>), so a
    /// character who never raises it keeps the life the balance was built on. Dodge stays capped at 50 percent where it
    /// is applied. Diminishing returns are not built. Pure.
    /// </summary>
    public readonly struct CharacterAttributes
    {
        public const int StartingValue = 10;

        public const float DamagePerStrength = 0.005f;
        public const float ArmorPerStrength = 1f;
        public const float CritChancePerAgility = 0.2f;
        public const float DodgePerAgility = 0.002f;
        public const float CritDamagePerAgility = 1f;
        public const float LifePerVitality = 8f;
        public const float AttackSpeedPerSpeed = 0.004f;
        public const float MoveSpeedPerSpeed = 0.002f;
        public const float FocusRegenPerFocus = 0.005f;
        public const float CooldownReductionPerFocus = 0.003f;
        public const float MaxFocusPerFocus = 1f;

        /// <summary>Points spent on each.</summary>
        public readonly int Strength, Agility, Vitality, Speed, Focus;

        public CharacterAttributes(int strength, int agility, int vitality, int speed, int focus)
        {
            Strength = strength;
            Agility = agility;
            Vitality = vitality;
            Speed = speed;
            Focus = focus;
        }

        /// <summary>A character's own points.</summary>
        public static CharacterAttributes Of(AttributePoints points) => points == null
            ? default
            : new CharacterAttributes(points.Spent(Attribute.Strength), points.Spent(Attribute.Agility),
                points.Spent(Attribute.Vitality), points.Spent(Attribute.Speed), points.Spent(Attribute.Focus));

        /// <summary>
        /// A typical build at a level, one point of every five in each attribute: what the balance report models and
        /// what the power score assumes when it is not given a character's own points.
        /// </summary>
        public static CharacterAttributes At(int level)
        {
            var each = Mathf.Max(0, level - 1);
            return new CharacterAttributes(each, each, each, each, each);
        }

        /// <summary>The value shown for an attribute: the base 10 plus the points spent.</summary>
        public int Shown(Attribute attribute) => StartingValue + PointsIn(attribute);

        public int PointsIn(Attribute attribute) => attribute switch
        {
            Attribute.Strength => Strength,
            Attribute.Agility => Agility,
            Attribute.Vitality => Vitality,
            Attribute.Speed => Speed,
            _ => Focus,
        };

        public float IncreasedDamage => Strength * DamagePerStrength;
        public float Armor => Strength * ArmorPerStrength;

        /// <summary>In percent, like the gear's.</summary>
        public float CriticalChance => Agility * CritChancePerAgility;

        /// <summary>In percent, like the gear's.</summary>
        public float CriticalDamage => Agility * CritDamagePerAgility;
        public float Dodge => Agility * DodgePerAgility;
        public float Life => Vitality * LifePerVitality;
        public float AttackSpeed => Speed * AttackSpeedPerSpeed;
        public float MoveSpeed => Speed * MoveSpeedPerSpeed;
        public float FocusRegen => Focus * FocusRegenPerFocus;
        public float CooldownReduction => Focus * CooldownReductionPerFocus;
        public float MaxFocus => Focus * MaxFocusPerFocus;

        /// <summary>A one-line summary of what an attribute gives per point, for the stats page.</summary>
        public static string Describe(Attribute attribute) => attribute switch
        {
            Attribute.Strength => "+0.5% arrow damage, +1 armor",
            Attribute.Agility => "+0.2% crit, +0.2% dodge, +1% crit damage",
            Attribute.Vitality => "+8 life",
            Attribute.Speed => "+0.4% attack speed, +0.2% move speed",
            _ => "+0.5% Focus regen, +0.3% cooldowns, +1 max Focus",
        };
    }
}
