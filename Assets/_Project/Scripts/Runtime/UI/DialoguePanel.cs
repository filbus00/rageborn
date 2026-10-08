using System;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// A conversation (2026-10-08, the owner: dialogue panels): a sheet at the bottom of the screen with the speaker's
    /// portrait (their baked idle, facing the camera) and name, one line at a time; a tap on the sheet or on Next moves
    /// on, and the last line shows the answers (Accept and Not now, or one button). It does not pause the game: it is
    /// opened in town.
    /// </summary>
    public class DialoguePanel : TownSheets
    {
        static DialoguePanel instance;

        Image portrait;
        Text speakerName;
        Text line;
        Button next;
        Button accept;
        Button decline;
        Text acceptText;
        Text declineText;

        string[] lines;
        int index;
        Action onAccept;
        Action onDecline;

        public static bool IsOpen => instance != null && instance.gameObject.activeSelf;

        /// <summary>
        /// Opens a conversation. <paramref name="acceptLabel"/> is the answer that calls <paramref name="onAccept"/>; with
        /// a <paramref name="declineLabel"/> there is a second answer that only closes (and calls <paramref name="onDecline"/>).
        /// </summary>
        public static void Show(string speaker, string character, string[] lines, string acceptLabel, Action onAccept,
            string declineLabel = null, Action onDecline = null)
        {
            if (instance == null)
                instance = Build();
            instance.lines = lines;
            instance.index = 0;
            instance.onAccept = onAccept;
            instance.onDecline = onDecline;
            instance.speakerName.text = speaker;
            var sheet = character != null ? CharacterSheets.Load($"Characters/{character}/{character}_idle") : null;
            instance.portrait.sprite = sheet != null ? sheet.Rows[0][0] : null;
            instance.portrait.enabled = sheet != null;
            instance.acceptText.text = acceptLabel;
            instance.declineText.text = declineLabel ?? "";
            instance.decline.gameObject.SetActive(declineLabel != null);
            instance.gameObject.SetActive(true);
            instance.ShowLine();
        }

        protected override void Render()
        {
        }

        void ShowLine()
        {
            line.text = lines.Length > 0 ? lines[index] : "";
            var last = index >= lines.Length - 1;
            next.gameObject.SetActive(!last);
            accept.gameObject.SetActive(last);
            decline.gameObject.SetActive(last && declineText.text.Length > 0);
        }

        void Next()
        {
            if (index < lines.Length - 1)
            {
                index++;
                ShowLine();
            }
        }

        void Answer(bool yes)
        {
            gameObject.SetActive(false);
            if (yes)
                onAccept?.Invoke();
            else
                onDecline?.Invoke();
        }

        static DialoguePanel Build()
        {
            var canvasObject = new GameObject("Dialogue Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(canvasObject);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 55;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight = 0.5f;

            // A dim backdrop over the world; a tap on it moves the talk on, as a tap on the sheet does.
            var root = new GameObject("Dialogue", typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(canvasObject.transform, false);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
            var panel = root.AddComponent<DialoguePanel>();
            root.GetComponent<Button>().onClick.AddListener(panel.Next);

            var sheet = new GameObject("Sheet", typeof(RectTransform), typeof(Image), typeof(Button));
            sheet.transform.SetParent(root.transform, false);
            sheet.GetComponent<Button>().transition = Selectable.Transition.None;
            sheet.GetComponent<Button>().onClick.AddListener(panel.Next);
            var sheetRect = (RectTransform)sheet.transform;
            sheetRect.anchorMin = new Vector2(0f, 0f);
            sheetRect.anchorMax = new Vector2(1f, 0f);
            sheetRect.pivot = new Vector2(0.5f, 0f);
            sheetRect.sizeDelta = new Vector2(0f, 980f);
            sheet.GetComponent<Image>().color = UiStyle.Sheet;
            UiStyle.Leather(sheet.GetComponent<Image>());

            // The portrait, in a frame at the top left.
            var frame = new GameObject("Portrait Frame", typeof(RectTransform), typeof(Image));
            frame.transform.SetParent(sheet.transform, false);
            frame.GetComponent<Image>().color = UiStyle.EmptySlotFill;
            UiStyle.Rim(frame);
            var frameRect = (RectTransform)frame.transform;
            frameRect.anchorMin = frameRect.anchorMax = new Vector2(0f, 1f);
            frameRect.pivot = new Vector2(0f, 1f);
            frameRect.anchoredPosition = new Vector2(40f, -40f);
            frameRect.sizeDelta = new Vector2(300f, 360f);
            var portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portraitObject.transform.SetParent(frame.transform, false);
            panel.portrait = portraitObject.GetComponent<Image>();
            panel.portrait.preserveAspect = true;
            panel.portrait.raycastTarget = false;
            var portraitRect = (RectTransform)portraitObject.transform;
            portraitRect.anchorMin = new Vector2(0.05f, 0.04f);
            portraitRect.anchorMax = new Vector2(0.95f, 0.96f);
            portraitRect.offsetMin = portraitRect.offsetMax = Vector2.zero;

            panel.speakerName = Label(sheet.transform, "", 48, FontStyle.Bold, TextAnchor.UpperLeft);
            panel.speakerName.color = UiStyle.Gold;
            Place(panel.speakerName.rectTransform, new Vector2(370f, -50f), new Vector2(-40f, 80f));

            panel.line = Label(sheet.transform, "", 40, FontStyle.Normal, TextAnchor.UpperLeft);
            panel.line.lineSpacing = 1.15f;
            Place(panel.line.rectTransform, new Vector2(370f, -140f), new Vector2(-40f, 520f));

            panel.next = Button(sheet.transform, "Next", panel.Next, 40);
            Corner((RectTransform)panel.next.transform, 1f, new Vector2(360f, 140f));
            panel.accept = Button(sheet.transform, "Accept", () => panel.Answer(true), 40);
            Corner((RectTransform)panel.accept.transform, 1f, new Vector2(360f, 140f));
            panel.acceptText = panel.accept.GetComponentInChildren<Text>();
            panel.decline = Button(sheet.transform, "Not now", () => panel.Answer(false), 40);
            Corner((RectTransform)panel.decline.transform, 0f, new Vector2(360f, 140f));
            panel.declineText = panel.decline.GetComponentInChildren<Text>();

            root.SetActive(false);
            return panel;
        }

        // A text's rectangle from the sheet's top left: x in from the left, y down; size is (right inset, height).
        static void Place(RectTransform rect, Vector2 topLeft, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.offsetMin = new Vector2(topLeft.x, topLeft.y - size.y);
            rect.offsetMax = new Vector2(size.x, topLeft.y);
        }

        // A button at the sheet's bottom left (x 0) or right (x 1).
        static void Corner(RectTransform rect, float x, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(x, 0f);
            rect.pivot = new Vector2(x, 0f);
            rect.anchoredPosition = new Vector2(x > 0.5f ? -40f : 40f, 56f);
            rect.sizeDelta = size;
        }
    }
}
