using System;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Momentum shots (the owner, 2026-10-17, from the mechanics offered: "lets test 5 and 6"): the longer she moves
    /// without stopping, the faster she shoots and the further and faster her arrows fly. The stacks are the old
    /// Momentum of <see cref="StanceStacks"/> (one each 0.6 s on the move, five at most, lost after 1.2 s standing; a dive
    /// keeps them), drawn as the pips under her arc. Claude's numbers, to review. Pure.
    /// </summary>
    public static class MomentumShots
    {
        public const float AttackSpeedPerStack = 0.06f;
        public const float ArrowReachPerStack = 0.04f;

        public static float AttackSpeed(int stacks) => Math.Max(0, stacks) * AttackSpeedPerStack;

        /// <summary>Her arrows' speed and reach multiplier.</summary>
        public static float ArrowFactor(int stacks) => 1f + Math.Max(0, stacks) * ArrowReachPerStack;
    }

    /// <summary>
    /// Cover (the owner, 2026-10-17: "Crouching behind pillars and crates (just standing close) blocks enemy projectiles
    /// from that side and gives a damage bonus to the next shot"): standing within <see cref="Reach"/> of a wall, pillar
    /// or prop, she is covered on that side; an enemy projectile coming from that side breaks on it. After
    /// <see cref="AmbushAfter"/> seconds in cover her next basic arrow is an ambush, <see cref="AmbushDamage"/> times as
    /// hard, and another comes every <see cref="AmbushEvery"/> seconds she stays. Claude's numbers, to review. Pure.
    /// </summary>
    public static class CoverRules
    {
        public const float Reach = 1.05f;
        public const float AmbushAfter = 0.5f;
        public const float AmbushEvery = 2f;
        public const float AmbushDamage = 1.5f;

        /// <summary>How directly a projectile must come from the covered side to break on it (the cosine of its angle).</summary>
        public const float BlockCosine = 0.35f;

        /// <summary>Which way her cover lies (a unit vector, ground space), or zero when there is none near.</summary>
        public static Vector2 Direction(Func<Vector2Int, bool> blocked, Vector2 ground)
        {
            var cell = IsoMath.GroundToCell(ground);
            var sum = Vector2.zero;
            for (var dx = -1; dx <= 1; dx++)
                for (var dy = -1; dy <= 1; dy++)
                {
                    var c = new Vector2Int(cell.x + dx, cell.y + dy);
                    if ((dx == 0 && dy == 0) || !blocked(c))
                        continue;
                    var toward = IsoMath.CellToGround(c) - ground;
                    var distance = toward.magnitude;
                    if (distance > 1e-4f && distance <= Reach)
                        sum += toward / distance;
                }
            return sum.sqrMagnitude > 1e-6f ? sum.normalized : Vector2.zero;
        }

        /// <summary>Whether a projectile flying with this velocity comes from the covered side.</summary>
        public static bool Blocks(Vector2 cover, Vector2 velocity) =>
            cover != Vector2.zero && velocity.sqrMagnitude > 1e-6f && Vector2.Dot(-velocity.normalized, cover) > BlockCosine;

        /// <summary>Whether an ambush is ready after this long in cover, the last one fired at <paramref name="lastAmbush"/>
        /// seconds into it (a negative value when none yet).</summary>
        public static bool AmbushReady(float coverSeconds, float lastAmbush) =>
            coverSeconds >= AmbushAfter && (lastAmbush < 0f || coverSeconds - lastAmbush >= AmbushEvery);
    }
}
