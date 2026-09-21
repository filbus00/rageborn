using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The life bar at the top of the screen (Docs/06-ui-ux.md: the top of the screen is read-only). The fill's
    /// right anchor tracks the player's life, so the bar needs no art.
    /// </summary>
    public class LifeBar : MonoBehaviour
    {
        [SerializeField] RectTransform fill;

        PlayerHealth health;

        void Update()
        {
            if (health == null)
                health = FindAnyObjectByType<PlayerHealth>();
            if (health == null || fill == null)
                return;

            fill.anchorMax = new Vector2(health.Fraction, 1f);
        }
    }
}
