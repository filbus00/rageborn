using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ARPG
{
    /// <summary>
    /// What happens when the player dies (Docs/01-core-gameplay.md; the Vigil's road, 2026-10-06): the character stops,
    /// loses a share of her carried gold, and the screen fades to the stretch's bottom beacon, the last one lit, where
    /// she wakes with full life and her gear. Off the road (a hand-built scene) she wakes in town. Nothing is asked of
    /// the player.
    /// </summary>
    public class DeathFlow : MonoBehaviour
    {
        [Tooltip("The scene the character is sent to, by name.")]
        [SerializeField] string townScene = "Town";

        [Tooltip("The road's scene, reloaded at the stretch's bottom beacon.")]
        [SerializeField] string dungeonScene = "Dungeon";

        [SerializeField, Min(0f)] float fadeSeconds = 0.8f;

        [Tooltip("How long the fallen character is seen before the fade starts. The Wrathborn's death takes 1.4 s; with the fade straight away it was dark before the body reached the floor.")]
        [SerializeField, Min(0f)] float holdSeconds = 1.2f;

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
            Sfx.Play(SoundId.Death);
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

            var lost = GameSession.Current.FallOnRoad();
            if (lost > 0 && player != null)
                DamageNumbers.Current?.ShowText(player.transform.position + Vector3.up * 1.4f, $"-{lost} GOLD", new Color(1f, 0.8f, 0.3f), 44);
            var level = FindAnyObjectByType<DungeonLevel>();
            wakeDepth = level != null ? level.Depth : 0;

            // At once, not after the debounce: quitting the app during the fade must not undo the death.
            SaveDirector.SaveNow();
            StartCoroutine(SendToBeacon());
        }

        IEnumerator SendToBeacon()
        {
            yield return new WaitForSeconds(holdSeconds);
            var fade = ScreenFade.Current;
            if (fade != null)
                yield return fade.FadeOut(fadeSeconds);

            if (wakeDepth > 0)
                GameSession.Current.Travel = new LevelTravel(wakeDepth, Arrival.FromAbove);
            SceneManager.LoadScene(wakeDepth > 0 ? dungeonScene : townScene);
        }

        // The stretch she fell on, 0 off the road.
        int wakeDepth;
    }
}
