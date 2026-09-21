using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The player's automatic combat from Docs/01-core-gameplay.md. Every frame it picks a target, swings the basic
    /// attack at the attack rate, and fires auto-cast skills in slot order whenever the global cast timer is free.
    /// There is no input: movement is the only thing the player controls.
    /// Runs in Update for now; combat will move to a fixed timestep (Docs/07-technical.md).
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        // From Docs/01-core-gameplay.md.
        const float GlobalCastSeconds = 0.35f;
        const float FocusMax = 100f;
        const float FocusRegenPerSecond = 6f;
        const float FocusPerBasicAttack = 4f;

        // The docs give no starting Focus; a fresh character starts full. Tuning value.
        const float StartingFocus = 100f;

        // A body reaches into a sweep before its center does, so target queries look this far past the sweep's range.
        // It only has to exceed the largest enemy body radius.
        const float QueryMargin = 1f;

        // Below this speed, in ground units per second, the character keeps its last facing.
        const float FacingSpeedThreshold = 0.2f;

        const float EffectSeconds = 0.14f;
        const int EffectPoolSize = 6;

        [Tooltip("Left empty, the first PlayerController in the scene is used.")]
        [SerializeField] PlayerController player;

        [Tooltip("Left empty, the first EnemyManager in the scene is used.")]
        [SerializeField] EnemyManager enemies;

        [Tooltip("Docs: attacks per second starts at 1.4.")]
        [SerializeField, Min(0.1f)] float attacksPerSecond = 1.4f;

        [Tooltip("Docs: Warden reach 2.0 units.")]
        [SerializeField, Min(0.1f)] float basicRange = 2f;

        [Tooltip("Docs: basic attack is a 120 degree sweep.")]
        [SerializeField, Range(10f, 360f)] float basicArcDegrees = 120f;

        [SerializeField] Sprite basicEffectSprite;

        [SerializeField] Color basicEffectColor = new Color(1f, 1f, 1f, 0.55f);

        [Tooltip("Auto-cast skills in priority order, slot 1 first.")]
        [SerializeField] SkillDefinition[] skills;

        // Everything the query found near the character, and the subset within reach that can be chosen as the target.
        readonly List<EnemyController> candidates = new List<EnemyController>(32);
        readonly List<EnemyController> inReach = new List<EnemyController>(32);
        readonly List<Vector2> inReachPositions = new List<Vector2>(32);

        FocusPool focus;
        float[] cooldowns;
        float attackTimer;
        float castTimer;
        Vector2 facing = Vector2.up;
        SweepEffect[] effects;
        int nextEffect;

        public FocusPool Focus => focus;

        /// <summary>Enemies this character has killed since the scene started.</summary>
        public int Kills { get; private set; }

        /// <summary>How many basic attacks and skill casts have fired, for tests and tuning.</summary>
        public int BasicAttackCount { get; private set; }

        public int SkillCastCount { get; private set; }

        /// <summary>
        /// Average damage of the equipped weapon, before skill multipliers and armor. A character that has lost its
        /// weapon fights unarmed, which the weapon curve values at item level 0.
        /// </summary>
        public float WeaponDamage => GameSession.Current.Equipment.WeaponDamage;

        public float SkillCooldownRemaining(int slot) => cooldowns != null && slot >= 0 && slot < cooldowns.Length ? cooldowns[slot] : 0f;

        void Awake()
        {
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
            if (enemies == null)
                enemies = FindAnyObjectByType<EnemyManager>();

            if (skills == null)
                skills = new SkillDefinition[0];

            focus = new FocusPool(FocusMax, FocusRegenPerSecond, StartingFocus);
            cooldowns = new float[skills.Length];

            // Effects share a parent scaled to half height, which projects ground space onto the isometric view.
            var root = new GameObject("Sweep Effects");
            root.transform.SetParent(transform, false);
            root.transform.localScale = new Vector3(1f, IsoMath.GroundSquash, 1f);
            effects = new SweepEffect[EffectPoolSize];
            for (var i = 0; i < effects.Length; i++)
                effects[i] = SweepEffect.Create(root.transform);
        }

        void Update()
        {
            if (player == null || enemies == null || !enemies.IsReady)
                return;

            var deltaTime = Time.deltaTime;
            var origin = IsoMath.WorldToGround(player.transform.position);
            UpdateFacing();

            focus.Tick(deltaTime);
            attackTimer = Mathf.Max(0f, attackTimer - deltaTime);
            castTimer = Mathf.Max(0f, castTimer - deltaTime);
            for (var i = 0; i < cooldowns.Length; i++)
                cooldowns[i] = Mathf.Max(0f, cooldowns[i] - deltaTime);

            enemies.QueryEnemies(origin, LongestReach() + QueryMargin, candidates);
            if (candidates.Count == 0)
                return;

            var target = PickTarget(origin);
            if (target == null)
                return;

            var toTarget = target.GroundPosition - origin;
            var aim = toTarget.sqrMagnitude > 1e-6f ? toTarget.normalized : facing;

            if (castTimer <= 0f)
                TryCastSkill(origin, aim, target);

            if (attackTimer <= 0f && InReach(origin, target, basicRange))
                BasicAttack(origin, aim);
        }

        float LongestReach()
        {
            var reach = basicRange;
            for (var i = 0; i < skills.Length; i++)
                if (skills[i] != null && skills[i].Range > reach)
                    reach = skills[i].Range;
            return reach;
        }

        void UpdateFacing()
        {
            // The body faces the movement direction (Docs/01-core-gameplay.md); the weapon turns to the target.
            var velocity = player.GroundVelocity;
            if (velocity.magnitude > FacingSpeedThreshold)
                facing = velocity.normalized;
        }

        // Docs: the target is the nearest enemy in attack range inside the forward cone, else the nearest in range.
        EnemyController PickTarget(Vector2 origin)
        {
            var reach = LongestReach();
            inReach.Clear();
            inReachPositions.Clear();
            for (var i = 0; i < candidates.Count; i++)
            {
                if (!InReach(origin, candidates[i], reach))
                    continue;

                inReach.Add(candidates[i]);
                inReachPositions.Add(candidates[i].GroundPosition);
            }

            var index = SweepGeometry.PickTarget(origin, facing, inReachPositions);
            return index >= 0 ? inReach[index] : null;
        }

        static bool InReach(Vector2 origin, EnemyController target, float range) =>
            Vector2.Distance(origin, target.GroundPosition) <= range + target.Definition.BodyRadius;

        void BasicAttack(Vector2 origin, Vector2 aim)
        {
            attackTimer = 1f / attacksPerSecond;
            BasicAttackCount++;

            var hits = Sweep(origin, aim, basicRange, basicArcDegrees, 1f, basicEffectSprite, basicEffectColor);

            // Docs: Focus is gained on a basic attack hit. One gain per swing that lands, however many it hits.
            if (hits > 0)
                focus.Gain(FocusPerBasicAttack);
        }

        void TryCastSkill(Vector2 origin, Vector2 aim, EnemyController target)
        {
            for (var i = 0; i < skills.Length; i++)
            {
                var skill = skills[i];
                if (skill == null || cooldowns[i] > 0f)
                    continue;
                if (!TriggerPasses(skill) || !InReach(origin, target, skill.Range))
                    continue;
                if (!focus.TrySpend(skill.FocusCost))
                    continue;

                cooldowns[i] = skill.CooldownSeconds;
                castTimer = GlobalCastSeconds;
                SkillCastCount++;
                Sweep(origin, aim, skill.Range, skill.ArcDegrees, skill.DamageMultiplier, skill.EffectSprite, skill.EffectColor);
                return;
            }
        }

        static bool TriggerPasses(SkillDefinition skill)
        {
            switch (skill.Trigger)
            {
                case SkillTrigger.Always:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Damages every candidate inside the sweep. Returns how many it hit.</summary>
        int Sweep(Vector2 origin, Vector2 aim, float range, float arcDegrees, float multiplier, Sprite sprite, Color color)
        {
            var weaponDamage = WeaponDamage;
            var hits = 0;

            for (var i = 0; i < candidates.Count; i++)
            {
                var enemy = candidates[i];
                if (!enemy.IsAlive)
                    continue;

                var reach = range + enemy.Definition.BodyRadius;
                if (!SweepGeometry.Contains(origin, aim, enemy.GroundPosition, reach, arcDegrees))
                    continue;

                hits++;
                var damage = CombatFormulas.HitDamage(
                    weaponDamage, multiplier, 0f, 0f, 1f, false, 0f, enemy.Definition.Armor, enemy.Definition.Level);
                if (enemy.TakeDamage(damage))
                    Kills++;
            }

            var effect = effects[nextEffect];
            nextEffect = (nextEffect + 1) % effects.Length;
            effect.Play(origin, aim, range, sprite, color, EffectSeconds);
            return hits;
        }
    }
}
