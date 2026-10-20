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

        /// <summary>How long a woken enemy stands before it chases, attacks or casts (the owner, 2026-10-18: "the enemies
        /// react too quick, give the a reaction-time of 1.5 seconds"): from sight, a hit or its pack's alarm. Enemies
        /// that spawn awake (a boss's adds) and bosses have none.</summary>
        public const float ReactionSeconds = 1.5f;

        public static float ReactionFor(EnemyRank rank) => rank == EnemyRank.Boss ? 0f : ReactionSeconds;

        /// <summary>
        /// Screen-shaped reach (2026-10-20, the owner: enemies at her sides "attacking from the side being way too close
        /// before they are even on screen"; the portrait view is only about 5 units to either side of her, while aggro
        /// and an archer's range are about 7): an enemy starts an attack, a shot, a lunge or a special only while it
        /// stands inside the camera's view, its feet this far in from the sides and the bottom, and its whole body (about
        /// <see cref="ViewMarginTop"/>) in from the top. Out of view a ranged enemy walks in as if it could not see her.
        /// Bosses are exempt (their arenas are built round them). World units.
        /// </summary>
        public const float ViewMarginSide = 0.6f, ViewMarginBottom = 0.4f, ViewMarginTop = 1.4f;

        /// <summary>Whether a point in world space at <paramref name="offset"/> from the view's centre stands inside a
        /// view of half size <paramref name="halfView"/>, by the margins above.</summary>
        public static bool InView(Vector2 offset, Vector2 halfView) =>
            Mathf.Abs(offset.x) <= halfView.x - ViewMarginSide &&
            offset.y <= halfView.y - ViewMarginTop && offset.y >= -halfView.y + ViewMarginBottom;

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
        // 2026-10-17 (the owner, "the game feels too easy": steady pressure): the ranged fire twice as often, hitting softer.
        public const float QuickerRanged = 2f;

        /// <summary>Their projectiles fly half again as fast (the owner, 2026-10-11); they hit 40 percent harder until
        /// 2026-10-17, now at Docs/03's damage, since they come twice as often.</summary>
        public const float ProjectileSpeed = 1.5f;
        public const float ProjectileDamage = 1f;

        /// <summary>The owner, 2026-10-17: "lower the move speed of most enemies by 10%": every enemy but the bosses (their
        /// fights are timed by hand).</summary>
        public const float SlowerWalk = 0.9f;

        public static float MoveSpeedFactor(EnemyRank rank) => rank == EnemyRank.Boss ? 1f : SlowerWalk;

        /// <summary>Enemy telegraphs fill twice as fast (the owner, 2026-10-11: attacks "telegraphed too far in advance"):
        /// slam circles, aim lines, casters' circles, lunges, every special and elite attack. Bosses keep their own.</summary>
        public const float TelegraphSpeed = 2f;

        public static float Telegraph(float seconds) => seconds / TelegraphSpeed;

        /// <summary>Regular enemies' pools lie this long before they start to hurt (the owner, 2026-10-18: "increase the
        /// time it takes for regular enemies pools to start hurting. Increase it by 0.7 sec"): the burning or poisoned
        /// ground left by their specials and casters, not elite affixes or bosses. A hit on landing is not delayed.</summary>
        public const float PoolGraceSeconds = 0.7f;

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
