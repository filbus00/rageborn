using System;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The passive tree (Docs/02, Passive tree; Docs/06 proposes three columns that scroll together). Shown as three
    /// tabs, Wrath, Stampede and Scar, each a scrolling list of its branch in order, a row per node with its name, what
    /// it does and a button: Buy, or why it cannot be bought yet (the node before it, the gateway's 15 points, the
    /// keystone's level 20, points). A bought keystone offers to become the active one (Q5: both can be bought, one is in
    /// effect). A list rather than a drawn tree is Claude's choice for one thumb, 2026-09-28. Built in code the first time
    /// it opens, over the paused Bag. Placeholder look until the UI art exists.
    /// </summary>
    public class PassiveTreeScreen : MonoBehaviour
    {
        static readonly Color SheetColor = new Color(0.09f, 0.08f, 0.08f, 0.98f);
        static readonly Color StrokeColor = new Color(0.9f, 0.45f, 0.2f);
        static readonly Color ButtonColor = new Color(1f, 1f, 1f, 0.16f);
        static readonly Color TabActiveColor = new Color(0.9f, 0.45f, 0.2f, 0.45f);
        static readonly Color BoughtColor = new Color(0.9f, 0.45f, 0.2f, 0.18f);
        static readonly Color RowColor = new Color(1f, 1f, 1f, 0.05f);

        static PassiveTreeScreen current;

        RectTransform content;
        ScrollRect scroll;
        Text points;
        Button[] tabs;
        PassiveBranch branch = PassiveBranch.Wrath;

        public static bool IsOpen => current != null && current.gameObject.activeSelf;

        public static void Open()
        {
            if (current == null)
                current = Create();
            current.gameObject.SetActive(true);
            current.Render(true);
        }

        public static void CloseIfOpen()
        {
            if (IsOpen)
                current.Close();
        }

        public void Close() => gameObject.SetActive(false);

        static PassiveTreeScreen Create()
        {
            var canvasObject = new GameObject("Passive Tree Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(canvasObject);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 55;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = new GameObject("Passive Tree", typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(canvasObject.transform, false);
            Stretch((RectTransform)root.transform);
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            var screen = root.AddComponent<PassiveTreeScreen>();
            root.GetComponent<Button>().onClick.AddListener(screen.Close);

            var sheet = new GameObject("Sheet", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(Button));
            sheet.transform.SetParent(root.transform, false);
            sheet.GetComponent<Button>().transition = Selectable.Transition.None;
            var sheetRect = (RectTransform)sheet.transform;
            sheetRect.anchorMin = new Vector2(0f, 0f);
            sheetRect.anchorMax = new Vector2(1f, 0f);
            sheetRect.pivot = new Vector2(0.5f, 0f);
            sheetRect.sizeDelta = new Vector2(0f, 2100f);
            sheet.GetComponent<Image>().color = SheetColor;
            var layout = sheet.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 40, 48);
            layout.spacing = 16f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var title = NewText(sheet.transform, "Passive Tree", 56, FontStyle.Bold, TextAnchor.MiddleCenter);
            title.color = StrokeColor;
            Height(title.gameObject, 90f);
            screen.points = NewText(sheet.transform, "", 34, FontStyle.Normal, TextAnchor.MiddleCenter);
            Height(screen.points.gameObject, 60f);

            var tabRow = new GameObject("Tabs", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabRow.transform.SetParent(sheet.transform, false);
            var tabLayout = tabRow.GetComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 12f;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = true;
            tabLayout.childForceExpandWidth = true;
            tabLayout.childForceExpandHeight = true;
            Height(tabRow, 150f);
            var branches = new[] { PassiveBranch.Wrath, PassiveBranch.Stampede, PassiveBranch.Scar };
            screen.tabs = new Button[branches.Length];
            for (var i = 0; i < branches.Length; i++)
            {
                var chosen = branches[i];
                screen.tabs[i] = NewButton(tabRow.transform, chosen.ToString(), () =>
                {
                    screen.branch = chosen;
                    screen.Render(true);
                });
            }

            // A scrolling list: a masked viewport and a content that grows with its rows.
            var scrollObject = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
            scrollObject.transform.SetParent(sheet.transform, false);
            scrollObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            var scrollElement = scrollObject.AddComponent<LayoutElement>();
            scrollElement.flexibleHeight = 1f;
            var contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(scrollObject.transform, false);
            var contentRect = (RectTransform)contentObject.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = Vector2.zero;
            var contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 10f;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            screen.scroll = scrollObject.GetComponent<ScrollRect>();
            screen.scroll.content = contentRect;
            screen.scroll.horizontal = false;
            screen.scroll.movementType = ScrollRect.MovementType.Clamped;
            screen.content = contentRect;

            var close = NewButton(sheet.transform, "Close", screen.Close);
            Height(close.gameObject, 168f);
            return screen;
        }

        void Render(bool toTop)
        {
            var session = GameSession.Current;
            var tree = session.PassiveTree;
            var level = session.Level;
            var available = tree.Available(level);
            points.text = available > 0 ? $"<color=#FFB45A>{available} passive point{(available == 1 ? "" : "s")} to spend</color>"
                : "<color=#999999>A point every level</color>";
            var branches = new[] { PassiveBranch.Wrath, PassiveBranch.Stampede, PassiveBranch.Scar };
            for (var i = 0; i < tabs.Length; i++)
                tabs[i].GetComponent<Image>().color = branches[i] == branch ? TabActiveColor : ButtonColor;

            for (var i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
            foreach (var node in PassiveTree.Nodes)
            {
                if (node.Branch != branch)
                    continue;
                AddRow(node, tree, level);
            }
            if (toTop)
            {
                Canvas.ForceUpdateCanvases();
                scroll.verticalNormalizedPosition = 1f;
            }
        }

        void AddRow(PassiveNode node, PassiveTree tree, int level)
        {
            var row = new GameObject(node.Id, typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(content, false);
            var bought = tree.IsBought(node.Id);
            row.GetComponent<Image>().color = bought ? BoughtColor : RowColor;
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 12, 8, 8);
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            var big = node.Kind == PassiveKind.Notable || node.Kind == PassiveKind.Keystone || node.Kind == PassiveKind.Gateway;
            Height(row, big ? 150f : 120f);

            var nameColor = node.Kind == PassiveKind.Keystone ? "#FF6A3D" : node.Kind == PassiveKind.Notable ? "#FFB45A" : "#FFFFFF";
            var label = NewText(row.transform, $"<color={nameColor}><b>{node.Name}</b></color>\n<size=28><color=#CCCCCC>{node.Text}</color></size>",
                34, FontStyle.Normal, TextAnchor.MiddleLeft);
            Width(label.gameObject, 0f, 1f);

            string action;
            Action onClick = null;
            var interactable = false;
            if (bought && node.Kind == PassiveKind.Keystone)
            {
                var active = tree.ActiveKeystone == node.Id;
                action = active ? "Active" : "Make active";
                interactable = !active;
                onClick = () => tree.SetActiveKeystone(node.Id);
            }
            else if (bought)
                action = "Bought";
            else
            {
                var blocker = tree.Blocker(node, level);
                action = blocker == null ? (node.Cost > 1 ? $"Buy ({node.Cost})" : "Buy") : blocker;
                interactable = blocker == null;
                onClick = () => tree.Buy(node.Id, level);
            }
            var button = NewButton(row.transform, action, () =>
            {
                onClick?.Invoke();
                Render(false);
            });
            Width(button.gameObject, 300f, 0f);
            button.interactable = interactable;
            button.GetComponentInChildren<Text>().fontSize = 28;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static Button NewButton(Transform parent, string label, Action onClick)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = ButtonColor;
            go.GetComponent<Button>().onClick.AddListener(() => onClick());
            var text = NewText(go.transform, label, 40, FontStyle.Bold, TextAnchor.MiddleCenter);
            var rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(10f, 0f);
            rect.offsetMax = new Vector2(-10f, 0f);
            return go.GetComponent<Button>();
        }

        static Text NewText(Transform parent, string content, int size, FontStyle style, TextAnchor alignment)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
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

        static void Width(GameObject go, float width, float flexible)
        {
            var element = go.TryGetComponent<LayoutElement>(out var existing) ? existing : go.AddComponent<LayoutElement>();
            element.minWidth = width;
            element.preferredWidth = width;
            element.flexibleWidth = flexible;
        }
    }
}
