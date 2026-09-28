using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The Portal Tome's side of travel and the town's Waystone (Docs/05-world-and-content.md): in the dungeon, a Portal
    /// button (once the character owns the Tome) that opens a town portal where the character stands; in town, the
    /// Waystone, which opens the waypoint list, and the matching portal back while one is open. The dungeon side of an open portal is placed by <see cref="DungeonLevel"/>. Created from code
    /// and kept across scenes.
    /// </summary>
    public class TravelDirector : MonoBehaviour
    {
        const string TownScene = "Town";

        // In town the Waystone and the return portal stand a few cells below the start, away from the Forge and stairs.
        static readonly Vector2Int TownPortalCell = new Vector2Int(-2, -4);
        static readonly Vector2Int WaystoneCell = new Vector2Int(3, -3);

        GameSession session;
        GameObject button;
        bool inDungeon;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            var go = new GameObject("Travel Director");
            DontDestroyOnLoad(go);
            go.AddComponent<TravelDirector>();
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SettingsDirector.Changed += PlaceButton;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SettingsDirector.Changed -= PlaceButton;
            if (session != null)
                session.Changed -= Refresh;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (session != GameSession.Current)
            {
                if (session != null)
                    session.Changed -= Refresh;
                session = GameSession.Current;
                session.Changed += Refresh;
            }

            inDungeon = FindAnyObjectByType<DungeonLevel>() != null;
            button = null;
            if (scene.name == TownScene)
            {
                Waypoint.Create(0, IsoMath.CellToGround(WaystoneCell), null);
                if (session.PortalDepth > 0)
                    TownPortal.Create(true, session.PortalDepth, IsoMath.CellToGround(TownPortalCell), null);
            }
            Refresh();
        }

        // The button appears the moment the Wanderer hands over the Tome, not only on the next level.
        void Refresh()
        {
            if (button == null && inDungeon && session.HasPortalTome)
            {
                button = CreateButton();
                PlaceButton();
            }
        }

        // Above the Bag button, in whichever corner handedness puts it.
        void PlaceButton()
        {
            if (button != null)
                Handedness.PlaceInCorner((RectTransform)button.transform.GetChild(0));
        }

        GameObject CreateButton()
        {
            var canvasObject = new GameObject("Portal Button Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 11; // Just above the HUD (10).
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight = 0.5f;

            // Above the Bag button, the same size, in the thumb's corner.
            var go = new GameObject("Portal Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(canvasObject.transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-24f, 132f);
            rect.sizeDelta = new Vector2(150f, 84f);
            go.GetComponent<Image>().color = new Color(0.35f, 0.55f, 1f, 0.35f);
            go.GetComponent<Button>().onClick.AddListener(OpenPortal);

            var label = new GameObject("Text", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(go.transform, false);
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var text = label.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 30;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = "Portal";
            text.raycastTarget = false;
            return canvasObject;
        }

        // A new portal replaces the old one (one at a time), at the character's feet: walk out of it and back in to go.
        void OpenPortal()
        {
            var level = FindAnyObjectByType<DungeonLevel>();
            var player = FindAnyObjectByType<PlayerController>();
            if (level == null || player == null || SceneTravel.IsTravelling)
                return;

            var old = GameObject.Find("Town Portal");
            if (old != null)
                Destroy(old);
            var at = IsoMath.WorldToGround(player.transform.position);
            session.OpenPortal(level.Depth, at);
            TownPortal.Create(false, level.Depth, at, level.transform);
            Sfx.Play(SoundId.Hint);
        }
    }
}
