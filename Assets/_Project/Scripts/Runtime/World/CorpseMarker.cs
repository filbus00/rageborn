using UnityEngine;

namespace ARPG
{
    /// <summary>The corpse in the world. Picks its gear up when the player walks onto it.</summary>
    public class CorpseMarker : MonoBehaviour
    {
        Corpse corpse;

        public void Init(Corpse data) => corpse = data;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (corpse == null || other.GetComponentInParent<PlayerController>() == null)
                return;

            if (GameSession.Current.Retrieve(corpse))
                Destroy(gameObject);
        }
    }
}
