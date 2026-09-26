using UnityEngine;

namespace ARPG
{
    /// <summary>Docs/05-world-and-content.md: every act boss has three phases. Pure.</summary>
    public static class BossPhases
    {
        /// <summary>Phase 2 starts below two thirds of the boss's life, phase 3 below one third (tuning: the docs give
        /// three phases but not where they split).</summary>
        public const float PhaseTwoBelow = 2f / 3f;
        public const float PhaseThreeBelow = 1f / 3f;

        public static int PhaseFor(float lifeFraction) =>
            lifeFraction > PhaseTwoBelow ? 1 : lifeFraction > PhaseThreeBelow ? 2 : 3;
    }

    /// <summary>
    /// The boss stagger meter (Docs/05-world-and-content.md): it fills only through repeated hits, not by how hard they
    /// hit, so a weak build fills it as fast as a strong one, and when full it briefly stops the boss, then empties.
    /// Values are tuning. Pure.
    /// </summary>
    public sealed class StaggerMeter
    {
        public const float Full = 100f;

        /// <summary>About 30 hits: roughly 20 seconds of basic attacks and a skill now and then.</summary>
        public const float PerHit = 3.5f;

        public const float StaggerSeconds = 3f;

        public float Value { get; private set; }

        /// <summary>0 to 1, for the bar under the boss's life.</summary>
        public float Fraction => IsStaggered ? 1f : Value / Full;

        public bool IsStaggered => StaggerLeft > 0f;

        public float StaggerLeft { get; private set; }

        /// <summary>Counts one hit. Returns true when this hit filled the meter and the boss is now staggered. Hits
        /// during a stagger do not count toward the next one.</summary>
        public bool AddHit()
        {
            if (IsStaggered)
                return false;

            Value = Mathf.Min(Full, Value + PerHit);
            if (Value < Full)
                return false;

            StaggerLeft = StaggerSeconds;
            return true;
        }

        /// <summary>Runs the stagger down; when it ends the meter starts empty again.</summary>
        public void Tick(float deltaSeconds)
        {
            if (!IsStaggered)
                return;

            StaggerLeft = Mathf.Max(0f, StaggerLeft - deltaSeconds);
            if (StaggerLeft <= 0f)
                Value = 0f;
        }
    }
}
