using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// A double-tap skill evolving (2026-11-24, the owner: "add a banner and effect when it evolves"): when the boss that
    /// evolves it falls, a burst of gold and green swirls round her for about a second, the level-up sound plays, then a
    /// banner says what changed. Follows her while it plays and removes itself.
    /// </summary>
    public class SkillEvolution : MonoBehaviour
    {
        const float Seconds = 1.2f;
        const int Sparks = 16;
        static readonly Color Gold = new Color(1f, 0.85f, 0.35f, 1f);
        static readonly Color Green = new Color(0.45f, 1f, 0.45f, 1f);

        string message;
        PlayerController player;
        float clock;
        int lit;

        /// <summary>Plays the evolution on her and shows the banner after it.</summary>
        public static void Play(string message)
        {
            var go = new GameObject("Skill Evolution");
            var evolution = go.AddComponent<SkillEvolution>();
            evolution.message = message;
            evolution.player = FindAnyObjectByType<PlayerController>();
            Sfx.Play(SoundId.LevelUp, 1f);
        }

        void Update()
        {
            clock += Time.unscaledDeltaTime;
            var fx = ArrowFx.Current;
            if (player != null && fx != null)
            {
                var at = (Vector2)player.transform.position;
                // Sparks thrown out round her one after another, a full turn, gold and green in turn, rising as it goes.
                var due = Mathf.Min(Sparks, Mathf.FloorToInt(clock / (Seconds * 0.8f) * Sparks));
                for (; lit < due; lit++)
                {
                    var angle = lit / (float)Sparks * Mathf.PI * 2f;
                    var way = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * IsoMath.GroundSquash);
                    fx.Sparks(at + way * 0.5f, way, 0.2f + lit * 0.04f, lit % 2 == 0 ? Gold : Green, 3);
                }
                if (clock >= Seconds && lit >= Sparks)
                {
                    fx.Burst(at, 1.4f, Gold);
                    fx.Burst(at, 0.8f, Green);
                }
            }
            if (clock < Seconds)
                return;
            Sfx.Play(SoundId.LevelUp, 0.8f);
            (HintBanner.Current != null ? HintBanner.Current : HintBanner.Create()).Show(message);
            Destroy(gameObject);
        }
    }
}
