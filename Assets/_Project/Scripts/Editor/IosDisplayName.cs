#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace ARPG.Editor
{
    /// <summary>
    /// The name under the icon on the home screen (the owner, 2026-10-10): "Wild Arrow". The product name stays
    /// Rageborn, so the app's file, the bundle ID and the editor's save folder do not move; only the Xcode project's
    /// Info.plist gets the display name.
    /// </summary>
    public static class IosDisplayName
    {
        public const string DisplayName = "Wild Arrow";

        [PostProcessBuild(100)]
        static void OnPostprocessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS)
                return;
            var plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetString("CFBundleDisplayName", DisplayName);
            plist.WriteToFile(plistPath);
        }
    }
}
#endif
