using System;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The Codex (Docs/03, "The farming loop": "The Codex shows each legendary's home once it has been seen once, so a
    /// player can farm for a missing piece"): every named legendary in a scrolling list, how many are found, and for each
    /// its kind and build archetype. A found one shows its name, power and home (and whether it is worn); one not yet
    /// found shows only its kind and archetype, so the player knows a piece exists for their build. Opened from the Bag's
    /// Codex tab, over the paused Bag, in the same bottom-sheet look as the other sheets (<see cref="UiStyle"/>). Built in
    /// code the first time it opens.
    /// </summary>
    public class CodexScreen : MonoBehaviour
    {
        static CodexScreen current;

        RectTransform content;
        Text count;
        ScrollRect scroll;

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

        static CodexScreen Create()
        {
            var canvasObject = new GameObject("Codex Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(canvasObject);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 55;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight = 0.5f;

            // A dimmed backdrop that closes the sheet when tapped above it.
            var root = new GameObject("Codex", typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(canvasObject.transform, false);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            var screen = root.AddComponent<CodexScreen>();
            root.GetComponent<Button>().onClick.AddListener(screen.Close);

            var sheet = new GameObject("Sheet", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(Button));
            sheet.transform.SetParent(root.transform, false);
            sheet.GetComponent<Button>().transition = Selectable.Transition.None;
            var sheetRect = (RectTransform)sheet.transform;
            sheetRect.anchorMin = new Vector2(0f, 0f);
            sheetRect.anchorMax = new Vector2(1f, 0f);
            sheetRect.pivot = new Vector2(0.5f, 0f);
            sheetRect.sizeDelta = new Vector2(0f, 1900f);
            sheet.GetComponent<Image>().color = UiStyle.Sheet;
            var layout = sheet.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 40, 48);
            layout.spacing = 16f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var title = NewText(sheet.transform, "Codex", 56, FontStyle.Bold, TextAnchor.MiddleCenter);
            title.color = UiStyle.Gold;
            Height(title.gameObject, 90f);
            screen.count = NewText(sheet.transform, "", 34, FontStyle.Normal, TextAnchor.MiddleCenter);
            Height(screen.count.gameObject, 60f);

            // The list scrolls: 24 entries do not fit a phone's sheet.
            var viewport = new GameObject("List", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
            viewport.transform.SetParent(sheet.transform, false);
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            var viewportElement = viewport.AddComponent<LayoutElement>();
            viewportElement.flexibleHeight = 1f;

            var list = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            list.transform.SetParent(viewport.transform, false);
            var listRect = (RectTransform)list.transform;
            listRect.anchorMin = new Vector2(0f, 1f);
            listRect.anchorMax = new Vector2(1f, 1f);
            listRect.pivot = new Vector2(0.5f, 1f);
            listRect.offsetMin = listRect.offsetMax = Vector2.zero;
            var listLayout = list.GetComponent<VerticalLayoutGroup>();
            listLayout.spacing = 12f;
            listLayout.childControlHeight = true;
            listLayout.childControlWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childForceExpandWidth = true;
            list.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            screen.content = listRect;

            var scrollRect = viewport.GetComponent<ScrollRect>();
            scrollRect.content = listRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 40f;
            screen.scroll = scrollRect;

            var close = NewButton(sheet.transform, "Close", screen.Close);
            Height(close.gameObject, 168f);
            return screen;
        }

        void Render()
        {
            var session = GameSession.Current;
            var total = 0;
            var found = 0;
            foreach (var legendary in Legendaries.All)
            {
                total++;
                if (session.HasSeen(legendary.Id))
                    found++;
            }
            count.text = $"<color=#FFB45A>{found}</color> of {total} legendaries found";

            for (var i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
            // Found ones first, then the rest, each group in table order.
            foreach (var legendary in Legendaries.All)
                if (session.HasSeen(legendary.Id))
                    AddRow(legendary, true, session.Equipment.Wears(legendary.Id));
            foreach (var legendary in Legendaries.All)
                if (!session.HasSeen(legendary.Id))
                    AddRow(legendary, false, false);
            scroll.verticalNormalizedPosition = 1f;
        }

        void AddRow(LegendaryDefinition legendary, bool seen, bool worn)
        {
            var row = new GameObject(legendary.Id.ToString(), typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            row.transform.SetParent(content, false);
            row.GetComponent<Image>().color = seen ? UiStyle.RowFill : UiStyle.Locked;
            var layout = row.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 16, 16);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var kind = $"{ItemComparison.KindName(legendary.Slot)} · {legendary.Archetype}";
            if (DevGive)
                AddGiveButton(row.transform, legendary);
            if (seen)
            {
                var color = ColorUtility.ToHtmlStringRGB(LootColors.Of(ItemRarity.Legendary));
                NewText(row.transform, $"<b><color=#{color}>{legendary.Name}</color></b>{(worn ? "  <color=#7FD07F>worn</color>" : "")}",
                    38, FontStyle.Normal, TextAnchor.MiddleLeft);
                NewText(row.transform, $"<color=#999999>{kind}</color>", 28, FontStyle.Normal, TextAnchor.MiddleLeft);
                NewText(row.transform, legendary.Power, 30, FontStyle.Normal, TextAnchor.UpperLeft);
                NewText(row.transform, $"<color=#999999>Drops most from:</color> {legendary.HomeText}", 28, FontStyle.Normal, TextAnchor.MiddleLeft);
            }
            else
            {
                NewText(row.transform, "<color=#777777><b>Not yet found</b></color>", 34, FontStyle.Normal, TextAnchor.MiddleLeft);
                NewText(row.transform, $"<color=#777777>{kind}</color>", 28, FontStyle.Normal, TextAnchor.MiddleLeft);
            }
        }

        // Development builds and the editor only: a button per entry that puts that legendary in the backpack, at the
        // character's level (at least the legendaries' minimum), so a power can be tried without farming for it.
        static bool DevGive => Debug.isDebugBuild;

        static readonly System.Random DevRandom = new System.Random();

        void AddGiveButton(Transform row, LegendaryDefinition legendary)
        {
            var button = NewButton(row, "DEV: give", () =>
            {
                var session = GameSession.Current;
                var item = Legendaries.Create(legendary.Id, Mathf.Max(Legendaries.MinItemLevel, session.Level), DevRandom);
                if (!session.PickUp(item))
                {
                    count.text = "<color=#FF6060>The backpack is full</color>";
                    return;
                }
                Render();
            });
            var text = button.GetComponentInChildren<Text>();
            text.fontSize = 30;
            text.color = new Color(1f, 0.6f, 0.2f);
            Height(button.gameObject, 64f);
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
            text.verticalOverflow = VerticalWrapMode.Overflow;
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
    }
}
