using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG.Editor
{
    /// <summary>
    /// Adds loot to the sandbox scene: the placeholder beam, marker and coin art, a Loot System object that drops
    /// and picks up loot, and a small text readout of gold, the backpack and the weapon on the HUD.
    /// Run from Tools > ARPG > Add Loot To Sandbox, after Add Survival To Sandbox (it needs the HUD). Running it again
    /// rebuilds the object and the readout. Create Town Scene copies the result, so run that afterwards.
    /// </summary>
    public static class LootSceneBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/Sandbox.unity";
        const string EffectsArtFolder = "Assets/_Project/Art/Effects";

        const int PixelsPerUnit = 128;

        // Names of the scene objects this builder owns, so a rerun can replace them.
        const string LootObjectName = "Loot System";
        const string HudObjectName = "HUD Canvas";
        const string TextObjectName = "Loot Text";

        [MenuItem("Tools/ARPG/Add Loot To Sandbox")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogError($"[ARPG] {ScenePath} not found. Run Tools > ARPG > Create Sandbox Scene first.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var hud = FindRoot(scene, HudObjectName);
            if (hud == null || Object.FindAnyObjectByType<EnemyManager>() == null)
            {
                Debug.LogError("[ARPG] The scene needs an Enemy Manager and the HUD. Run Add Enemies To Sandbox and Add Survival To Sandbox first.");
                return;
            }

            PlayerSceneBuilder.RemoveExisting(scene, LootObjectName);

            var beam = PlaceholderArt.ImportSprite(
                $"{EffectsArtFolder}/LootBeam.png", PlaceholderArt.Beam(24, 256),
                PixelsPerUnit, SpriteAlignment.BottomCenter, FilterMode.Bilinear);
            var diamond = PlaceholderArt.ImportSprite(
                $"{EffectsArtFolder}/LootDiamond.png", PlaceholderArt.Diamond(64, 32),
                PixelsPerUnit, SpriteAlignment.Center, FilterMode.Bilinear);
            var coin = PlaceholderArt.ImportSprite(
                $"{EffectsArtFolder}/LootCoin.png", PlaceholderArt.Disc(64),
                PixelsPerUnit, SpriteAlignment.Center, FilterMode.Bilinear);

            var loot = new GameObject(LootObjectName, typeof(LootDirector), typeof(PlayerLoot));
            var director = loot.GetComponent<LootDirector>();
            EnemySceneBuilder.SetReference(director, "diamondSprite", diamond);
            EnemySceneBuilder.SetReference(director, "coinSprite", coin);
            EnemySceneBuilder.SetReference(director, "beamSprite", beam);

            BuildReadout(hud.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ARPG] Loot drops, auto-loot and the gold and weapon readout added to the sandbox scene.");
        }

        static GameObject FindRoot(UnityEngine.SceneManagement.Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == name)
                    return root;
            return null;
        }

        static void BuildReadout(Transform hud)
        {
            var old = hud.Find(TextObjectName);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);

            var go = new GameObject(TextObjectName, typeof(RectTransform), typeof(Text), typeof(LootHud));
            go.transform.SetParent(hud, false);

            // Under the life bar, in the read-only top of the screen.
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -205f);
            rect.sizeDelta = new Vector2(900f, 100f);

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 32;
            text.alignment = TextAnchor.UpperCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = "Gold 0";

            go.AddComponent<Shadow>().effectDistance = new Vector2(2f, -2f);
            EnemySceneBuilder.SetReference(go.GetComponent<LootHud>(), "label", text);
        }
    }
}
