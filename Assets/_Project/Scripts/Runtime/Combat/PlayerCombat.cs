using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The player's automatic combat from Docs/01-core-gameplay.md, for the Wrathborn (Docs/02-classes-and-skills.md).
    /// Every frame it picks a target, swings the basic attack at the attack rate, and fires the skills in the four
    /// loadout slots (<see cref="SkillLoadout"/>) in slot order whenever the global cast timer is free, the skill is off
    /// cooldown and paid for, a valid target or area exists and the slot's trigger passes: the skill's own (Docs/02's
    /// trigger column) or one of its two alternatives (<see cref="SkillTriggerRules"/>). The class's eight skills:
    /// Ground Breaker, Hurl Axe, Bull Rush and Hew (the M1 slice), Battle Roar and Blood Frenzy (buffs), Rending Spin (a
    /// channel that holds back the lower slots and the basic attack while it spins) and Skullsplitter (an execute).
    /// Skills cost Rage, which the basic attack, hits taken, Bull Rush and Battle Roar build (<see cref="RagePool"/>);
    /// Stillness adds damage (<see cref="StanceStacks"/>).
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

        [Tooltip("The class's skills, in the order an unchosen loadout fills its slots. Each is usable from its own unlock level.")]
        [SerializeField] SkillDefinition[] skills;

        // Which class skill each loadout slot holds (an index into skills), -1 for none.
        readonly int[] slotSkill = { -1, -1, -1, -1 };
        float stillSeconds;

        // Buffs (Battle Roar, Blood Frenzy): what they add and how long is left.
        float damageBuff;
        float damageBuffTimer;
        float speedBuff;
        float speedBuffPerMomentum;
        float speedBuffTimer;

        // The channel in progress (Rending Spin): it holds back the slots below its own and the basic attack.
        SkillDefinition channelSkill;
        int channelSlot = -1;
        float channelTimer;
        float channelTickTimer;

        // The execute in its wind-up (Skullsplitter): the blow lands on its target when the timer runs out.
        SkillDefinition executeSkill;
        EnemyController executeTarget;
        float executeTimer;

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

        /// <summary>The enemy the basic attack is aimed at this frame, or null. The animation follows it through a swing.</summary>
        public EnemyController Target { get; private set; }

        /// <summary>A basic attack starts, with its ground direction and the time until the next may start (so an
        /// animation can fit the attack rate).</summary>
        public event System.Action<Vector2, float> BasicAttackStarted;

        /// <summary>A skill fires, with its ground direction.</summary>
        public event System.Action<SkillDefinition, Vector2> SkillCast;

        public IReadOnlyList<SkillDefinition> Skills => skills;

        /// <summary>
        /// Average damage of the equipped weapon, before skill multipliers and armor. A character that has lost its
        /// weapon fights unarmed, which the weapon curve values at item level 0.
        /// </summary>
        public float WeaponDamage => GameSession.Current.Equipment.WeaponDamage;

        /// <summary>How many times the skill in a slot has fired since the scene started, for tests and tuning.</summary>
        public int CastCount(int slot) => castCounts != null && slot >= 0 && slot < castCounts.Length ? castCounts[slot] : 0;

        public float SkillCooldownRemaining(int slot) => cooldowns != null && slot >= 0 && slot < cooldowns.Length ? cooldowns[slot] : 0f;

        /// <summary>Whether the class skill at this index is unlocked at the character's level.</summary>
        public bool IsUnlocked(int skillIndex) =>
            skills != null && skillIndex >= 0 && skillIndex < skills.Length && skills[skillIndex] != null &&
            SkillRules.IsUnlocked(skills[skillIndex].UnlockLevel, GameSession.Current.Level);

        /// <summary>The skill in a loadout slot, or null.</summary>
        public SkillDefinition SlotSkill(int slot) =>
            slot >= 0 && slot < slotSkill.Length && slotSkill[slot] >= 0 ? skills[slotSkill[slot]] : null;

        /// <summary>Whether a channel (Rending Spin) is spinning now.</summary>
        public bool Channeling => channelTimer > 0f;

        /// <summary>The class's skills with their unlock levels, in fill order, for the loadout.</summary>
        public List<(string id, int unlockLevel)> ClassSkillList()
        {
            var list = new List<(string, int)>(skills.Length);
            foreach (var skill in skills)
                if (skill != null)
                    list.Add((skill.name, skill.UnlockLevel));
            return list;
        }

        /// <summary>The class skill with this asset name, or null.</summary>
        public SkillDefinition FindSkill(string id)
        {
            foreach (var skill in skills)
                if (skill != null && skill.name == id)
                    return skill;
            return null;
        }

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
            session.Loadout.Changed += RefreshSlots;
            session.Loadout.Fill(ClassSkillList(), session.Level, session.Level);
            RefreshSlots();
            if (health != null)
                health.HitTaken += OnHitTaken;
            if (player != null)
                RageArc.Create(this, player);
        }

        void OnDestroy()
        {
            if (session != null)
            {
                session.LeveledUp -= OnLeveledUp;
                session.Loadout.Changed -= RefreshSlots;
            }
            if (health != null)
                health.HitTaken -= OnHitTaken;
        }

        void RefreshSlots()
        {
            for (var s = 0; s < slotSkill.Length; s++)
            {
                var id = session.Loadout.SkillAt(s);
                slotSkill[s] = -1;
                for (var i = 0; i < skills.Length && id != null; i++)
                    if (skills[i] != null && skills[i].name == id)
                        slotSkill[s] = i;
            }
        }

        void OnHitTaken(float damage) => rage.Gain(RagePool.PerHitTaken);

        // Docs/02: a new skill equips itself; say so above the character, after the level callout.
        void OnLeveledUp(int level)
        {
            var levels = new List<int>(skills.Length);
            foreach (var skill in skills)
                levels.Add(skill != null ? skill.UnlockLevel : int.MaxValue);
            var unlocked = SkillRules.NewlyUnlocked(levels, knownLevel, level);
            // A new skill takes an empty slot by itself; with the four full it waits in the Bag's Skills page.
            session.Loadout.Fill(ClassSkillList(), level, knownLevel);
            knownLevel = level;
            if (player == null)
                return;
            for (var i = 0; i < unlocked.Count; i++)
            {
                var equipped = session.Loadout.SlotOf(skills[unlocked[i]].name) >= 0;
                DamageNumbers.Current?.ShowText(player.transform.position + new Vector3(0f, 2.3f + 0.5f * i, 0f),
                    "NEW SKILL: " + skills[unlocked[i]].DisplayName + (equipped ? "" : " (Bag > Skills)"), NewSkillColor, 46);
            }
        }

        void Update()
        {
            if (player == null || enemies == null || !enemies.IsReady)
                return;

            var deltaTime = Time.deltaTime;
            var origin = IsoMath.WorldToGround(player.transform.position);
            UpdateFacing();

            // The passive tree's Rage changes (Docs/02): Short Fuse's longer delay, Berserker's double gain below half life.
            var tree = session.PassiveTree.Bonuses;
            rage.DrainDelay = RagePool.DrainDelaySeconds + tree.RageDrainDelay;
            var attributes = CharacterAttributes.At(session.Level);
            rage.GainMultiplier = (tree.Berserker && health != null && health.Fraction < 0.5f ? 2f : 1f) * (1f + attributes.RageGain);
            rage.Tick(deltaTime);
            stillSeconds = player.GroundVelocity.magnitude < FacingSpeedThreshold ? stillSeconds + deltaTime : 0f;
            damageBuffTimer = Mathf.Max(0f, damageBuffTimer - deltaTime);
            speedBuffTimer = Mathf.Max(0f, speedBuffTimer - deltaTime);
            attackTimer = Mathf.Max(0f, attackTimer - deltaTime);
            castTimer = Mathf.Max(0f, castTimer - deltaTime);
            for (var i = 0; i < cooldowns.Length; i++)
                cooldowns[i] = Mathf.Max(0f, cooldowns[i] - deltaTime);

            UpdateAxes(deltaTime);
            UpdateSlamEffect(deltaTime);
            if (charging)
                UpdateCharge(origin);
            if (channelTimer > 0f)
                UpdateChannel(origin, deltaTime);
            if (executeSkill != null)
                UpdateExecute(origin, deltaTime);

            enemies.QueryEnemies(origin, LongestReach() + QueryMargin, candidates);
            Target = null;
            if (candidates.Count == 0)
                return;

            var target = PickTarget(origin);
            Target = target;
            var aim = target != null ? (target.GroundPosition - origin).normalized : facing;
            if (aim.sqrMagnitude < 1e-6f)
                aim = facing;

            if (castTimer <= 0f && !charging)
                TryCastSkill(origin, aim, target);

            if (attackTimer <= 0f && target != null && !charging && channelTimer <= 0f)
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
            var tree = session.PassiveTree.Bonuses;
            attackTimer = 1f / (attacksPerSecond * (1f + equipment.AttackSpeedPercent / 100f + AttackSpeedBuff +
                                                    tree.AttackSpeed + tree.AttackSpeedPerMomentum * player.Stance.Momentum +
                                                    CharacterAttributes.At(session.Level).AttackSpeed));
            BasicAttackCount++;
            BasicAttackStarted?.Invoke(aim, attackTimer);

            Sfx.Play(SoundId.Swing, 0.6f);
            var hits = Sweep(origin, aim, basicRange, basicArcDegrees, 1f, basicEffectSprite, basicEffectColor);

            // Docs/02: Rage is gained on a basic attack hit, once per swing that lands, however many it hits.
            if (hits > 0)
                rage.Gain(RagePool.PerBasicHit + tree.RagePerBasicHit);
        }

        /// <summary>Increased attack speed from Blood Frenzy while it lasts: its own, and more for each live Momentum
        /// stack (Docs/02).</summary>
        float AttackSpeedBuff => speedBuffTimer > 0f ? speedBuff + speedBuffPerMomentum * player.Stance.Momentum : 0f;

        void TryCastSkill(Vector2 origin, Vector2 aim, EnemyController target)
        {
            // Docs/01: a channel locks the lower priority slots until it ends.
            var slots = channelTimer > 0f ? channelSlot : slotSkill.Length;
            for (var s = 0; s < slots; s++)
            {
                var i = slotSkill[s];
                if (i < 0 || !IsUnlocked(i) || cooldowns[i] > 0f)
                    continue;
                var skill = skills[i];
                if (skill.RageCost > rage.Current)
                    continue;
                if (!Ready(skill, session.Loadout.TriggerAt(s), origin, aim, target, out var skillTarget))
                    continue;
                Cast(i, s, skill, origin, aim, skillTarget);
                return;
            }
        }

        // A valid target or area first (Docs/01), then the slot's trigger: the skill's own condition (Docs/02's trigger
        // column), or the alternative the player chose, which replaces it.
        bool Ready(SkillDefinition skill, SkillTrigger trigger, Vector2 origin, Vector2 aim, EnemyController target, out EnemyController skillTarget)
        {
            skillTarget = null;
            var own = trigger == SkillTrigger.Default;
            switch (skill.Kind)
            {
                case SkillKind.Sweep:
                {
                    var inSweep = target != null ? CountInSweep(origin, aim, skill.Range, skill.ArcDegrees) : 0;
                    if (inSweep < 1)
                        return false;
                    if (own)
                        return inSweep >= skill.MinEnemies;
                    break;
                }
                case SkillKind.Slam:
                {
                    var around = CountAround(origin, skill.Range);
                    if (around < 1)
                        return false;
                    if (own)
                        return around >= skill.MinEnemies;
                    break;
                }
                case SkillKind.Projectile:
                    skillTarget = FindRangedTarget(origin, skill);
                    if (skillTarget == null)
                        return false;
                    if (own)
                        return true;
                    break;
                case SkillKind.Charge:
                    skillTarget = FindChargeTarget(origin, skill);
                    if (skillTarget == null)
                        return false;
                    if (own)
                        return true;
                    break;
                case SkillKind.Buff:
                {
                    var near = CountAround(origin, skill.Range);
                    if (near < 1)
                        return false;
                    if (own)
                        return near >= skill.MinEnemies &&
                               (skill.MaxRage <= 0f || rage.Current < skill.MaxRage) &&
                               (skill.MinMomentum <= 0 || (rage.InCombat && player.Stance.Momentum >= skill.MinMomentum));
                    break;
                }
                case SkillKind.Channel:
                {
                    var near = CountAround(origin, skill.Range);
                    if (near < 1)
                        return false;
                    if (own)
                        return near >= skill.MinEnemies && Moving;
                    break;
                }
                case SkillKind.Execute:
                    skillTarget = FindExecuteTarget(origin, skill, own);
                    if (skillTarget == null)
                        return false;
                    if (own)
                        return true;
                    break;
            }
            return SkillTriggerRules.Passes(trigger, new TriggerContext
            {
                EnemiesInArea = CountAround(origin, skill.Range),
                ElitePresent = EliteAround(origin, skill.Range),
                LifeFraction = health != null ? health.Fraction : 1f,
                StillSeconds = stillSeconds,
                Moving = Moving,
            });
        }

        bool Moving => player.GroundVelocity.magnitude >= ChargeMinSpeed;

        void Cast(int i, int slot, SkillDefinition skill, Vector2 origin, Vector2 aim, EnemyController skillTarget)
        {
            rage.TrySpend(skill.RageCost);
            if (skill.RageGain > 0f)
                rage.Gain(skill.RageGain);
            var cdr = GameSession.Current.Equipment.CooldownReductionPercent / 100f + CharacterAttributes.At(session.Level).CooldownReduction;
            cooldowns[i] = skill.CooldownSeconds * Mathf.Max(0.1f, 1f - cdr);
            castTimer = GlobalCastSeconds;
            SkillCastCount++;
            castCounts[i]++;
            rage.MarkCombat();
            var direction = skillTarget != null ? (skillTarget.GroundPosition - origin).normalized : aim;
            SkillCast?.Invoke(skill, direction);

            switch (skill.Kind)
            {
                case SkillKind.Sweep:
                    Sfx.Play(SoundId.Hew);
                    Sweep(origin, aim, skill.Range, skill.ArcDegrees, DamageOf(skill, skill.DamageMultiplier), skill.EffectSprite, skill.EffectColor);
                    break;
                case SkillKind.Slam:
                    Sfx.Play(SoundId.GroundBreaker);
                    Slam(origin, skill);
                    break;
                case SkillKind.Projectile:
                    Sfx.Play(SoundId.AxeThrow);
                    ThrowAxe(origin, skillTarget.GroundPosition - origin, skill);
                    break;
                case SkillKind.Charge:
                    Sfx.Play(SoundId.BullRush);
                    StartCharge(origin, skillTarget, skill);
                    break;
                case SkillKind.Buff:
                    StartBuff(origin, skill);
                    break;
                case SkillKind.Channel:
                    channelSkill = skill;
                    channelSlot = slot;
                    channelTimer = skill.DurationSeconds;
                    channelTickTimer = 0f;
                    break;
                case SkillKind.Execute:
                    Sfx.Play(SoundId.Swing);
                    executeSkill = skill;
                    executeTarget = skillTarget;
                    executeTimer = skill.WindupSeconds;
                    break;
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

        bool EliteAround(Vector2 origin, float radius)
        {
            for (var i = 0; i < candidates.Count; i++)
            {
                var enemy = candidates[i];
                var rank = enemy.Definition.Rank;
                if (enemy.IsAlive && (rank == EnemyRank.Elite || rank == EnemyRank.Boss) && InReach(origin, enemy, radius))
                    return true;
            }
            return false;
        }

        // Docs/02: Skullsplitter fires on a target below its threshold, or an elite or boss, in reach. The most wounded
        // comes first. With another trigger chosen, any enemy in reach will do.
        EnemyController FindExecuteTarget(Vector2 origin, SkillDefinition skill, bool strict)
        {
            EnemyController best = null;
            var bestFraction = float.MaxValue;
            for (var i = 0; i < candidates.Count; i++)
            {
                var enemy = candidates[i];
                if (!enemy.IsAlive || !InReach(origin, enemy, skill.Range))
                    continue;
                var fraction = enemy.MaxLife > 0f ? enemy.Life / enemy.MaxLife : 1f;
                var rank = enemy.Definition.Rank;
                if (strict && fraction >= skill.ExecuteThreshold && rank != EnemyRank.Elite && rank != EnemyRank.Boss)
                    continue;
                if (fraction >= bestFraction)
                    continue;
                best = enemy;
                bestFraction = fraction;
            }
            return best;
        }

        // --- Battle Roar and Blood Frenzy ----------------------------------------------------------------------

        void StartBuff(Vector2 origin, SkillDefinition skill)
        {
            var level = session.SkillLevels.LevelOf(skill.name);
            if (skill.BuffDamage > 0f)
            {
                damageBuff = SkillLevels.Buff(skill.BuffDamage, level);
                damageBuffTimer = skill.DurationSeconds;
            }
            if (skill.BuffAttackSpeed > 0f || skill.BuffAttackSpeedPerMomentum > 0f)
            {
                speedBuff = SkillLevels.Buff(skill.BuffAttackSpeed, level);
                speedBuffPerMomentum = skill.BuffAttackSpeedPerMomentum;
                speedBuffTimer = skill.DurationSeconds;
            }
            Sfx.Play(skill.BuffDamage > 0f ? SoundId.BossPhase : SoundId.Crit, 0.6f);
            // Placeholder feedback until the art exists: a ring on the ground and the name over the character.
            if (slamMarker == null)
                slamMarker = GroundMarker.Circle(origin, skill.Range, 0.08f, skill.EffectColor, transform);
            else
                slamMarker.RestartCircle(origin, skill.Range, 0.08f);
            slamMarkerTimer = SlamEffectSeconds;
            DamageNumbers.Current?.ShowText(player.transform.position + new Vector3(0f, 1.8f, 0f),
                skill.DisplayName.ToUpperInvariant(), new Color(skill.EffectColor.r, skill.EffectColor.g, skill.EffectColor.b, 1f), 40);
        }

        // --- Rending Spin --------------------------------------------------------------------------------------

        void UpdateChannel(Vector2 origin, float deltaTime)
        {
            channelTimer -= deltaTime;
            channelTickTimer -= deltaTime;
            if (channelTickTimer <= 0f)
            {
                channelTickTimer += channelSkill.TickSeconds;
                SpinHit(origin, channelSkill);
            }
            if (channelTimer <= 0f)
            {
                channelTimer = 0f;
                channelSkill = null;
                channelSlot = -1;
            }
        }

        // Every enemy within the basic reach is hit and bleeds (Docs/02: 90 percent every 0.3 s, a bleed).
        void SpinHit(Vector2 origin, SkillDefinition skill)
        {
            enemies.QueryEnemies(origin, basicRange + QueryMargin, nearby);
            var hits = 0;
            for (var i = 0; i < nearby.Count; i++)
            {
                var enemy = nearby[i];
                if (!enemy.IsAlive || !InReach(origin, enemy, basicRange))
                    continue;
                hits++;
                Strike(enemy, DamageOf(skill, skill.DamageMultiplier), movementSkill: true);
                if (enemy.IsAlive && skill.BleedMultiplier > 0f && skill.BleedSeconds > 0f)
                    enemy.ApplyBleed(Damage(enemy, DamageOf(skill, skill.BleedMultiplier), false) / skill.BleedSeconds, skill.BleedSeconds);
            }
            if (hits > 0)
            {
                HealOnHit(hits);
                rage.MarkCombat();
            }
            Sfx.Play(SoundId.Swing, 0.5f);
            PlayEffect(origin, facing, basicRange, skill.EffectSprite, skill.EffectColor);
            PlayEffect(origin, -facing, basicRange, skill.EffectSprite, skill.EffectColor);
        }

        // --- Skullsplitter -------------------------------------------------------------------------------------

        // The blow lands after the wind-up; the life threshold is read then (Docs/02's proposal), with half a unit of
        // slack for a target that moved.
        void UpdateExecute(Vector2 origin, float deltaTime)
        {
            executeTimer -= deltaTime;
            if (executeTimer > 0f)
                return;
            var skill = executeSkill;
            var target = executeTarget;
            executeSkill = null;
            executeTarget = null;
            if (target == null || !target.IsAlive || !InReach(origin, target, skill.Range + 0.5f))
                return;
            var fraction = target.MaxLife > 0f ? target.Life / target.MaxLife : 1f;
            var multiplier = DamageOf(skill, fraction < skill.ExecuteThreshold ? skill.ExecuteMultiplier : skill.DamageMultiplier);
            var direction = (target.GroundPosition - origin).normalized;
            Strike(target, multiplier);
            HealOnHit(1);
            rage.MarkCombat();
            Sfx.Play(SoundId.GroundBreaker, 0.8f);
            PlayEffect(origin, direction, skill.Range, skill.EffectSprite, skill.EffectColor);
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
                Strike(enemy, DamageOf(skill, skill.DamageMultiplier));
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
                Multiplier = DamageOf(skill, skill.DamageMultiplier),
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
                        Strike(enemy, axe.Multiplier, projectile: true);
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
                Strike(enemy, DamageOf(chargeSkill, chargeSkill.DamageMultiplier), movementSkill: true);
                // Crashing Wave (the passive tree): each enemy hit takes a second off Bull Rush's cooldown.
                var refund = session.PassiveTree.Bonuses.BullRushRefund;
                if (refund > 0f)
                    for (var c = 0; c < skills.Length; c++)
                        if (skills[c] == chargeSkill)
                            cooldowns[c] = Mathf.Max(0f, cooldowns[c] - refund);

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
        void Strike(EnemyController enemy, float multiplier, bool movementSkill = false, bool projectile = false)
        {
            var tree = session.PassiveTree.Bonuses;
            var attributes = CharacterAttributes.At(session.Level);
            var critical = Random.value < (GameSession.Current.Equipment.CriticalChancePercent + tree.CriticalChance + attributes.CriticalChance) / 100f;
            var damage = Damage(enemy, multiplier, critical, movementSkill, projectile);

            var world = IsoMath.GroundToWorld(enemy.GroundPosition);
            DamageNumbers.Current?.Show(new Vector3(world.x, world.y, 0f), damage, critical, isDamageToPlayer: false);

            if (enemy.TakeDamage(damage))
            {
                Kills++;
                if (tree.RageOnKill > 0f)
                    rage.Gain(tree.RageOnKill);
                HitStop.Instance?.Trigger(HitStopOnKillSeconds);
                Sfx.Play(SoundId.Kill);
            }
            else
            {
                Sfx.Play(critical ? SoundId.Crit : SoundId.Hit);
            }
        }

        /// <summary>A skill's damage multiplier at its level (Docs/02's proposal: plus 7 percent of level 1 a level).</summary>
        float DamageOf(SkillDefinition skill, float levelOneMultiplier) =>
            SkillLevels.Damage(levelOneMultiplier, session.SkillLevels.LevelOf(skill.name));

        /// <summary>The hit formula for this character against an enemy: the gear's modifiers, Stillness and Battle
        /// Roar as increased damage.</summary>
        float Damage(EnemyController enemy, float multiplier, bool critical, bool movementSkill = false, bool projectile = false)
        {
            var equipment = GameSession.Current.Equipment;
            var tree = session.PassiveTree.Bonuses;
            var increased = equipment.IncreasedDamagePercent / 100f + player.Stance.IncreasedDamage + (damageBuffTimer > 0f ? damageBuff : 0f);
            // Might: melee and area damage (Docs/02), so not the thrown axe.
            if (!projectile)
                increased += CharacterAttributes.At(session.Level).IncreasedDamage;
            // The passive tree (Docs/02, proposed numbers): its flat increase, Bloodied Edge against the wounded, Hatred
            // for Rage held, Battering Ram for the movement skills, and Berserker below half life.
            increased += tree.IncreasedDamage + tree.DamagePerTenRage * Mathf.Floor(rage.Current / 10f);
            if (enemy.MaxLife > 0f && enemy.Life / enemy.MaxLife < 0.5f)
                increased += tree.DamageVsWounded;
            if (movementSkill)
                increased += tree.MovementSkillDamage;
            if (tree.Berserker && health != null && health.Fraction < 0.5f)
                increased += 0.3f;
            // Butcher: critical hits on a bleeding enemy hit harder.
            var criticalDamage = equipment.CriticalDamagePercent / 100f + tree.CriticalDamage / 100f +
                                 (critical && enemy.IsBleeding ? tree.CritDamageVsBleeding : 0f);
            return CombatFormulas.HitDamage(
                WeaponDamage, multiplier, equipment.FlatWeaponDamageBonus, increased, 1f,
                critical, criticalDamage, enemy.Definition.Armor, enemy.Level);
        }

        void HealOnHit(int hits)
        {
            var tree = session.PassiveTree.Bonuses;
            var lifeOnHit = (GameSession.Current.Equipment.LifeOnHit + tree.LifeOnHit) * (1f + tree.MoreLifeOnHit);
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
