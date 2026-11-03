using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// The game's splash screen (the owner, 2026-11-03: "Make. splash screen for the game"): over black, the studio, then
    /// the title, WILD ARROW, with an arrow drawn across under it and the company's line, which fades into the menu. A
    /// tap skips it. Shown once a launch, over everything (sorting 200) while the first scene settles behind it.
    /// </summary>
    public class SplashScreen : MonoBehaviour
    {
        const float StudioSeconds = 1.3f, TitleSeconds = 2.4f, Fade = 0.45f;

        CanvasGroup group, studio, title;
        bool skip;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            if (!Features.CompanyMenu)
                return;
            var go = new GameObject("Splash Screen");
            DontDestroyOnLoad(go);
            go.AddComponent<SplashScreen>().Build();
        }

        void Build()
        {
            var canvasObject = new GameObject("Splash Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight = 0.5f;
            group = canvasObject.GetComponent<CanvasGroup>();

            var black = UiStyle.Image(canvasObject.transform, "Black", Color.black, true);
            UiStyle.Stretch(black.rectTransform);
            black.gameObject.AddComponent<Button>().onClick.AddListener(() => skip = true);

            studio = Layer(canvasObject.transform, "Studio");
            var by = UiStyle.Text(studio.transform, "F I L B U S   S O F T W A R E", 44, UiStyle.TextDim, TextAnchor.MiddleCenter, true);
            UiStyle.Place(by.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1100f, 80f));
            var presents = UiStyle.Text(studio.transform, "presents", 34, new Color(0.45f, 0.42f, 0.38f, 1f), TextAnchor.MiddleCenter, true,
                FontStyle.Italic);
            UiStyle.Place(presents.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), new Vector2(1100f, 60f));

            title = Layer(canvasObject.transform, "Title");
            if (UiTextures.Glow != null)
            {
                var glow = UiStyle.Image(title.transform, "Glow", new Color(0.9f, 0.4f, 0.15f, 0.35f));
                glow.sprite = UiTextures.Glow;
                UiStyle.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1500f, 1000f));
            }
            var name = UiStyle.Text(title.transform, "WILD ARROW", 150, UiStyle.Gold, TextAnchor.MiddleCenter, true, FontStyle.Bold);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiStyle.Place(name.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1160f, 220f));
            // An arrow drawn across under the title: a shaft, a head and fletching.
            var shaft = UiStyle.Image(title.transform, "Shaft", UiStyle.Gold);
            UiStyle.Place(shaft.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(760f, 6f));
            var head = MenuChrome.Diamond(title.transform, 40f, UiStyle.BloodBright);
            UiStyle.Place(head.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(390f, -10f), new Vector2(40f, 40f));
            for (var k = 0; k < 3; k++)
            {
                var feather = UiStyle.Image(title.transform, "Fletching", UiStyle.BloodBright);
                UiStyle.Place(feather.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-360f + k * 22f, -10f), new Vector2(10f, 44f));
                feather.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 35f);
            }
            var line = UiStyle.Text(title.transform, "M E R C E N A R Y   C O M P A N Y", 40, UiStyle.TextMain, TextAnchor.MiddleCenter, true);
            UiStyle.Place(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -90f), new Vector2(1160f, 60f));
            var motto = UiStyle.Text(title.transform, "Sworn to the string. Paid in coin.", 34, UiStyle.TextDim, TextAnchor.MiddleCenter, true,
                FontStyle.Italic);
            UiStyle.Place(motto.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -160f), new Vector2(1160f, 60f));

            studio.alpha = title.alpha = 0f;
            StartCoroutine(Play());
        }

        static CanvasGroup Layer(Transform parent, string name)
        {
            var rect = UiStyle.Rect(parent, name);
            UiStyle.Stretch(rect);
            return rect.gameObject.AddComponent<CanvasGroup>();
        }

        IEnumerator Play()
        {
            yield return Show(studio, StudioSeconds);
            yield return Show(title, TitleSeconds);
            // The whole splash fades away into the menu behind it.
            for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime / Fade)
            {
                group.alpha = 1f - t;
                yield return null;
            }
            Destroy(gameObject);
        }

        IEnumerator Show(CanvasGroup layer, float seconds)
        {
            for (var t = 0f; t < 1f && !skip; t += Time.unscaledDeltaTime / Fade)
            {
                layer.alpha = t;
                yield return null;
            }
            layer.alpha = 1f;
            for (var t = 0f; t < seconds && !skip; t += Time.unscaledDeltaTime)
                yield return null;
            if (layer == title)
                yield break;
            for (var t = 0f; t < 1f && !skip; t += Time.unscaledDeltaTime / Fade)
            {
                layer.alpha = 1f - t;
                yield return null;
            }
            layer.alpha = 0f;
        }
    }
}
