using System.IO;
using UnityEditor;
using UnityEngine;

namespace ARPG.Editor
{
    /// <summary>
    /// Creates the two sprite materials the lighting pass loads from Resources (<see cref="SpriteMaterials"/>): the URP
    /// unlit sprite material for effects that must read in the dark, and the hit flash and dissolve material. Idempotent.
    /// </summary>
    public static class LightingSetup
    {
        const string Folder = "Assets/_Project/Resources/Materials";
        const string UnlitPath = Folder + "/SpriteUnlit.mat";
        const string EffectPath = Folder + "/SpriteEffect.mat";

        [MenuItem("Tools/ARPG/Create Lighting Materials")]
        public static void CreateMaterials()
        {
            Directory.CreateDirectory(Folder);
            Ensure(UnlitPath, "Universal Render Pipeline/2D/Sprite-Unlit-Default");
            Ensure(EffectPath, "ARPG/Sprite Effect");
            AssetDatabase.SaveAssets();
        }

        static void Ensure(string path, string shaderName)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
                return;
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"Shader {shaderName} not found; {path} not created.");
                return;
            }
            AssetDatabase.CreateAsset(new Material(shader), path);
        }
    }
}
