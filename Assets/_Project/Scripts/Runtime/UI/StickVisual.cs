using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Draws the floating stick: a ring where the thumb landed and a knob that follows it.
    /// Both images anchor to the bottom-left of a screen-space overlay canvas.
    /// </summary>
    public class StickVisual : MonoBehaviour
    {
        // The knob is smaller than the ring; both are sized from the stick radius.
        const float KnobDiameterFactor = 0.9f;

        [SerializeField] FloatingStickInput input;
        [SerializeField] Canvas canvas;
        [SerializeField] RectTransform baseImage;
        [SerializeField] RectTransform knobImage;

        public void Configure(FloatingStickInput stickInput, Canvas parentCanvas, RectTransform ring, RectTransform knob)
        {
            input = stickInput;
            canvas = parentCanvas;
            baseImage = ring;
            knobImage = knob;
        }

        void LateUpdate()
        {
            var active = input != null && input.IsActive;
            baseImage.gameObject.SetActive(active);
            knobImage.gameObject.SetActive(active);
            if (!active)
                return;

            // On an overlay canvas, canvas units are screen pixels divided by the canvas scale factor.
            var toCanvas = 1f / canvas.scaleFactor;
            var radius = input.RadiusPixels;

            baseImage.sizeDelta = Vector2.one * (radius * 2f * toCanvas);
            baseImage.anchoredPosition = input.BaseScreenPosition * toCanvas;

            var knobOffset = Vector2.ClampMagnitude(input.ThumbScreenPosition - input.BaseScreenPosition, radius);
            knobImage.sizeDelta = Vector2.one * (radius * KnobDiameterFactor * toCanvas);
            knobImage.anchoredPosition = (input.BaseScreenPosition + knobOffset) * toCanvas;
        }
    }
}
