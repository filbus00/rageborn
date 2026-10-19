using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// When iOS takes the screen from the game (an edge swipe that opens the notification shade or Control Center, the
    /// home swipe, a call), the Bag opens, which pauses the game, so she is not left fighting while the player looks
    /// away (the owner, 2026-10-19: "make sure that the game is paused (inventory screen opened) when edge gesture is
    /// acidently triggered"). Unity is told by OnApplicationFocus(false), or OnApplicationPause(true) when the app goes
    /// to the background. Not in the editor, where the game loses focus whenever another window is clicked.
    /// </summary>
    public class FocusPause : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (Application.isEditor || FindAnyObjectByType<FocusPause>() != null)
                return;
            var go = new GameObject("Focus Pause", typeof(FocusPause));
            DontDestroyOnLoad(go);
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                PauseNow();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
                PauseNow();
        }

        static void PauseNow()
        {
            var bag = InventoryScreen.Current;
            var health = FindAnyObjectByType<PlayerHealth>();
            if (FocusPauseRules.OpensBag(bag != null, bag != null && bag.IsOpen, Time.timeScale, health != null && health.IsAlive,
                    SettingsScreen.IsOpen))
                bag.Open();
        }
    }

    /// <summary>When losing the screen opens the Bag (pure, tested).</summary>
    public static class FocusPauseRules
    {
        /// <summary>Only in play: a Bag to open, not open yet, the game running (a boon choice, the Bag or Settings
        /// already paused it, or a load is under way) and her alive (death has its own screen).</summary>
        public static bool OpensBag(bool hasBag, bool bagOpen, float timeScale, bool alive, bool settingsOpen) =>
            hasBag && !bagOpen && timeScale > 0f && alive && !settingsOpen;
    }
}
