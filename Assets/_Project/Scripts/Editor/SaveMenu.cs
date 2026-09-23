using UnityEditor;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>Tools > ARPG > Save: turn saving on for editor plays (off by default, see <see cref="SaveDirector"/>),
    /// delete the save, or open its folder.</summary>
    public static class SaveMenu
    {
        const string ToggleItem = "Tools/ARPG/Save/Save In Editor";

        [MenuItem(ToggleItem)]
        static void Toggle()
        {
            var enabled = !EditorPrefs.GetBool(SaveDirector.EditorPrefKey, false);
            EditorPrefs.SetBool(SaveDirector.EditorPrefKey, enabled);
            Debug.Log($"[Save] Saving in the editor is {(enabled ? "on" : "off")}; it applies from the next play.");
        }

        [MenuItem(ToggleItem, true)]
        static bool ToggleValidate()
        {
            Menu.SetChecked(ToggleItem, EditorPrefs.GetBool(SaveDirector.EditorPrefKey, false));
            return true;
        }

        [MenuItem("Tools/ARPG/Save/Delete Save")]
        static void Delete()
        {
            new SaveStore(SaveDirector.SaveDirectory).DeleteAll();
            Debug.Log($"[Save] Deleted the save and its backups in {SaveDirector.SaveDirectory}");
        }

        [MenuItem("Tools/ARPG/Save/Show Save Folder")]
        static void Show()
        {
            System.IO.Directory.CreateDirectory(SaveDirector.SaveDirectory);
            EditorUtility.RevealInFinder(SaveDirector.SaveDirectory);
        }
    }
}
