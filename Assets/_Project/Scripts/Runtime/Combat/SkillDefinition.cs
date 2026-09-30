using UnityEngine;
using UnityEngine.Serialization;

namespace ARPG
{
    /// <summary>What a skill does when it fires (Docs/02-classes-and-skills.md). New kinds go at the end: assets store
    /// the number.</summary>
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

        /// <summary>A timed boost to the character: damage and Rage (Battle Roar), attack speed (Blood Frenzy).</summary>
        Buff,

        /// <summary>A spin that hits everything in reach on a beat for a few seconds while the character keeps moving,
        /// and holds back the lower slots and the basic attack meanwhile (Rending Spin).</summary>
        Channel,

        /// <summary>One heavy blow at one target, far heavier on a wounded one (Skullsplitter).</summary>
        Execute,

        /// <summary>The Wild Arrow's (Docs/02, 2026-09-30): a fan of arrows at the target, each stopped by the first
        /// enemy it meets (Split Arrow).</summary>
        Volley,

        /// <summary>One arrow that passes through every enemy on its line; walls stop it (Pierce Arrow).</summary>
        PierceShot,

        /// <summary>Arrows that curve after their own targets, each a different enemy when it can; the targets must be
        /// in sight when they are loosed (Homing Arrow).</summary>
        HomingShot,

        /// <summary>An arrow that bursts on the first enemy or wall it hits, hurting everything around (Explosive Arrow).</summary>
        ExplosiveShot,
    }

    /// <summary>
    /// When a slot's skill fires (Docs/01-core-gameplay.md, trigger conditions). Default is the skill's own designed
    /// condition (the trigger column of Docs/02); the others replace it, from the choices the loadout offers (Q15: each
    /// skill keeps its default and has two alternatives). Every skill still needs a valid target or area.
    /// </summary>
    public enum SkillTrigger
    {
        Default,
        Always,
        EnemiesThreePlus,
        ElitePresent,
        LifeBelowHalf,
        LifeAboveHalf,
        Standing,
        Moving,
    }

    /// <summary>
    /// Data for one auto-cast skill. Ranges are in ground units. The Wild Arrow's four (Docs/02-classes-and-skills.md,
    /// 2026-09-30) are Split, Pierce, Homing and Explosive Arrow; the retired Wrathborn's kinds are kept for reference.
    /// Each fires by itself when its kind's trigger passes (see <see cref="PlayerCombat"/>), and unlocks at
    /// <see cref="UnlockLevel"/>. The cost is Focus (the field keeps its old name, rageCost, so assets keep their values).
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

        [Tooltip("Rage gained on each cast: a generator. Docs: Bull Rush costs nothing and gains 15, so it can open a fight.")]
        [SerializeField, Min(0f)] float rageGain;

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

        [Tooltip("Buff: Battle Roar fires only below this much Rage (0 for no limit).")]
        [SerializeField, Min(0f)] float maxRage;

        [Tooltip("Buff: Blood Frenzy needs this many Momentum stacks, and combat within the last 3 s.")]
        [SerializeField, Min(0)] int minMomentum;

        [Tooltip("Buff: how long it lasts. Channel: how long the spin lasts.")]
        [SerializeField, Min(0f)] float durationSeconds;

        [Tooltip("Buff: increased damage while it lasts, 0.2 for 20 percent.")]
        [SerializeField, Min(0f)] float buffDamage;

        [Tooltip("Buff: increased attack speed while it lasts, 0.3 for 30 percent.")]
        [SerializeField, Min(0f)] float buffAttackSpeed;

        [Tooltip("Buff: more attack speed per live Momentum stack, 0.05 for 5 percent.")]
        [SerializeField, Min(0f)] float buffAttackSpeedPerMomentum;

        [Tooltip("Channel: seconds between the spin's hits.")]
        [SerializeField, Min(0.05f)] float tickSeconds = 0.3f;

        [Tooltip("Channel: the bleed each hit leaves, as a multiple of weapon damage over its whole duration (0.4 for 40 percent).")]
        [SerializeField, Min(0f)] float bleedMultiplier;

        [SerializeField, Min(0f)] float bleedSeconds;

        [Tooltip("Execute: below this share of its life a target takes the heavier blow (0.25).")]
        [SerializeField, Range(0f, 1f)] float executeThreshold;

        [Tooltip("Execute: the heavier blow's multiple of weapon damage (8 for 800 percent).")]
        [SerializeField, Min(0f)] float executeMultiplier;

        [Tooltip("Execute: seconds from the cast to the blow; the life threshold is read when it lands.")]
        [SerializeField, Min(0f)] float windupSeconds;

        [Tooltip("Volley and Homing: how many arrows it looses.")]
        [SerializeField, Min(1)] int projectileCount = 1;

        [Tooltip("Volley: the fan's width in degrees. Homing: how wide the arrows fan out before they curve.")]
        [SerializeField, Min(0f)] float spreadDegrees;

        [Tooltip("Explosive: the burst's radius. Its trigger counts the enemies within it around the target.")]
        [SerializeField, Min(0f)] float burstRadius;

        [Tooltip("Homing: how fast an arrow turns toward its target, degrees a second.")]
        [SerializeField, Min(0f)] float turnDegreesPerSecond = 360f;

        [Tooltip("The skill's own trigger in words, for the loadout (Docs/02's trigger column).")]
        [SerializeField] string triggerText = "";

        [Tooltip("The character animation it plays (hew, hurl_axe, ...); falls back to the attack, or none for a buff.")]
        [SerializeField] string animationName = "";

        [Tooltip("The two other triggers the loadout offers for this skill besides its own (Q15).")]
        [SerializeField] SkillTrigger[] alternativeTriggers = new SkillTrigger[0];

        [Tooltip("Placeholder art: the sweep's wedge, the projectile or the slam's disc.")]
        [SerializeField] Sprite effectSprite;

        [SerializeField] Color effectColor = new Color(1f, 0.75f, 0.35f, 0.55f);

        public string DisplayName => displayName;
        public SkillKind Kind => kind;
        public int UnlockLevel => unlockLevel;
        public float RageCost => rageCost;
        public float RageGain => rageGain;
        public float CooldownSeconds => cooldownSeconds;
        public float DamageMultiplier => damageMultiplier;
        public float ArcDegrees => arcDegrees;
        public float Range => range;
        public float MinRange => minRange;
        public int MinEnemies => minEnemies;
        public float Speed => speed;
        public float EffectStrength => effectStrength;
        public float EffectSeconds => effectSeconds;
        public float MaxRage => maxRage;
        public int MinMomentum => minMomentum;
        public float DurationSeconds => durationSeconds;
        public float BuffDamage => buffDamage;
        public float BuffAttackSpeed => buffAttackSpeed;
        public float BuffAttackSpeedPerMomentum => buffAttackSpeedPerMomentum;
        public float TickSeconds => tickSeconds;
        public float BleedMultiplier => bleedMultiplier;
        public float BleedSeconds => bleedSeconds;
        public float ExecuteThreshold => executeThreshold;
        public float ExecuteMultiplier => executeMultiplier;
        public float WindupSeconds => windupSeconds;
        public int ProjectileCount => Mathf.Max(1, projectileCount);
        public float SpreadDegrees => spreadDegrees;
        public float BurstRadius => burstRadius;
        public float TurnDegreesPerSecond => turnDegreesPerSecond;
        /// <summary>The Focus spent on each cast (the same field as <see cref="RageCost"/>).</summary>
        public float FocusCost => rageCost;
        public System.Collections.Generic.IReadOnlyList<SkillTrigger> AlternativeTriggers => alternativeTriggers;
        public string TriggerText => triggerText;
        public string AnimationName => animationName;
        public Sprite EffectSprite => effectSprite;
        public Color EffectColor => effectColor;
    }
}
