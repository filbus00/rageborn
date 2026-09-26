using UnityEngine;
using UnityEngine.SceneManagement;

namespace ARPG
{
    /// <summary>
    /// Runs the session's <see cref="AutoPotion"/>: counts kills toward charges, fires and applies the heal through
    /// <see cref="PlayerHealth.Heal"/>, calls out a potion above the player, and refills the charges in town (tuning:
    /// the docs do not say when charges come back other than from kills; the town is safe, and a death lands there
    /// too). Created from code and kept across scenes, finding each scene's player and enemy manager on load, so no
    /// scene needs rebuilding for it. Uses scaled time: the inventory screen's pause stops the heal too.
    /// </summary>
    public class AutoPotionDirector : MonoBehaviour
    {
        // The town's scene name, as DeathFlow and the stairways use it.
        const string TownScene = "Town";

        const int CalloutFontSize = 52;
        static readonly Color CalloutColor = new Color(0.45f, 1f, 0.55f);
        static readonly Vector3 CalloutOffset = new Vector3(0f, 1.6f, 0f);

        PlayerHealth health;
        PlayerController player;
        EnemyManager enemies;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            var go = new GameObject("Auto Potion Director");
            DontDestroyOnLoad(go);
            go.AddComponent<AutoPotionDirector>();
        }

        void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Unsubscribe();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Unsubscribe();
            health = FindAnyObjectByType<PlayerHealth>();
            player = FindAnyObjectByType<PlayerController>();
            enemies = FindAnyObjectByType<EnemyManager>();
            if (enemies != null)
                enemies.Killed += OnKilled;

            var potion = GameSession.Current.Potion;
            potion.Cancel();
            if (scene.name == TownScene)
                potion.Refill();
        }

        void Unsubscribe()
        {
            if (enemies != null)
                enemies.Killed -= OnKilled;
            enemies = null;
        }

        void OnKilled(EnemyController enemy) => GameSession.Current.Potion.RegisterKill();

        void Update()
        {
            if (health == null)
                return;

            var heal = GameSession.Current.Potion.Tick(Time.deltaTime, health.Fraction, health.IsAlive, out var fired);
            if (heal > 0f)
                health.Heal(heal * health.MaxLife);

            if (fired && player != null)
                DamageNumbers.Current?.ShowText(player.transform.position + CalloutOffset, "POTION", CalloutColor, CalloutFontSize);
                Sfx.Play(SoundId.Potion);
        }
    }
}
