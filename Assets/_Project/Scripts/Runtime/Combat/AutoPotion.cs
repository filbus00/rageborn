using System;

namespace ARPG
{
    /// <summary>
    /// The auto-potion from Docs/01-core-gameplay.md: one potion type that heals 40 percent of life over 3 seconds,
    /// fires by itself when life drops to 35 percent, and refills from kills. The docs leave the charge count, the
    /// kills per charge and the cooldown open; the values here are tuning (see the constants). Pure: the caller feeds
    /// it time, life and kills, and applies the healing it returns.
    /// </summary>
    public sealed class AutoPotion
    {
        public const float HealFraction = 0.4f;
        public const float HealSeconds = 3f;

        /// <summary>The docs' default. They make it configurable from 20 to 60 percent in Settings, which do not exist yet.</summary>
        public const float DefaultTriggerFraction = 0.35f;
        public const float MinTriggerFraction = 0.2f;
        public const float MaxTriggerFraction = 0.6f;

        /// <summary>Tuning: charges held at most.</summary>
        public const int MaxCharges = 3;

        /// <summary>Tuning: kills that refill one charge. Kills made while full are not banked.</summary>
        public const int KillsPerCharge = 10;

        float triggerFraction = DefaultTriggerFraction;

        public AutoPotion(int charges = MaxCharges, int killProgress = 0, int extraCharges = 0)
        {
            ExtraCharges = Math.Max(0, extraCharges);
            Charges = Math.Max(0, Math.Min(Capacity, charges));
            KillProgress = Charges >= Capacity ? 0 : Math.Max(0, Math.Min(KillsPerCharge - 1, killProgress));
        }

        /// <summary>Charges beyond <see cref="MaxCharges"/> (2026-10-08: Sister Ivy's blessing, a quest).</summary>
        public int ExtraCharges { get; set; }

        /// <summary>The charges held at most.</summary>
        public int Capacity => MaxCharges + ExtraCharges;

        /// <summary>Raised when charges or kill progress change, so the session can be saved.</summary>
        public event Action Changed;

        public int Charges { get; private set; }

        /// <summary>Kills counted toward the next charge.</summary>
        public int KillProgress { get; private set; }

        /// <summary>Seconds of healing left from the potion in use; 0 when none is.</summary>
        public float HealRemaining { get; private set; }

        /// <summary>A potion is healing. Another cannot fire until it ends: the cooldown is the heal itself (tuning).</summary>
        public bool IsHealing => HealRemaining > 0f;

        /// <summary>The life fraction at or below which a potion fires, clamped to the docs' 20 to 60 percent.</summary>
        public float TriggerFraction
        {
            get => triggerFraction;
            set => triggerFraction = Math.Max(MinTriggerFraction, Math.Min(MaxTriggerFraction, value));
        }

        public void RegisterKill()
        {
            if (Charges >= Capacity)
                return;

            KillProgress++;
            if (KillProgress >= KillsPerCharge)
            {
                KillProgress = 0;
                Charges++;
            }
            Changed?.Invoke();
        }

        /// <summary>Back to full charges, for arriving in town (tuning: the docs do not say).</summary>
        public void Refill()
        {
            if (Charges == Capacity && KillProgress == 0)
                return;

            Charges = Capacity;
            KillProgress = 0;
            Changed?.Invoke();
        }

        /// <summary>Stops a heal in progress without refunding it, for a death.</summary>
        public void Cancel() => HealRemaining = 0f;

        /// <summary>
        /// Advances by <paramref name="deltaSeconds"/>. Fires a potion when the character is alive, at or under the
        /// trigger, has a charge and none is healing; <paramref name="fired"/> says whether this call did. Returns
        /// the healing for this step as a fraction of maximum life, spread evenly over <see cref="HealSeconds"/>.
        /// </summary>
        public float Tick(float deltaSeconds, float lifeFraction, bool isAlive, out bool fired)
        {
            fired = false;
            if (!isAlive)
            {
                HealRemaining = 0f;
                return 0f;
            }

            if (!IsHealing && Charges > 0 && lifeFraction <= triggerFraction)
            {
                Charges--;
                HealRemaining = HealSeconds;
                fired = true;
                Changed?.Invoke();
            }

            if (!IsHealing || deltaSeconds <= 0f)
                return 0f;

            var step = Math.Min(deltaSeconds, HealRemaining);
            HealRemaining -= step;
            return HealFraction * step / HealSeconds;
        }
    }
}
