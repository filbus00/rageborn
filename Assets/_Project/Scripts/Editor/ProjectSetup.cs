using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace ARPG.Editor
{
    /// <summary>
    /// One-shot, idempotent project configuration for an isometric 2D mobile ARPG.
    /// Run from Tools > ARPG > Apply Project Setup.
    /// </summary>
    public static class ProjectSetup
    {
        const string Renderer2DPath = "Assets/Settings/Renderer2D.asset";
        static readonly Vector3 IsoSortAxis = new Vector3(0f, 1f, 0f);

        [MenuItem("Tools/ARPG/Apply Project Setup")]
        public static void Apply()
        {
            ConfigureIOS();
            ConfigureIsometricSorting();
            AddSortingLayers();
            AddPhysicsLayers();

            AssetDatabase.SaveAssets();
            Debug.Log("[ARPG] Project setup applied.");
        }

        static void ConfigureIOS()
        {
            PlayerSettings.companyName = "Filbus Software";
            PlayerSettings.productName = "Rageborn";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.filipbusic.rageborn");

            // Portrait only (Docs/00-vision-and-scope.md).
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            PlayerSettings.iOS.targetOSVersionString = "16.0";

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);

            PlayerSettings.iOS.hideHomeButton = true;
        }

        static void ConfigureIsometricSorting()
        {
            // Sprites lower on screen (smaller Y) draw in front of sprites higher up.
            GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
            GraphicsSettings.transparencySortAxis = IsoSortAxis;

            var renderer2D = AssetDatabase.LoadAssetAtPath<ScriptableObject>(Renderer2DPath);
            if (renderer2D == null)
            {
                Debug.LogWarning($"[ARPG] {Renderer2DPath} not found; set Transparency Sort Mode to Custom Axis (0,1,0) on your 2D Renderer manually.");
                return;
            }

            var so = new SerializedObject(renderer2D);
            so.FindProperty("m_TransparencySortMode").intValue = (int)TransparencySortMode.CustomAxis;
            so.FindProperty("m_TransparencySortAxis").vector3Value = IsoSortAxis;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(renderer2D);
        }

        static void AddSortingLayers()
        {
            var tagManager = LoadTagManager();
            var layers = tagManager.FindProperty("m_SortingLayers");

            foreach (var name in GameSortingLayers.All)
            {
                if (HasSortingLayer(layers, name))
                    continue;

                layers.InsertArrayElementAtIndex(layers.arraySize);
                var layer = layers.GetArrayElementAtIndex(layers.arraySize - 1);
                layer.FindPropertyRelative("name").stringValue = name;
                layer.FindPropertyRelative("uniqueID").uintValue = (uint)Random.Range(1, int.MaxValue);
                layer.FindPropertyRelative("locked").boolValue = false;
            }

            tagManager.ApplyModifiedProperties();
        }

        static void AddPhysicsLayers()
        {
            var tagManager = LoadTagManager();
            var layers = tagManager.FindProperty("layers");

            foreach (var name in GameLayers.PhysicsLayers)
            {
                if (HasPhysicsLayer(layers, name))
                    continue;

                // Layers 0-7 are built-in; user layers start at 8.
                var slot = FindEmptyPhysicsSlot(layers);
                if (slot < 0)
                {
                    Debug.LogWarning($"[ARPG] No free physics layer slot for '{name}'.");
                    continue;
                }
                layers.GetArrayElementAtIndex(slot).stringValue = name;
            }

            tagManager.ApplyModifiedProperties();
        }

        static SerializedObject LoadTagManager() =>
            new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        static bool HasSortingLayer(SerializedProperty layers, string name)
        {
            for (var i = 0; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name)
                    return true;
            return false;
        }

        static bool HasPhysicsLayer(SerializedProperty layers, string name)
        {
            for (var i = 0; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).stringValue == name)
                    return true;
            return false;
        }

        static int FindEmptyPhysicsSlot(SerializedProperty layers)
        {
            for (var i = 8; i < layers.arraySize; i++)
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                    return i;
            return -1;
        }
    }
}
