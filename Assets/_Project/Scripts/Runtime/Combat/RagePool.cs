using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The Wrathborn's Rage (Docs/02-classes-and-skills.md): 0 to 100, starts empty, gained by hitting (6 per basic
    /// swing that lands) and by being hit (3 per hit), spent by skills. After 3 seconds without dealing or taking a hit
    /// it drains 5 per second. Pure.
    /// </summary>
    public sealed class RagePool
    {
        public const float DefaultMax = 100f;
        public const float PerBasicHit = 6f;
        public const float PerHitTaken = 3f;
        public const float DrainDelaySeconds = 3f;
        public const float DrainPerSecond = 5f;

        float sinceCombat;

        public RagePool(float max = DefaultMax, float start = 0f)
        {
            Max = max;
            Current = Mathf.Clamp(start, 0f, max);
            sinceCombat = DrainDelaySeconds;
        }

        public float Max { get; }

        public float Current { get; private set; }

        public float Fraction => Max > 0f ? Current / Max : 0f;

        /// <summary>Adds Rage and counts as combat, which holds off the drain.</summary>
        public void Gain(float amount)
        {
            Current = Mathf.Clamp(Current + amount, 0f, Max);
            sinceCombat = 0f;
        }

        /// <summary>Whether a hit was dealt or taken within the drain delay: "in combat" for Blood Frenzy's trigger (the
        /// same test as the drain, Docs/02's proposal).</summary>
        public bool InCombat => sinceCombat <= DrainDelaySeconds;

        /// <summary>Combat that gives no Rage of its own (a skill hit) still holds off the drain.</summary>
        public void MarkCombat() => sinceCombat = 0f;

        public void Tick(float deltaTime)
        {
            sinceCombat += deltaTime;
            if (sinceCombat > DrainDelaySeconds)
                Current = Mathf.Max(0f, Current - DrainPerSecond * deltaTime);
        }

        /// <summary>Spends the cost if the pool holds it. Returns false and changes nothing otherwise.</summary>
        public bool TrySpend(float cost)
        {
            if (cost > Current)
                return false;

            Current -= cost;
            return true;
        }
    }
}
