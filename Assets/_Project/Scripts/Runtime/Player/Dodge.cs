using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The dodge (the owner, 2026-10-10): a quick flick of the stick dives her about 3 units that way, untouchable for
    /// most of it and holding her fire; two charges, one back every 3 s. Numbers here, the state in
    /// <see cref="DodgeCharges"/> and <see cref="FlickDetector"/>, the dive in <see cref="PlayerController.TryDodge"/>.
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

        public const int MaxCharges = 2;
        public const float RechargeSeconds = 3f;

        /// <summary>A flick starts with the thumb this close to the stick's middle (in radii)...</summary>
        public const float FlickStart = 0.35f;

        /// <summary>...and reaches the ring's edge (in radii) within <see cref="FlickSeconds"/>.</summary>
        public const float FlickReach = 1f;

        public const float FlickSeconds = 0.15f;
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

    /// <summary>
    /// Spots a flick: the thumb near the stick's middle, then at the ring's edge within <see cref="DodgeRules.FlickSeconds"/>.
    /// One flick per trip out: the thumb has to come back near the middle (or lift) before the next. A slow push past the
    /// ring is no flick, and still disengages her as before.
    /// </summary>
    public sealed class FlickDetector
    {
        float lastNearMiddle = float.NegativeInfinity;
        bool spent;

        /// <summary>A new touch: the thumb is at the middle now.</summary>
        public void Begin(float time)
        {
            lastNearMiddle = time;
            spent = false;
        }

        public void Release()
        {
            lastNearMiddle = float.NegativeInfinity;
            spent = false;
        }

        /// <summary>Feeds the thumb's offset from the stick's middle, in radii; true when it completes a flick.</summary>
        public bool Feed(Vector2 offsetInRadii, float time)
        {
            var distance = offsetInRadii.magnitude;
            if (distance <= DodgeRules.FlickStart)
            {
                lastNearMiddle = time;
                spent = false;
                return false;
            }
            if (spent || distance < DodgeRules.FlickReach || time - lastNearMiddle > DodgeRules.FlickSeconds)
                return false;
            spent = true;
            return true;
        }
    }
}
