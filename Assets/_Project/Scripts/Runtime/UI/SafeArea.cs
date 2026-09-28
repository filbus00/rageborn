using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Keeps a full-screen panel's contents inside <see cref="Screen.safeArea"/>, out from under the Dynamic
    /// Island, the status bar and the home indicator. The panel's own background still covers the whole screen.
    /// Found on the simulator: the Bag's top row sat under the island and taps there often missed.
    /// </summary>
    public static class SafeArea
    {
        /// <summary>Moves every child of the panel into a new child fitted to the safe area, keeping their order
        /// and their anchoring (a layout anchored to the panel's top is now anchored to the safe area's top).</summary>
        public static RectTransform WrapChildren(RectTransform panel)
        {
            var children = new List<Transform>();
            foreach (Transform child in panel)
                children.Add(child);

            var area = new GameObject("Safe Area", typeof(RectTransform));
            var rect = (RectTransform)area.transform;
            rect.SetParent(panel, false);
            Fit(rect);

            foreach (var child in children)
                child.SetParent(rect, false);
            return rect;
        }

        /// <summary>Anchors a rect to the safe area as fractions of the screen, so the canvas scale does not
        /// matter. Portrait only, so the safe area does not change while the game runs.</summary>
        public static void Fit(RectTransform rect)
        {
            var safe = Screen.safeArea;
            var size = new Vector2(Screen.width, Screen.height);
            if (size.x <= 0f || size.y <= 0f)
                return;

            rect.anchorMin = new Vector2(safe.xMin / size.x, safe.yMin / size.y);
            rect.anchorMax = new Vector2(safe.xMax / size.x, safe.yMax / size.y);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
