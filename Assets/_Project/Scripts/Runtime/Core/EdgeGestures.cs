using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Swipes at the screen's edges go to the game first (the owner, 2026-10-19: "make it so that swipe downwards on the
    /// bottom edge is blocked/stoped/defered"): iOS acts on its own edge gesture only on a second swipe there. The home
    /// bar stays shown, since iOS ignores the deferral while it hides itself. The player settings say the same
    /// (<c>ProjectSetup</c>); set again here at start so the build cannot drift from them.
    /// </summary>
    public static class EdgeGestures
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
#if UNITY_IOS && !UNITY_EDITOR
            UnityEngine.iOS.Device.hideHomeButton = false;
            UnityEngine.iOS.Device.deferSystemGesturesMode = UnityEngine.iOS.SystemGestureDeferMode.All;
#endif
        }
    }
}
