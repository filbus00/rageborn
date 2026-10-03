using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ARPG
{
    /// <summary>
    /// Makes a Bag tile draggable (the owner, 2026-10-03: drag gear onto its slot). The screen that builds the tile
    /// handles the drag through the three callbacks; a tap without a drag still reaches the tile's Button.
    /// </summary>
    public class ItemDrag : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Action<ItemDrag, PointerEventData> Began, Moved, Ended;

        public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = true;

        public void OnBeginDrag(PointerEventData eventData) => Began?.Invoke(this, eventData);

        public void OnDrag(PointerEventData eventData) => Moved?.Invoke(this, eventData);

        public void OnEndDrag(PointerEventData eventData) => Ended?.Invoke(this, eventData);
    }

    /// <summary>Where a dragged Bag item can land: a place on the paper doll, or the backpack (to take gear off).</summary>
    public class ItemDropTarget : MonoBehaviour
    {
        public ItemSlot Place;
        public bool Backpack;
    }
}
