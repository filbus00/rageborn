using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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

        [Tooltip("Touches must begin below this fraction of the screen height to spawn the stick.")]
        [SerializeField, Range(0.3f, 1f)] float touchZoneHeight = 0.62f;

        Vector2 origin;
        Vector2 thumb;
        int activeFinger = -1;

        readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        PointerEventData uiPointer;
        EventSystem uiPointerOwner;

        /// <summary>Screen-space stick value. Direction is the thumb direction, magnitude is 0 to 1.</summary>
        public Vector2 Value { get; private set; }

        public bool IsActive => activeFinger >= 0;

        /// <summary>The thumb has left the ring (<see cref="StickMath.Disengaged"/>): the character stops fighting, turns
        /// and runs. A test override longer than 1 counts as outside.</summary>
        public bool Disengaged { get; private set; }

        /// <summary>Editor and development builds only: a stick value a script sets to drive the character in a play-mode
        /// test (no touches can be injected there). Null hands control back to the touch.</summary>
        public Vector2? TestOverride { get; set; }

        /// <summary>Where the stick base sits, in screen pixels.</summary>
        public Vector2 BaseScreenPosition => origin;

        /// <summary>Where the thumb is, in screen pixels.</summary>
        public Vector2 ThumbScreenPosition => thumb;

        /// <summary>The stick's radius from Settings (48 to 96 points, Docs/01).</summary>
        public float RadiusPixels => SettingsDirector.Current.stickSize * PixelsPerPoint;

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
            if (TestOverride.HasValue && Debug.isDebugBuild)
            {
                Value = Vector2.ClampMagnitude(TestOverride.Value, 1f);
                Disengaged = TestOverride.Value.sqrMagnitude > 1.0001f;
                return;
            }

            var touches = Touch.activeTouches;
            if (!IsActive)
                Disengaged = false;

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
                    Disengaged = StickMath.Disengaged((thumb - origin).magnitude, RadiusPixels, Disengaged);
                    Value = StickMath.Evaluate(ref origin, thumb, RadiusPixels, SettingsDirector.Current.DeadZoneFraction);
                    return;
                }

                // The touch vanished without an end event.
                Release();
                return;
            }

            for (var i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (!touch.began || touch.screenPosition.y > Screen.height * touchZoneHeight)
                    continue;

                // Enhanced Touch reads raw touches, with no idea a Canvas is on top. A touch that begins on a UI
                // element (the inventory screen's buttons, now that one exists in this zone) starts that UI
                // interaction only, not the stick.
                if (IsOverUi(touch.screenPosition))
                    continue;

                activeFinger = touch.finger.index;
                origin = touch.screenPosition;
                thumb = origin;
                Value = Vector2.zero;
                return;
            }
        }

        /// <summary>Raycasts the UI at the touch directly. <c>EventSystem.IsPointerOverGameObject</c> asks the UI
        /// input module, which has not seen a touch yet on the frame it begins, so a tap on the Bag button moved
        /// the character instead (found on the simulator, 2026-09-28).</summary>
        bool IsOverUi(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            if (uiPointerOwner != eventSystem)
            {
                uiPointer = new PointerEventData(eventSystem);
                uiPointerOwner = eventSystem;
            }
            uiPointer.position = screenPosition;
            uiHits.Clear();
            eventSystem.RaycastAll(uiPointer, uiHits);
            return uiHits.Count > 0;
        }

        void Release()
        {
            activeFinger = -1;
            Value = Vector2.zero;
            Disengaged = false;
        }
    }
}
