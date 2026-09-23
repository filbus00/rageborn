using UnityEngine;
using UnityEngine.SceneManagement;

namespace ARPG
{
    /// <summary>
    /// Grants XP for every kill (<see cref="Experience.KillXp"/>, from the character's level, the enemy's level and its
    /// rank) and calls out a level up above the player. Created from code and kept across scenes; it finds each
    /// scene's <see cref="EnemyManager"/> when the scene loads, so no scene needs rebuilding for it. The docs' level-up
    /// sound and passive point badge (Docs/06-ui-ux.md) wait for audio and passives.
    /// </summary>
    public class ExperienceDirector : MonoBehaviour
    {
        const int LevelUpFontSize = 72;
        static readonly Color LevelUpColor = new Color(1f, 0.84f, 0.3f);

        // Above the player's head: the capsule is about 1.3 world units tall.
        static readonly Vector3 CalloutOffset = new Vector3(0f, 1.6f, 0f);

        EnemyManager enemies;
        PlayerController player;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            var go = new GameObject("Experience Director");
            DontDestroyOnLoad(go);
            go.AddComponent<ExperienceDirector>();
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
            player = FindAnyObjectByType<PlayerController>();
            enemies = FindAnyObjectByType<EnemyManager>();
            if (enemies != null)
                enemies.Killed += OnKilled;
        }

        void Unsubscribe()
        {
            if (enemies != null)
                enemies.Killed -= OnKilled;
            enemies = null;
        }

        void OnKilled(EnemyController enemy)
        {
            var session = GameSession.Current;
            var definition = enemy.Definition;
            var gained = session.GrantExperience(Experience.KillXp(session.Level, definition.Level, definition.Rank));
            if (gained > 0 && player != null)
                DamageNumbers.Current?.ShowText(player.transform.position + CalloutOffset, $"LEVEL {session.Level}", LevelUpColor, LevelUpFontSize);
        }
    }
}
