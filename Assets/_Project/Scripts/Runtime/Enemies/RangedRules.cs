using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Where an archer wants to be (Docs/01-core-gameplay.md: keeps distance, counterplay is closing in or breaking line
    /// of sight). Pure.
    /// </summary>
    public static class ArcherSteering
    {
        /// <summary>How far inside its preferred range the player can come before the archer backs off. Without the band
        /// it would twitch back and forth at the exact distance. Tuning value.</summary>
        public const float Band = 0.75f;

        /// <summary>+1 to close in (it cannot see the player, or the player is out of range), -1 to back away (the
        /// player is too close), 0 to hold its spot.</summary>
        public static int Intent(float distance, bool lineOfSight, float preferredRange, float attackRange)
        {
            if (!lineOfSight || distance > attackRange)
                return 1;
            if (distance < preferredRange - Band)
                return -1;
            return 0;
        }
    }

    /// <summary>
    /// One enemy projectile's flight, a frame at a time: it moves in short pieces (a wall is one cell, 0.707 across, so
    /// a long frame must not jump it), stops on the first cell that is not walkable, hits the player when it comes within
    /// the hit radius, and is spent after its range. Pure.
    /// </summary>
    public static class EnemyProjectileFlight
    {
        public const float MaxStep = 0.25f;

        public enum Outcome
        {
            Flying,
            HitPlayer,
            Blocked,
            Spent,
        }

        public static Outcome Step(ref Vector2 position, ref float travelled, Vector2 velocity, float deltaTime,
            float maxDistance, NavGrid nav, Vector2 player, float hitRadius)
        {
            var step = velocity * deltaTime;
            var length = step.magnitude;
            if (length <= 0f)
                return Outcome.Flying;

            var pieces = Mathf.CeilToInt(length / MaxStep);
            var piece = step / pieces;
            var pieceLength = length / pieces;
            for (var i = 0; i < pieces; i++)
            {
                var next = position + piece;
                // Over a rim (a low cell), not through a wall.
                if (!nav.IsOpenToShots(IsoMath.GroundToCell(next)))
                    return Outcome.Blocked;

                position = next;
                travelled += pieceLength;
                if ((player - position).sqrMagnitude <= hitRadius * hitRadius)
                    return Outcome.HitPlayer;
                if (travelled >= maxDistance)
                    return Outcome.Spent;
            }
            return Outcome.Flying;
        }
    }

    /// <summary>The kinds of enemy projectile (the owner, 2026-10-17: "add more ranged attacks, and not in the way that
    /// they spawn damaging pools under your feet ... add thrown axes, knifes, fireballs, exploding projectiles, bouncing
    /// projectiles").</summary>
    public enum ProjectileKind
    {
        /// <summary>An arrow or bolt streak.</summary>
        Plain,
        /// <summary>A small fast blade.</summary>
        Knife,
        /// <summary>A spinning hand axe: slower, harder.</summary>
        Axe,
        /// <summary>A ball of fire that bursts where it stops (a wall, or the end of its flight).</summary>
        Fireball,
        /// <summary>A bomb thrown to where she stood, which blows up after a short fuse shown on the ground.</summary>
        Bomb,
        /// <summary>A spinning blade that bounces off walls.</summary>
        Bouncing,
    }

    /// <summary>The kinds' numbers and the bounce off a wall. Claude's numbers, to review. Pure.</summary>
    public static class ProjectileKinds
    {
        public const float FireballBurstRadius = 1.1f;
        public const float FireballBurstFill = 0.35f;
        public const float FireballBurstShare = 0.6f;
        public const float BombRadius = 1.5f;
        public const float BombFuse = 0.7f;
        public const int Bounces = 3;
        public const float BouncingReach = 16f;
        public const float AxeSpinDegrees = 900f;
        public const float BladeSpinDegrees = 1400f;

        /// <summary>Whether it is drawn spinning.</summary>
        public static bool Spins(ProjectileKind kind) => kind == ProjectileKind.Axe || kind == ProjectileKind.Bouncing || kind == ProjectileKind.Bomb;

        /// <summary>
        /// The velocity after striking a wall from <paramref name="position"/> (the last open point): the side that is
        /// blocked turns back, both in a corner.
        /// </summary>
        public static Vector2 Bounce(Vector2 position, Vector2 velocity, System.Func<Vector2Int, bool> walkable)
        {
            var step = velocity.normalized * EnemyProjectileFlight.MaxStep;
            var blockedX = !walkable(IsoMath.GroundToCell(position + new Vector2(step.x, 0f)));
            var blockedY = !walkable(IsoMath.GroundToCell(position + new Vector2(0f, step.y)));
            if (!blockedX && !blockedY)
                return -velocity;
            return new Vector2(blockedX ? -velocity.x : velocity.x, blockedY ? -velocity.y : velocity.y);
        }
    }
}
