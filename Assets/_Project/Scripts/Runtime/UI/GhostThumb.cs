using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// Docs/06-ui-ux.md: the stick is taught by a ghost thumb overlay for 5 seconds only. A faint stick base in the lower
    /// half of the screen, where a touch starts the stick, and a thumb that slides from it and back in a slow loop, as a
    /// real drag would. No text. It ends after <see cref="Onboarding.StickLessonSeconds"/>, or at once when the player
    /// touches the stick. Built in code by <see cref="OnboardingDirector"/>.
    /// </summary>
    public class GhostThumb : MonoBehaviour
    {
        const float LoopSeconds = 2.4f;
        const float Reach = 150f;
        const float FadeSeconds = 0.4f;

        CanvasGroup group;
        RectTransform thumb;
        FloatingStickInput stick;
        float elapsed;

        /// <summary>Raised once when the lesson ends, whichever way.</summary>
        public event System.Action Finished;

        public static GhostThumb Create(FloatingStickInput stick)
        {
            var canvasObject = new GameObject("Ghost Thumb Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 44;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight = 0.5f;

            var ghost = canvasObject.AddComponent<GhostThumb>();
            ghost.stick = stick;
            ghost.Build();
            return ghost;
        }

        void Build()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            var anchor = new Vector2(0.5f, 0.24f);
            NewCircle("Base", anchor, 260f, new Color(1f, 1f, 1f, 0.14f));
            thumb = NewCircle("Thumb", anchor, 130f, new Color(1f, 1f, 1f, 0.45f));
        }

        RectTransform NewCircle(string name, Vector2 anchor, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = TelegraphArt.Disc;
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= Onboarding.StickLessonSeconds || (stick != null && stick.IsActive))
            {
                Finished?.Invoke();
                Finished = null;
                Destroy(gameObject);
                return;
            }

            // Out and back, turning a quarter each loop: up, right, down, left, so all directions are shown.
            var loop = elapsed / LoopSeconds;
            var turn = Mathf.Floor(loop) * Mathf.PI * 0.5f + Mathf.PI * 0.5f;
            var t = Mathf.Sin((loop - Mathf.Floor(loop)) * Mathf.PI);
            thumb.anchoredPosition = new Vector2(Mathf.Cos(turn), Mathf.Sin(turn)) * (Reach * t);

            var fadeOut = Onboarding.StickLessonSeconds - elapsed;
            group.alpha = Mathf.Clamp01(Mathf.Min(elapsed, fadeOut) / FadeSeconds);
        }
    }
}
