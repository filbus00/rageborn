using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ARPG
{
    /// <summary>
    /// A full-screen black overlay. A scene starts black and fades in; scene changes and death fade it out first.
    /// </summary>
    public class ScreenFade : MonoBehaviour
    {
        [SerializeField] Image overlay;

        [Tooltip("Seconds to fade in when the scene starts.")]
        [SerializeField, Min(0f)] float fadeInSeconds = 0.4f;

        /// <summary>The fade in the current scene, or null when the scene has none.</summary>
        public static ScreenFade Current { get; private set; }

        void Awake()
        {
            Current = this;
            SetAlpha(1f);
        }

        void Start() => StartCoroutine(Fade(0f, fadeInSeconds));

        void OnDestroy()
        {
            if (Current == this)
                Current = null;
        }

        /// <summary>Fades to black. Yield it from a coroutine to wait for the fade to finish.</summary>
        public IEnumerator FadeOut(float seconds) => Fade(1f, seconds);

        IEnumerator Fade(float target, float seconds)
        {
            var start = overlay != null ? overlay.color.a : target;
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                // Unscaled, so a fade still finishes if time is slowed or paused.
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(start, target, elapsed / seconds));
                yield return null;
            }
            SetAlpha(target);
        }

        void SetAlpha(float alpha)
        {
            if (overlay == null)
                return;

            var color = overlay.color;
            color.a = alpha;
            overlay.color = color;
        }
    }
}
