namespace ARPG
{
    /// <summary>
    /// The countdown behind <see cref="HitStop"/>, pulled out pure so it can be unit tested without a running
    /// Player Loop. A request extends rather than stacks an already active stop.
    /// </summary>
    public sealed class HitStopTimer
    {
        public float RemainingSeconds { get; private set; }

        public bool IsActive => RemainingSeconds > 0f;

        /// <summary>Requests a stop of at least this many seconds.</summary>
        public void Trigger(float seconds)
        {
            if (seconds > RemainingSeconds)
                RemainingSeconds = seconds;
        }

        /// <summary>Advances by unscaled delta time. Returns true on the exact tick that ends an active stop, so
        /// the caller restores state once, not every idle frame.</summary>
        public bool Tick(float unscaledDeltaTime)
        {
            if (RemainingSeconds <= 0f)
                return false;

            RemainingSeconds -= unscaledDeltaTime;
            if (RemainingSeconds > 0f)
                return false;

            RemainingSeconds = 0f;
            return true;
        }
    }
}
