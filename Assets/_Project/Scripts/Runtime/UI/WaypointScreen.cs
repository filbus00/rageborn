using System;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The waypoint list (Docs/05-world-and-content.md: a list screen opens from a waypoint or the Waystone; Docs/06: a
    /// bottom sheet). Town first, then every activated waypoint by depth with its enemy level; where the player stands is
    /// shown but not a button. A tap travels there (<see cref="SceneTravel"/>); a tap above the sheet closes it. Built in
    /// code the first time it opens. It does not pause: waypoints stand where no pack can reach.
    /// </summary>
    public class WaypointScreen : MonoBehaviour
    {
        static readonly Color SheetColor = UiStyle.Sheet;
        static readonly Color StrokeColor = UiStyle.Blood;
        static readonly Color ButtonColor = UiStyle.ButtonFill;
        static readonly Color HereColor = UiStyle.Locked;

        const float RowHeight = 168f; // 56 points, the docs' preferred tap target.

        static WaypointScreen current;

        RectTransform list;
        int here;

        /// <summary>Opens the list at a waypoint of the given depth, or at the Waystone (0).</summary>
        public static void Open(int fromDepth)
        {
            if (current == null)
                current = Create();
            current.here = fromDepth;
            current.gameObject.SetActive(true);
            current.Render();
        }

        public static bool IsOpen => current != null && current.gameObject.activeSelf;

        static WaypointScreen Create()
        {
            var canvasObject = new GameObject("Waypoint Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = new GameObject("Waypoints", typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(canvasObject.transform, false);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            var screen = root.AddComponent<WaypointScreen>();
            root.GetComponent<Button>().onClick.AddListener(screen.Close);

            var sheet = new GameObject("Sheet", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            sheet.transform.SetParent(root.transform, false);
            var sheetRect = (RectTransform)sheet.transform;
            sheetRect.anchorMin = new Vector2(0f, 0f);
            sheetRect.anchorMax = new Vector2(1f, 0f);
            sheetRect.pivot = new Vector2(0.5f, 0f);
            sheetRect.sizeDelta = new Vector2(0f, 1500f);
            sheet.GetComponent<Image>().color = SheetColor;
            UiStyle.Leather(sheet.GetComponent<Image>());
            var layout = sheet.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 40, 48);
            layout.spacing = 16f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.childAlignment = TextAnchor.UpperCenter;

            var stroke = new GameObject("Stroke", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            stroke.transform.SetParent(sheet.transform, false);
            stroke.GetComponent<Image>().color = StrokeColor;
            stroke.GetComponent<LayoutElement>().ignoreLayout = true;
            var strokeRect = (RectTransform)stroke.transform;
            strokeRect.anchorMin = new Vector2(0f, 1f);
            strokeRect.anchorMax = new Vector2(1f, 1f);
            strokeRect.pivot = new Vector2(0.5f, 1f);
            strokeRect.sizeDelta = new Vector2(0f, 4f);

            var title = NewText(sheet.transform, "Waypoints", 56, FontStyle.Bold);
            title.color = UiStyle.Gold;
            Height(title.gameObject, 100f);

            var listObject = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listObject.transform.SetParent(sheet.transform, false);
            var listLayout = listObject.GetComponent<VerticalLayoutGroup>();
            listLayout.spacing = 12f;
            listLayout.childControlHeight = true;
            listLayout.childControlWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childForceExpandWidth = true;
            screen.list = (RectTransform)listObject.transform;

            var close = NewButton(sheet.transform, "Close", screen.Close);
            Height(close.gameObject, RowHeight);
            return screen;
        }

        public void Close() => gameObject.SetActive(false);

        void Render()
        {
            for (var i = list.childCount - 1; i >= 0; i--)
            {
                var child = list.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            AddRow(0, "Town", "");
            foreach (var depth in GameSession.Current.Waypoints)
                AddRow(depth, $"Level {depth}", $"   <size=30><color=#999999>enemy level {DungeonRules.EnemyLevel(depth)}</color></size>");
        }

        void AddRow(int depth, string name, string detail)
        {
            var isHere = depth == here;
            var button = NewButton(list, name + detail + (isHere ? "   <size=30><color=#999999>you are here</color></size>" : ""), () =>
            {
                Close();
                if (depth == 0)
                    SceneTravel.ToTown();
                else
                    SceneTravel.ToDepth(depth, Arrival.AtWaypoint);
            });
            Height(button.gameObject, RowHeight);
            button.interactable = !isHere;
            button.GetComponent<Image>().color = isHere ? HereColor : ButtonColor;
        }

        static Button NewButton(Transform parent, string label, Action onClick)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = ButtonColor;
            UiStyle.Rim(go);
            go.GetComponent<Button>().onClick.AddListener(() => onClick());
            var text = NewText(go.transform, label, 40, FontStyle.Bold);
            var rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return go.GetComponent<Button>();
        }

        static Text NewText(Transform parent, string content, int size, FontStyle style)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = UiStyle.Title;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = UiStyle.TextMain;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.text = content;
            return text;
        }

        static void Height(GameObject go, float height)
        {
            var element = go.TryGetComponent<LayoutElement>(out var existing) ? existing : go.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleHeight = 0f;
        }
    }
}
