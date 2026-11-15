using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Runs once before the first scene loads. Mobile-wide runtime defaults live here.
    /// </summary>
    public static class GameBootstrap
    {
        const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            // On iOS vSync is controlled by the display link, so targetFrameRate is what caps the frame rate.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
            // Physics in step with the frames (2026-11-15, the owner: movement "feels less responsive"): at 50 steps a
            // second against 60 frames, a frame in six had no step and her position was one step behind her stick.
            Time.fixedDeltaTime = 1f / TargetFrameRate;

            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}
