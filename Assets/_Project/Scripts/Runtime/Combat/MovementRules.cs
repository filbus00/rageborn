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

}
