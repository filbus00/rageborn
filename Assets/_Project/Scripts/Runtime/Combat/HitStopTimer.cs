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

    /// <summary>
    /// When a kill earns a hit stop (the owner, 2026-10-03: "stuttering when enemies are hit ... feels very heavy"). A
    /// stop on every kill suited the axe, one kill a swing; with a bow killing a pack one arrow at a time, a dozen 50 ms
    /// near-freezes in a few seconds read as stutter while the frame rate stayed at 60. Now only a champion, elite or boss
    /// kill stops the game, at most once every <see cref="MinGapSeconds"/>.
    /// </summary>
    public static class HitStopRules
    {
        public const float MinGapSeconds = 1f;

        public static bool OnKill(EnemyRank rank, float secondsSinceLastStop) =>
            rank != EnemyRank.Normal && secondsSinceLastStop >= MinGapSeconds;
    }
}
