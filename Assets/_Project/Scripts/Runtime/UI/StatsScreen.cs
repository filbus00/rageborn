using System;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The stats page (Docs/02, decided 2026-09-30: "No skill trees, add stat points to spend on attributes"): the five
    /// attributes, each a row with its value (10 plus the points spent), what a point gives, and a + button while there
    /// are points to spend. It takes the passive tree's place in the Bag's tabs. Built in code the first time it opens,
    /// over the paused Bag, in the same bottom-sheet look as the other sheets (<see cref="UiStyle"/>).
    /// </summary>
    public class StatsScreen : MonoBehaviour
    {
        static StatsScreen current;

        RectTransform content;
        Text points;

        public static bool IsOpen => current != null && current.gameObject.activeSelf;

        public static void Open()
        {
            if (current == null)
                current = Create();
            current.gameObject.SetActive(true);
            current.Render();
        }

        public static void CloseIfOpen()
        {
            if (IsOpen)
                current.Close();
        }

        public void Close() => gameObject.SetActive(false);

        static StatsScreen Create()
        {
            var canvasObject = new GameObject("Stats Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(canvasObject);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 55;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight = 0.5f;

            // A dimmed backdrop that closes the sheet when tapped above it.
            var root = new GameObject("Stats", typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(canvasObject.transform, false);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            var screen = root.AddComponent<StatsScreen>();
            root.GetComponent<Button>().onClick.AddListener(screen.Close);

            var sheet = new GameObject("Sheet", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(Button));
            sheet.transform.SetParent(root.transform, false);
            sheet.GetComponent<Button>().transition = Selectable.Transition.None;
            var sheetRect = (RectTransform)sheet.transform;
            sheetRect.anchorMin = new Vector2(0f, 0f);
            sheetRect.anchorMax = new Vector2(1f, 0f);
            sheetRect.pivot = new Vector2(0.5f, 0f);
            sheetRect.sizeDelta = new Vector2(0f, 1560f);
            sheet.GetComponent<Image>().color = UiStyle.Sheet;
            UiStyle.Leather(sheet.GetComponent<Image>());
            var layout = sheet.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 40, 48);
            layout.spacing = 16f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var title = NewText(sheet.transform, "Attributes", 56, FontStyle.Bold, TextAnchor.MiddleCenter);
            title.color = UiStyle.Gold;
            Height(title.gameObject, 90f);
            screen.points = NewText(sheet.transform, "", 34, FontStyle.Normal, TextAnchor.MiddleCenter);
            Height(screen.points.gameObject, 60f);

            var list = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup));
            list.transform.SetParent(sheet.transform, false);
            var listLayout = list.GetComponent<VerticalLayoutGroup>();
            listLayout.spacing = 12f;
            listLayout.childControlHeight = true;
            listLayout.childControlWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childForceExpandWidth = true;
            var listElement = list.AddComponent<LayoutElement>();
            listElement.flexibleHeight = 1f;
            screen.content = (RectTransform)list.transform;

            var close = NewButton(sheet.transform, "Close", screen.Close);
            Height(close.gameObject, 168f);
            return screen;
        }

        void Render()
        {
            var session = GameSession.Current;
            var level = session.Level;
            var available = session.Attributes.Available(level);
            points.text = available > 0
                ? $"<color=#FFB45A>{available} stat point{(available == 1 ? "" : "s")} to spend</color>"
                : "<color=#999999>5 points every level</color>";

            for (var i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
            var bonuses = session.AttributeBonuses;
            foreach (Attribute attribute in Enum.GetValues(typeof(Attribute)))
                AddRow(attribute, bonuses.Shown(attribute), available > 0, level);
        }

        void AddRow(Attribute attribute, int value, bool canSpend, int level)
        {
            var row = new GameObject(attribute.ToString(), typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(content, false);
            row.GetComponent<Image>().color = UiStyle.RowFill;
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 12, 8, 8);
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            Height(row, 170f);

            var label = NewText(row.transform,
                $"<b>{attribute}</b>  <color=#FFB45A>{value}</color>\n<size=28><color=#CCCCCC>Each point: {CharacterAttributes.Describe(attribute)}</color></size>",
                38, FontStyle.Normal, TextAnchor.MiddleLeft);
            Width(label.gameObject, 0f, 1f);

            var button = NewButton(row.transform, "+", () =>
            {
                GameSession.Current.Attributes.Spend(attribute, level);
                Render();
            });
            Width(button.gameObject, 150f, 0f);
            button.interactable = canSpend;
        }

        static Button NewButton(Transform parent, string label, Action onClick)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = UiStyle.ButtonFill;
            UiStyle.Rim(go);
            go.GetComponent<Button>().onClick.AddListener(() => onClick());
            var text = NewText(go.transform, label, 48, FontStyle.Bold, TextAnchor.MiddleCenter);
            var rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(10f, 0f);
            rect.offsetMax = new Vector2(-10f, 0f);
            return go.GetComponent<Button>();
        }

        static Text NewText(Transform parent, string contents, int size, FontStyle style, TextAnchor alignment)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = UiStyle.Title;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = UiStyle.TextMain;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.text = contents;
            return text;
        }

        static void Height(GameObject go, float height)
        {
            var element = go.TryGetComponent<LayoutElement>(out var existing) ? existing : go.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleHeight = 0f;
        }

        static void Width(GameObject go, float width, float flexible)
        {
            var element = go.TryGetComponent<LayoutElement>(out var existing) ? existing : go.AddComponent<LayoutElement>();
            element.minWidth = width;
            element.preferredWidth = width;
            element.flexibleWidth = flexible;
        }
    }
}
