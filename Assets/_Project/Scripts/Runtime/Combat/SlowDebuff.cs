using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// A temporary move-speed multiplier below 1, such as an elite's Frozen modifier. Reapplying while active keeps
    /// the stronger slow and the longer remaining duration rather than stacking multiplicatively, so repeated hits
    /// cannot creep speed toward zero. Pure, so it can be unit tested.
    /// </summary>
    public sealed class SlowDebuff
    {
        public float Multiplier { get; private set; } = 1f;

        float remainingSeconds;

        public bool IsActive => remainingSeconds > 0f;

        public void Apply(float multiplier, float seconds)
        {
            var clamped = Mathf.Clamp01(multiplier);
            if (!IsActive || clamped < Multiplier)
                Multiplier = clamped;
            remainingSeconds = Mathf.Max(remainingSeconds, seconds);
        }

        public void Tick(float deltaTime)
        {
            if (!IsActive)
                return;

            remainingSeconds -= deltaTime;
            if (remainingSeconds <= 0f)
            {
                remainingSeconds = 0f;
                Multiplier = 1f;
            }
        }
    }
}
