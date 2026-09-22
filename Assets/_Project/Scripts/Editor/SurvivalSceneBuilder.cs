using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG.Editor
{
    /// <summary>
    /// Adds what makes the sandbox survivable to the sandbox scene: the life bar and the fade overlay, the player's
    /// death handling, the corpse markers and a stairway up to the town. Run from Tools > ARPG > Add Survival To
    /// Sandbox. Running it again rebuilds these objects. Create Town Scene copies the result, so run this first.
    /// </summary>
    public static class SurvivalSceneBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/Sandbox.unity";
        const string CharacterArtFolder = "Assets/_Project/Art/Characters";
        const string EnvironmentArtFolder = "Assets/_Project/Art/Environment";

        const int PixelsPerUnit = 128;

        // Names of the scene objects this builder owns, so a rerun can replace them.
        const string HudObjectName = "HUD Canvas";
        const string FadeObjectName = "Fade Canvas";
        const string VitalsObjectName = "Player Vitals";
        const string CorpsesObjectName = "Corpse Spawner";
        const string StairsObjectName = "Stairs Up";

        // The stairway sits in the near-west part of the room, about 4.5 units from the player start at (0, -7) so it
        // is not walked onto by accident. It must stay at least 3 cells clear of the two near walls (the bottom-left
        // and bottom-right edges of the room): wall blocks are tall sprites that draw over the cells just behind
        // them, which hid the stairway when it stood against the wall at (2, -8).
        static readonly Vector2Int StairsCell = new Vector2Int(-6, -3);

        [MenuItem("Tools/ARPG/Add Survival To Sandbox")]
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
            if (Object.FindAnyObjectByType<PlayerController>() == null)
            {
                Debug.LogError("[ARPG] The scene needs a Player. Run Add Player And Camera To Sandbox first.");
                return;
            }

            PlayerSceneBuilder.RemoveExisting(scene, HudObjectName, FadeObjectName, VitalsObjectName, CorpsesObjectName, StairsObjectName);

            BuildHud();
            BuildFade();
            new GameObject(VitalsObjectName, typeof(PlayerHealth), typeof(DeathFlow));
            BuildCorpseSpawner();
            AddStairs(StairsObjectName, StairsCell, "Town");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ARPG] Life bar, fade, death handling, corpse markers and a stairway to the town added to the sandbox scene.");
        }

        // Also used by the town builder for the stairway down.
        internal static GameObject AddStairs(string objectName, Vector2Int cell, string targetScene)
        {
            var sprite = PlaceholderArt.ImportSprite(
                $"{EnvironmentArtFolder}/PlaceholderStairs.png",
                PlaceholderArt.Stairs(128, 64, new Color32(38, 34, 40, 255), new Color32(78, 70, 82, 255)),
                PixelsPerUnit, SpriteAlignment.Center, FilterMode.Point);

            var go = new GameObject(objectName, typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(SceneExit));
            var world = IsoMath.GroundToWorld(IsoMath.CellToGround(cell));
            go.transform.position = new Vector3(world.x, world.y, 0f);
            PlayerSceneBuilder.SetLayer(go, GameLayers.Interactable);

            // Flat on the ground, under every character.
            var spriteRenderer = go.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingLayerName = GameSortingLayers.Decals;
            var material = PlayerSceneBuilder.Default2DMaterial();
            if (material != null)
                spriteRenderer.sharedMaterial = material;

            var collider = go.GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.4f;

            var serialized = new SerializedObject(go.GetComponent<SceneExit>());
            serialized.FindProperty("targetScene").stringValue = targetScene;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        static void BuildHud()
        {
            var canvasObject = NewCanvas(HudObjectName, 10);

            // A dark bar with a red fill, near the top where the docs keep read-only status. The fill's right anchor
            // is driven by LifeBar.
            var bar = NewImage("Life Bar", canvasObject.transform, new Color(0f, 0f, 0f, 0.6f));
            var barRect = bar.rectTransform;
            barRect.anchorMin = barRect.anchorMax = new Vector2(0.5f, 1f);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.anchoredPosition = new Vector2(0f, -160f);
            barRect.sizeDelta = new Vector2(720f, 30f);

            var fill = NewImage("Fill", bar.transform, new Color(0.78f, 0.13f, 0.13f, 1f));
            Stretch(fill.rectTransform, 4f);

            var lifeBar = bar.gameObject.AddComponent<LifeBar>();
            EnemySceneBuilder.SetReference(lifeBar, "fill", fill.rectTransform);
        }

        static void BuildFade()
        {
            // Its own canvas above the HUD, so a fade also covers the life bar. Saved transparent: ScreenFade.Awake
            // sets it opaque the moment play starts, and every Screen Space Overlay canvas draws as a huge rectangle
            // in the Scene view (its corner sits at the world origin), so a black-saved overlay showed there as a
            // black square over the ground beyond that corner.
            var canvasObject = NewCanvas(FadeObjectName, 100);
            var overlay = NewImage("Overlay", canvasObject.transform, new Color(0f, 0f, 0f, 0f));
            Stretch(overlay.rectTransform, 0f);

            var fade = overlay.gameObject.AddComponent<ScreenFade>();
            EnemySceneBuilder.SetReference(fade, "overlay", overlay);
        }

        static void BuildCorpseSpawner()
        {
            var sprite = PlaceholderArt.ImportSprite(
                $"{CharacterArtFolder}/PlaceholderCorpse.png",
                PlaceholderArt.Capsule(56, 88, new Color32(120, 128, 150, 255)),
                PixelsPerUnit, SpriteAlignment.BottomCenter, FilterMode.Bilinear);

            var spawner = new GameObject(CorpsesObjectName, typeof(CorpseSpawner)).GetComponent<CorpseSpawner>();
            EnemySceneBuilder.SetReference(spawner, "corpseSprite", sprite);
        }

        static GameObject NewCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight = 0.5f;
            return go;
        }

        static Image NewImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
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
