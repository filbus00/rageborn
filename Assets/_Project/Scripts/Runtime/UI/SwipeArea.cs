using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ARPG
{
    /// <summary>Reports a horizontal swipe across its graphic: -1 for a swipe to the left, +1 to the right. Used by the
    /// item sheet to move between items (Docs/03-itemization.md: swipe left and right on the sheet).</summary>
    public class SwipeArea : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary>How far the finger must travel sideways, in screen pixels, relative to the screen width.</summary>
        const float MinSwipeScreenShare = 0.15f;

        public event Action<int> Swiped;

        Vector2 start;

        public void OnBeginDrag(PointerEventData eventData) => start = eventData.position;

        // Needed for OnEndDrag to be called; nothing follows the finger.
        public void OnDrag(PointerEventData eventData)
        {
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            var delta = eventData.position - start;
            if (Mathf.Abs(delta.x) < Screen.width * MinSwipeScreenShare || Mathf.Abs(delta.x) < Mathf.Abs(delta.y))
                return;
            Swiped?.Invoke(delta.x < 0f ? -1 : 1);
        }
    }
}
