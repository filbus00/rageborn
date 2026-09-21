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

            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}
