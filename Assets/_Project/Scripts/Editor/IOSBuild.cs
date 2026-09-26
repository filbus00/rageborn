using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// Writes the Xcode project for the iOS Simulator or a device. Development builds, so the performance overlay
    /// shows. The scenes are the build settings' (Town then Sandbox, written by Create Town Scene).
    /// Build the Xcode project afterwards with xcodebuild or Xcode, scheme Unity-iPhone.
    /// </summary>
    public static class IOSBuild
    {
        public const string SimulatorPath = "Builds/iOS-Simulator";
        public const string DevicePath = "Builds/iOS";

        [MenuItem("Tools/ARPG/Build iOS Simulator (Xcode project)")]
        public static void BuildSimulator() => Build(iOSSdkVersion.SimulatorSDK, SimulatorPath);

        [MenuItem("Tools/ARPG/Build iOS Device (Xcode project)")]
        public static void BuildDevice() => Build(iOSSdkVersion.DeviceSDK, DevicePath);

        public static BuildReport Build(iOSSdkVersion sdk, string path)
        {
            ProjectSetup.Apply();

            // The SDK is a project setting; put it back so a simulator build does not leave the project on it.
            var previousSdk = PlayerSettings.iOS.sdkVersion;
            PlayerSettings.iOS.sdkVersion = sdk;
            // Saved before building: without it the export sometimes kept the SDK on disk and a simulator build came
            // out as a device project that xcodebuild could not build for the simulator (2026-09-26).
            AssetDatabase.SaveAssets();
            try
            {
                var options = new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    locationPathName = path,
                    target = BuildTarget.iOS,
                    options = BuildOptions.Development,
                };
                var report = BuildPipeline.BuildPlayer(options);
                var summary = report.summary;
                Debug.Log($"[ARPG] iOS {sdk} build: {summary.result}, {summary.totalErrors} errors, {summary.totalTime.TotalSeconds:0} s, at {path}");
                return report;
            }
            finally
            {
                // The build writes ProjectSettings.asset with the SDK it used, so save again after restoring.
                PlayerSettings.iOS.sdkVersion = previousSdk;
                AssetDatabase.SaveAssets();
            }
        }
    }
}
