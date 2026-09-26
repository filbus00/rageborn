using UnityEngine;
using UnityEngine.Serialization;

namespace ARPG
{
    /// <summary>What a skill does when it fires. Buff, Channel and Execute skills come with the rest of the Wrathborn's
    /// list after the M1 slice (Docs/02-classes-and-skills.md).</summary>
    public enum SkillKind
    {
        /// <summary>A melee sweep in front of the character (Hew).</summary>
        Sweep,

        /// <summary>A thrown projectile at an enemy further away, stopped by the first enemy or wall (Hurl Axe).</summary>
        Projectile,

        /// <summary>A dash along the movement direction that hits and knocks back everything on the way (Bull Rush).</summary>
        Charge,

        /// <summary>A slam around the character that hits and slows everything in its radius (Ground Breaker).</summary>
        Slam,
    }

    /// <summary>
    /// Data for one auto-cast skill. Ranges are in ground units. The Wrathborn's first four (Docs/02-classes-and-skills.md)
    /// are Hew, Hurl Axe, Bull Rush and Ground Breaker; each fires by itself when its kind's trigger passes (see
    /// <see cref="PlayerCombat"/>), and unlocks at <see cref="UnlockLevel"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "ARPG/Skill Definition", fileName = "Skill")]
    public class SkillDefinition : ScriptableObject
    {
        [SerializeField] string displayName = "Hew";

        [SerializeField] SkillKind kind = SkillKind.Sweep;

        [Tooltip("The character level that unlocks the skill. Docs: Hew 1, Hurl Axe 2, Bull Rush 4, Ground Breaker 6.")]
        [SerializeField, Min(1)] int unlockLevel = 1;

        [Tooltip("Rage spent on each cast.")]
        [FormerlySerializedAs("focusCost")]
        [SerializeField, Min(0f)] float rageCost = 20f;

        [SerializeField, Min(0f)] float cooldownSeconds = 3f;

        [Tooltip("Damage as a multiple of weapon damage: 1.7 for 170 percent.")]
        [SerializeField, Min(0f)] float damageMultiplier = 1.7f;

        [Tooltip("Sweep only: width in degrees.")]
        [SerializeField, Range(10f, 360f)] float arcDegrees = 180f;

        [Tooltip("Sweep: reach. Projectile and Charge: the farthest target that triggers it. Slam: radius.")]
        [SerializeField, Min(0.1f)] float range = 2f;

        [Tooltip("Projectile and Charge: the nearest target that triggers it, so the skill is not wasted in melee.")]
        [SerializeField, Min(0f)] float minRange;

        [Tooltip("Sweep and Slam: how many enemies must be in the area for it to fire.")]
        [SerializeField, Min(1)] int minEnemies = 1;

        [Tooltip("Projectile: speed. Charge: dash speed. Ground units per second.")]
        [SerializeField, Min(0f)] float speed = 14f;

        [Tooltip("Charge: how far enemies are knocked back. Slam: movement speed multiplier while slowed (0.7 for 30 percent).")]
        [SerializeField, Min(0f)] float effectStrength;

        [Tooltip("Slam: how long the slow lasts.")]
        [SerializeField, Min(0f)] float effectSeconds;

        [Tooltip("Placeholder art: the sweep's wedge, the projectile or the slam's disc.")]
        [SerializeField] Sprite effectSprite;

        [SerializeField] Color effectColor = new Color(1f, 0.75f, 0.35f, 0.55f);

        public string DisplayName => displayName;
        public SkillKind Kind => kind;
        public int UnlockLevel => unlockLevel;
        public float RageCost => rageCost;
        public float CooldownSeconds => cooldownSeconds;
        public float DamageMultiplier => damageMultiplier;
        public float ArcDegrees => arcDegrees;
        public float Range => range;
        public float MinRange => minRange;
        public int MinEnemies => minEnemies;
        public float Speed => speed;
        public float EffectStrength => effectStrength;
        public float EffectSeconds => effectSeconds;
        public Sprite EffectSprite => effectSprite;
        public Color EffectColor => effectColor;
    }
}
