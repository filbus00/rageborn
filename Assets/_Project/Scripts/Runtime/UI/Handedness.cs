using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Left-hand and right-hand mode (Docs/01: the touch zone is symmetric, a setting moves the inventory button to the
    /// matching corner). Puts a button anchored to a bottom corner into the corner the setting asks for, keeping its
    /// distance from the edge. Used by the Bag and Portal buttons.
    /// </summary>
    public static class Handedness
    {
        public static void PlaceInCorner(RectTransform rect)
        {
            if (rect == null)
                return;
            var x = SettingsDirector.Current.leftHanded ? 0f : 1f;
            rect.anchorMin = new Vector2(x, rect.anchorMin.y);
            rect.anchorMax = new Vector2(x, rect.anchorMax.y);
            rect.pivot = new Vector2(x, rect.pivot.y);
            var position = rect.anchoredPosition;
            rect.anchoredPosition = new Vector2(x > 0f ? -Mathf.Abs(position.x) : Mathf.Abs(position.x), position.y);
        }
    }
}
