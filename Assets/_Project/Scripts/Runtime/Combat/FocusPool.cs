using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The Focus resource from Docs/01-core-gameplay.md, the Wild Arrow's (Docs/02-classes-and-skills.md): 0 to 100,
    /// full on every level load, regenerates 6 a second and gains 4 on each basic arrow that hits. Skills spend it.
    /// Pure, so it can be unit tested and simulated.
    /// </summary>
    public sealed class FocusPool
    {
        public const float DefaultMax = 100f;
        public const float DefaultRegenPerSecond = 6f;
        public const float PerBasicHit = 4f;

        // "In combat" is a hit dealt or taken within this many seconds (Docs/02's test for combat-only triggers).
        public const float CombatSeconds = 3f;

        readonly float baseMax;
        readonly float regenPerSecond;
        float sinceCombat = CombatSeconds + 1f;

        public FocusPool(float max = DefaultMax, float regenPerSecond = DefaultRegenPerSecond, float start = DefaultMax)
        {
            baseMax = max;
            this.regenPerSecond = regenPerSecond;
            Current = Mathf.Clamp(start, 0f, max);
        }

        /// <summary>The pool's size: its base and what the Focus attribute adds (<see cref="BonusMax"/>).</summary>
        public float Max => baseMax + BonusMax;

        /// <summary>More room in the pool, from the Focus attribute. Lowering it trims what is held.</summary>
        public float BonusMax
        {
            get => bonusMax;
            set
            {
                bonusMax = Mathf.Max(0f, value);
                Current = Mathf.Min(Current, Max);
            }
        }

        float bonusMax;

        public float Current { get; private set; }

        public float Fraction => Max > 0f ? Current / Max : 0f;

        /// <summary>Multiplies the regeneration (the Focus attribute adds to it).</summary>
        public float RegenMultiplier { get; set; } = 1f;

        /// <summary>Multiplies every gain from hits.</summary>
        public float GainMultiplier { get; set; } = 1f;

        /// <summary>Whether a hit was dealt or taken within <see cref="CombatSeconds"/>.</summary>
        public bool InCombat => sinceCombat <= CombatSeconds;

        public void Tick(float deltaTime)
        {
            sinceCombat += deltaTime;
            Add(regenPerSecond * RegenMultiplier * deltaTime);
        }

        /// <summary>Adds Focus from a hit and counts as combat.</summary>
        public void Gain(float amount)
        {
            Add(amount * GainMultiplier);
            sinceCombat = 0f;
        }

        /// <summary>Combat that gives no Focus of its own (a skill hit, a hit taken).</summary>
        public void MarkCombat() => sinceCombat = 0f;

        /// <summary>Fills the pool (a level load, after the attribute has set its size).</summary>
        public void Fill() => Current = Max;

        void Add(float amount) => Current = Mathf.Clamp(Current + amount, 0f, Max);

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
