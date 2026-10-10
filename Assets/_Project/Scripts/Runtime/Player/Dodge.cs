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
        public const float Distance = 3f;
        public const float Seconds = 0.4f;
        public const float Speed = Distance / Seconds;

        /// <summary>The dive animation, travel and getting up (Mixamo's Standing Dive Forward is 1.6 s, played faster):
        /// she holds her fire this long, longer than the travel.</summary>
        public const float AnimationSeconds = 0.65f;

        /// <summary>She cannot be hit for this long from the dive's start.</summary>
        public const float UntouchableSeconds = 0.35f;

        /// <summary>Whether to dive this step: the thumb has just left the stick's ring, and a fight is on.</summary>
        public static bool Triggers(bool disengaged, bool wasDisengaged, bool inCombat) => disengaged && !wasDisengaged && inCombat;

        public const int MaxCharges = 2;
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

        public bool TryUse()
        {
            if (Charges <= 0)
                return false;
            Charges--;
            return true;
        }
    }
}
