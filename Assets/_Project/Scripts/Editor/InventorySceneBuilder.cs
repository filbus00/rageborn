using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ARPG.Editor
{
    /// <summary>
    /// Adds the inventory and equip screen: a "Bag" button on the HUD that opens a full-screen panel, whose contents
    /// (paper doll, stats, backpack grid, tabs) <see cref="InventoryScreen"/> builds in code. Opening it pauses the game (Docs/08-production.md: no separate pause menu, just
    /// this). Replaces the placeholder "Loot Text" readout from Add Loot To Sandbox.
    /// Run from Tools > ARPG > Add Inventory Screen To Sandbox, after Add Loot To Sandbox. Running it again rebuilds
    /// the screen. Create Town Scene copies the result, so run that afterwards.
    /// </summary>
    public static class InventorySceneBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/Sandbox.unity";

        const string HudObjectName = "HUD Canvas";
        const string CanvasObjectName = "Inventory Canvas";
        const string LegacyReadoutName = "Loot Text";

        static readonly Color PanelColor = new Color(0.04f, 0.04f, 0.06f, 0.93f);
        static readonly Color ButtonColor = new Color(1f, 1f, 1f, 0.16f);

        [MenuItem("Tools/ARPG/Add Inventory Screen To Sandbox")]
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
            if (hud == null)
            {
                Debug.LogError("[ARPG] The scene needs the HUD. Run Add Survival To Sandbox first.");
                return;
            }

            // Migration: the placeholder text readout this screen replaces.
            var legacyReadout = hud.transform.Find(LegacyReadoutName);
            if (legacyReadout != null)
                Object.DestroyImmediate(legacyReadout.gameObject);

            PlayerSceneBuilder.RemoveExisting(scene, CanvasObjectName);

            // A Canvas needs its own GraphicRaycaster for its buttons to be clickable, and the scene needs exactly
            // one EventSystem to route input to any of them. Neither existed before this screen added the first
            // real UI buttons the player taps.
            if (hud.GetComponent<GraphicRaycaster>() == null)
                hud.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            var openButton = BuildBagButton(hud.transform);
            BuildInventoryCanvas(openButton);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ARPG] Inventory and equip screen added to the sandbox scene.");
        }

        static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
                return;

            // The new Input System's own UI input module, not the legacy StandaloneInputModule (CLAUDE.md: new
            // Input System only). Its Reset() wires sensible default point/click/touch actions on its own.
            new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == name)
                    return root;
            return null;
        }

        static Button BuildBagButton(Transform hud)
        {
            var go = new GameObject("Bag Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(hud, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-24f, 24f);
            rect.sizeDelta = new Vector2(150f, 84f);

            go.GetComponent<Image>().color = ButtonColor;

            var label = NewText(go.transform, "Bag", 30, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 0f);

            return go.GetComponent<Button>();
        }

        static void BuildInventoryCanvas(Button openButton)
        {
            var canvasObject = new GameObject(CanvasObjectName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // Above the HUD (10), below the fade (100).

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = NewImage(canvasObject.transform, "Panel", PanelColor);
            Stretch(panel.rectTransform, 0f);
            panel.raycastTarget = true;

            // Everything inside the panel is built in code by InventoryScreen (the owner's reference of 2026-09-28).
            var screen = canvasObject.AddComponent<InventoryScreen>();
            EnemySceneBuilder.SetReference(screen, "panelRoot", panel.gameObject);
            EnemySceneBuilder.SetReference(screen, "openButton", openButton);

            panel.gameObject.SetActive(false);
        }

        static Image NewImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static Text NewText(Transform parent, string text, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var label = go.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
