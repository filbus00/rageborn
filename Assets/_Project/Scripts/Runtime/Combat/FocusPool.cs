using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The Focus resource from Docs/01-core-gameplay.md: 0 to 100, regenerates over time and is gained on basic
    /// attack hits. Skills spend it. Pure, so it can be unit tested and simulated.
    /// </summary>
    public sealed class FocusPool
    {
        readonly float regenPerSecond;

        public FocusPool(float max, float regenPerSecond, float start)
        {
            Max = max;
            this.regenPerSecond = regenPerSecond;
            Current = Mathf.Clamp(start, 0f, max);
        }

        public float Max { get; }

        public float Current { get; private set; }

        public void Tick(float deltaTime) => Gain(regenPerSecond * deltaTime);

        public void Gain(float amount) => Current = Mathf.Clamp(Current + amount, 0f, Max);

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
