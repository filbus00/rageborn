using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Stillness and Momentum from Docs/01-core-gameplay.md, the tradeoff that movement carries. Standing for 0.4 s
    /// starts Stillness, one stack per 0.4 s up to 5, each 6 percent increased damage and 4 percent damage reduction,
    /// all lost on moving. Moving for 0.6 s starts Momentum, one stack per 0.6 s up to 5, each 5 percent movement
    /// speed and 5 percent chance to dodge, lost after 1.2 s of standing. Pure.
    /// </summary>
    public sealed class StanceStacks
    {
        public const int MaxStacks = 5;
        public const float StillnessStepSeconds = 0.4f;
        public const float MomentumStepSeconds = 0.6f;
        public const float MomentumGraceSeconds = 1.2f;

        public const float StillnessDamagePerStack = 0.06f;
        public const float StillnessReductionPerStack = 0.04f;
        public const float MomentumSpeedPerStack = 0.05f;
        public const float MomentumDodgePerStack = 0.05f;

        float standing;
        float stillnessTimer;
        float momentumTimer;

        /// <summary>The passive tree's changes (Docs/02): Road Runner's faster step, Sure Footed's longer grace,
        /// Tailwind's and Old Wounds' higher caps, and Juggernaut's keeping half on a stop. Defaults are Docs/01's.</summary>
        public float MomentumStep { get; set; } = MomentumStepSeconds;
        public float MomentumGrace { get; set; } = MomentumGraceSeconds;
        public int MomentumCap { get; set; } = MaxStacks;
        public int StillnessCap { get; set; } = MaxStacks;
        public bool KeepHalfOnStop { get; set; }
        bool halved;

        public int Stillness { get; private set; }

        public int Momentum { get; private set; }

        /// <summary>Increased damage, as a fraction to add to the other increased modifiers (0.3 for 30 percent).</summary>
        public float IncreasedDamage => Stillness * StillnessDamagePerStack;

        /// <summary>Share of damage taken that Stillness removes.</summary>
        public float DamageReduction => Stillness * StillnessReductionPerStack;

        public float MoveSpeedMultiplier => 1f + Momentum * MomentumSpeedPerStack;

        public float DodgeChance => Momentum * MomentumDodgePerStack;

        public void Tick(float deltaTime, bool isMoving)
        {
            if (isMoving)
            {
                standing = 0f;
                Stillness = 0;
                stillnessTimer = 0f;

                halved = false;
                momentumTimer += deltaTime;
                while (momentumTimer >= MomentumStep)
                {
                    momentumTimer -= MomentumStep;
                    Momentum = Mathf.Min(MomentumCap, Momentum + 1);
                }
            }
            else
            {
                momentumTimer = 0f;

                standing += deltaTime;
                if (standing >= MomentumGrace && Momentum > 0)
                {
                    // Juggernaut: a stop loses half the stacks, once, rather than all.
                    if (KeepHalfOnStop && !halved)
                    {
                        Momentum /= 2;
                        halved = true;
                    }
                    else if (!KeepHalfOnStop)
                        Momentum = 0;
                }

                stillnessTimer += deltaTime;
                while (stillnessTimer >= StillnessStepSeconds)
                {
                    stillnessTimer -= StillnessStepSeconds;
                    Stillness = Mathf.Min(StillnessCap, Stillness + 1);
                }
            }
        }

        /// <summary>Stacks from a skill (Bull Rush gives one per enemy it hits), capped at <see cref="MaxStacks"/>.</summary>
        public void AddMomentum(int stacks) => Momentum = Mathf.Clamp(Momentum + stacks, 0, MomentumCap);
    }
}
