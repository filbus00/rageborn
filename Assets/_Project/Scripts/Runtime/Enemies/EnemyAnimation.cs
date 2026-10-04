using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// An enemy type's baked animations (Docs/09-art-brief.md, 5.1: idle, run, attack, hit, death in 8 directions), loaded
    /// once from Resources/Characters/&lt;character&gt; and shared by every pooled enemy of that type. A type without
    /// sheets (no art yet) has none, and its enemies keep the placeholder body and its swell tells.
    /// </summary>
    public sealed class EnemyAnimationSet
    {
        public CharacterSheet Idle, Run, Attack, Hit, Death;
        public float IdleSeconds, RunSeconds, AttackSeconds;

        /// <summary>The ground speed the run looks right at (from the bake), or 0 when unknown.</summary>
        public float RunRecordedSpeed;

        /// <summary>How much larger than the base look the sheets were baked (the timing file's "scale" line): a rank or
        /// boss drawn at its in-game size, so the body is drawn this much smaller to cancel the enemy's VisualScale and
        /// its pixels stay whole. 1 for a look baked at the base size.</summary>
        public float BakedScale = 1f;

        public int DirectionCount => Idle.Rows.Length;

        static readonly Dictionary<string, EnemyAnimationSet> Cache = new Dictionary<string, EnemyAnimationSet>();

        /// <summary>The set for a character, or null when it has no idle sheet. Cached, including the misses.</summary>
        public static EnemyAnimationSet For(string character)
        {
            if (string.IsNullOrEmpty(character))
                return null;
            if (Cache.TryGetValue(character, out var cached))
                return cached;
            var idle = CharacterSheets.Load($"Characters/{character}/{character}_idle");
            EnemyAnimationSet set = null;
            if (idle != null)
            {
                var seconds = new Dictionary<string, float>();
                var speeds = new Dictionary<string, float>();
                CharacterSheets.LoadTiming(character, seconds, speeds);
                CharacterSheet Sheet(string name) => CharacterSheets.Load($"Characters/{character}/{character}_{name}");
                float Seconds(string name, CharacterSheet sheet) =>
                    seconds.TryGetValue(name, out var s) ? s : sheet != null ? sheet.Frames / LayeredCharacterSprite.FramesPerSecond : 0f;
                set = new EnemyAnimationSet
                {
                    Idle = idle,
                    Run = Sheet("run"),
                    Attack = Sheet("attack"),
                    Hit = Sheet("hit"),
                    Death = Sheet("death"),
                };
                set.IdleSeconds = Seconds("idle", set.Idle);
                set.RunSeconds = Seconds("run", set.Run);
                set.AttackSeconds = Seconds("attack", set.Attack);
                speeds.TryGetValue("run", out set.RunRecordedSpeed);
                if (seconds.TryGetValue("scale", out var baked))
                    set.BakedScale = baked;
            }
            Cache[character] = set;
            return set;
        }
    }

    /// <summary>How an enemy's frames follow its state, pure so it is tested.</summary>
    public static class EnemyAnimationRules
    {
        /// <summary>Where in the attack animation the blow lands: the wind-up plays the frames before it, the recovery the
        /// rest. Tuning (Mixamo's attacks strike a little past the middle).</summary>
        public const float HitFraction = 0.55f;

        /// <summary>Speeds below this count as standing (ground units a second).</summary>
        public const float MovingSpeed = 0.3f;

        /// <summary>How long a hit reaction shows when the enemy is not attacking.</summary>
        public const float HitSeconds = 0.25f;

        /// <summary>
        /// The attack's progress, 0 to 1: the wind-up runs from the start to <see cref="HitFraction"/>, so the blow lands
        /// on the animation's strike exactly when the game lands it, and the recovery runs the rest.
        /// </summary>
        public static float AttackProgress(bool recovering, float elapsed, float duration)
        {
            var t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            return recovering ? HitFraction + (1f - HitFraction) * t : HitFraction * t;
        }

        /// <summary>A loop's frame at a time into it.</summary>
        public static int LoopFrame(float time, float seconds, int frames)
        {
            if (frames <= 1 || seconds <= 0f)
                return 0;
            return Mathf.FloorToInt(Mathf.Repeat(time, seconds) / seconds * frames) % frames;
        }

        /// <summary>A one-shot's frame at a progress of 0 to 1.</summary>
        public static int OneShotFrame(float progress, int frames) =>
            frames <= 1 ? 0 : Mathf.Clamp(Mathf.FloorToInt(progress * frames), 0, frames - 1);
    }
}
