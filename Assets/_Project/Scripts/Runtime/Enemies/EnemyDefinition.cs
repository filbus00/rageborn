using UnityEngine;

namespace ARPG
{
    /// <summary>Docs/00-vision-and-scope.md and Docs/03-itemization.md: a pack has rank-and-file Normal members, at
    /// most one Champion (its pack leader), or is entirely made of Elite members. Drives loot source, the targeting
    /// preference bonus and the armor-ignoring bite on an attack (Docs/01 and 03), on top of tougher stats.</summary>
    public enum EnemyRank
    {
        Normal,
        Champion,
        Elite,

        /// <summary>An act boss (Docs/05-world-and-content.md), driven by its own fight script, not the pack AI.</summary>
        Boss,
    }

    /// <summary>How an enemy fights (Docs/01-core-gameplay.md, enemy behavior). Caster, Charger and Support are not
    /// built yet.</summary>
    public enum EnemyArchetype
    {
        /// <summary>Fast, low health, chases in a loose cluster and bites at melee reach.</summary>
        Swarmer,

        /// <summary>Slow, high health; its slam paints a ground circle that fills during the wind-up, then hits
        /// whoever is still inside.</summary>
        Brute,

        /// <summary>Keeps its distance and shoots when it can see the player: a line telegraph, then a projectile
        /// that walls stop.</summary>
        Archer,

        /// <summary>Lunges along a telegraphed line at a player a few units away, then bites at reach (the Ash Wolf).</summary>
        Charger,

        /// <summary>Keeps its distance like an archer and places a filling circle on the player's spot that burns on
        /// after it lands (the Ember Acolyte).</summary>
        Caster,

        /// <summary>Stays behind its pack and makes allies near it hit harder, shown by a ring on the ground; backs off
        /// when the player comes close (the Pyre Keeper).</summary>
        Support,
    }

    /// <summary>
    /// Data for one enemy type. Ranges are in ground units. Values marked as tuning are starting points that
    /// Docs/01-core-gameplay.md does not fix yet.
    /// </summary>
    [CreateAssetMenu(menuName = "ARPG/Enemy Definition", fileName = "Enemy")]
    public class EnemyDefinition : ScriptableObject
    {
        [Tooltip("Sets life, damage and how much armor is worth through the formulas in Docs/03-itemization.md.")]
        [SerializeField, Min(1)] int level = 1;

        [Tooltip("Normal, Champion (a pack's lone leader) or Elite (every member of its pack). See Docs/00 and Docs/03.")]
        [SerializeField] EnemyRank rank = EnemyRank.Normal;

        [Tooltip("How it fights: Swarmer bites at reach, Brute slams a ground circle, Archer shoots from range.")]
        [SerializeField] EnemyArchetype archetype = EnemyArchetype.Swarmer;

        [Tooltip("Brute only: radius of the slam, in ground units. Docs/05: the Ghoul's slam is 2 units.")]
        [SerializeField, Min(0.5f)] float slamRadius = 2f;

        [Tooltip("Archer only: the distance it tries to keep from the player, backing off when closer. Tuning value.")]
        [SerializeField, Min(1f)] float preferredRange = 5f;

        [Tooltip("Archer only: projectile speed in ground units per second. Tuning value.")]
        [SerializeField, Min(1f)] float projectileSpeed = 9f;

        [Tooltip("Archer: the projectile's colour; clear for the default arrow streak (the cultist's fire bolt, 2026-10-04).")]
        [SerializeField] Color projectileColor = Color.clear;

        [Tooltip("Archetype adjustment on the level's base life. The docs call the Husk swarmer low health but give no number, so this is 1 until tuned.")]
        [SerializeField, Min(0.1f)] float lifeMultiplier = 1f;

        [Tooltip("Reduces the damage taken through the armor formula. The docs give no enemy armor values, so this is 0 until tuned.")]
        [SerializeField, Min(0f)] float armor;

        [Tooltip("Radius of the body on the ground, in ground units. A sweep hits when it reaches this far past the enemy's center. Tuning value.")]
        [SerializeField, Min(0.05f)] float bodyRadius = 0.3f;

        [Tooltip("Seconds the death animation takes before the enemy leaves the level. Tuning value.")]
        [SerializeField, Min(0f)] float deathSeconds = 0.45f;

        [Tooltip("The enemy starts an attack when the player is this close, in ground units. The docs give no swarmer attack numbers, so the attack values are tuning.")]
        [SerializeField, Min(0.1f)] float attackRange = 1f;

        [Tooltip("Seconds between starting an attack and it landing. The enemy swells during this time as a tell, and the player can step out of range. Tuning value.")]
        [SerializeField, Min(0f)] float attackWindupSeconds = 0.35f;

        [Tooltip("Seconds the enemy stands after an attack before it can attack again. Tuning value.")]
        [SerializeField, Min(0f)] float attackRecoverSeconds = 0.65f;

        [Tooltip("Archetype adjustment on the level's base hit damage from Docs/03-itemization.md. 1 until tuned.")]
        [SerializeField, Min(0f)] float damageMultiplier = 1f;

        [Tooltip("Ground units per second. Tuning value: swarmers are fast, but this stays under the player's 4 so they can be kited.")]
        [SerializeField, Min(0f)] float moveSpeed = 3.6f;

        [Tooltip("An idle enemy starts chasing when the player comes this close. Docs: 7 for normal enemies, 10 for elites.")]
        [SerializeField, Min(0f)] float aggroRange = 7f;

        [Tooltip("Beyond this distance the enemy starts losing track of the player. Docs: 20.")]
        [SerializeField, Min(0f)] float leashRange = 20f;

        [Tooltip("Seconds beyond the leash range before the enemy gives up. Docs: 4.")]
        [SerializeField, Min(0f)] float leashSeconds = 4f;

        [Tooltip("The enemy stops closing in at this distance from the player. Tuning value, becomes the melee reach later.")]
        [SerializeField, Min(0f)] float stopDistance = 0.7f;

        [Tooltip("Enemies closer than this push each other apart, which gives a swarm its loose cluster. Tuning value.")]
        [SerializeField, Min(0.1f)] float separationRadius = 0.8f;

        [Tooltip("Multiplies the sprite's base size, so a Champion or Elite reads as bigger at a glance. Tuning value.")]
        [SerializeField, Min(0.1f)] float visualScale = 1f;

        [Tooltip("Left empty, the prefab's own body sprite is used (every Normal enemy). Set to give a Champion or " +
                 "Elite variant its own color: SpriteRenderer.color multiplies the sprite's own pixels, so tinting " +
                 "an already-colored sprite at runtime cannot reach a clean, distinct color, only a darker shade " +
                 "of the same hue. A separate sprite is the only way to get a real color difference.")]
        [SerializeField] Sprite bodySprite;

        // The rest of act 1's roster (2026-10-05, Docs/05). Tuning values; zero or false leaves the behaviour out.
        [Header("Charger: the lunge")]
        [SerializeField, Min(0f)] float lungeDistance = 4f;
        [SerializeField, Min(0f)] float lungeMinRange = 3f;
        [SerializeField, Min(0.1f)] float lungeSpeed = 12f;
        [SerializeField, Min(0f)] float lungeWindupSeconds = 0.5f;
        [SerializeField, Min(0f)] float lungeCooldownSeconds = 4f;

        [Header("Strikes and backing off (the Cutthroat)")]
        [SerializeField, Min(1)] int strikes = 1;
        [SerializeField, Min(0f)] float strikeGapSeconds = 0.25f;
        [SerializeField, Min(0f)] float retreatDistance;
        [SerializeField, Min(0f)] float retreatSeconds;

        [Header("Caster: burning ground")]
        [SerializeField, Min(0f)] float burnSeconds = 3f;
        [SerializeField, Min(0f)] float burnHitsPerSecond = 0.5f;

        [Header("Support: the aura")]
        [SerializeField, Min(0f)] float auraRadius = 5f;
        [SerializeField, Min(0f)] float auraDamageBonus = 0.2f;

        // The deep levels' roster (2026-10-05, from Docs/05's later acts).
        [Header("A hit that slows the player (the Drowned, the Harpooner)")]
        [SerializeField, Range(0f, 0.9f)] float hitSlowFraction;
        [SerializeField, Min(0f)] float hitSlowSeconds = 1f;

        [Header("A brute's slam lands this far ahead of it, toward the player (0: around itself)")]
        [SerializeField, Min(0f)] float slamForward;

        [Header("Support: healing (the Grave Priest)")]
        [SerializeField, Range(0f, 1f)] float healPercent;
        [SerializeField, Min(0.5f)] float healEverySeconds = 6f;
        [SerializeField, Min(0f)] float healRadius = 6f;

        [Header("Caster: a circle that pulses instead of burning (the Rift Caller)")]
        [SerializeField, Min(0)] int castPulses;
        [SerializeField, Min(0.1f)] float castPulseGap = 1f;

        [Header("Burst (the Carrion Bloat)")]
        [SerializeField] bool burstOnDeath;
        [SerializeField] bool diesOnAttack;
        [SerializeField, Min(0f)] float burstFillSeconds = 0.8f;

        [Tooltip("The baked character whose sheets animate this enemy (Resources/Characters/<name>: <name>_idle, _run, " +
                 "_attack, _hit, _death, from the sprite bake). While they do not exist the placeholder body shows. A " +
                 "Champion or Elite may name its own rank look (husk_champion) or share the base one.")]
        [SerializeField] string spriteCharacter = "";

        public int Level => level;
        public EnemyRank Rank => rank;
        public EnemyArchetype Archetype => archetype;
        public float SlamRadius => slamRadius;
        public float PreferredRange => preferredRange;
        public float ProjectileSpeed => projectileSpeed;
        public float MaxLife => MaxLifeAt(level);

        /// <summary>Life for this archetype at another level, for an enemy a dungeon level spawns above or below the
        /// definition's own level.</summary>
        public float MaxLifeAt(int atLevel) => CombatFormulas.EnemyLife(atLevel) * lifeMultiplier;
        public float Armor => armor;
        public float BodyRadius => bodyRadius;
        public float DeathSeconds => deathSeconds;
        public float AttackRange => attackRange;
        public float AttackWindupSeconds => attackWindupSeconds;
        public float AttackRecoverSeconds => attackRecoverSeconds;
        public float DamageMultiplier => damageMultiplier;
        public float MoveSpeed => moveSpeed;
        public float AggroRange => aggroRange;
        public float LeashRange => leashRange;
        public float LeashSeconds => leashSeconds;
        public float StopDistance => stopDistance;
        public float SeparationRadius => separationRadius;
        public float VisualScale => visualScale;
        public Color ProjectileColor => projectileColor;
        public float LungeDistance => lungeDistance;
        public float LungeMinRange => lungeMinRange;
        public float LungeSpeed => lungeSpeed;
        public float LungeWindupSeconds => lungeWindupSeconds;
        public float LungeCooldownSeconds => lungeCooldownSeconds;
        public int Strikes => Mathf.Max(1, strikes);
        public float StrikeGapSeconds => strikeGapSeconds;
        public float RetreatDistance => retreatDistance;
        public float RetreatSeconds => retreatSeconds;
        public float BurnSeconds => burnSeconds;
        public float BurnHitsPerSecond => burnHitsPerSecond;
        public float AuraRadius => auraRadius;
        public float AuraDamageBonus => auraDamageBonus;
        public bool BurstOnDeath => burstOnDeath;
        public bool DiesOnAttack => diesOnAttack;
        public float BurstFillSeconds => burstFillSeconds;
        public float HitSlowFraction => hitSlowFraction;
        public float HitSlowSeconds => hitSlowSeconds;
        public float SlamForward => slamForward;
        public float HealPercent => healPercent;
        public float HealEverySeconds => healEverySeconds;
        public float HealRadius => healRadius;
        public int CastPulses => castPulses;
        public float CastPulseGap => castPulseGap;

        /// <summary>Null for a Normal enemy, which keeps the prefab's own body sprite.</summary>
        public Sprite BodySprite => bodySprite;

        /// <summary>The baked character that animates this enemy, or empty for the placeholder body.</summary>
        public string SpriteCharacter => spriteCharacter;

#if UNITY_EDITOR
        /// <summary>For the editor tools that wire baked art to the definitions.</summary>
        public void SetSpriteCharacter(string value) => spriteCharacter = value;
#endif
    }
}
