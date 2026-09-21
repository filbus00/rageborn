using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ARPG
{
    /// <summary>
    /// What happens when the player dies (Docs/01-core-gameplay.md): the character stops, its equipped gear stays
    /// behind as a corpse where it fell, and the screen fades to the town. Nothing is asked of the player.
    /// </summary>
    public class DeathFlow : MonoBehaviour
    {
        [Tooltip("The scene the character is sent to, by name.")]
        [SerializeField] string townScene = "Town";

        [SerializeField, Min(0f)] float fadeSeconds = 0.8f;

        PlayerHealth health;

        void Start()
        {
            health = FindAnyObjectByType<PlayerHealth>();
            if (health != null)
                health.Died += OnDied;
        }

        void OnDestroy()
        {
            if (health != null)
                health.Died -= OnDied;
        }

        void OnDied()
        {
            var player = FindAnyObjectByType<PlayerController>();
            var combat = FindAnyObjectByType<PlayerCombat>();
            if (combat != null)
                combat.enabled = false;

            var ground = Vector2.zero;
            if (player != null)
            {
                ground = IsoMath.WorldToGround(player.transform.position);
                player.enabled = false;
                if (player.TryGetComponent<Rigidbody2D>(out var body))
                    body.linearVelocity = Vector2.zero;
            }

            GameSession.Current.Die(SceneManager.GetActiveScene().name, ground);
            StartCoroutine(SendToTown());
        }

        IEnumerator SendToTown()
        {
            var fade = ScreenFade.Current;
            if (fade != null)
                yield return fade.FadeOut(fadeSeconds);

            SceneManager.LoadScene(townScene);
        }
    }
}
