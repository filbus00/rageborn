using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// A life total that can take damage and be healed. Pure, so the same class can serve the player, and be
    /// tested and simulated without a scene.
    /// </summary>
    public sealed class LifePool
    {
        public LifePool(float max)
        {
            Max = Mathf.Max(max, 1e-4f);
            Current = Max;
        }

        public float Max { get; private set; }

        public float Current { get; private set; }

        public bool IsDead => Current <= 0f;

        /// <summary>Current life as a fraction of the maximum, between 0 and 1.</summary>
        public float Fraction => Current / Max;

        /// <summary>Takes damage that has already been through armor. Returns true when this hit killed.</summary>
        public bool TakeDamage(float amount)
        {
            if (IsDead)
                return false;

            Current = Mathf.Max(0f, Current - Mathf.Max(0f, amount));
            return IsDead;
        }

        public void Heal(float amount)
        {
            if (!IsDead)
                Current = Mathf.Min(Max, Current + Mathf.Max(0f, amount));
        }

        /// <summary>Sets life to a fraction of the maximum. Used to carry life across scenes and to revive.</summary>
        public void SetFraction(float fraction) => Current = Mathf.Clamp01(fraction) * Max;

        /// <summary>Changes the maximum, keeping the current fraction (equipping more life does not itself heal or
        /// hurt the character in absolute terms, it scales with the new maximum). Used when gear changes mid-session.</summary>
        public void SetMax(float newMax)
        {
            newMax = Mathf.Max(newMax, 1e-4f);
            var fraction = Fraction;
            Max = newMax;
            Current = Mathf.Clamp01(fraction) * Max;
        }
    }
}
