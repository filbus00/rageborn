using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The player's automatic combat from Docs/01-core-gameplay.md. The class is the Wild Arrow (Docs/02-classes-and-skills.md,
    /// bows only since 2026-09-30): every frame it picks a target in the bow's reach and in sight, looses the basic arrow at
    /// the attack rate, and fires the skills in the four
    /// loadout slots (<see cref="SkillLoadout"/>) in slot order whenever the global cast timer is free, the skill is off
    /// cooldown and paid for, a valid target or area exists and the slot's trigger passes: the skill's own (Docs/02's
    /// trigger column) or one of its two alternatives (<see cref="SkillTriggerRules"/>). The class's eight skills:
    /// Ground Breaker, Hurl Axe, Bull Rush and Hew (the M1 slice), Battle Roar and Blood Frenzy (buffs), Rending Spin (a
    /// channel that holds back the lower slots and the basic attack while it spins) and Skullsplitter (an execute).
    /// The skills are still the retired Wrathborn's until the Wild Arrow's are built; they cost Focus, which starts full,
    /// regenerates and grows with every basic arrow that hits (<see cref="FocusPool"/>); Stillness adds damage
    /// (<see cref="StanceStacks"/>).
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

        // A projectile (an arrow, a thrown axe) hits an enemy whose body it comes this close to.
        const float ShotHitRadius = 0.3f;
        const float AxeSpinDegreesPerSecond = 900f;

        // The basic arrow (Docs/02, the Wild Arrow): its flight speed, and how far past the reach it flies before it is
        // spent. The reach is the bow's (Docs/03: a short bow 7.5, a longbow 9).
        const float ArrowSpeed = 14f;
        const float ArrowOvershoot = 2f;

        // The retired Wrathborn's Rending Spin hits within his old melee reach, not the bow's, until the Wild Arrow's own
        // skills replace his (Docs/02).
        const float SpinReach = 2f;
        // Arrows fly at about chest height over their ground point (world units), and leave a trail puff this often.
        const float ArrowHeight = 0.45f;
        const float TrailSeconds = 0.018f;
        const float BloodTrailSeconds = 0.03f;
        static readonly Color BasicTrailColor = new Color(1f, 0.86f, 0.55f, 0.55f);

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

        // What a projectile in flight is: the basic arrow (it gives Focus when it hits), one of the Wild Arrow's skill
        // arrows, or the retired Wrathborn's thrown axe (it spins).
        enum ShotStyle { Basic, Volley, Pierce, Homing, Explosive, Axe }

        struct Shot
        {
            public Transform Transform;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Travelled;
            public float MaxDistance;
            public float Multiplier;
            public ShotStyle Style;
            public Color Glow;
            public float TrailTimer;
            // Pierce Arrow: who it went through already, and whether it carries blood since (its trail).
            public HashSet<EnemyController> Pierced;
            public bool Bloodied;
            public float BloodTimer;
            // Homing Arrow: what it chases and how fast it turns (radians a second).
            public EnemyController Chasing;
            public float TurnRadians;
            // Explosive Arrow: the burst's radius.
            public float BurstRadius;
        }

        readonly List<Shot> shots = new List<Shot>(24);
        readonly Stack<Transform> spareArrows = new Stack<Transform>(24);
        readonly Stack<Transform> spareAxes = new Stack<Transform>(4);
        readonly Stack<HashSet<EnemyController>> sparePierceSets = new Stack<HashSet<EnemyController>>(4);
        readonly List<EnemyController> homingTargets = new List<EnemyController>(8);
        ArrowFx fx;

        // The charge in progress: who it already hit, so each enemy is hit once, and what it does.
        readonly HashSet<EnemyController> chargeHits = new HashSet<EnemyController>();
        bool charging;
        Vector2 chargeDirection;
        SkillDefinition chargeSkill;

        FocusPool focus;
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

        /// <summary>The class's resource (Docs/02: the Wild Arrow's Focus).</summary>
        public FocusPool Focus => focus;

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

        /// <summary>The basic arrow's reach (Docs/03: 7.5 with a short bow, 9 with a longbow). Unarmed shoots as far as a
        /// short bow, so a character never has to walk into melee.</summary>
        public float BasicRange => GripRules.ShortBowReach + GameSession.Current.Equipment.GripReach;

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

            // Focus starts full every time a level loads: a fight opens on skills (Docs/02).
            focus = new FocusPool();
            cooldowns = new float[skills.Length];
            castCounts = new int[skills.Length];

            // Effects share a parent scaled to half height, which projects ground space onto the isometric view.
            var root = new GameObject("Sweep Effects");
            root.transform.SetParent(transform, false);
            root.transform.localScale = new Vector3(1f, IsoMath.GroundSquash, 1f);
            effects = new SweepEffect[EffectPoolSize];
            for (var i = 0; i < effects.Length; i++)
                effects[i] = SweepEffect.Create(root.transform);
            fx = new ArrowFx(transform);

            session = GameSession.Current;
            focus.BonusMax = session.AttributeBonuses.MaxFocus;
            focus.Fill();
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

        void OnHitTaken(float damage) => focus.MarkCombat();

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

            // Until the Wild Arrow's attributes replace the tree (Docs/02): Will adds to Focus regeneration, and the tree's
            // Berserker doubles the gain from hits below half life.
            var tree = session.PassiveTree.Bonuses;
            // The Focus attribute (Docs/02): faster regeneration and a larger pool.
            var attributes = session.AttributeBonuses;
            focus.RegenMultiplier = 1f + attributes.FocusRegen;
            focus.BonusMax = attributes.MaxFocus;
            focus.GainMultiplier = tree.Berserker && health != null && health.Fraction < 0.5f ? 2f : 1f;
            focus.Tick(deltaTime);
            stillSeconds = player.GroundVelocity.magnitude < FacingSpeedThreshold ? stillSeconds + deltaTime : 0f;
            damageBuffTimer = Mathf.Max(0f, damageBuffTimer - deltaTime);
            speedBuffTimer = Mathf.Max(0f, speedBuffTimer - deltaTime);
            attackTimer = Mathf.Max(0f, attackTimer - deltaTime);
            castTimer = Mathf.Max(0f, castTimer - deltaTime);
            for (var i = 0; i < cooldowns.Length; i++)
                cooldowns[i] = Mathf.Max(0f, cooldowns[i] - deltaTime);

            UpdateShots(deltaTime);
            fx.Tick(deltaTime);
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
            var reach = BasicRange;
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

        // Docs: the target is the nearest enemy in attack range inside the forward cone, else the nearest in range. With a
        // bow the range is its reach and the target must be in sight (Docs/01, 2026-09-30): an enemy behind a wall is never
        // picked. The skills pick their own targets.
        EnemyController PickTarget(Vector2 origin)
        {
            inReach.Clear();
            inReachPositions.Clear();
            inReachWeights.Clear();
            for (var i = 0; i < candidates.Count; i++)
            {
                if (!candidates[i].IsAlive || !InReach(origin, candidates[i], BasicRange) ||
                    !enemies.Nav.HasLineOfSight(origin, candidates[i].GroundPosition))
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
                                                    session.AttributeBonuses.AttackSpeed + equipment.GripAttackSpeedBonus) *
                                equipment.GripAttackSpeedFactor);
            BasicAttackCount++;
            BasicAttackStarted?.Invoke(aim, attackTimer);

            // One arrow at the target; it gives Focus when it hits (Docs/02).
            Sfx.Play(SoundId.ArrowShot, 0.5f);
            shots.Add(NewArrow(origin, aim * ArrowSpeed, BasicRange + ArrowOvershoot, 1f, ShotStyle.Basic, BasicTrailColor));

            // The quiver's extra arrow (Docs/03): now and then a second one, at another enemy in reach when there is one.
            if (Random.value < equipment.ExtraArrowPercent / 100f)
            {
                var second = aim;
                for (var i = 0; i < inReach.Count; i++)
                    if (inReach[i] != Target && inReach[i].IsAlive)
                    {
                        second = (inReach[i].GroundPosition - origin).normalized;
                        break;
                    }
                if (second == aim)
                    second = Rotate(aim, 8f);
                shots.Add(NewArrow(origin, second * ArrowSpeed, BasicRange + ArrowOvershoot, 1f, ShotStyle.Basic, BasicTrailColor));
            }
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
                if (skill.RageCost > focus.Current)
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
                               (skill.MaxRage <= 0f || focus.Current < skill.MaxRage) &&
                               (skill.MinMomentum <= 0 || (focus.InCombat && player.Stance.Momentum >= skill.MinMomentum));
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
                case SkillKind.Volley:
                {
                    // Split Arrow: at the basic attack's target, with enough enemies ahead inside the fan (and a margin).
                    if (target == null)
                        return false;
                    skillTarget = target;
                    var ahead = CountInSweep(origin, aim, skill.Range, Mathf.Max(60f, skill.SpreadDegrees + 30f));
                    if (own)
                        return ahead >= skill.MinEnemies;
                    break;
                }
                case SkillKind.PierceShot:
                case SkillKind.HomingShot:
                    // Pierce Arrow: an enemy 4 to 9 away in sight. Homing Arrow: any enemy in reach and in sight.
                    skillTarget = FindRangedTarget(origin, skill);
                    if (skillTarget == null)
                        return false;
                    if (own)
                        return true;
                    break;
                case SkillKind.ExplosiveShot:
                {
                    // Explosive Arrow: the enemy in sight with the most others within the burst around it.
                    skillTarget = FindClusterTarget(origin, skill, out var cluster);
                    if (skillTarget == null)
                        return false;
                    if (own)
                        return cluster >= skill.MinEnemies;
                    break;
                }
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
            focus.TrySpend(skill.RageCost);
            if (skill.RageGain > 0f)
                focus.Gain(skill.RageGain);
            var cdr = GameSession.Current.Equipment.CooldownReductionPercent / 100f + session.AttributeBonuses.CooldownReduction;
            cooldowns[i] = skill.CooldownSeconds * Mathf.Max(0.1f, 1f - cdr);
            castTimer = GlobalCastSeconds;
            SkillCastCount++;
            castCounts[i]++;
            focus.MarkCombat();
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
                case SkillKind.Volley:
                    Sfx.Play(SoundId.ArrowShot, 0.9f);
                    LooseVolley(origin, direction, skill);
                    break;
                case SkillKind.PierceShot:
                    Sfx.Play(SoundId.AxeThrow, 0.8f);
                    LoosePierce(origin, direction, skill);
                    break;
                case SkillKind.HomingShot:
                    Sfx.Play(SoundId.ArrowShot, 0.9f);
                    LooseHoming(origin, direction, skill);
                    break;
                case SkillKind.ExplosiveShot:
                    Sfx.Play(SoundId.ArrowShot, 1f);
                    shots.Add(Explosive(origin, direction, skill));
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

        // Explosive Arrow's target: of the enemies in reach and in sight, the one with the most enemies (itself counted)
        // within the burst around it; the nearer one on a tie.
        EnemyController FindClusterTarget(Vector2 origin, SkillDefinition skill, out int clusterSize)
        {
            EnemyController best = null;
            clusterSize = 0;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < candidates.Count; i++)
            {
                var enemy = candidates[i];
                if (!enemy.IsAlive || !InReach(origin, enemy, skill.Range))
                    continue;
                var count = 0;
                for (var j = 0; j < candidates.Count; j++)
                    if (candidates[j].IsAlive &&
                        Vector2.Distance(candidates[j].GroundPosition, enemy.GroundPosition) <= skill.BurstRadius + candidates[j].Definition.BodyRadius)
                        count++;
                var distance = Vector2.Distance(origin, enemy.GroundPosition);
                if (count < clusterSize || (count == clusterSize && distance >= bestDistance))
                    continue;
                if (!enemies.Nav.HasLineOfSight(origin, enemy.GroundPosition))
                    continue;
                best = enemy;
                clusterSize = count;
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

        // Every enemy within the spin's reach is hit and bleeds (Docs/02: 90 percent every 0.3 s, a bleed).
        void SpinHit(Vector2 origin, SkillDefinition skill)
        {
            enemies.QueryEnemies(origin, SpinReach + QueryMargin, nearby);
            var hits = 0;
            for (var i = 0; i < nearby.Count; i++)
            {
                var enemy = nearby[i];
                if (!enemy.IsAlive || !InReach(origin, enemy, SpinReach))
                    continue;
                hits++;
                Strike(enemy, DamageOf(skill, skill.DamageMultiplier), movementSkill: true);
                if (enemy.IsAlive && skill.BleedMultiplier > 0f && skill.BleedSeconds > 0f)
                    enemy.ApplyBleed(Damage(enemy, DamageOf(skill, skill.BleedMultiplier), false) / skill.BleedSeconds, skill.BleedSeconds);
            }
            if (hits > 0)
            {
                HealOnHit(hits);
                focus.MarkCombat();
            }
            Sfx.Play(SoundId.Swing, 0.5f);
            PlayEffect(origin, facing, SpinReach, skill.EffectSprite, skill.EffectColor);
            PlayEffect(origin, -facing, SpinReach, skill.EffectSprite, skill.EffectColor);
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
            focus.MarkCombat();
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

        // --- Arrows and Hurl Axe -----------------------------------------------------------------------------

        void ThrowAxe(Vector2 origin, Vector2 toTarget, SkillDefinition skill)
        {
            var direction = toTarget.sqrMagnitude > 1e-6f ? toTarget.normalized : facing;
            var axe = spareAxes.Count > 0 ? spareAxes.Pop() : CreateAxe(skill);
            axe.gameObject.SetActive(true);
            axe.position = IsoMath.GroundToWorld(origin);
            shots.Add(new Shot
            {
                Transform = axe,
                Position = origin,
                Velocity = direction * skill.Speed,
                MaxDistance = skill.Range + 1f,
                Multiplier = DamageOf(skill, skill.DamageMultiplier),
                Style = ShotStyle.Axe,
            });
        }

        // Split Arrow (Docs/02): the arrows spread evenly over the fan, centered on the target.
        void LooseVolley(Vector2 origin, Vector2 direction, SkillDefinition skill)
        {
            var count = skill.ProjectileCount;
            var multiplier = DamageOf(skill, skill.DamageMultiplier);
            var glow = Glow(skill);
            for (var k = 0; k < count; k++)
            {
                var angle = count > 1 ? skill.SpreadDegrees * (k / (float)(count - 1) - 0.5f) : 0f;
                shots.Add(NewArrow(origin, Rotate(direction, angle) * skill.Speed, skill.Range + ArrowOvershoot, multiplier, ShotStyle.Volley, glow));
            }
        }

        // Pierce Arrow (Docs/02): one arrow through every enemy on its line, until a wall or its range.
        void LoosePierce(Vector2 origin, Vector2 direction, SkillDefinition skill)
        {
            var shot = NewArrow(origin, direction * skill.Speed, skill.Range + ArrowOvershoot, DamageOf(skill, skill.DamageMultiplier), ShotStyle.Pierce, Glow(skill));
            shot.Pierced = sparePierceSets.Count > 0 ? sparePierceSets.Pop() : new HashSet<EnemyController>();
            shots.Add(shot);
        }

        // Homing Arrow (Docs/02): each arrow chases a different enemy in reach and in sight when there are enough,
        // nearest first; they leave fanned out and curve in.
        void LooseHoming(Vector2 origin, Vector2 direction, SkillDefinition skill)
        {
            homingTargets.Clear();
            var count = skill.ProjectileCount;
            // The nearest few in sight, picked without sorting the whole list (no allocation).
            for (var pick = 0; pick < count; pick++)
            {
                EnemyController best = null;
                var bestDistance = float.MaxValue;
                for (var i = 0; i < candidates.Count; i++)
                {
                    var enemy = candidates[i];
                    if (!enemy.IsAlive || homingTargets.Contains(enemy) || !InReach(origin, enemy, skill.Range))
                        continue;
                    var distance = Vector2.Distance(origin, enemy.GroundPosition);
                    if (distance >= bestDistance || !enemies.Nav.HasLineOfSight(origin, enemy.GroundPosition))
                        continue;
                    best = enemy;
                    bestDistance = distance;
                }
                if (best == null)
                    break;
                homingTargets.Add(best);
            }
            if (homingTargets.Count == 0)
                return;

            var multiplier = DamageOf(skill, skill.DamageMultiplier);
            var glow = Glow(skill);
            for (var k = 0; k < count; k++)
            {
                var chased = homingTargets[k % homingTargets.Count];
                var toTarget = (chased.GroundPosition - origin).normalized;
                if (toTarget.sqrMagnitude < 1e-6f)
                    toTarget = direction;
                var angle = count > 1 ? skill.SpreadDegrees * (k / (float)(count - 1) - 0.5f) : 0f;
                // Curving flight covers more ground than a straight line, so it may fly further.
                var shot = NewArrow(origin, Rotate(toTarget, angle) * skill.Speed, skill.Range * 1.6f + ArrowOvershoot, multiplier, ShotStyle.Homing, glow);
                shot.Chasing = chased;
                shot.TurnRadians = skill.TurnDegreesPerSecond * Mathf.Deg2Rad;
                shots.Add(shot);
            }
        }

        // Explosive Arrow (Docs/02): bursts on the first enemy or wall it meets.
        Shot Explosive(Vector2 origin, Vector2 direction, SkillDefinition skill)
        {
            var shot = NewArrow(origin, direction * skill.Speed, skill.Range + ArrowOvershoot, DamageOf(skill, skill.DamageMultiplier), ShotStyle.Explosive, Glow(skill));
            shot.BurstRadius = skill.BurstRadius;
            return shot;
        }

        static Color Glow(SkillDefinition skill) => new Color(skill.EffectColor.r, skill.EffectColor.g, skill.EffectColor.b, 0.8f);

        Shot NewArrow(Vector2 origin, Vector2 velocity, float maxDistance, float multiplier, ShotStyle style, Color glow)
        {
            var arrow = spareArrows.Count > 0 ? spareArrows.Pop() : CreateArrow();
            arrow.gameObject.SetActive(true);
            PlaceArrow(arrow, origin, velocity);
            return new Shot
            {
                Transform = arrow,
                Position = origin,
                Velocity = velocity,
                MaxDistance = maxDistance,
                Multiplier = multiplier,
                Style = style,
                Glow = glow,
            };
        }

        // An arrow is drawn at chest height over its ground point, pointing along its flight as seen on screen.
        static void PlaceArrow(Transform arrow, Vector2 ground, Vector2 velocity)
        {
            var world = IsoMath.GroundToWorld(ground);
            arrow.position = new Vector3(world.x, world.y + ArrowHeight, 0f);
            var screen = IsoMath.GroundToWorld(velocity);
            arrow.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(screen.y, screen.x) * Mathf.Rad2Deg);
        }

        Transform CreateAxe(SkillDefinition skill)
        {
            var sprite = skill.EffectSprite != null ? skill.EffectSprite : TelegraphArt.Ember;
            var go = GroundMarker.NewSprite("Thrown Axe", sprite, skill.EffectSprite != null ? Color.white : skill.EffectColor, transform, 0);
            go.GetComponent<SpriteRenderer>().sortingLayerName = GameSortingLayers.Effects;
            go.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            return go.transform;
        }

        // The arrow's pixel-art sprite (ArrowArt), drawn over the characters.
        Transform CreateArrow()
        {
            var go = GroundMarker.NewSprite("Arrow", ArrowArt.Arrow, Color.white, transform, 0);
            go.GetComponent<SpriteRenderer>().sortingLayerName = GameSortingLayers.Effects;
            return go.transform;
        }

        // Each projectile flies in short pieces so it can neither pass a wall nor an enemy on a long frame. Most stop at
        // the first enemy they meet; the pierce arrow goes on through, bleeding, and the explosive one bursts.
        void UpdateShots(float deltaTime)
        {
            for (var i = shots.Count - 1; i >= 0; i--)
            {
                var shot = shots[i];
                if (shot.Style == ShotStyle.Homing)
                    Steer(ref shot, deltaTime);

                var step = shot.Velocity * deltaTime;
                var pieces = Mathf.Max(1, Mathf.CeilToInt(step.magnitude / EnemyProjectileFlight.MaxStep));
                var piece = step / pieces;
                var direction = shot.Velocity.sqrMagnitude > 1e-6f ? shot.Velocity.normalized : Vector2.right;
                var done = false;
                for (var p = 0; p < pieces && !done; p++)
                {
                    var next = shot.Position + piece;
                    if (!enemies.Nav.IsWalkable(IsoMath.GroundToCell(next)))
                    {
                        HitWall(ref shot, direction);
                        done = true;
                        break;
                    }
                    shot.Position = next;
                    shot.Travelled += piece.magnitude;

                    enemies.QueryEnemies(shot.Position, QueryMargin + ShotHitRadius, nearby);
                    for (var n = 0; n < nearby.Count && !done; n++)
                    {
                        var enemy = nearby[n];
                        if (!enemy.IsAlive || Vector2.Distance(enemy.GroundPosition, shot.Position) > enemy.Definition.BodyRadius + ShotHitRadius)
                            continue;
                        if (shot.Pierced != null && shot.Pierced.Contains(enemy))
                            continue;
                        done = HitEnemy(ref shot, enemy, direction);
                    }
                    if (shot.Travelled >= shot.MaxDistance)
                        done = true;
                }

                if (done)
                {
                    Retire(shot);
                    shots[i] = shots[shots.Count - 1];
                    shots.RemoveAt(shots.Count - 1);
                    continue;
                }

                if (shot.Style == ShotStyle.Axe)
                {
                    shot.Transform.position = IsoMath.GroundToWorld(shot.Position);
                    shot.Transform.Rotate(0f, 0f, -AxeSpinDegreesPerSecond * deltaTime);
                }
                else
                {
                    PlaceArrow(shot.Transform, shot.Position, shot.Velocity);
                    Trail(ref shot, direction, deltaTime);
                }
                shots[i] = shot;
            }
        }

        // A homing arrow turns toward what it chases, at most its turn rate; once that dies it flies straight on.
        static void Steer(ref Shot shot, float deltaTime)
        {
            if (shot.Chasing == null || !shot.Chasing.IsAlive)
                return;
            var speed = shot.Velocity.magnitude;
            var want = shot.Chasing.GroundPosition - shot.Position;
            if (speed < 1e-4f || want.sqrMagnitude < 1e-6f)
                return;
            var current = Mathf.Atan2(shot.Velocity.y, shot.Velocity.x);
            var target = Mathf.Atan2(want.y, want.x);
            var turn = Mathf.DeltaAngle(current * Mathf.Rad2Deg, target * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            var limit = shot.TurnRadians * deltaTime;
            var angle = current + Mathf.Clamp(turn, -limit, limit);
            shot.Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
        }

        // The glow trail behind every arrow, and the pierce arrow's blood once it has gone through someone.
        void Trail(ref Shot shot, Vector2 direction, float deltaTime)
        {
            var world = IsoMath.GroundToWorld(shot.Position);
            shot.TrailTimer -= deltaTime;
            if (shot.TrailTimer <= 0f)
            {
                shot.TrailTimer += TrailSeconds;
                var size = shot.Style == ShotStyle.Basic ? 0.07f : 0.13f;
                fx.Trail(world, IsoMath.GroundToWorld(direction), ArrowHeight, shot.Glow, size);
            }
            if (!shot.Bloodied)
                return;
            shot.BloodTimer -= deltaTime;
            if (shot.BloodTimer <= 0f)
            {
                shot.BloodTimer += BloodTrailSeconds;
                fx.BloodTrail(world, IsoMath.GroundToWorld(direction), ArrowHeight);
            }
        }

        // Returns whether the shot is spent.
        bool HitEnemy(ref Shot shot, EnemyController enemy, Vector2 direction)
        {
            var world = IsoMath.GroundToWorld(enemy.GroundPosition);
            var screenDirection = IsoMath.GroundToWorld(direction);
            if (shot.Style == ShotStyle.Explosive)
            {
                Explode(enemy.GroundPosition, shot);
                return true;
            }

            var killed = Strike(enemy, shot.Multiplier, projectile: true);
            HealOnHit(1);
            if (shot.Style == ShotStyle.Basic)
                focus.Gain(FocusPool.PerBasicHit + session.PassiveTree.Bonuses.RagePerBasicHit);
            else
                focus.MarkCombat();

            if (shot.Style == ShotStyle.Axe)
                return true;
            if (shot.Style == ShotStyle.Pierce)
            {
                // Through the body and on, carrying its blood (the owner's blood trail, 2026-09-30).
                shot.Pierced.Add(enemy);
                shot.Bloodied = true;
                fx.Blood(world, screenDirection, ArrowHeight, killed ? 2.4f : 1.6f);
                return false;
            }
            fx.Blood(world, screenDirection, ArrowHeight, killed ? 1.8f : 0.8f);
            return true;
        }

        void HitWall(ref Shot shot, Vector2 direction)
        {
            if (shot.Style == ShotStyle.Explosive)
            {
                Explode(shot.Position, shot);
                return;
            }
            if (shot.Style == ShotStyle.Axe)
                return;
            var world = IsoMath.GroundToWorld(shot.Position);
            var screenDirection = IsoMath.GroundToWorld(direction);
            fx.Sparks(world, screenDirection, ArrowHeight, shot.Glow, shot.Style == ShotStyle.Basic ? 3 : 6);
            fx.Stuck(world, screenDirection, ArrowHeight, ArrowArt.Arrow);
        }

        // Everything within the burst is hit once (the enemy the arrow struck included); a flash and embers show it.
        void Explode(Vector2 at, Shot shot)
        {
            enemies.QueryEnemies(at, shot.BurstRadius + QueryMargin, nearby);
            var hits = 0;
            for (var n = 0; n < nearby.Count; n++)
            {
                var enemy = nearby[n];
                if (!enemy.IsAlive || Vector2.Distance(enemy.GroundPosition, at) > shot.BurstRadius + enemy.Definition.BodyRadius)
                    continue;
                hits++;
                if (Strike(enemy, shot.Multiplier, projectile: true))
                    fx.Blood(IsoMath.GroundToWorld(enemy.GroundPosition), IsoMath.GroundToWorld((enemy.GroundPosition - at).normalized), ArrowHeight, 1.4f);
            }
            if (hits > 0)
            {
                HealOnHit(hits);
                focus.MarkCombat();
            }
            Sfx.Play(SoundId.GroundBreaker, 0.8f);
            fx.Burst(IsoMath.GroundToWorld(at), shot.BurstRadius, shot.Glow);
        }

        void Retire(Shot shot)
        {
            shot.Transform.gameObject.SetActive(false);
            (shot.Style == ShotStyle.Axe ? spareAxes : spareArrows).Push(shot.Transform);
            if (shot.Pierced != null)
            {
                shot.Pierced.Clear();
                sparePierceSets.Push(shot.Pierced);
            }
        }

        static Vector2 Rotate(Vector2 v, float degrees)
        {
            var r = degrees * Mathf.Deg2Rad;
            var c = Mathf.Cos(r);
            var s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
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
                focus.MarkCombat();
            }
        }

        // --- Shared --------------------------------------------------------------------------------------------

        /// <summary>One hit on one enemy: the hit formula with the gear's modifiers and Stillness, a crit roll, the
        /// damage number, and the kill's hit stop. Returns whether it killed.</summary>
        bool Strike(EnemyController enemy, float multiplier, bool movementSkill = false, bool projectile = false)
        {
            var tree = session.PassiveTree.Bonuses;
            var attributes = session.AttributeBonuses;
            var critical = Random.value < (GameSession.Current.Equipment.CriticalChancePercent + tree.CriticalChance + attributes.CriticalChance) / 100f;
            var damage = Damage(enemy, multiplier, critical, movementSkill, projectile);

            var world = IsoMath.GroundToWorld(enemy.GroundPosition);
            DamageNumbers.Current?.Show(new Vector3(world.x, world.y, 0f), damage, critical, isDamageToPlayer: false);

            if (enemy.TakeDamage(damage))
            {
                Kills++;
                if (tree.RageOnKill > 0f)
                    focus.Gain(tree.RageOnKill);
                HitStop.Instance?.Trigger(HitStopOnKillSeconds);
                Sfx.Play(SoundId.Kill);
                return true;
            }
            Sfx.Play(critical ? SoundId.Crit : SoundId.Hit);
            return false;
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
            // Strength, on every arrow (Docs/02, the Wild Arrow's attributes).
            var attributes = session.AttributeBonuses;
            increased += attributes.IncreasedDamage;
            // The passive tree (Docs/02, proposed numbers): its flat increase, Bloodied Edge against the wounded, Hatred
            // for Rage held, Battering Ram for the movement skills, and Berserker below half life.
            increased += tree.IncreasedDamage + tree.DamagePerTenRage * Mathf.Floor(focus.Current / 10f);
            if (enemy.MaxLife > 0f && enemy.Life / enemy.MaxLife < 0.5f)
                increased += tree.DamageVsWounded;
            if (movementSkill)
                increased += tree.MovementSkillDamage;
            if (tree.Berserker && health != null && health.Fraction < 0.5f)
                increased += 0.3f;
            // Butcher: critical hits on a bleeding enemy hit harder.
            var criticalDamage = equipment.CriticalDamagePercent / 100f + tree.CriticalDamage / 100f + attributes.CriticalDamage / 100f +
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
