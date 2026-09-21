using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace ARPG.Editor
{
    /// <summary>
    /// Adds the placeholder player, the floating stick and the follow camera to the sandbox scene.
    /// Run from Tools > ARPG > Add Player And Camera To Sandbox. Running it again rebuilds these objects.
    /// </summary>
    public static class PlayerSceneBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/Sandbox.unity";
        const string PlayerPrefabPath = "Assets/_Project/Prefabs/Characters/Player.prefab";
        const string CharacterArtFolder = "Assets/_Project/Art/Characters";
        const string UIArtFolder = "Assets/_Project/Art/UI";

        const int PixelsPerUnit = 128;

        // Names of the scene objects this builder owns, so a rerun can replace them.
        const string InputObjectName = "Stick Input";
        const string CanvasObjectName = "Stick Canvas";
        const string PlayerObjectName = "Player";

        [MenuItem("Tools/ARPG/Add Player And Camera To Sandbox")]
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
            RemoveExisting(scene, InputObjectName, CanvasObjectName, PlayerObjectName);

            var stickInput = new GameObject(InputObjectName, typeof(FloatingStickInput)).GetComponent<FloatingStickInput>();
            BuildStickCanvas(stickInput);
            BuildPlayer();
            BuildCamera();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ARPG] Player, floating stick and follow camera added to the sandbox scene.");
        }

        static void RemoveExisting(UnityEngine.SceneManagement.Scene scene, params string[] names)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (System.Array.IndexOf(names, root.name) >= 0)
                    Object.DestroyImmediate(root);
        }

        static void BuildPlayer()
        {
            var bodySprite = PlaceholderArt.ImportSprite(
                $"{CharacterArtFolder}/PlaceholderPlayer.png",
                PlaceholderArt.Capsule(96, 160, new Color32(224, 150, 60, 255)),
                PixelsPerUnit, SpriteAlignment.BottomCenter, FilterMode.Bilinear);
            var shadowSprite = PlaceholderArt.ImportSprite(
                $"{CharacterArtFolder}/PlaceholderShadow.png",
                PlaceholderArt.ShadowEllipse(96, 48, 0.45f),
                PixelsPerUnit, SpriteAlignment.Center, FilterMode.Bilinear);

            var material = Default2DMaterial();

            var root = new GameObject(PlayerObjectName, typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(PlayerController));
            SetLayer(root, GameLayers.Player);

            var body = root.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            // A small collider at the feet: 0.2 units is about a third of the sprite width.
            var collider = root.GetComponent<CircleCollider2D>();
            collider.radius = 0.2f;
            collider.offset = new Vector2(0f, 0.1f);

            // The shadow sits on the Decals layer so it always draws under every character.
            var shadow = AddSprite(root.transform, "Shadow", shadowSprite, GameSortingLayers.Decals, material);
            shadow.transform.localPosition = Vector3.zero;

            // Sorted by pivot (the feet) so characters Y-sort against each other correctly.
            var bodyRenderer = AddSprite(root.transform, "Body", bodySprite, GameSortingLayers.Entities, material);
            bodyRenderer.spriteSortPoint = SpriteSortPoint.Pivot;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = Vector3.zero;
        }

        static void BuildStickCanvas(FloatingStickInput stickInput)
        {
            var ringSprite = PlaceholderArt.ImportSprite(
                $"{UIArtFolder}/StickRing.png", PlaceholderArt.Ring(256, 10),
                100, SpriteAlignment.Center, FilterMode.Bilinear);
            var knobSprite = PlaceholderArt.ImportSprite(
                $"{UIArtFolder}/StickKnob.png", PlaceholderArt.Disc(128),
                100, SpriteAlignment.Center, FilterMode.Bilinear);

            var canvasObject = new GameObject(CanvasObjectName, typeof(Canvas), typeof(CanvasScaler), typeof(StickVisual));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight = 0.5f;

            var ring = AddStickImage(canvasObject.transform, "Ring", ringSprite, new Color(1f, 1f, 1f, 0.45f));
            var knob = AddStickImage(canvasObject.transform, "Knob", knobSprite, new Color(1f, 1f, 1f, 0.6f));

            canvasObject.GetComponent<StickVisual>().Configure(stickInput, canvas, ring, knob);
        }

        static RectTransform AddStickImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            // Anchored to the bottom-left so anchoredPosition equals screen pixels divided by the canvas scale.
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;

            go.SetActive(false);
            return rect;
        }

        static void BuildCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogWarning("[ARPG] No Main Camera found; add a FollowCamera to your camera manually.");
                return;
            }

            if (camera.TryGetComponent<FollowCamera>(out _) == false)
                camera.gameObject.AddComponent<FollowCamera>();
        }

        static SpriteRenderer AddSprite(Transform parent, string name, Sprite sprite, string sortingLayer, Material material)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);

            var spriteRenderer = go.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingLayerName = sortingLayer;
            if (material != null)
                spriteRenderer.sharedMaterial = material;
            return spriteRenderer;
        }

        static void SetLayer(GameObject go, string layerName)
        {
            var layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                Debug.LogWarning($"[ARPG] Physics layer '{layerName}' does not exist. Run Tools > ARPG > Apply Project Setup.");
                return;
            }
            go.layer = layer;
        }

        static Material Default2DMaterial() =>
            GraphicsSettings.defaultRenderPipeline != null
                ? GraphicsSettings.defaultRenderPipeline.default2DMaterial
                : null;
    }
}
