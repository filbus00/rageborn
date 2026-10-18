using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The dodge (the owner, 2026-10-10): in a fight, the thumb leaving the stick's ring dives her about 3 units that
    /// way, untouchable for most of it and holding her fire; two charges, one back every 3 s. Numbers here, the charges
    /// in <see cref="DodgeCharges"/>, the dive in <see cref="PlayerController.TryDodge"/>.
    /// </summary>
    public static class DodgeRules
    {
        // Snappier (the owner, 2026-10-18: "it feels somewhat sluggish and unresponsive"): it was 3 units over 0.4 s at
        // an even 7.5 a second (under twice her run) and held her fire for 0.65 s. Now a little further in 0.24 s, fastest
        // at the start and easing out (DiveSpeed), and she is back on her feet and shooting after 0.38 s.
        public const float Distance = 3.2f;
        public const float Seconds = 0.24f;
        public const float Speed = Distance / Seconds;

        /// <summary>The dive's speed at its start, as a multiple of its average: it falls evenly to
        /// (2 - <see cref="StartBurst"/>) times the average at its end, so the distance stays <see cref="Distance"/>.</summary>
        public const float StartBurst = 1.6f;

        /// <summary>The speed share at a point of the dive (0 to 1 through it): a burst that eases out.</summary>
        public static float DiveSpeed(float t) => Mathf.Lerp(StartBurst, 2f - StartBurst, Mathf.Clamp01(t));

        /// <summary>The dive animation, travel and getting up (Mixamo's Standing Dive Forward is 1.6 s, played faster):
        /// she holds her fire this long, a little longer than the travel.</summary>
        public const float AnimationSeconds = 0.38f;

        /// <summary>She cannot be hit for this long from the dive's start: the whole travel.</summary>
        public const float UntouchableSeconds = 0.24f;

        /// <summary>Whether to dive this step: the thumb has just left the stick's ring, and a fight is on.</summary>
        public static bool Triggers(bool disengaged, bool wasDisengaged, bool inCombat) => disengaged && !wasDisengaged && inCombat;

        /// <summary>Three since 2026-10-18 (the owner: "Add a third roll that the character gets from the start").</summary>
        public const int MaxCharges = 3;
        public const float RechargeSeconds = 3f;

    }

    /// <summary>Dodge charges: up to two, one refilling at a time.</summary>
    public sealed class DodgeCharges
    {
        float refill;

        public int Charges { get; private set; } = DodgeRules.MaxCharges;

        /// <summary>How far the next charge has refilled, 0 to 1; 1 when all are full.</summary>
        public float Refill => Charges >= DodgeRules.MaxCharges ? 1f : refill / DodgeRules.RechargeSeconds;

        public void Tick(float deltaTime)
        {
            if (Charges >= DodgeRules.MaxCharges)
            {
                refill = 0f;
                return;
            }
            refill += deltaTime;
            if (refill < DodgeRules.RechargeSeconds)
                return;
            refill -= DodgeRules.RechargeSeconds;
            Charges++;
        }

        /// <summary>Gives a charge back (Marked Prey's kill), up to the most.</summary>
        public void Refund()
        {
            if (Charges < DodgeRules.MaxCharges)
                Charges++;
        }

        public bool TryUse()
        {
            if (Charges <= 0)
                return false;
            Charges--;
            return true;
        }
    }
}
