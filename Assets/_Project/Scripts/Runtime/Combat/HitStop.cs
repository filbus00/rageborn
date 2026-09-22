using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// A brief, near-total freeze frame on an impactful hit, for weight. Global (<see cref="Time.timeScale"/>), so
    /// it stutters the whole game for a fraction of a second, not just the target. The countdown itself is
    /// <see cref="HitStopTimer"/>, pure and tested; this is the thin MonoBehaviour wrapper that touches
    /// <see cref="Time.timeScale"/>. Docs give no numbers for this; all values here are tuning.
    /// </summary>
    public class HitStop : MonoBehaviour
    {
        [Tooltip("How near-frozen the game goes during a hit stop. Not fully 0, so it reads as a stutter rather than a hitch.")]
        [SerializeField, Range(0f, 0.2f)] float frozenTimeScale = 0.02f;

        readonly HitStopTimer timer = new HitStopTimer();
        float normalTimeScale = 1f;

        /// <summary>The hit stop in the current scene, or null when it has none.</summary>
        public static HitStop Instance { get; private set; }

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Requests a hit stop of at least this many real seconds. Does nothing while the game is already paused
        /// (the inventory screen set <see cref="Time.timeScale"/> to 0) so a kill landed with the menu open cannot
        /// un-pause it.
        /// </summary>
        public void Trigger(float seconds)
        {
            if (!timer.IsActive && Time.timeScale <= 0f)
                return;

            if (!timer.IsActive)
                normalTimeScale = Time.timeScale;

            timer.Trigger(seconds);
            Time.timeScale = frozenTimeScale;
        }

        void Update()
        {
            if (!timer.Tick(Time.unscaledDeltaTime))
                return;

            // The inventory screen may have opened mid-stop (or already was open); defer to it rather than
            // stomping its pause back to running.
            var inventoryOpen = InventoryScreen.Current != null && InventoryScreen.Current.IsOpen;
            Time.timeScale = inventoryOpen ? 0f : normalTimeScale;
        }
    }
}
