using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The run summary (2026-10-15): when a run ends, the company's hub opens on what it came to (<see cref="RunSummary"/>):
    /// how it ended, the depth, time, kills, coins, items and blueprints, and the boons she had. In the company menu's
    /// look since 2026-11-08 (the owner: "the menu that appears when you portal home is not the correct style"): the
    /// backdrop and the screen's frame, the title on the gold plaque, the numbers in a framed panel with icons and rules,
    /// the boons with their pictures, and Continue at the bottom (it was a town sheet in the pixel fonts).
    /// </summary>
    public class RunSummaryScreen : MonoBehaviour
    {
        const float Edge = 6f;
        const float PlaqueHeight = 84f;
        const float ButtonHeight = 40f;

        static RunSummaryScreen current;
        RectTransform content;

        public static bool IsOpen => current != null;

        /// <summary>Puts the summary away (the autopilot's runs).</summary>
        public static void CloseOpen()
        {
            if (current != null)
                Destroy(current.gameObject);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => current = null;

        public static void Open(RunSummary summary)
        {
            if (summary == null)
                return;
            if (current != null)
                Destroy(current.gameObject);
            var go = new GameObject("Run Summary");
            current = go.AddComponent<RunSummaryScreen>();
            current.Build(summary);
            Sfx.Play(summary.End == RunEnd.Died ? SoundId.Death : SoundId.LevelUp, 0.6f);
        }

        void OnDestroy()
        {
            if (current == this)
                current = null;
        }

        void Close()
        {
            Sfx.Play(SoundId.Hint);
            Destroy(gameObject);
        }

        void Build(RunSummary summary)
        {
            // Over the company menu (40) and the hub's HUD, under the fade (100).
            var canvas = PixelUi.MakeCanvas(transform, "Run Summary Canvas", 50);
            var root = UiStyle.Rect(canvas.transform, "Root");
            UiStyle.Stretch(root);
            var backdrop = PixelUi.Tiled(root, "Backdrop", "backdrop");
            backdrop.raycastTarget = true;
            UiStyle.Stretch(backdrop.rectTransform);
            var safe = UiStyle.Rect(root, "Frame");
            UiStyle.Stretch(safe);
            var body = UiStyle.Rect(safe, "Body");
            UiStyle.Stretch(body);
            SafeArea.WrapChildren(safe);
            UiStyle.Stretch(PixelUi.Frame(root, "Screen Edge", "frame_screen_b4").rectTransform, 1f);

            // The title plaque: how it ended, and the archer's name under it.
            var died = summary.End == RunEnd.Died;
            var plaque = PixelUi.Frame(body, "Title", "frame_plaque_b5").rectTransform;
            PixelUi.Across(plaque, Edge, PlaqueHeight, Edge, Edge);
            // The title in the big letters, on two lines when it needs them.
            var title = PixelUi.Label(plaque, summary.Title.ToUpperInvariant(), PixelUi.TitleBigFont, died ? PixelUi.Red : PixelUi.Gold,
                TextAnchor.UpperCenter);
            PixelUi.Across(title.rectTransform, 8f, 56f, 16f, 16f);
            var who = PixelUi.Label(plaque, (summary.Mercenary ?? "").ToUpperInvariant(), PixelUi.BodyFont, PixelUi.Gold, TextAnchor.UpperCenter);
            PixelUi.Across(who.rectTransform, 66f, 10f, 0f, 0f);
            var half = who.preferredWidth / 2f;
            UiStyle.Place(PixelUi.Picture(plaque, "Feather Left", "small_arrow").rectTransform, new Vector2(0.5f, 1f),
                new Vector2(-half - 12f, -69f), new Vector2(11f, 3f));
            var right = PixelUi.Picture(plaque, "Feather Right", "small_arrow").rectTransform;
            UiStyle.Place(right, new Vector2(0.5f, 1f), new Vector2(half + 12f, -69f), new Vector2(11f, 3f));
            right.localScale = new Vector3(-1f, 1f, 1f);

            // The page, scrolling between the plaque and Continue.
            var viewport = UiStyle.Rect(body, "Page");
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(Edge, Edge + ButtonHeight + 6f);
            viewport.offsetMax = new Vector2(-Edge, -(Edge + PlaqueHeight + 6f));
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            content = UiStyle.Rect(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 12f;

            // How it ended, in words.
            var story = Stack(content, "Story", "frame_card_b3", 9, 3f);
            var headline = PixelUi.Label(story, summary.Headline, PixelUi.BodyFont, PixelUi.Text);
            headline.lineSpacing = 1.1f;

            // The numbers.
            var stats = Stack(content, "Numbers", "frame_panel_b4", 9, 2f);
            Heading(stats, "icon_scroll", "THE RUN");
            var s = summary.Stats;
            Row(stats, "icon_key", "Depth reached", (summary.NewRecord ? "<color=#DEB258>new deepest</color>  " : "") + $"{summary.Depth}");
            Row(stats, "icon_eye", "Deepest ever", $"{summary.DeepestEver}");
            Row(stats, "icon_scroll", "Time", RunSummary.Duration(s.Seconds));
            Row(stats, "icon_skull", "Enemies slain", $"{s.Kills:N0}" + (s.Bosses > 0 ? $"  <color=#CC342C>·</color>  {s.Bosses} boss{(s.Bosses > 1 ? "es" : "")}" : ""));
            Row(stats, "icon_coin", "Coins found", $"{s.Coins:N0}");
            Row(stats, "icon_coin", "Coins in the chest", $"{summary.CoinsNow:N0}", PixelUi.Gold);
            Row(stats, "icon_bag", "Items found", $"{s.Items}");
            if (!died)
                Row(stats, "icon_bag", "Brought home", $"{summary.ItemsHome}");
            if (s.Blueprints > 0 || summary.BlueprintsLearned > 0)
            {
                Row(stats, "icon_hammer", "Blueprints found", $"{s.Blueprints}");
                Row(stats, "icon_hammer", died ? "Blueprints lost" : "New for Thorin", died ? $"{s.Blueprints}" : $"{summary.BlueprintsLearned}");
            }

            // The boons she took, with their pictures.
            var boons = Stack(content, "Boons", "frame_panel_b4", 9, 2f);
            Heading(boons, "icon_shield", summary.Boons.Count > 0 ? $"BOONS ({summary.Boons.Count})" : "NO BOONS TAKEN");
            foreach (var (name, rank) in summary.Boons)
            {
                TalentNode node = null;
                foreach (var each in TalentRules.Nodes)
                    if (each.Name == name)
                        node = each;
                BoonRow(boons, node != null ? BoonBar.SpriteFor(node) : null, name, node != null && node.MaxRanks > 1 ? $"{rank} of {node.MaxRanks}" : "");
            }

            var note = Stack(content, "Note", null, 2, 0f);
            PixelUi.Label(note, "Coins buy the quarters' upgrades. Elara's vault keeps what you put in it.", PixelUi.BodyFont, PixelUi.TextDim,
                TextAnchor.UpperCenter);

            // Continue.
            var frame = PixelUi.Frame(body, "Continue", "frame_button_b5", true);
            var rect = frame.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(Edge, Edge);
            rect.offsetMax = new Vector2(-Edge, Edge + ButtonHeight);
            var button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.onClick.AddListener(Close);
            var label = PixelUi.Label(frame.transform, "CONTINUE", PixelUi.TitleFont, PixelUi.Gold, TextAnchor.MiddleCenter);
            UiStyle.Stretch(label.rectTransform);
            label.rectTransform.anchoredPosition += new Vector2(0f, 1f);
        }

        // A framed card whose rows stack inside it and size it.
        static RectTransform Stack(Transform parent, string name, string frame, int pad, float spacing)
        {
            var rect = frame != null ? PixelUi.Frame(parent, name, frame).rectTransform : UiStyle.Rect(parent, name);
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(pad, pad, pad, pad);
            layout.spacing = spacing;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            return rect;
        }

        static RectTransform Line(Transform parent, string name, float height)
        {
            var rect = UiStyle.Rect(parent, name);
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = height;
            return rect;
        }

        // A panel's heading: an icon, the words in the title font, a dim rule after them.
        static void Heading(Transform panel, string icon, string words)
        {
            var head = Line(panel, "Heading", 18f);
            var picture = PixelUi.Picture(head, "Icon", icon).rectTransform;
            UiStyle.Place(picture, new Vector2(0f, 1f), new Vector2(0f, -1f), picture.sizeDelta);
            var title = PixelUi.Label(head, words, PixelUi.TitleFont, PixelUi.Gold);
            PixelUi.At(title.rectTransform, 16f, 2f, 300f, 12f);
            var rule = PixelUi.Tiled(head, "Rule", "rule_dim").rectTransform;
            PixelUi.Across(rule, 16f, 1f, 0f, 0f);
        }

        // A number: its icon and name on the left, the value on the right, a dim rule under it.
        static void Row(Transform panel, string icon, string name, string value, Color? valueColor = null)
        {
            var row = Line(panel, name, 15f);
            var picture = PixelUi.Picture(row, "Icon", icon).rectTransform;
            UiStyle.Place(picture, new Vector2(0f, 1f), new Vector2(0f, -1f), new Vector2(9f, 9f));
            var left = PixelUi.Label(row, name, PixelUi.BodyFont, PixelUi.TextDim);
            PixelUi.Across(left.rectTransform, 1f, 11f, 14f, 0f);
            var right = PixelUi.Label(row, value, PixelUi.BodyFont, valueColor ?? PixelUi.Text, TextAnchor.UpperRight);
            right.horizontalOverflow = HorizontalWrapMode.Overflow;
            PixelUi.Across(right.rectTransform, 1f, 11f, 100f, 0f);
            var rule = PixelUi.Tiled(row, "Rule", "rule_dim").rectTransform;
            PixelUi.Across(rule, 13f, 1f, 0f, 0f);
        }

        // A boon: its picture in an inset, its name, its ranks.
        static void BoonRow(Transform panel, Sprite icon, string name, string ranks)
        {
            var row = PixelUi.Frame(panel, name, "frame_inset_b3").rectTransform;
            var element = row.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = 24f;
            if (icon != null)
            {
                var picture = UiStyle.Image(row, "Icon", Color.white);
                picture.sprite = icon;
                picture.preserveAspect = true;
                UiStyle.Place(picture.rectTransform, new Vector2(0f, 0.5f), new Vector2(13f, 0f), new Vector2(18f, 18f));
                picture.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }
            var label = PixelUi.Label(row, name, PixelUi.BodyFont, PixelUi.Text);
            PixelUi.Across(label.rectTransform, 7f, 11f, 28f, 60f);
            if (ranks.Length > 0)
            {
                var rank = PixelUi.Label(row, ranks, PixelUi.BodyFont, PixelUi.GoldDim, TextAnchor.UpperRight);
                PixelUi.Across(rank.rectTransform, 7f, 11f, 140f, 7f);
            }
        }
    }
}
