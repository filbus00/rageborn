using System.IO;
using UnityEditor;

namespace ARPG.Editor
{
    /// <summary>
    /// Recompiles when a script changes even while the editor is in the background. Unity only looks for changed files
    /// when its window gets focus; the editor loop still ticks unfocused (at full rate with Preferences > General >
    /// Interaction Mode set to No Throttling), so a file watcher flags a change and the next update refreshes. Waits
    /// while playing, since a recompile then would reload the scripts mid-play.
    /// </summary>
    [InitializeOnLoad]
    static class BackgroundRefresh
    {
        static readonly FileSystemWatcher Watcher;
        static volatile bool dirty;

        static BackgroundRefresh()
        {
            Watcher = new FileSystemWatcher("Assets", "*.cs") { IncludeSubdirectories = true };
            Watcher.Changed += (_, _) => dirty = true;
            Watcher.Created += (_, _) => dirty = true;
            Watcher.Renamed += (_, _) => dirty = true;
            Watcher.Deleted += (_, _) => dirty = true;
            Watcher.EnableRaisingEvents = true;

            // The watcher's thread outlives a domain reload unless it is stopped.
            AssemblyReloadEvents.beforeAssemblyReload += () => Watcher.Dispose();
            EditorApplication.update += Update;
        }

        static void Update()
        {
            if (!dirty || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            dirty = false;
            AssetDatabase.Refresh();
        }
    }
}
