using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace ARPG
{
    /// <summary>
    /// The only gameplay input: a floating thumb stick. A touch that begins in the lower part of the screen
    /// spawns the stick base under the thumb and stays in control until it lifts. See Docs/01-core-gameplay.md.
    /// </summary>
    public class FloatingStickInput : MonoBehaviour
    {
        // iOS points are roughly 1/163 inch.
        const float PointsPerInch = 163f;

        [Tooltip("Stick radius in points. The settings screen allows 48 to 96.")]
        [SerializeField, Range(48f, 96f)] float radiusPoints = 64f;

        [Tooltip("Dead zone as a fraction of the stick radius.")]
        [SerializeField, Range(0f, 0.3f)] float deadZone = 0.08f;

        [Tooltip("Touches must begin below this fraction of the screen height to spawn the stick.")]
        [SerializeField, Range(0.3f, 1f)] float touchZoneHeight = 0.62f;

        Vector2 origin;
        Vector2 thumb;
        int activeFinger = -1;

        /// <summary>Screen-space stick value. Direction is the thumb direction, magnitude is 0 to 1.</summary>
        public Vector2 Value { get; private set; }

        public bool IsActive => activeFinger >= 0;

        /// <summary>Where the stick base sits, in screen pixels.</summary>
        public Vector2 BaseScreenPosition => origin;

        /// <summary>Where the thumb is, in screen pixels.</summary>
        public Vector2 ThumbScreenPosition => thumb;

        public float RadiusPixels => radiusPoints * PixelsPerPoint;

        static float PixelsPerPoint
        {
            get
            {
                var dpi = Screen.dpi;
                return dpi > 0f ? Mathf.Max(1f, dpi / PointsPerInch) : 1f;
            }
        }

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
#if UNITY_EDITOR
            // Lets the mouse drive the stick in the Game view.
            TouchSimulation.Enable();
#endif
        }

        void OnDisable()
        {
            Release();
#if UNITY_EDITOR
            TouchSimulation.Disable();
#endif
            EnhancedTouchSupport.Disable();
        }

        void Update()
        {
            var touches = Touch.activeTouches;

            if (IsActive)
            {
                for (var i = 0; i < touches.Count; i++)
                {
                    var touch = touches[i];
                    if (touch.finger.index != activeFinger)
                        continue;

                    if (touch.ended)
                    {
                        Release();
                        return;
                    }

                    thumb = touch.screenPosition;
                    Value = StickMath.Evaluate(ref origin, thumb, RadiusPixels, deadZone);
                    return;
                }

                // The touch vanished without an end event.
                Release();
                return;
            }

            for (var i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (touch.began && touch.screenPosition.y <= Screen.height * touchZoneHeight)
                {
                    activeFinger = touch.finger.index;
                    origin = touch.screenPosition;
                    thumb = origin;
                    Value = Vector2.zero;
                    return;
                }
            }
        }

        void Release()
        {
            activeFinger = -1;
            Value = Vector2.zero;
        }
    }
}
