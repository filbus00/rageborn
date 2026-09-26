using UnityEditor;
using UnityEditor.SceneManagement;

namespace ARPG.Editor
{
    /// <summary>Tools > ARPG > Autopilot Act 1: plays act 1 from a new character in town, by itself, and writes
    /// Logs/AutopilotReport.md (<see cref="DevAutopilot"/>). Saving stays off in the editor unless Save In Editor is
    /// ticked, so every run starts from a fresh character.</summary>
    public static class AutopilotMenu
    {
        [MenuItem("Tools/ARPG/Autopilot Act 1")]
        public static void Run()
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EditorSceneManager.OpenScene(DungeonSceneBuilder.TownPath, OpenSceneMode.Single);
            EditorPrefs.SetBool(DevAutopilot.RequestKey, true);
            EditorApplication.isPlaying = true;
        }
    }
}
