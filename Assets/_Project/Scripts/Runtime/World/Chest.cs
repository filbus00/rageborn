using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// A dungeon level's chest (Docs/05-world-and-content.md: one guaranteed per level). Walking onto it opens it, with
    /// no button, and its loot spills around it (<see cref="LootDirector.DropChest"/>). It stays open while the game
    /// session lasts, keyed by level like killed enemies, so leaving and coming back does not refill it.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Chest : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite closedSprite;
        [SerializeField] Sprite openSprite;

        string key;
        int itemLevel;
        bool open;

        /// <summary>Sets the chest up from code, as the dungeon generator does.</summary>
        public void Configure(string chestKey, int level, SpriteRenderer rendererToSwap, Sprite closed, Sprite opened)
        {
            key = chestKey;
            itemLevel = level;
            spriteRenderer = rendererToSwap;
            closedSprite = closed;
            openSprite = opened;
            SetOpen(GameSession.Current.IsOpened(key));
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (open || other.GetComponentInParent<PlayerController>() == null)
                return;

            SetOpen(true);
            GameSession.Current.RecordOpened(key);

            var loot = FindAnyObjectByType<LootDirector>();
            if (loot != null)
                loot.DropChest(itemLevel, IsoMath.WorldToGround(transform.position));
        }

        void SetOpen(bool value)
        {
            open = value;
            if (spriteRenderer != null)
                spriteRenderer.sprite = open ? openSprite : closedSprite;
        }
    }
}
