using System;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// A small read-out in the top left corner, under the life bar and clear of the mini-map, for checking the 60 fps
    /// target (Docs/08-production.md, M0 exit criteria): average fps and frame time, the worst frame and the hitch
    /// count over the last two seconds,
    /// garbage collections since start, the managed heap and the live enemy count. Created from code in the editor
    /// and in development builds only, and kept across scene changes, so no scene needs to be rebuilt for it.
    /// </summary>
    public class PerfOverlay : MonoBehaviour
    {
        const int WindowFrames = 120;
        const float RefreshSeconds = 0.5f;

        // A frame half again as long as the 60 fps budget reads as a visible stutter.
        const float HitchSeconds = 1.5f / 60f;

        readonly FrameStats stats = new FrameStats(WindowFrames);
        Text label;
        RectTransform panel;
        float untilRefresh;
        int startCollections;
        EnemyManager enemies;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (!Debug.isDebugBuild)
                return;

            var go = new GameObject("Perf Overlay");
            DontDestroyOnLoad(go);
            go.AddComponent<PerfOverlay>();
        }

        void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;

            var background = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(transform, false);
            panel = (RectTransform)background.transform;
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0f, 1f);
            panel.sizeDelta = new Vector2(380f, 70f);
            // No backdrop, half see-through (the owner, 2026-09-28), under the Bag button (2026-09-29).
            var image = background.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = false;

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(panel, false);
            var rect = (RectTransform)textObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            label = textObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 22;
            label.alignment = TextAnchor.UpperLeft;
            label.color = Color.white;
            label.raycastTarget = false;

            startCollections = GC.CollectionCount(0);
        }

        void Update()
        {
            // Unscaled: hit stop and the inventory screen change Time.timeScale, but frames still cost real time.
            stats.Record(Time.unscaledDeltaTime);

            // Hidden while the Bag is open: under the Bag button it would sit on the paper doll's Helm slot.
            var bagOpen = InventoryScreen.Current != null && InventoryScreen.Current.IsOpen;
            if (panel.gameObject.activeSelf == bagOpen)
                panel.gameObject.SetActive(!bagOpen);

            untilRefresh -= Time.unscaledDeltaTime;
            if (untilRefresh > 0f)
                return;
            untilRefresh = RefreshSeconds;

            PlaceBelowSafeAreaTop();

            if (enemies == null)
                enemies = FindAnyObjectByType<EnemyManager>();

            // Building this string allocates, but only twice a second, so it barely moves the GC count it reports.
            var fps = stats.AverageFps;
            var color = fps >= 57f ? Color.white : fps >= 45f ? new Color(1f, 0.8f, 0.2f) : new Color(1f, 0.35f, 0.3f);
            color.a = 0.5f;
            label.color = color;
            label.text =
                $"{fps:0} fps  {stats.AverageSeconds * 1000f:0.0} ms  worst {stats.WorstSeconds * 1000f:0.0}\n" +
                $"hitch {stats.CountOver(HitchSeconds)}  GC {GC.CollectionCount(0) - startCollections}  " +
                $"{GC.GetTotalMemory(false) / (1024f * 1024f):0.0} MB  {(enemies != null ? enemies.ActiveCount : 0)} en";
        }

        // Under the HUD's Bag button, inside the safe area (the owner, 2026-09-29). The band above the safe area was
        // tried first: beside the Dynamic Island the rounded corner cut the text's left edge, and on the owner's
        // iPhone 11 the notch leaves too little room either side for two lines. Without a Bag button (a scene with no
        // HUD) it sits at the safe area's top left.
        void PlaceBelowSafeAreaTop()
        {
            var canvasRect = (RectTransform)transform;
            if (Screen.height <= 0)
                return;
            var unitsPerPixel = canvasRect.rect.height / Screen.height;
            var safe = Screen.safeArea;
            var left = safe.xMin * unitsPerPixel + 24f;
            var top = (Screen.height - safe.yMax) * unitsPerPixel + 8f;

            if (bag == null)
            {
                var found = GameObject.Find(BagButtonName);
                bag = found != null ? found.GetComponent<RectTransform>() : null;
            }
            if (bag != null && bag.gameObject.activeInHierarchy)
            {
                bag.GetWorldCorners(corners);
                // Overlay canvases: world corners are screen pixels. corners[0] is the bottom left.
                left = corners[0].x * unitsPerPixel;
                top = (Screen.height - corners[0].y) * unitsPerPixel + 10f;
            }
            panel.anchoredPosition = new Vector2(left, -top);
        }

        const string BagButtonName = "Bag Button";
        RectTransform bag;
        readonly Vector3[] corners = new Vector3[4];
    }
}
