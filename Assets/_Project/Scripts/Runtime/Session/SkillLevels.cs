using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Skill points and skill levels (Docs/04, level rewards: a skill point a level to 30, then one every 2 levels; Docs/02's
    /// proposal for levels 1 to 20: a skill starts at 1 when it unlocks, one point raises it one level, never past the
    /// character's level minus its unlock level plus 1, and each level above 1 adds 7 percent of its level 1 damage;
    /// Battle Roar and Blood Frenzy gain 1 percent a level instead; costs and cooldowns stay). Skills are named by their
    /// asset names. Pure, saved with the character. There is no respec until the Trainer exists.
    /// </summary>
    public sealed class SkillLevels
    {
        public const int MaxLevel = 20;

        /// <summary>A damage skill's multiplier grows by this share of its level 1 value each level.</summary>
        public const float DamagePerLevel = 0.07f;

        /// <summary>A buff's bonus grows by this much each level (1 percent).</summary>
        public const float BuffPerLevel = 0.01f;

        readonly Dictionary<string, int> levels = new Dictionary<string, int>();

        public event Action Changed;

        /// <summary>Points a character of this level has earned: one a level from 2 to 30, then one every 2 levels.</summary>
        public static int EarnedPoints(int characterLevel)
        {
            var level = Mathf.Max(1, characterLevel);
            return Mathf.Min(level, 30) - 1 + Mathf.Max(0, (level - 30) / 2);
        }

        /// <summary>The highest a skill may be raised to at this character level.</summary>
        public static int Cap(int unlockLevel, int characterLevel) =>
            Mathf.Clamp(characterLevel - unlockLevel + 1, 1, MaxLevel);

        /// <summary>A skill's damage multiplier at a level.</summary>
        public static float Damage(float levelOneMultiplier, int level) =>
            levelOneMultiplier * (1f + DamagePerLevel * (Mathf.Max(1, level) - 1));

        /// <summary>A buff's bonus at a level (only a bonus the skill has grows).</summary>
        public static float Buff(float levelOneBonus, int level) =>
            levelOneBonus > 0f ? levelOneBonus + BuffPerLevel * (Mathf.Max(1, level) - 1) : 0f;

        /// <summary>A skill's level: 1 until points are spent on it.</summary>
        public int LevelOf(string skill) => skill != null && levels.TryGetValue(skill, out var level) ? level : 1;

        public int Spent
        {
            get
            {
                var spent = 0;
                foreach (var level in levels.Values)
                    spent += level - 1;
                return spent;
            }
        }

        public int Available(int characterLevel) => Mathf.Max(0, EarnedPoints(characterLevel) - Spent);

        public bool CanRaise(string skill, int unlockLevel, int characterLevel) =>
            !string.IsNullOrEmpty(skill) && characterLevel >= unlockLevel && Available(characterLevel) > 0 &&
            LevelOf(skill) < Cap(unlockLevel, characterLevel);

        /// <summary>Spends a point on a skill. False, and nothing changes, when it cannot be raised.</summary>
        public bool Raise(string skill, int unlockLevel, int characterLevel)
        {
            if (!CanRaise(skill, unlockLevel, characterLevel))
                return false;
            levels[skill] = LevelOf(skill) + 1;
            Changed?.Invoke();
            return true;
        }

        public IEnumerable<KeyValuePair<string, int>> All => levels;

        /// <summary>Sets a level from a save.</summary>
        /// <summary>Every skill back to level 1, all points unspent (the Trainer).</summary>
        public void ResetAll()
        {
            levels.Clear();
            Changed?.Invoke();
        }

        public void Restore(string skill, int level)
        {
            if (!string.IsNullOrEmpty(skill) && level > 1)
                levels[skill] = Mathf.Min(level, MaxLevel);
        }
    }
}
