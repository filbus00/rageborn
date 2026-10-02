using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The smith in town (Docs/05-world-and-content.md: NPCs stand at fixed spots, and walking up to one opens its
    /// bottom sheet, so nothing in the world needs a tap). Entering its trigger opens the <see cref="ForgeScreen"/>,
    /// which it builds the first time. To open it again the player walks away and back.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ForgeNpc : MonoBehaviour
    {
        ForgeScreen screen;

        // The Forge is switched off for now (Features.Forge): the smith is not in town.
        void Awake()
        {
            if (!Features.Forge)
                gameObject.SetActive(false);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null)
                return;
            // The inventory screen pauses the game; the player cannot walk in with it open, but a load could.
            if (InventoryScreen.Current != null && InventoryScreen.Current.IsOpen)
                return;

            if (screen == null)
                screen = ForgeScreen.Create();
            screen.Open();
        }

        void OnDestroy()
        {
            if (screen != null)
                Destroy(screen.transform.parent.gameObject);
        }
    }
}
