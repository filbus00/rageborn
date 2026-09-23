using System;

namespace ARPG
{
    /// <summary>
    /// The XP curves from Docs/04-progression-and-economy.md. Pure.
    /// </summary>
    public static class Experience
    {
        public const int MaxLevel = 60;

        /// <summary>Enemies within this many levels of the character give full XP.</summary>
        public const int FullXpLevelBand = 3;

        public const float PenaltyPerLevelBelow = 0.12f;
        public const float BonusPerLevelAbove = 0.08f;
        public const float MaxLevelDifferenceMultiplier = 1.5f;

        /// <summary>Elites give 8 times normal XP (the docs).</summary>
        public const float EliteXpMultiplier = 8f;

        /// <summary>A Champion gives more than a normal enemy (the user's decision, 2026-09-23); 3 times is a tuning
        /// value, between normal and elite, the same as its gold.</summary>
        public const float ChampionXpMultiplier = 3f;

        /// <summary>XP needed to go from <paramref name="level"/> to the next: 400 times level to the power 2.2,
        /// rounded.</summary>
        public static int XpToNextLevel(int level)
        {
            if (level < 1)
                level = 1;
            return (int)Math.Round(400.0 * Math.Pow(level, 2.2));
        }

        /// <summary>XP a normal enemy of this level is worth before the level difference: 8 times level to the power 1.5.</summary>
        public static float EnemyBaseXp(int enemyLevel) => 8f * (float)Math.Pow(Math.Max(1, enemyLevel), 1.5);

        /// <summary>
        /// Full XP within 3 levels either way. Beyond that, 12 percent less per level the enemy is further below the
        /// character (down to nothing) and 8 percent more per level it is further above (up to 150 percent). The steps
        /// count from the edge of the band (the user's decision, 2026-09-23), so an enemy 4 levels below gives 88
        /// percent, not 52.
        /// </summary>
        public static float LevelDifferenceMultiplier(int characterLevel, int enemyLevel)
        {
            var difference = enemyLevel - characterLevel;
            if (difference < -FullXpLevelBand)
                return Math.Max(0f, 1f - PenaltyPerLevelBelow * (-difference - FullXpLevelBand));
            if (difference > FullXpLevelBand)
                return Math.Min(MaxLevelDifferenceMultiplier, 1f + BonusPerLevelAbove * (difference - FullXpLevelBand));
            return 1f;
        }

        public static float RankMultiplier(EnemyRank rank)
        {
            switch (rank)
            {
                case EnemyRank.Elite: return EliteXpMultiplier;
                case EnemyRank.Champion: return ChampionXpMultiplier;
                default: return 1f;
            }
        }

        /// <summary>The XP a kill gives, rounded.</summary>
        public static int KillXp(int characterLevel, int enemyLevel, EnemyRank rank) =>
            (int)Math.Round(EnemyBaseXp(enemyLevel) * RankMultiplier(rank) * LevelDifferenceMultiplier(characterLevel, enemyLevel));
    }

    /// <summary>
    /// The character's level and the XP it has toward the next one. At the level cap XP stops counting; the docs send
    /// it to Paragon levels, which are not built yet.
    /// </summary>
    public sealed class CharacterProgress
    {
        public CharacterProgress(int level = 1, int xp = 0)
        {
            Level = Math.Max(1, Math.Min(Experience.MaxLevel, level));
            Xp = IsMaxLevel ? 0 : Math.Max(0, Math.Min(xp, XpToNextLevel - 1));
        }

        public int Level { get; private set; }

        /// <summary>XP gained since reaching the current level.</summary>
        public int Xp { get; private set; }

        public bool IsMaxLevel => Level >= Experience.MaxLevel;

        public int XpToNextLevel => Experience.XpToNextLevel(Level);

        /// <summary>How far through the current level, 0 to 1. Full at the level cap.</summary>
        public float Fraction => IsMaxLevel ? 1f : (float)Xp / XpToNextLevel;

        /// <summary>Adds XP, carrying the remainder over each level gained. Returns how many levels were gained.</summary>
        public int Add(int amount)
        {
            if (amount <= 0 || IsMaxLevel)
                return 0;

            var gained = 0;
            var xp = (long)Xp + amount;
            while (!IsMaxLevel && xp >= XpToNextLevel)
            {
                xp -= XpToNextLevel;
                Level++;
                gained++;
            }
            Xp = IsMaxLevel ? 0 : (int)xp;
            return gained;
        }
    }
}
