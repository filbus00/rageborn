using System;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The talent trees (2026-10-07, in place of the loadout): a bottom sheet over the Bag with a tab for each of the
    /// three trees, the tree's rows from the first (top) to the capstone, its talents with their ranks, and below them the
    /// chosen talent's text, what stands in its way and a Learn button. Learned skills fire on their own; there is no
    /// loadout. Changes go straight into <see cref="GameSession.Talents"/>, which is saved. Built in code the first time it
    /// opens; the Bag has paused the game.
    /// </summary>
    public class TalentScreen : MonoBehaviour
    {
        static readonly Color SheetColor = UiStyle.Sheet;
        static readonly Color StrokeColor = UiStyle.Blood;
        static readonly Color ButtonColor = UiStyle.ButtonFill;
        static readonly Color LockedColor = UiStyle.Locked;
        static readonly Color LearnedColor = new Color(0.42f, 0.3f, 0.1f, 1f);
        static readonly Color ChosenColor = new Color(0.55f, 0.18f, 0.12f, 1f);

        const float CellHeight = 150f;

        static TalentScreen current;

        Text hint;
        RectTransform tabs;
        RectTransform grid;
        Text detail;
        Button learn;
        TalentTree tree = TalentTree.Marksmanship;
        string chosen;

        public static bool IsOpen => current != null && current.gameObject.activeSelf;

        public static void Open()
        {
            if (current == null)
                current = Create();
            current.gameObject.SetActive(true);
            current.Render();
        }

        public void Close() => gameObject.SetActive(false);

        /// <summary>Closes the trees if they are open (the Bag closing takes them along).</summary>
        public static void CloseIfOpen()
        {
            if (IsOpen)
                current.Close();
        }

        static TalentScreen Create()
        {
            var canvasObject = new GameObject("Talent Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(canvasObject);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 55;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = new GameObject("Talents", typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(canvasObject.transform, false);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            var screen = root.AddComponent<TalentScreen>();
            root.GetComponent<Button>().onClick.AddListener(screen.Close);

            var sheet = new GameObject("Sheet", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(Button));
            sheet.transform.SetParent(root.transform, false);
            // The sheet swallows taps so only the dimmed area above it closes.
            sheet.GetComponent<Button>().transition = Selectable.Transition.None;
            var sheetRect = (RectTransform)sheet.transform;
            sheetRect.anchorMin = new Vector2(0f, 0f);
            sheetRect.anchorMax = new Vector2(1f, 0f);
            sheetRect.pivot = new Vector2(0.5f, 0f);
            sheetRect.sizeDelta = new Vector2(0f, 2150f);
            sheet.GetComponent<Image>().color = SheetColor;
            UiStyle.Leather(sheet.GetComponent<Image>());
            var layout = sheet.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(36, 36, 36, 44);
            layout.spacing = 14f;
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

            var title = NewText(sheet.transform, "Talents", 56, FontStyle.Bold);
            title.color = UiStyle.Gold;
            Height(title.gameObject, 80f);
            screen.hint = NewText(sheet.transform, "", 32, FontStyle.Normal);
            screen.hint.color = new Color(1f, 1f, 1f, 0.75f);
            Height(screen.hint.gameObject, 80f);

            screen.tabs = Row(sheet.transform, 120f);
            var gridObject = new GameObject("Grid", typeof(RectTransform), typeof(VerticalLayoutGroup));
            gridObject.transform.SetParent(sheet.transform, false);
            var gridLayout = gridObject.GetComponent<VerticalLayoutGroup>();
            gridLayout.spacing = 10f;
            gridLayout.childControlHeight = true;
            gridLayout.childControlWidth = true;
            gridLayout.childForceExpandHeight = false;
            gridLayout.childForceExpandWidth = true;
            screen.grid = (RectTransform)gridObject.transform;

            screen.detail = NewText(sheet.transform, "", 34, FontStyle.Normal);
            Height(screen.detail.gameObject, 230f);
            var buttons = Row(sheet.transform, 150f);
            screen.learn = NewButton(buttons, "Learn", screen.LearnChosen);
            Width(screen.learn.gameObject, 0f, 1f);
            var close = NewButton(buttons, "Close", screen.Close);
            Width(close.gameObject, 0f, 1f);
            return screen;
        }

        void Render()
        {
            var session = GameSession.Current;
            var talents = session.Talents;
            var points = talents.Available(session.Level);
            hint.text = (points > 0 ? $"<color=#FFB45A>{points} talent point{(points == 1 ? "" : "s")} to spend.</color> " : "No points to spend: one comes with every level. ") +
                        $"A row opens with {TalentRules.PointsPerTier} points in its tree. Learned skills fire on their own.";

            Clear(tabs);
            foreach (TalentTree each in Enum.GetValues(typeof(TalentTree)))
            {
                var which = each;
                var tab = NewButton(tabs, $"{TalentRules.TreeName(each)}\n<size=28>{talents.PointsIn(each)} points</size>", () =>
                {
                    tree = which;
                    chosen = null;
                    Render();
                });
                tab.GetComponentInChildren<Text>().fontSize = 34;
                tab.GetComponent<Image>().color = each == tree ? ChosenColor : ButtonColor;
                Width(tab.gameObject, 0f, 1f);
            }

            Clear(grid);
            for (var tier = 0; tier < TalentRules.Tiers; tier++)
            {
                var row = Row(grid, CellHeight);
                for (var column = 0; column < 3; column++)
                {
                    var node = Find(tree, tier, column);
                    if (node == null)
                    {
                        var gap = new GameObject("Gap", typeof(RectTransform));
                        gap.transform.SetParent(row, false);
                        Width(gap, 0f, 1f);
                        continue;
                    }
                    var id = node.Id;
                    var rank = talents.RankOf(id);
                    var label = $"{node.Name}\n<size=26>{(node.IsSkill ? "Skill" : "Talent")}  {rank}/{node.MaxRanks}</size>";
                    var cell = NewButton(row, label, () =>
                    {
                        chosen = id;
                        Render();
                    });
                    cell.GetComponentInChildren<Text>().fontSize = 30;
                    var open = talents.CanLearn(node, session.Level) || rank > 0;
                    cell.GetComponent<Image>().color = id == chosen ? ChosenColor : rank > 0 ? LearnedColor : open ? ButtonColor : LockedColor;
                    Width(cell.gameObject, 0f, 1f);
                }
            }

            var picked = TalentRules.Get(chosen);
            if (picked == null)
            {
                detail.text = "Tap a talent to see what it does.";
                learn.interactable = false;
                return;
            }
            var have = talents.RankOf(picked.Id);
            var blocker = talents.Blocker(picked, session.Level);
            var text = $"<b>{picked.Name}</b>  <size=28>({TalentRules.TreeName(picked.Tree)}, rank {have}/{picked.MaxRanks})</size>\n" +
                       TalentRules.Describe(picked, have);
            if (have > 0 && have < picked.MaxRanks)
                text += $"\n<size=28><color=#BBBBBB>Next rank: {TalentRules.Describe(picked, have + 1)}</color></size>";
            if (blocker != null && have < picked.MaxRanks)
                text += $"\n<size=28><color=#E08A6A>{blocker}</color></size>";
            detail.text = text;
            learn.interactable = blocker == null;
        }

        void LearnChosen()
        {
            var session = GameSession.Current;
            if (chosen != null && session.Talents.Learn(chosen, session.Level))
                Sfx.Play(SoundId.Hint);
            Render();
        }

        static TalentNode Find(TalentTree tree, int tier, int column)
        {
            foreach (var node in TalentRules.Nodes)
                if (node.Tree == tree && node.Tier == tier && node.Column == column)
                    return node;
            return null;
        }

        static void Clear(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
        }

        static RectTransform Row(Transform parent, float height)
        {
            var row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            Height(row, height);
            return (RectTransform)row.transform;
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
            rect.offsetMin = new Vector2(10f, 0f);
            rect.offsetMax = new Vector2(-10f, 0f);
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
