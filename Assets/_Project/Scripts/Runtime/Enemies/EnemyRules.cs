using UnityEngine;

namespace ARPG
{
    /// <summary>Small enemy rules shared by every archetype. Pure.</summary>
    public static class EnemyRules
    {
        /// <summary>
        /// An idle enemy wakes when the player is within its aggro range in a straight line and either in sight or within
        /// this many times that range by a walkable route (2026-10-05: enemies in the next room woke through the wall,
        /// found no route and pressed into it).
        /// </summary>
        public const float RouteAggroFactor = 1.75f;

        /// <summary>Whether an idle enemy wakes: straight-line distance within aggro range and a route short enough
        /// (the route is the straight distance when in sight, infinity when there is none).</summary>
        public static bool Wakes(float distance, float route, float aggroRange) =>
            distance <= aggroRange && route <= aggroRange * RouteAggroFactor;

        /// <summary>A charger lunges at a player it can see between its lunge's minimum range and a unit past the lunge's
        /// length, once its cooldown is over (Docs/05: the Ash Wolf lunges 4 units at a target 3 to 5 away).</summary>
        public static bool ShouldLunge(float distance, float minRange, float lungeDistance, bool lineOfSight, float cooldownLeft) =>
            lungeDistance > 0f && cooldownLeft <= 0f && lineOfSight && distance >= minRange && distance <= lungeDistance + 1f;

        /// <summary>How much harder an enemy hits standing in a support's aura: one aura's bonus, however many reach it
        /// (they do not stack).</summary>
        public static float AuraMultiplier(float bestBonusInReach) => 1f + Mathf.Max(0f, bestBonusInReach);

        /// <summary>A lunge hits the player when it passes within this of her, beyond the two bodies' radii.</summary>
        public const float LungeHitSlack = 0.35f;

        /// <summary>The owner, 2026-10-11: enemies quicker in their attacks, 20 percent, and the ranged ones (archers,
        /// casters) firing half again as often ("make their ranged attacks faster"). They walk at their own speed again
        /// ("enemies do not need to move faster"). Bosses keep their own pace (their fights are timed by hand).</summary>
        public const float QuickerAttack = 1.2f;
        public const float QuickerRanged = 1.5f;

        /// <summary>Their projectiles fly half again as fast and hit 40 percent harder (the owner, 2026-10-11).</summary>
        public const float ProjectileSpeed = 1.5f;
        public const float ProjectileDamage = 1.4f;

        public static float MoveSpeedFactor(EnemyRank rank) => 1f;

        /// <summary>Enemy telegraphs fill twice as fast (the owner, 2026-10-11: attacks "telegraphed too far in advance"):
        /// slam circles, aim lines, casters' circles, lunges, every special and elite attack. Bosses keep their own.</summary>
        public const float TelegraphSpeed = 2f;

        public static float Telegraph(float seconds) => seconds / TelegraphSpeed;

        static bool Ranged(EnemyArchetype archetype) => archetype == EnemyArchetype.Archer || archetype == EnemyArchetype.Caster;

        public static float AttackSpeedFactor(EnemyRank rank, EnemyArchetype archetype) =>
            rank == EnemyRank.Boss ? 1f : Ranged(archetype) ? QuickerRanged : QuickerAttack;

        /// <summary>Wind-ups that paint a telegraph (brute, archer, caster) keep their warning time; the rest quicken.</summary>
        public static float WindupSpeedFactor(EnemyRank rank, EnemyArchetype archetype) =>
            rank == EnemyRank.Boss ? 1f : archetype == EnemyArchetype.Brute || Ranged(archetype) ? TelegraphSpeed : AttackSpeedFactor(rank, archetype);

        public static float ProjectileSpeedFactor(EnemyRank rank) => rank == EnemyRank.Boss ? 1f : ProjectileSpeed;

        public static float ProjectileDamageFactor(EnemyRank rank) => rank == EnemyRank.Boss ? 1f : ProjectileDamage;
    }
}
