using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// An Android APK for playtesters (the owner, 2026-10-25: "lets do an android build now so that my friend can
    /// playtest"): a release build (no performance overlay, no DEV buttons), ARM64, IL2CPP, signed with Unity's debug key
    /// (enough to install by hand), named "Wild Arrow" on the phone. The scenes are the build settings'. The editor is put
    /// back on iOS afterwards.
    /// </summary>
    public static class AndroidBuild
    {
        public const string ApkPath = "Builds/Android/WildArrow.apk";

        [MenuItem("Tools/ARPG/Build Android APK")]
        public static BuildReport Build()
        {
            ProjectSetup.Apply();
            var android = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, "com.filipbusic.rageborn");
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            EditorUserBuildSettings.buildAppBundle = false;
            var productName = PlayerSettings.productName;
            PlayerSettings.productName = "Wild Arrow";
            AssetDatabase.SaveAssets();
            try
            {
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                var options = new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    locationPathName = ApkPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.None,
                };
                var report = BuildPipeline.BuildPlayer(options);
                var summary = report.summary;
                Debug.Log($"[ARPG] Android build: {summary.result}, {summary.totalErrors} errors, {summary.totalTime.TotalSeconds:0} s, {summary.totalSize / (1024 * 1024)} MB, at {ApkPath}");
                return report;
            }
            finally
            {
                PlayerSettings.productName = productName;
                AssetDatabase.SaveAssets();
            }
        }

        [MenuItem("Tools/ARPG/Switch Editor Back To iOS")]
        public static void BackToIOS()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
        }
    }
}
