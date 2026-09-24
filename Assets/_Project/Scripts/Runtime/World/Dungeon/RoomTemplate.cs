using System;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// One hand-authored room for the dungeon generator (Docs/05-world-and-content.md, room library), drawn as text in
    /// the inspector: one row per line, '.' for floor and '#' for a pillar or wall piece, square, with the middle five
    /// cells of each side and the two rows inside them left clear for the doorways. See <see cref="RoomShape.Parse"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "ARPG/Room Template", fileName = "Room")]
    public class RoomTemplate : ScriptableObject
    {
        [TextArea(8, 30)]
        [SerializeField] string layout;

        public string Layout => layout;

        /// <summary>Parses the layout, or returns null and logs why when it is not a usable room.</summary>
        public RoomShape TryGetShape()
        {
            try
            {
                return RoomShape.Parse(name, layout);
            }
            catch (FormatException e)
            {
                Debug.LogError($"[ARPG] Room template {name} is not usable: {e.Message}", this);
                return null;
            }
        }

#if UNITY_EDITOR
        public void SetLayout(string text) => layout = text;
#endif
    }
}
