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
                if (!nav.IsWalkable(IsoMath.GroundToCell(next)))
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
}
