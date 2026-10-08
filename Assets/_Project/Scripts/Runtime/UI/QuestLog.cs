using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The Bag's Quests tab (2026-10-08): the quests under way, each with its steps (the done ones dimmed, the current
    /// one bright), then the finished quests with what each gave the town.
    /// </summary>
    public class QuestLogScreen : TownSheets
    {
        static QuestLogScreen instance;

        public static bool IsOpen => instance != null && instance.gameObject.activeSelf;

        public static void Open()
        {
            if (instance == null)
            {
                instance = Make<QuestLogScreen>("Quests");
                // Over the Bag, which it is opened from.
                instance.GetComponentInParent<Canvas>().sortingOrder = 52;
            }
            instance.Show();
        }

        public static void CloseIfOpen()
        {
            if (IsOpen)
                instance.Close();
        }

        protected override void Render()
        {
            var quests = GameSession.Current.Quests;
            var any = false;
            foreach (var main in new[] { true, false })
                foreach (var quest in QuestRules.All)
                {
                    if (quest.Main != main || !quests.IsActive(quest.Id))
                        continue;
                    any = true;
                    Heading(quest.Main ? quest.Title : quest.Title + "  <size=26><color=#9A8F84>side quest</color></size>");
                    var current = quests.StepIndex(quest.Id);
                    for (var i = 0; i < quest.Steps.Length && i <= current; i++)
                    {
                        var done = i < current;
                        var text = Label(content, (done ? "<color=#7E766E>" : "") + "· " + quest.Steps[i].Log + (done ? "</color>" : ""),
                            32, done ? FontStyle.Italic : FontStyle.Normal, TextAnchor.MiddleLeft);
                        Height(text.gameObject, 64f);
                    }
                    var from = Label(content, "<color=#9A8F84>From " + QuestRules.GiverName(quest.Giver) + "</color>", 26, FontStyle.Normal, TextAnchor.MiddleLeft);
                    Height(from.gameObject, 50f);
                }
            subtitle.text = any ? "Walk up to someone with a gold ! in town for more." : "No quests under way. Look for a gold ! over someone in town.";

            var anyDone = false;
            foreach (var quest in QuestRules.All)
            {
                if (!quests.IsDone(quest.Id))
                    continue;
                if (!anyDone)
                    Heading("Done");
                anyDone = true;
                var text = Label(content, $"{quest.Title}  <color=#9A8F84>{quest.Gift}</color>", 30, FontStyle.Normal, TextAnchor.MiddleLeft);
                Height(text.gameObject, 84f);
            }
        }
    }

    /// <summary>
    /// The line under the minimap naming the step being followed (2026-10-08): the main quest's, else the first side
    /// quest's. Hidden with nothing under way.
    /// </summary>
    public class QuestTracker : MonoBehaviour
    {
        Text text;
        float timer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            var canvasObject = new GameObject("Quest Tracker", typeof(Canvas), typeof(CanvasScaler));
            DontDestroyOnLoad(canvasObject);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 14;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight = 0.5f;

            var go = new GameObject("Step", typeof(RectTransform), typeof(Text), typeof(Shadow));
            go.transform.SetParent(canvasObject.transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            // Under the minimap (top right, 440 wide, about 230 tall).
            rect.anchoredPosition = new Vector2(-20f, -262f);
            rect.sizeDelta = new Vector2(560f, 120f);
            var text = go.GetComponent<Text>();
            text.font = UiStyle.Title;
            text.fontSize = 30;
            text.alignment = TextAnchor.UpperRight;
            text.color = new Color(1f, 0.8f, 0.4f);
            text.supportRichText = true;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            go.GetComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.9f);
            var tracker = canvasObject.AddComponent<QuestTracker>();
            tracker.text = text;
        }

        void Update()
        {
            timer -= Time.unscaledDeltaTime;
            if (timer > 0f)
                return;
            timer = 0.5f;
            var session = GameSession.Current;
            var (quest, step) = session != null ? session.Quests.Tracked() : (null, null);
            text.text = step != null ? $"<b>{quest.Title}</b>\n{step.Log}" : "";
        }
    }
}
