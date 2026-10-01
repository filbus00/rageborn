using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Burn (Docs/03, decided 2026-10-01): fire damage over time, up to <see cref="MaxStacks"/> stacks that each burn on
    /// their own clock, a new one replacing the stack closest to running out when all are taken. Each stack's damage a
    /// second was worked out by the hit formula when it was applied. Ticked by its enemy, which deals what
    /// <see cref="Tick"/> returns. Pure; allocates nothing.
    /// </summary>
    public sealed class BurnStacks
    {
        public const int MaxStacks = 3;
        public const float Seconds = 3f;

        readonly float[] perSecond = new float[MaxStacks];
        readonly float[] left = new float[MaxStacks];

        public int Count
        {
            get
            {
                var count = 0;
                for (var i = 0; i < MaxStacks; i++)
                    if (left[i] > 0f)
                        count++;
                return count;
            }
        }

        public bool IsBurning => Count > 0;

        public void Apply(float damagePerSecond, float seconds = Seconds)
        {
            var slot = 0;
            for (var i = 1; i < MaxStacks; i++)
                if (left[i] < left[slot])
                    slot = i;
            perSecond[slot] = damagePerSecond;
            left[slot] = seconds;
        }

        /// <summary>Advances every stack; returns the damage dealt over the step.</summary>
        public float Tick(float deltaTime)
        {
            var damage = 0f;
            for (var i = 0; i < MaxStacks; i++)
            {
                if (left[i] <= 0f)
                    continue;
                var step = Mathf.Min(deltaTime, left[i]);
                damage += perSecond[i] * step;
                left[i] -= deltaTime;
            }
            return damage;
        }

        public void Clear()
        {
            for (var i = 0; i < MaxStacks; i++)
                left[i] = 0f;
        }
    }

    /// <summary>
    /// Chill (Docs/03, decided 2026-10-01): each chill slows the enemy by <see cref="SlowFraction"/> for
    /// <see cref="SlowSeconds"/>, and the <see cref="ChillsToFreeze"/>th chill within <see cref="WindowSeconds"/> freezes it
    /// for <see cref="FreezeSeconds"/> (not a boss: the caller says whether it can freeze). A frozen enemy neither moves
    /// nor attacks. Pure.
    /// </summary>
    public sealed class ChillMeter
    {
        public const float SlowFraction = 0.25f;
        public const float SlowSeconds = 2f;
        public const int ChillsToFreeze = 4;
        public const float WindowSeconds = 2f;
        public const float FreezeSeconds = 1f;

        int count;
        float window;

        public float FrozenLeft { get; private set; }

        public bool IsFrozen => FrozenLeft > 0f;

        /// <summary>Counts a chill; returns true when it froze the enemy.</summary>
        public bool Apply(bool canFreeze)
        {
            if (window <= 0f)
                count = 0;
            count++;
            window = WindowSeconds;
            if (!canFreeze || count < ChillsToFreeze || IsFrozen)
                return false;
            count = 0;
            FrozenLeft = FreezeSeconds;
            return true;
        }

        public void Tick(float deltaTime)
        {
            window = Mathf.Max(0f, window - deltaTime);
            FrozenLeft = Mathf.Max(0f, FrozenLeft - deltaTime);
        }

        public void Clear()
        {
            count = 0;
            window = 0f;
            FrozenLeft = 0f;
        }
    }
}
