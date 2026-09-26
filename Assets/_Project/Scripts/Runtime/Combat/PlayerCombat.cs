using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The player's automatic combat from Docs/01-core-gameplay.md, for the Wrathborn (Docs/02-classes-and-skills.md).
    /// Every frame it picks a target, swings the basic attack at the attack rate, and fires the unlocked skills in slot
    /// order whenever the global cast timer is free and a skill's trigger passes. The slots, in order: Ground Breaker (a
    /// slam with 4 or more enemies around), Hurl Axe (a thrown axe at an enemy 4 to 9 units away in sight), Bull Rush (a
    /// charge while moving at an enemy 3 to 6 units ahead, which gains Rage instead of costing it) and Hew (a sweep, 2 or
    /// more enemies in reach). Skills cost Rage, which the basic attack, hits taken and Bull Rush build
    /// (<see cref="RagePool"/>); Stillness adds damage (<see cref="StanceStacks"/>).
    /// There is no input: movement is the only thing the player controls.
    /// Runs in Update for now; combat will move to a fixed timestep (Docs/07-technical.md).
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        // From Docs/01-core-gameplay.md.
        const float GlobalCastSeconds = 0.35f;

        // A body reaches into a sweep before its center does, so target queries look this far past the sweep's range.
        // It only has to exceed the largest enemy body radius.
        const float QueryMargin = 1f;

        // Tuning values; Docs give no numbers for hit feedback.
        const float HitStopOnKillSeconds = 0.05f;

        // Below this speed, in ground units per second, the character keeps its last facing, and does not count as
        // moving for Bull Rush's trigger.
        const float FacingSpeedThreshold = 0.2f;
        const float ChargeMinSpeed = 1f;

        // A charge hits what it passes within this distance of the character's center, plus the enemy's body.
        const float ChargeHitRadius = 0.8f;

        // An axe hits an enemy whose body it comes this close to.
        const float AxeHitRadius = 0.3f;
        const float AxeSpinDegreesPerSecond = 900f;

        const float EffectSeconds = 0.14f;
        const float SlamEffectSeconds = 0.25f;
        const int EffectPoolSize = 6;

        static readonly Color NewSkillColor = new Color(1f, 0.6f, 0.25f);

        [Tooltip("Left empty, the first PlayerController in the scene is used.")]
        [SerializeField] PlayerController player;

        [Tooltip("Left empty, the first EnemyManager in the scene is used.")]
        [SerializeField] EnemyManager enemies;

        [Tooltip("Left empty, the first PlayerHealth in the scene is used. Heals from the Life on Hit affix.")]
        [SerializeField] PlayerHealth health;

        [Tooltip("Docs: attacks per second starts at 1.4.")]
        [SerializeField, Min(0.1f)] float attacksPerSecond = 1.4f;

        [Tooltip("Docs: Wrathborn reach 2.0 units.")]
        [SerializeField, Min(0.1f)] float basicRange = 2f;

        [Tooltip("Docs: basic attack is a 120 degree sweep.")]
        [SerializeField, Range(10f, 360f)] float basicArcDegrees = 120f;

        [SerializeField] Sprite basicEffectSprite;

        [SerializeField] Color basicEffectColor = new Color(1f, 1f, 1f, 0.55f);

        [Tooltip("The class's skills in priority order, slot 1 first. Each is usable from its own unlock level.")]
        [SerializeField] SkillDefinition[] skills;

        // Everything the query found near the character, and the subset within reach that can be chosen as the target.
        readonly List<EnemyController> candidates = new List<EnemyController>(32);
        readonly List<EnemyController> inReach = new List<EnemyController>(32);
        readonly List<Vector2> inReachPositions = new List<Vector2>(32);
        readonly List<float> inReachWeights = new List<float>(32);
        readonly List<EnemyController> nearby = new List<EnemyController>(16);

        struct Axe
        {
            public Transform Transform;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Travelled;
            public float MaxDistance;
            public float Multiplier;
        }

        readonly List<Axe> axes = new List<Axe>(4);
        readonly Stack<Transform> spareAxes = new Stack<Transform>(4);

        // The charge in progress: who it already hit, so each enemy is hit once, and what it does.
        readonly HashSet<EnemyController> chargeHits = new HashSet<EnemyController>();
        bool charging;
        Vector2 chargeDirection;
        SkillDefinition chargeSkill;

        RagePool rage;
        float[] cooldowns;
        int[] castCounts;
        float attackTimer;
        float castTimer;
        Vector2 facing = Vector2.up;
        SweepEffect[] effects;
        int nextEffect;
        GroundMarker slamMarker;
        float slamMarkerTimer;
        GameSession session;
        int knownLevel;

        public RagePool Rage => rage;

        /// <summary>Enemies this character has killed since the scene started.</summary>
        public int Kills { get; private set; }

        /// <summary>How many basic attacks and skill casts have fired, for tests and tuning.</summary>
        public int BasicAttackCount { get; private set; }

        public int SkillCastCount { get; private set; }

        public IReadOnlyList<SkillDefinition> Skills => skills;

        /// <summary>
        /// Average damage of the equipped weapon, before skill multipliers and armor. A character that has lost its
        /// weapon fights unarmed, which the weapon curve values at item level 0.
        /// </summary>
        public float WeaponDamage => GameSession.Current.Equipment.WeaponDamage;

        /// <summary>How many times the skill in a slot has fired since the scene started, for tests and tuning.</summary>
        public int CastCount(int slot) => castCounts != null && slot >= 0 && slot < castCounts.Length ? castCounts[slot] : 0;

        public float SkillCooldownRemaining(int slot) => cooldowns != null && slot >= 0 && slot < cooldowns.Length ? cooldowns[slot] : 0f;

        public bool IsUnlocked(int slot) =>
            skills != null && slot >= 0 && slot < skills.Length && skills[slot] != null &&
            SkillRules.IsUnlocked(skills[slot].UnlockLevel, GameSession.Current.Level);

        void Awake()
        {
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
            if (enemies == null)
                enemies = FindAnyObjectByType<EnemyManager>();
            if (health == null)
                health = FindAnyObjectByType<PlayerHealth>();

            if (skills == null)
                skills = new SkillDefinition[0];

            // Rage starts empty every time a level loads: a fight opens on plain swings (Docs/02).
            rage = new RagePool();
            cooldowns = new float[skills.Length];
            castCounts = new int[skills.Length];

            // Effects share a parent scaled to half height, which projects ground space onto the isometric view.
            var root = new GameObject("Sweep Effects");
            root.transform.SetParent(transform, false);
            root.transform.localScale = new Vector3(1f, IsoMath.GroundSquash, 1f);
            effects = new SweepEffect[EffectPoolSize];
            for (var i = 0; i < effects.Length; i++)
                effects[i] = SweepEffect.Create(root.transform);

            session = GameSession.Current;
            knownLevel = session.Level;
            session.LeveledUp += OnLeveledUp;
            if (health != null)
                health.HitTaken += OnHitTaken;
            if (player != null)
                RageArc.Create(this, player);
        }

        void OnDestroy()
        {
            if (session != null)
                session.LeveledUp -= OnLeveledUp;
            if (health != null)
                health.HitTaken -= OnHitTaken;
        }

        void OnHitTaken(float damage) => rage.Gain(RagePool.PerHitTaken);

        // Docs/02: a new skill equips itself; say so above the character, after the level callout.
        void OnLeveledUp(int level)
        {
            var levels = new List<int>(skills.Length);
            foreach (var skill in skills)
                levels.Add(skill != null ? skill.UnlockLevel : int.MaxValue);
            var unlocked = SkillRules.NewlyUnlocked(levels, knownLevel, level);
            knownLevel = level;
            if (player == null)
                return;
            for (var i = 0; i < unlocked.Count; i++)
                DamageNumbers.Current?.ShowText(player.transform.position + new Vector3(0f, 2.3f + 0.5f * i, 0f),
                    "NEW SKILL: " + skills[unlocked[i]].DisplayName, NewSkillColor, 46);
        }

        void Update()
        {
            if (player == null || enemies == null || !enemies.IsReady)
                return;

            var deltaTime = Time.deltaTime;
            var origin = IsoMath.WorldToGround(player.transform.position);
            UpdateFacing();

            rage.Tick(deltaTime);
            attackTimer = Mathf.Max(0f, attackTimer - deltaTime);
            castTimer = Mathf.Max(0f, castTimer - deltaTime);
            for (var i = 0; i < cooldowns.Length; i++)
                cooldowns[i] = Mathf.Max(0f, cooldowns[i] - deltaTime);

            UpdateAxes(deltaTime);
            UpdateSlamEffect(deltaTime);
            if (charging)
                UpdateCharge(origin);

            enemies.QueryEnemies(origin, LongestReach() + QueryMargin, candidates);
            if (candidates.Count == 0)
                return;

            var target = PickTarget(origin);
            var aim = target != null ? (target.GroundPosition - origin).normalized : facing;
            if (aim.sqrMagnitude < 1e-6f)
                aim = facing;

            if (castTimer <= 0f && !charging)
                TryCastSkill(origin, aim, target);

            if (attackTimer <= 0f && target != null && !charging)
                BasicAttack(origin, aim);
        }

        float LongestReach()
        {
            var reach = basicRange;
            for (var i = 0; i < skills.Length; i++)
                if (IsUnlocked(i) && skills[i].Range > reach)
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
        // Melee reach: the skills that work further out pick their own targets.
        EnemyController PickTarget(Vector2 origin)
        {
            inReach.Clear();
            inReachPositions.Clear();
            inReachWeights.Clear();
            for (var i = 0; i < candidates.Count; i++)
            {
                if (!InReach(origin, candidates[i], basicRange))
                    continue;

                inReach.Add(candidates[i]);
                inReachPositions.Add(candidates[i].GroundPosition);
                // Docs: elites and bosses get a range weight bonus so they are preferred.
                var rank = candidates[i].Definition.Rank;
                inReachWeights.Add(rank == EnemyRank.Elite || rank == EnemyRank.Boss ? SweepGeometry.PreferredTargetWeight : 1f);
            }

            var index = SweepGeometry.PickTarget(origin, facing, inReachPositions, inReachWeights);
            return index >= 0 ? inReach[index] : null;
        }

        static bool InReach(Vector2 origin, EnemyController target, float range) =>
            Vector2.Distance(origin, target.GroundPosition) <= range + target.Definition.BodyRadius;

        void BasicAttack(Vector2 origin, Vector2 aim)
        {
            var equipment = GameSession.Current.Equipment;
            attackTimer = 1f / (attacksPerSecond * (1f + equipment.AttackSpeedPercent / 100f));
            BasicAttackCount++;

            var hits = Sweep(origin, aim, basicRange, basicArcDegrees, 1f, basicEffectSprite, basicEffectColor);

            // Docs/02: Rage is gained on a basic attack hit, once per swing that lands, however many it hits.
            if (hits > 0)
                rage.Gain(RagePool.PerBasicHit);
        }

        void TryCastSkill(Vector2 origin, Vector2 aim, EnemyController target)
        {
            for (var i = 0; i < skills.Length; i++)
            {
                var skill = skills[i];
                if (!IsUnlocked(i) || cooldowns[i] > 0f || skill.RageCost > rage.Current)
                    continue;

                EnemyController skillTarget = null;
                switch (skill.Kind)
                {
                    case SkillKind.Sweep:
                        if (target == null || CountInSweep(origin, aim, skill.Range, skill.ArcDegrees) < skill.MinEnemies)
                            continue;
                        break;
                    case SkillKind.Slam:
                        if (CountAround(origin, skill.Range) < skill.MinEnemies)
                            continue;
                        break;
                    case SkillKind.Projectile:
                        skillTarget = FindRangedTarget(origin, skill);
                        if (skillTarget == null)
                            continue;
                        break;
                    case SkillKind.Charge:
                        skillTarget = FindChargeTarget(origin, skill);
                        if (skillTarget == null)
                            continue;
                        break;
                }

                rage.TrySpend(skill.RageCost);
                if (skill.RageGain > 0f)
                    rage.Gain(skill.RageGain);
                var cdr = GameSession.Current.Equipment.CooldownReductionPercent / 100f;
                cooldowns[i] = skill.CooldownSeconds * Mathf.Max(0.1f, 1f - cdr);
                castTimer = GlobalCastSeconds;
                SkillCastCount++;
                castCounts[i]++;
                rage.MarkCombat();

                switch (skill.Kind)
                {
                    case SkillKind.Sweep:
                        Sweep(origin, aim, skill.Range, skill.ArcDegrees, skill.DamageMultiplier, skill.EffectSprite, skill.EffectColor);
                        break;
                    case SkillKind.Slam:
                        Slam(origin, skill);
                        break;
                    case SkillKind.Projectile:
                        ThrowAxe(origin, skillTarget.GroundPosition - origin, skill);
                        break;
                    case SkillKind.Charge:
                        StartCharge(origin, skillTarget, skill);
                        break;
                }
                return;
            }
        }

        int CountInSweep(Vector2 origin, Vector2 aim, float range, float arcDegrees)
        {
            var count = 0;
            for (var i = 0; i < candidates.Count; i++)
                if (candidates[i].IsAlive && SweepGeometry.Contains(origin, aim, candidates[i].GroundPosition, range + candidates[i].Definition.BodyRadius, arcDegrees))
                    count++;
            return count;
        }

        int CountAround(Vector2 origin, float radius)
        {
            var count = 0;
            for (var i = 0; i < candidates.Count; i++)
                if (candidates[i].IsAlive && InReach(origin, candidates[i], radius))
                    count++;
            return count;
        }

        // The nearest enemy in the skill's distance band that the character can see (walls block a throw).
        EnemyController FindRangedTarget(Vector2 origin, SkillDefinition skill)
        {
            EnemyController best = null;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < candidates.Count; i++)
            {
                var enemy = candidates[i];
                var distance = Vector2.Distance(origin, enemy.GroundPosition);
                if (!enemy.IsAlive || distance < skill.MinRange || distance > skill.Range || distance >= bestDistance)
                    continue;
                if (!enemies.Nav.HasLineOfSight(origin, enemy.GroundPosition))
                    continue;
                best = enemy;
                bestDistance = distance;
            }
            return best;
        }

        // Docs/02: Bull Rush fires while moving, at an enemy 3 to 6 units ahead.
        EnemyController FindChargeTarget(Vector2 origin, SkillDefinition skill)
        {
            var velocity = player.GroundVelocity;
            if (player.IsDashing || velocity.magnitude < ChargeMinSpeed)
                return null;

            EnemyController best = null;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < candidates.Count; i++)
            {
                var enemy = candidates[i];
                if (!enemy.IsAlive || !SkillRules.IsAhead(origin, velocity, enemy.GroundPosition, skill.MinRange, skill.Range))
                    continue;
                var distance = Vector2.Distance(origin, enemy.GroundPosition);
                if (distance >= bestDistance || !enemies.Nav.HasLineOfSight(origin, enemy.GroundPosition))
                    continue;
                best = enemy;
                bestDistance = distance;
            }
            return best;
        }

        // --- Hew and the basic attack --------------------------------------------------------------------------

        /// <summary>Damages every candidate inside the sweep. Returns how many it hit.</summary>
        int Sweep(Vector2 origin, Vector2 aim, float range, float arcDegrees, float multiplier, Sprite sprite, Color color)
        {
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
                Strike(enemy, multiplier);
            }

            HealOnHit(hits);
            PlayEffect(origin, aim, range, sprite, color);
            return hits;
        }

        // --- Ground Breaker ------------------------------------------------------------------------------------

        void Slam(Vector2 origin, SkillDefinition skill)
        {
            var hits = 0;
            for (var i = 0; i < candidates.Count; i++)
            {
                var enemy = candidates[i];
                if (!enemy.IsAlive || !InReach(origin, enemy, skill.Range))
                    continue;
                hits++;
                Strike(enemy, skill.DamageMultiplier);
                if (skill.EffectSeconds > 0f)
                    enemy.ApplySlow(skill.EffectStrength, skill.EffectSeconds);
            }
            HealOnHit(hits);

            // The slam's disc flashes on the ground where it landed.
            if (slamMarker == null)
                slamMarker = GroundMarker.Circle(origin, skill.Range, 0.08f, skill.EffectColor, transform);
            else
                slamMarker.RestartCircle(origin, skill.Range, 0.08f);
            slamMarkerTimer = SlamEffectSeconds;
        }

        void UpdateSlamEffect(float deltaTime)
        {
            if (slamMarker == null || slamMarkerTimer <= 0f)
                return;
            slamMarker.Advance(deltaTime);
            slamMarkerTimer -= deltaTime;
            if (slamMarkerTimer <= 0f)
                slamMarker.Hide();
        }

        // --- Hurl Axe ------------------------------------------------------------------------------------------

        void ThrowAxe(Vector2 origin, Vector2 toTarget, SkillDefinition skill)
        {
            var direction = toTarget.sqrMagnitude > 1e-6f ? toTarget.normalized : facing;
            var transformToUse = spareAxes.Count > 0 ? spareAxes.Pop() : CreateAxe(skill);
            transformToUse.gameObject.SetActive(true);
            transformToUse.position = IsoMath.GroundToWorld(origin);
            axes.Add(new Axe
            {
                Transform = transformToUse,
                Position = origin,
                Velocity = direction * skill.Speed,
                MaxDistance = skill.Range + 1f,
                Multiplier = skill.DamageMultiplier,
            });
        }

        Transform CreateAxe(SkillDefinition skill)
        {
            var sprite = skill.EffectSprite != null ? skill.EffectSprite : TelegraphArt.Ember;
            var go = GroundMarker.NewSprite("Thrown Axe", sprite, skill.EffectSprite != null ? Color.white : skill.EffectColor, transform, 0);
            go.GetComponent<SpriteRenderer>().sortingLayerName = GameSortingLayers.Effects;
            go.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            return go.transform;
        }

        // Each axe flies in short pieces so it can neither pass a wall nor an enemy on a long frame.
        void UpdateAxes(float deltaTime)
        {
            for (var i = axes.Count - 1; i >= 0; i--)
            {
                var axe = axes[i];
                var step = axe.Velocity * deltaTime;
                var pieces = Mathf.Max(1, Mathf.CeilToInt(step.magnitude / EnemyProjectileFlight.MaxStep));
                var piece = step / pieces;
                var done = false;
                for (var p = 0; p < pieces && !done; p++)
                {
                    var next = axe.Position + piece;
                    if (!enemies.Nav.IsWalkable(IsoMath.GroundToCell(next)))
                    {
                        done = true;
                        break;
                    }
                    axe.Position = next;
                    axe.Travelled += piece.magnitude;

                    enemies.QueryEnemies(axe.Position, QueryMargin + AxeHitRadius, nearby);
                    for (var n = 0; n < nearby.Count; n++)
                    {
                        var enemy = nearby[n];
                        if (!enemy.IsAlive || Vector2.Distance(enemy.GroundPosition, axe.Position) > enemy.Definition.BodyRadius + AxeHitRadius)
                            continue;
                        Strike(enemy, axe.Multiplier);
                        HealOnHit(1);
                        rage.MarkCombat();
                        done = true;
                        break;
                    }
                    if (axe.Travelled >= axe.MaxDistance)
                        done = true;
                }

                if (done)
                {
                    axe.Transform.gameObject.SetActive(false);
                    spareAxes.Push(axe.Transform);
                    axes[i] = axes[axes.Count - 1];
                    axes.RemoveAt(axes.Count - 1);
                    continue;
                }

                axe.Transform.position = IsoMath.GroundToWorld(axe.Position);
                axe.Transform.Rotate(0f, 0f, -AxeSpinDegreesPerSecond * deltaTime);
                axes[i] = axe;
            }
        }

        // --- Bull Rush -----------------------------------------------------------------------------------------

        void StartCharge(Vector2 origin, EnemyController target, SkillDefinition skill)
        {
            var toTarget = target.GroundPosition - origin;
            chargeDirection = toTarget.normalized;
            chargeSkill = skill;
            chargeHits.Clear();
            charging = true;
            player.Dash(chargeDirection, SkillRules.ChargeDistance(toTarget.magnitude, skill.Range + SkillRules.ChargeOvershoot), skill.Speed);
            PlayEffect(origin, chargeDirection, 1.4f, skill.EffectSprite, skill.EffectColor);
        }

        // Everything the charge passes is hit once and thrown aside, and each gives a Momentum stack (Docs/02).
        void UpdateCharge(Vector2 origin)
        {
            if (!player.IsDashing)
            {
                charging = false;
                return;
            }

            enemies.QueryEnemies(origin, ChargeHitRadius + QueryMargin, nearby);
            var hits = 0;
            for (var i = 0; i < nearby.Count; i++)
            {
                var enemy = nearby[i];
                if (!enemy.IsAlive || chargeHits.Contains(enemy) ||
                    Vector2.Distance(origin, enemy.GroundPosition) > ChargeHitRadius + enemy.Definition.BodyRadius)
                    continue;

                chargeHits.Add(enemy);
                hits++;
                Strike(enemy, chargeSkill.DamageMultiplier);

                // Knocked out of the path to whichever side it stands, and a little forward.
                var offset = enemy.GroundPosition - origin;
                var side = new Vector2(-chargeDirection.y, chargeDirection.x);
                if (Vector2.Dot(side, offset) < 0f)
                    side = -side;
                enemy.Push((side + chargeDirection * 0.5f).normalized * chargeSkill.EffectStrength);
                player.Stance.AddMomentum(1);
            }
            if (hits > 0)
            {
                HealOnHit(hits);
                rage.MarkCombat();
            }
        }

        // --- Shared --------------------------------------------------------------------------------------------

        /// <summary>One hit on one enemy: the hit formula with the gear's modifiers and Stillness, a crit roll, the
        /// damage number, and the kill's hit stop.</summary>
        void Strike(EnemyController enemy, float multiplier)
        {
            var equipment = GameSession.Current.Equipment;
            var stance = player.Stance;
            var critical = Random.value < equipment.CriticalChancePercent / 100f;
            var damage = CombatFormulas.HitDamage(
                WeaponDamage, multiplier, equipment.FlatWeaponDamageBonus,
                equipment.IncreasedDamagePercent / 100f + stance.IncreasedDamage, 1f,
                critical, equipment.CriticalDamagePercent / 100f, enemy.Definition.Armor, enemy.Level);

            var world = IsoMath.GroundToWorld(enemy.GroundPosition);
            DamageNumbers.Current?.Show(new Vector3(world.x, world.y, 0f), damage, critical, isDamageToPlayer: false);

            if (enemy.TakeDamage(damage))
            {
                Kills++;
                HitStop.Instance?.Trigger(HitStopOnKillSeconds);
            }
        }

        void HealOnHit(int hits)
        {
            var lifeOnHit = GameSession.Current.Equipment.LifeOnHit;
            if (hits > 0 && lifeOnHit > 0f && health != null)
                health.Heal(lifeOnHit * hits);
        }

        void PlayEffect(Vector2 origin, Vector2 aim, float range, Sprite sprite, Color color)
        {
            if (sprite == null)
                return;
            var effect = effects[nextEffect];
            nextEffect = (nextEffect + 1) % effects.Length;
            effect.Play(origin, aim, range, sprite, color, EffectSeconds);
        }
    }
}
