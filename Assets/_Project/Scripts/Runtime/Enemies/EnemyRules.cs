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

        /// <summary>The owner, 2026-10-11: enemies "quicker", noticeably: 25 percent faster on their feet, 20 percent
        /// quicker attacks. Bosses keep their own pace (their fights are timed by hand).</summary>
        public const float QuickerMove = 1.25f;
        public const float QuickerAttack = 1.2f;

        public static float MoveSpeedFactor(EnemyRank rank) => rank == EnemyRank.Boss ? 1f : QuickerMove;

        public static float AttackSpeedFactor(EnemyRank rank) => rank == EnemyRank.Boss ? 1f : QuickerAttack;

        /// <summary>Wind-ups that paint a telegraph (brute, archer, caster) keep their warning time; the rest quicken.</summary>
        public static float WindupSpeedFactor(EnemyRank rank, EnemyArchetype archetype) =>
            archetype == EnemyArchetype.Brute || archetype == EnemyArchetype.Archer || archetype == EnemyArchetype.Caster ? 1f : AttackSpeedFactor(rank);
    }
}
