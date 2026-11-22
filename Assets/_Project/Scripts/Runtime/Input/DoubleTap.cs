using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The double tap (2026-11-22, the owner: "An attack that is fired when double tapping in quick succession"): two
    /// quick taps, each a touch that lifts almost at once without moving, one soon after the other. It fires on the
    /// second tap's lift. Pure; <see cref="FloatingStickInput"/> feeds it the stick's touches, so a touch that began on UI
    /// never counts. A tap does not move the thumb out of the ring, so it never dives her.
    /// </summary>
    public sealed class DoubleTapDetector
    {
        /// <summary>A tap lifts within this long of touching down.</summary>
        public const float MaxTapSeconds = 0.22f;

        /// <summary>The second tap touches down within this long of the first one's lift.</summary>
        public const float MaxGapSeconds = 0.35f;

        /// <summary>A tap's finger moves no further than this, in screen points.</summary>
        public const float MaxTapTravelPoints = 18f;

        /// <summary>The two taps land within this many screen points of each other.</summary>
        public const float MaxApartPoints = 140f;

        float beganAt;
        Vector2 beganPosition;
        bool down;
        bool lastWasTap;
        float lastLiftAt;
        Vector2 lastBeganPosition;

        /// <summary>A touch went down.</summary>
        public void Began(float time, Vector2 screenPosition)
        {
            beganAt = time;
            beganPosition = screenPosition;
            down = true;
        }

        /// <summary>The touch lifted. True when this completes a double tap.</summary>
        public bool Ended(float time, Vector2 screenPosition, float pixelsPerPoint)
        {
            if (!down)
                return false;
            down = false;
            var points = Mathf.Max(0.01f, pixelsPerPoint);
            var isTap = time - beganAt <= MaxTapSeconds && Vector2.Distance(screenPosition, beganPosition) / points <= MaxTapTravelPoints;
            if (!isTap)
            {
                lastWasTap = false;
                return false;
            }
            if (lastWasTap && beganAt - lastLiftAt <= MaxGapSeconds &&
                Vector2.Distance(beganPosition, lastBeganPosition) / points <= MaxApartPoints)
            {
                lastWasTap = false;
                return true;
            }
            lastWasTap = true;
            lastLiftAt = time;
            lastBeganPosition = beganPosition;
            return false;
        }

        /// <summary>The touch ended without a lift (it vanished): it was no tap.</summary>
        public void Cancel()
        {
            down = false;
            lastWasTap = false;
        }
    }
}
