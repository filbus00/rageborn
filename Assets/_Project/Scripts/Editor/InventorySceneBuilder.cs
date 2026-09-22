using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ARPG.Editor
{
    /// <summary>
    /// Adds the inventory and equip screen: a "Bag" button on the HUD that opens a full-screen panel with the
    /// equipped Weapon, Chest and Helm slots, a character stats readout and the scrollable backpack, each item with
    /// Equip and Discard actions. Opening it pauses the game (Docs/08-production.md: no separate pause menu, just
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
        static readonly Color SlotEmptyColor = new Color(1f, 1f, 1f, 0.12f);
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

            var openButton = BuildBagButton(hud.transform);
            BuildInventoryCanvas(openButton);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ARPG] Inventory and equip screen added to the sandbox scene.");
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

            var title = NewText(panel.transform, "Inventory", 44, TextAnchor.UpperCenter);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -40f);
            title.rectTransform.sizeDelta = new Vector2(600f, 60f);

            var closeButton = BuildCloseButton(panel.transform);

            var weapon = BuildSlot(panel.transform, "Weapon Slot", new Vector2(-360f, -140f));
            var chest = BuildSlot(panel.transform, "Chest Slot", new Vector2(0f, -140f));
            var helm = BuildSlot(panel.transform, "Helm Slot", new Vector2(360f, -140f));

            var stats = NewText(panel.transform, "", 28, TextAnchor.UpperLeft);
            stats.rectTransform.anchorMin = stats.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            stats.rectTransform.pivot = new Vector2(0.5f, 1f);
            stats.rectTransform.anchoredPosition = new Vector2(0f, -340f);
            stats.rectTransform.sizeDelta = new Vector2(1000f, 220f);

            var backpackLabel = NewText(panel.transform, "Backpack", 32, TextAnchor.UpperLeft);
            backpackLabel.rectTransform.anchorMin = backpackLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            backpackLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            backpackLabel.rectTransform.anchoredPosition = new Vector2(-460f, -580f);
            backpackLabel.rectTransform.sizeDelta = new Vector2(300f, 50f);

            var content = BuildScrollView(panel.transform);

            var screen = canvasObject.AddComponent<InventoryScreen>();
            EnemySceneBuilder.SetReference(screen, "panelRoot", panel.gameObject);
            EnemySceneBuilder.SetReference(screen, "openButton", openButton);
            EnemySceneBuilder.SetReference(screen, "closeButton", closeButton);
            EnemySceneBuilder.SetReference(screen, "statsText", stats);
            EnemySceneBuilder.SetReference(screen, "listContent", content);
            EnemySceneBuilder.SetReference(screen, "weaponSwatch", weapon.swatch);
            EnemySceneBuilder.SetReference(screen, "weaponLabel", weapon.label);
            EnemySceneBuilder.SetReference(screen, "weaponButton", weapon.button);
            EnemySceneBuilder.SetReference(screen, "chestSwatch", chest.swatch);
            EnemySceneBuilder.SetReference(screen, "chestLabel", chest.label);
            EnemySceneBuilder.SetReference(screen, "chestButton", chest.button);
            EnemySceneBuilder.SetReference(screen, "helmSwatch", helm.swatch);
            EnemySceneBuilder.SetReference(screen, "helmLabel", helm.label);
            EnemySceneBuilder.SetReference(screen, "helmButton", helm.button);

            panel.gameObject.SetActive(false);
        }

        static Button BuildCloseButton(Transform panel)
        {
            var go = new GameObject("Close Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(panel, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-24f, -24f);
            rect.sizeDelta = new Vector2(110f, 70f);

            go.GetComponent<Image>().color = ButtonColor;
            var label = NewText(go.transform, "Close", 28, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 0f);
            return go.GetComponent<Button>();
        }

        static (Image swatch, Text label, Button button) BuildSlot(Transform panel, string name, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(panel, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(320f, 150f);

            var image = go.GetComponent<Image>();
            image.color = SlotEmptyColor;

            var label = NewText(go.transform, "", 24, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 8f);

            return (image, label, go.GetComponent<Button>());
        }

        static RectTransform BuildScrollView(Transform panel)
        {
            var scrollGo = new GameObject("Backpack Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(panel, false);

            var scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.offsetMin = new Vector2(40f, 40f);
            scrollRect.offsetMax = new Vector2(-40f, -620f);

            scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewportGo.GetComponent<RectTransform>();
            Stretch(viewportRect, 0f);
            viewportGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            return contentRect;
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
