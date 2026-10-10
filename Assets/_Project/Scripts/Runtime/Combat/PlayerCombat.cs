using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The player's automatic combat from Docs/01-core-gameplay.md. The class is the Wild Arrow (Docs/02-classes-and-skills.md,
    /// bows only since 2026-09-30): every frame it picks a target in the bow's reach and in sight, looses the basic arrow at
    /// the attack rate, and fires every skill she has learned in the talent trees (<see cref="TalentState"/>, since
    /// 2026-10-07; she starts with the bow alone) in class order whenever the global cast timer is free, the skill is off
    /// cooldown and paid for, a valid target or area exists and its default trigger passes. Skills cost Focus, which starts full,
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
        float lastHitStop = float.NegativeInfinity;

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
        // The skills she has learned in the talent trees, in the class's order (cast priority): every one fires on its own
        // trigger (2026-10-07: no loadout). Indices into skills.
        int[] slotSkill = new int[0];
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
        enum ShotStyle { Basic, Volley, Pierce, Homing, Explosive, Axe, Knockback, Barrage, KillShot }

        // An arrow's traits (Docs/03, "arrow traits", 2026-10-01) are set when it is loosed, from its skill and the gear
        // (ApplyGear), so every source of a trait (a skill, a legendary, an affix) lands in this one place.
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
            // Who it went through already (pierce, or a fork's parent's target), and whether it carries blood since.
            public HashSet<EnemyController> Pierced;
            public bool Bloodied;
            public float BloodTimer;
            // Pierce: how many more enemies it passes through (int.MaxValue for Pierce Arrow), how many it has, and the
            // damage each adds (Gallowsreach).
            public int PierceLeft;
            public int PierceHits;
            public float PierceBonus;
            // Fork: split into two on the first hit; a fork's arrows do not fork again.
            public bool CanFork;
            public bool IsFork;
            // Ricochet off walls toward the nearest enemy, and Seek's new target when the first dies.
            public int Ricochets;
            public int Retargets;
            // Ailments on hit, as chances (1 for certain).
            public float IgniteChance;
            public float ChillChance;
            // Stillwater Yew's certain crit, and Gallowsreach's blood pools along the trail.
            public bool CertainCrit;
            public bool BloodPools;
            public float PoolTimer;
            // Homing Arrow: what it chases and how fast it turns (radians a second).
            public EnemyController Chasing;
            public float TurnRadians;
            // Explosive Arrow: the burst's radius.
            public float BurstRadius;
            // Crown of the Unblinking Eye: a Pierce Arrow's crit resets its cooldown, once per cast.
            public bool ResetOnCrit;
            // Knockback Shot: how far it throws back the enemy it hits and those within the radius around it.
            public float KnockDistance;
            public float KnockRadius;
            // Kill Shot: below this share of its life a target takes ExecuteMultiplier instead, as a certain crit.
            public float ExecuteBelow;
            public float ExecuteMultiplier;
        }

        // Barrage (2026-10-04): the arrows still to loose, the time to the next and the skill they come from.
        SkillDefinition barrageSkill;
        int barrageLeft;
        float barrageTimer;
        // The angle every arrow loosed now leaves off its aim by (AimRules): rolled once per shot, so a fan turns as one.
        float strayDegrees;

        // A patch of floor that hurts what stands in it (Docs/03, ground marks): for now Gallowsreach's blood, which
        // makes enemies bleed. Pulsed twice a second.
        struct GroundMark
        {
            public Vector2 Center;
            public float Radius;
            public float Left;
            public float Pulse;
            // Cinder-Stitched Jerkin's burning ground instead of blood: it burns and slows.
            public bool Burning;
        }

        readonly List<GroundMark> marks = new List<GroundMark>(32);
        int basicShotCount;
        bool stillwaterSpent;
        // Windrunner Treads: how long the character has moved without stopping.
        float movingSeconds;
        // Whether the last Strike was a critical hit (the powers that fire on a crit read it).
        bool lastCritical;

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
        public float BasicRange => GripRules.ShortBowReach + GameSession.Current.Equipment.GripReach +
                                   (GameSession.Current.Equipment.Wears(LegendaryId.TheLongSilence) ? Legendaries.LongSilenceReach : 0f);

        /// <summary>How many times the skill in a slot has fired since the scene started, for tests and tuning.</summary>
        public int CastCount(int slot) => castCounts != null && slot >= 0 && slot < castCounts.Length ? castCounts[slot] : 0;

        public float SkillCooldownRemaining(int slot) => cooldowns != null && slot >= 0 && slot < cooldowns.Length ? cooldowns[slot] : 0f;

        /// <summary>Whether the class skill at this index has been learned in the talent trees (she starts with only her
        /// bow, 2026-10-07).</summary>
        public bool IsUnlocked(int skillIndex) =>
            skills != null && skillIndex >= 0 && skillIndex < skills.Length && skills[skillIndex] != null &&
            GameSession.Current.Talents.Knows(skills[skillIndex].name);

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
            // Her aim drawn on the ground (the owner, 2026-10-08).
            if (GetComponent<AimCone>() == null)
                gameObject.AddComponent<AimCone>();

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
            session.Talents.Changed += RefreshSlots;
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
                session.Talents.Changed -= RefreshSlots;
            }
            if (health != null)
                health.HitTaken -= OnHitTaken;
        }

        void RefreshSlots()
        {
            var learned = new List<int>(skills.Length);
            for (var i = 0; i < skills.Length; i++)
                if (IsUnlocked(i))
                    learned.Add(i);
            slotSkill = learned.ToArray();
            channelSlot = -1;
        }

        void OnHitTaken(float damage) => focus.MarkCombat();

        // A talent point comes with every level (2026-10-07): say so above the character, after the level callout.
        void OnLeveledUp(int level)
        {
            knownLevel = level;
            if (player == null)
                return;
            DamageNumbers.Current?.ShowText(player.transform.position + new Vector3(0f, 2.3f, 0f),
                "+1 TALENT POINT (Bag > Talents)", NewSkillColor, 46);
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
            var tree = session.Talents.Bonuses;
            // The Focus attribute (Docs/02): faster regeneration and a larger pool.
            var attributes = session.AttributeBonuses;
            focus.RegenMultiplier = 1f + attributes.FocusRegen + tree.FocusRegen;
            // Eye of the Storm: faster with enemies close (counted from last frame's query).
            if (session.Equipment.Wears(LegendaryId.EyeOfTheStorm) && CountAround(origin, Legendaries.StormRadius) >= Legendaries.StormEnemies)
                focus.RegenMultiplier *= 1f + Legendaries.StormRegenBonus;
            focus.BonusMax = attributes.MaxFocus;
            focus.GainMultiplier = tree.Berserker && health != null && health.Fraction < 0.5f ? 2f : 1f;
            focus.Tick(deltaTime);
            stillSeconds = player.GroundVelocity.magnitude < FacingSpeedThreshold ? stillSeconds + deltaTime : 0f;
            movingSeconds = stillSeconds > 0f ? 0f : movingSeconds + deltaTime;
            damageBuffTimer = Mathf.Max(0f, damageBuffTimer - deltaTime);
            speedBuffTimer = Mathf.Max(0f, speedBuffTimer - deltaTime);
            attackTimer = Mathf.Max(0f, attackTimer - deltaTime);
            castTimer = Mathf.Max(0f, castTimer - deltaTime);
            for (var i = 0; i < cooldowns.Length; i++)
                cooldowns[i] = Mathf.Max(0f, cooldowns[i] - deltaTime);

            UpdateShots(deltaTime);
            UpdateMarks(deltaTime);
            fx.Tick(deltaTime);
            UpdateSlamEffect(deltaTime);
            if (charging)
                UpdateCharge(origin);
            if (channelTimer > 0f)
                UpdateChannel(origin, deltaTime);
            if (executeSkill != null)
                UpdateExecute(origin, deltaTime);
            if (barrageLeft > 0 && !player.IsDodging)
                UpdateBarrage(origin, deltaTime);

            enemies.QueryEnemies(origin, LongestReach() + QueryMargin, candidates);
            // Only what she can see on screen (the owner, 2026-10-04): an enemy beside her, in reach but past the
            // screen's edge, is left alone.
            KeepOnScreen(candidates);
            var previousTarget = Target;
            Target = null;
            if (candidates.Count == 0)
                return;

            var target = PickTarget(origin, previousTarget);
            Target = target;
            var aim = target != null ? (target.GroundPosition - origin).normalized : facing;
            if (aim.sqrMagnitude < 1e-6f)
                aim = facing;

            // Disengaged (the thumb outside the stick's ring), she holds her fire and runs; mid-dive too.
            if (player.Disengaged || player.IsDodging)
                return;

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

        // Docs/01: the target is the nearest enemy in sight within the bow's reach, whichever way she moves (the forward
        // cone of the axe days picked a far enemy ahead while one stood behind her as she backed away, the owner,
        // 2026-10-03), and she keeps her target unless another is clearly closer. An enemy behind a wall is never picked.
        // The skills pick their own targets.
        EnemyController PickTarget(Vector2 origin, EnemyController previous)
        {
            var current = -1;
            inReach.Clear();
            inReachPositions.Clear();
            inReachWeights.Clear();
            for (var i = 0; i < candidates.Count; i++)
            {
                if (!candidates[i].IsAlive || !InReach(origin, candidates[i], BasicRange) ||
                    !enemies.Nav.HasLineOfSight(origin, candidates[i].GroundPosition))
                    continue;

                if (candidates[i] == previous)
                    current = inReach.Count;
                inReach.Add(candidates[i]);
                inReachPositions.Add(candidates[i].GroundPosition);
                // Docs: elites and bosses get a range weight bonus so they are preferred.
                var rank = candidates[i].Definition.Rank;
                inReachWeights.Add(rank == EnemyRank.Elite || rank == EnemyRank.Boss ? SweepGeometry.PreferredTargetWeight : 1f);
            }

            var index = SweepGeometry.PickNearest(origin, inReachPositions, inReachWeights, current);
            return index >= 0 ? inReach[index] : null;
        }

        static bool InReach(Vector2 origin, EnemyController target, float range) =>
            Vector2.Distance(origin, target.GroundPosition) <= range + target.Definition.BodyRadius;

        // Rolls how far the next loosed arrows stray, from how hard the stick is pushed (the owner, 2026-10-08).
        void RollStray() => strayDegrees = AimRules.StrayDegrees(AimRules.Aim(player.StickPush), Random.value);

        void BasicAttack(Vector2 origin, Vector2 aim)
        {
            RollStray();
            var equipment = GameSession.Current.Equipment;
            var tree = session.Talents.Bonuses;
            attackTimer = 1f / (attacksPerSecond * (1f + equipment.AttackSpeedPercent / 100f + AttackSpeedBuff +
                                                    tree.AttackSpeed + tree.AttackSpeedPerMomentum * player.Stance.Momentum + session.Quests.Boons.AttackSpeed +
                                                    session.AttributeBonuses.AttackSpeed + equipment.GripAttackSpeedBonus +
                                                    (equipment.Wears(LegendaryId.HideOfTheRunningStag)
                                                        ? Legendaries.StagAttackSpeedPerMomentum * player.Stance.Momentum : 0f)) *
                                equipment.GripAttackSpeedFactor);
            BasicAttackCount++;
            BasicAttackStarted?.Invoke(aim, attackTimer);

            // One arrow at the target; it gives Focus when it hits (Docs/02).
            Sfx.Play(SoundId.ArrowShot, 0.5f);
            basicShotCount++;
            var first = NewArrow(origin, aim * ArrowSpeed, BasicRange + ArrowOvershoot, 1f, ShotStyle.Basic, BasicTrailColor);
            // Stillwater Yew: once each time Stillness fills, a certain crit that pierces every enemy.
            var stance = player.Stance;
            if (stance.Stillness < stance.StillnessCap)
                stillwaterSpent = false;
            else if (equipment.Wears(LegendaryId.StillwaterYew) && !stillwaterSpent)
            {
                stillwaterSpent = true;
                first.CertainCrit = true;
                first.PierceLeft = int.MaxValue;
                EnsurePierceSet(ref first);
            }
            shots.Add(first);

            // Galeheart: at full Momentum, two more arrows in a narrow fan.
            if (equipment.Wears(LegendaryId.Galeheart) && stance.Momentum >= stance.MomentumCap)
                for (var k = 0; k < Legendaries.GaleheartArrows; k++)
                {
                    var angle = (k % 2 == 0 ? 1f : -1f) * Legendaries.GaleheartFanDegrees * 0.5f;
                    shots.Add(NewArrow(origin, Rotate(aim, angle) * ArrowSpeed, BasicRange + ArrowOvershoot, 1f, ShotStyle.Basic, BasicTrailColor));
                }

            // Ashfall Quiver: every fifth basic shot also looses a free Explosive Arrow at 60 percent.
            if (equipment.Wears(LegendaryId.AshfallQuiver) && basicShotCount % Legendaries.AshfallEvery == 0)
                shots.Add(AshfallArrow(origin, aim));

            // The quiver's extra arrow (Docs/03): now and then a second one, at another enemy in reach when there is one.
            var extraArrow = equipment.ExtraArrowPercent / 100f *
                             (equipment.Wears(LegendaryId.BandolierOfManyHeads) ? Legendaries.BandolierExtraArrowFactor : 1f);
            if (Random.value < extraArrow)
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

        Camera view;

        /// <summary>A margin inside the screen's edges, as a share of its size, so a target is seen whole enough.</summary>
        const float ScreenMargin = 0.03f;

        void KeepOnScreen(List<EnemyController> list)
        {
            if (view == null)
                view = Camera.main;
            if (view == null)
                return;
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var p = view.WorldToViewportPoint(list[i].transform.position);
                if (p.x < ScreenMargin || p.x > 1f - ScreenMargin || p.y < ScreenMargin || p.y > 1f - ScreenMargin)
                    list.RemoveAt(i);
            }
        }

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
                if (CostOf(skill) > focus.Current)
                    continue;
                if (!Ready(skill, SkillTrigger.Default, origin, aim, target, out var skillTarget))
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
                case SkillKind.KnockbackShot:
                    // Knockback Shot: the nearest enemy in sight within its short reach (2.5): one that has come close.
                    skillTarget = FindRangedTarget(origin, skill);
                    if (skillTarget == null)
                        return false;
                    if (own)
                        return true;
                    break;
                case SkillKind.Barrage:
                {
                    // Barrage: enough enemies in reach, and she is on the move.
                    if (target == null || barrageLeft > 0)
                        return false;
                    skillTarget = target;
                    var near = CountAround(origin, skill.Range);
                    if (own)
                        return near >= skill.MinEnemies && Moving;
                    break;
                }
                case SkillKind.KillShot:
                    // Kill Shot: the most wounded enemy in reach and in sight below the threshold, or an elite or boss.
                    skillTarget = FindExecuteTarget(origin, skill, own);
                    if (skillTarget == null || !enemies.Nav.HasLineOfSight(origin, skillTarget.GroundPosition))
                    {
                        skillTarget = null;
                        return false;
                    }
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

        // A skill's Focus cost after the gear (Docs/03): Fletcher's Fingers cuts Split Arrow's, Eye of the Storm cuts every
        // cost below 30 Focus, and Windrunner Treads makes the next one free after 3 s on the move.
        float CostOf(SkillDefinition skill)
        {
            var equipment = session.Equipment;
            if (equipment.Wears(LegendaryId.WindrunnerTreads) && movingSeconds >= Legendaries.WindrunnerSeconds)
                return 0f;
            var cost = skill.RageCost;
            if (skill.Kind == SkillKind.Volley && equipment.Wears(LegendaryId.FletchersFingers))
                cost -= Legendaries.FletcherFocusCut;
            if (equipment.Wears(LegendaryId.EyeOfTheStorm) && focus.Current < Legendaries.StormLowFocus)
                cost *= 1f - Legendaries.StormCostCut;
            return Mathf.Max(0f, cost);
        }

        void Cast(int i, int slot, SkillDefinition skill, Vector2 origin, Vector2 aim, EnemyController skillTarget)
        {
            RollStray();
            var cost = CostOf(skill);
            // Windrunner Treads' free skill is spent: the next needs another 3 s on the move.
            if (cost <= 0f && skill.RageCost > 0f && session.Equipment.Wears(LegendaryId.WindrunnerTreads))
                movingSeconds = 0f;
            focus.TrySpend(cost);
            if (skill.RageGain > 0f)
                focus.Gain(skill.RageGain);
            var cdr = GameSession.Current.Equipment.CooldownReductionPercent / 100f + session.AttributeBonuses.CooldownReduction +
                      session.Talents.Bonuses.CooldownReduction;
            var cooldown = skill.CooldownSeconds;
            // Falconer's Hood: Homing Arrow comes back 2 s sooner.
            if (skill.Kind == SkillKind.HomingShot && session.Equipment.Wears(LegendaryId.FalconersHood))
                cooldown = Mathf.Max(0.5f, cooldown - Legendaries.FalconerCooldownCut);
            cooldowns[i] = cooldown * Mathf.Max(0.1f, 1f - cdr);
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
                case SkillKind.KnockbackShot:
                {
                    Sfx.Play(SoundId.AxeThrow, 0.9f);
                    var shot = NewArrow(origin, direction * skill.Speed, skill.Range + ArrowOvershoot,
                        DamageOf(skill, skill.DamageMultiplier), ShotStyle.Knockback, Glow(skill));
                    shot.KnockDistance = skill.EffectStrength;
                    shot.KnockRadius = skill.BurstRadius;
                    shots.Add(shot);
                    break;
                }
                case SkillKind.Barrage:
                    barrageSkill = skill;
                    barrageLeft = skill.ProjectileCount + Mathf.RoundToInt(session.Equipment.AffixTotal(AffixId.ExtraSkillArrow));
                    barrageTimer = 0f;
                    break;
                case SkillKind.KillShot:
                {
                    Sfx.Play(SoundId.Crit, 0.7f);
                    var shot = NewArrow(origin, direction * skill.Speed, skill.Range + ArrowOvershoot,
                        DamageOf(skill, skill.DamageMultiplier), ShotStyle.KillShot, Glow(skill));
                    shot.ExecuteBelow = skill.ExecuteThreshold;
                    shot.ExecuteMultiplier = DamageOf(skill, skill.ExecuteMultiplier);
                    shots.Add(shot);
                    break;
                }
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
            const int level = 1; // Buffs no longer level (2026-10-07: talents in place of skill points).
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
            var equipment = session.Equipment;
            var count = skill.ProjectileCount +
                        (equipment.Wears(LegendaryId.FletchersFingers) ? Legendaries.FletcherArrows : 0) +
                        (equipment.Wears(LegendaryId.BandolierOfManyHeads) ? Legendaries.BandolierArrows : 0) +
                        Mathf.RoundToInt(equipment.AffixTotal(AffixId.ExtraSkillArrow));
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
            var shot = NewArrow(origin, direction * skill.Speed, skill.Range + ArrowOvershoot,
                DamageOf(skill, skill.DamageMultiplier) * StalkerFactor(), ShotStyle.Pierce, Glow(skill));
            shot.ResetOnCrit = session.Equipment.Wears(LegendaryId.CrownOfTheUnblinkingEye);
            shots.Add(shot);
        }

        // Stalker's Treads: Pierce and Explosive Arrow hit 40 percent harder at full Stillness.
        float StalkerFactor()
        {
            var stance = player.Stance;
            return session.Equipment.Wears(LegendaryId.StalkersTreads) && stance.Stillness >= stance.StillnessCap
                ? 1f + Legendaries.StalkerBonus : 1f;
        }

        // Homing Arrow (Docs/02): each arrow chases a different enemy in reach and in sight when there are enough,
        // nearest first; they leave fanned out and curve in. Hunter's Promise looses two more.
        void LooseHoming(Vector2 origin, Vector2 direction, SkillDefinition skill)
        {
            homingTargets.Clear();
            var equipment = session.Equipment;
            var count = skill.ProjectileCount + (equipment.Wears(LegendaryId.HuntersPromise) ? Legendaries.HuntersPromiseArrows : 0) +
                        (equipment.Wears(LegendaryId.BandolierOfManyHeads) ? Legendaries.BandolierArrows : 0) +
                        Mathf.RoundToInt(equipment.AffixTotal(AffixId.ExtraSkillArrow));
            // Quiver of the Hollow Hound: enemies the pet marked are sought first.
            var hound = equipment.Wears(LegendaryId.QuiverOfTheHollowHound);
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
                    if (hound && enemy.IsMarked)
                        distance -= 1000f;
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
            var shot = NewArrow(origin, direction * skill.Speed, skill.Range + ArrowOvershoot,
                DamageOf(skill, skill.DamageMultiplier) * StalkerFactor(), ShotStyle.Explosive, Glow(skill));
            shot.BurstRadius = skill.BurstRadius;
            return shot;
        }

        // Ashfall Quiver's free Explosive Arrow: the class's Explosive Arrow at 60 percent, or its default numbers when the
        // skill is not in the class list.
        Shot AshfallArrow(Vector2 origin, Vector2 direction)
        {
            SkillDefinition explosive = null;
            foreach (var skill in skills)
                if (skill != null && skill.Kind == SkillKind.ExplosiveShot)
                    explosive = skill;
            var multiplier = (explosive != null ? DamageOf(explosive, explosive.DamageMultiplier) : 2.5f) * Legendaries.AshfallShare;
            var shot = NewArrow(origin, direction * (explosive != null ? explosive.Speed : 15f), BasicRange + ArrowOvershoot, multiplier,
                ShotStyle.Explosive, explosive != null ? Glow(explosive) : new Color(1f, 0.55f, 0.15f, 0.8f));
            shot.BurstRadius = explosive != null ? explosive.BurstRadius : 2.5f;
            return shot;
        }

        static Color Glow(SkillDefinition skill) => new Color(skill.EffectColor.r, skill.EffectColor.g, skill.EffectColor.b, 0.8f);

        Shot NewArrow(Vector2 origin, Vector2 velocity, float maxDistance, float multiplier, ShotStyle style, Color glow)
        {
            var arrow = spareArrows.Count > 0 ? spareArrows.Pop() : CreateArrow();
            arrow.gameObject.SetActive(true);
            // Arrow speed (affix 97).
            velocity *= 1f + session.Equipment.AffixTotal(AffixId.ArrowSpeed) / 100f;
            // Her aim: on the move the arrow leaves off target; Homing Arrow steers itself and flies true.
            if (style != ShotStyle.Homing && strayDegrees != 0f)
                velocity = Rotate(velocity, strayDegrees);
            PlaceArrow(arrow, origin, velocity);
            var shot = new Shot
            {
                Transform = arrow,
                Position = origin,
                Velocity = velocity,
                MaxDistance = maxDistance,
                Multiplier = multiplier,
                Style = style,
                Glow = glow,
            };
            ApplyGear(ref shot);
            return shot;
        }

        // Every trait an arrow gets from its skill and the gear (Docs/03, arrow traits; the legendaries' powers).
        void ApplyGear(ref Shot shot)
        {
            var equipment = session.Equipment;
            var stance = player.Stance;
            shot.IgniteChance = equipment.IgniteChancePercent / 100f + session.Talents.Bonuses.IgniteChance;
            shot.ChillChance = equipment.ChillChancePercent / 100f + session.Talents.Bonuses.ChillChance;
            if (shot.Style == ShotStyle.Pierce)
                shot.PierceLeft = int.MaxValue;
            if (equipment.Wears(LegendaryId.MagpiesNest))
                shot.Ricochets = 1;
            if (equipment.Wears(LegendaryId.WindSwornQuiver) && stance.Momentum >= stance.MomentumCap && shot.PierceLeft != int.MaxValue)
                shot.PierceLeft += Legendaries.WindSwornPierce;
            // Affix 90: pierce on every arrow; 94: damage for each enemy already pierced.
            var extraPierce = Mathf.RoundToInt(equipment.AffixTotal(AffixId.ExtraPierce));
            if (extraPierce > 0 && shot.PierceLeft != int.MaxValue)
                shot.PierceLeft += extraPierce;
            shot.PierceBonus += equipment.AffixTotal(AffixId.DamagePerPierce) / 100f;
            switch (shot.Style)
            {
                case ShotStyle.Basic:
                    shot.CanFork = equipment.Wears(LegendaryId.QuiverOfEndlessSplinters);
                    if (equipment.Wears(LegendaryId.EmberTongue))
                        shot.IgniteChance = 1f;
                    break;
                case ShotStyle.Volley:
                    shot.CanFork = equipment.Wears(LegendaryId.Splinterbough);
                    break;
                case ShotStyle.Pierce:
                    if (equipment.Wears(LegendaryId.Gallowsreach))
                    {
                        shot.PierceBonus = Legendaries.GallowsPerPierce;
                        shot.BloodPools = true;
                    }
                    break;
                case ShotStyle.Homing:
                    if (equipment.Wears(LegendaryId.HuntersPromise))
                        shot.Retargets = 1;
                    break;
            }
            // Affix 91: a chance to fork on the first hit, for any arrow that does not already.
            if (!shot.CanFork && Random.value < equipment.AffixTotal(AffixId.ForkChance) / 100f)
                shot.CanFork = true;
            if (shot.PierceLeft > 0)
                EnsurePierceSet(ref shot);
        }

        void EnsurePierceSet(ref Shot shot)
        {
            if (shot.Pierced == null)
                shot.Pierced = sparePierceSets.Count > 0 ? sparePierceSets.Pop() : new HashSet<EnemyController>();
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

        // Each projectile flies in short pieces so it can neither pass a wall nor an enemy on a long frame. What it does
        // when it meets one comes from its traits: pierce on, fork, burst, ricochet off a wall, or stop.
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
                        // A ricochet turns it and ends this frame's flight; anything else spends it.
                        done = HitWall(ref shot, direction);
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

        // A homing arrow turns toward what it chases, at most its turn rate. When that dies, an arrow that may retarget
        // (Hunter's Promise) seeks the nearest enemy in sight once; otherwise it flies straight on.
        void Steer(ref Shot shot, float deltaTime)
        {
            if ((shot.Chasing == null || !shot.Chasing.IsAlive) && shot.Retargets > 0)
            {
                shot.Retargets--;
                shot.Chasing = NearestInSight(shot.Position, RetargetRadius, shot.Pierced);
            }
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

        // How far a retargeting or ricocheting arrow looks for a new enemy.
        const float RetargetRadius = 8f;

        EnemyController NearestInSight(Vector2 from, float radius, HashSet<EnemyController> skip)
        {
            enemies.QueryEnemies(from, radius, nearby);
            EnemyController best = null;
            var bestDistance = radius;
            for (var n = 0; n < nearby.Count; n++)
            {
                var enemy = nearby[n];
                if (!enemy.IsAlive || (skip != null && skip.Contains(enemy)))
                    continue;
                var distance = Vector2.Distance(from, enemy.GroundPosition);
                if (distance >= bestDistance || !enemies.Nav.HasLineOfSight(from, enemy.GroundPosition))
                    continue;
                best = enemy;
                bestDistance = distance;
            }
            return best;
        }

        // The glow trail behind every arrow, and the blood of one that has gone through a body: drops, and with
        // Gallowsreach pools on the floor that make enemies bleed.
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
            if (!shot.BloodPools)
                return;
            shot.PoolTimer -= deltaTime;
            if (shot.PoolTimer <= 0f)
            {
                shot.PoolTimer += BloodPoolSeconds;
                marks.Add(new GroundMark { Center = shot.Position, Radius = BloodPoolRadius, Left = Legendaries.GallowsPoolSeconds });
                fx.Pool(world, BloodPoolRadius * 2f, Legendaries.GallowsPoolSeconds);
            }
        }

        // Gallowsreach's blood on the floor: a pool every quarter second of the trail, 0.6 units across.
        const float BloodPoolSeconds = 0.25f;
        const float BloodPoolRadius = 0.6f;
        const float MarkPulseSeconds = 0.5f;

        // Enemies standing in a blood pool bleed (Gallowsreach: 40 percent weapon damage a second), refreshed each pulse.
        void UpdateMarks(float deltaTime)
        {
            for (var i = marks.Count - 1; i >= 0; i--)
            {
                var mark = marks[i];
                mark.Left -= deltaTime;
                mark.Pulse -= deltaTime;
                if (mark.Left <= 0f)
                {
                    marks[i] = marks[marks.Count - 1];
                    marks.RemoveAt(marks.Count - 1);
                    continue;
                }
                if (mark.Pulse <= 0f)
                {
                    mark.Pulse += MarkPulseSeconds;
                    if (mark.Burning)
                        fx.Embers(IsoMath.GroundToWorld(mark.Center), mark.Radius * 2f, 2);
                    enemies.QueryEnemies(mark.Center, mark.Radius + QueryMargin, nearby);
                    for (var n = 0; n < nearby.Count; n++)
                    {
                        var enemy = nearby[n];
                        if (!enemy.IsAlive || Vector2.Distance(enemy.GroundPosition, mark.Center) > mark.Radius + enemy.Definition.BodyRadius)
                            continue;
                        if (!mark.Burning)
                        {
                            enemy.ApplyBleed(Damage(enemy, Legendaries.GallowsBleedPerSecond, false), MarkPulseSeconds * 2f);
                            continue;
                        }
                        // Burning ground: 60 percent weapon damage a second, dealt each pulse, and a slow.
                        enemy.ApplySlow(1f - Legendaries.CinderGroundSlow, MarkPulseSeconds * 2f);
                        var damage = Damage(enemy, Legendaries.CinderGroundPerSecond * MarkPulseSeconds, false) * BurnFactor;
                        var world = IsoMath.GroundToWorld(enemy.GroundPosition);
                        DamageNumbers.Current?.Show(new Vector3(world.x, world.y, 0f), damage, false, isDamageToPlayer: false);
                        if (enemy.TakeDamage(damage))
                            Kills++;
                    }
                }
                marks[i] = mark;
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

            var multiplier = shot.Multiplier * (1f + shot.PierceBonus * shot.PierceHits);
            // Kill Shot: the heavier arrow, certain to crit, on a target below its threshold when it lands.
            var forceCrit = shot.CertainCrit;
            if (shot.ExecuteBelow > 0f && enemy.MaxLife > 0f && enemy.Life / enemy.MaxLife < shot.ExecuteBelow)
            {
                multiplier = shot.ExecuteMultiplier;
                forceCrit = true;
                DamageNumbers.Current?.ShowText(world + new Vector2(0f, 1.4f), "KILL SHOT", new Color(1f, 0.85f, 0.3f), 40);
            }
            var wasBleeding = enemy.IsBleeding;
            var bleed = enemy.BleedPerSecond;
            var killed = Strike(enemy, multiplier, projectile: true, forceCrit: forceCrit);
            if (shot.KnockDistance > 0f)
                KnockBack(enemy, direction, shot);
            HealOnHit(1);
            Afflict(enemy, shot);
            OnArrowHit(ref shot, enemy, wasBleeding, bleed);
            if (shot.Style == ShotStyle.Basic)
                focus.Gain(FocusPool.PerBasicHit + session.Talents.Bonuses.RagePerBasicHit);
            else
                focus.MarkCombat();

            if (shot.Style == ShotStyle.Axe)
                return true;
            if (shot.CanFork && !shot.IsFork)
            {
                shot.CanFork = false;
                Fork(shot, enemy, direction);
            }
            if (shot.PierceLeft > 0)
            {
                // Through the body and on, carrying its blood (the owner's blood trail, 2026-09-30).
                if (shot.PierceLeft != int.MaxValue)
                    shot.PierceLeft--;
                shot.PierceHits++;
                shot.Pierced.Add(enemy);
                shot.Bloodied = true;
                fx.Blood(world, screenDirection, ArrowHeight, killed ? 2.4f : 1.6f);
                return false;
            }
            fx.Blood(world, screenDirection, ArrowHeight, killed ? 1.8f : 0.8f);
            return true;
        }

        // The powers that fire when an arrow strikes (Docs/03): The Widow's Draw, Bloodletter's Grips, Falconer's Hood and
        // the Crown of the Unblinking Eye. Reads lastCritical, set by the Strike just made.
        void OnArrowHit(ref Shot shot, EnemyController enemy, bool wasBleeding, float bleedPerSecond)
        {
            var equipment = session.Equipment;
            if (shot.Style == ShotStyle.Basic && equipment.Wears(LegendaryId.WidowsDraw))
            {
                // A bleed already running jumps to the nearest other enemy within 3; a crit opens a fresh one.
                if (wasBleeding)
                {
                    var next = NearestOther(enemy, Legendaries.WidowSpreadRadius);
                    if (next != null)
                    {
                        next.ApplyBleed(bleedPerSecond, Legendaries.WidowBleedSeconds);
                        fx.Blood(IsoMath.GroundToWorld(next.GroundPosition), Vector2.up, ArrowHeight, 0.8f);
                    }
                }
                if (lastCritical && enemy.IsAlive)
                    enemy.ApplyBleed(Damage(enemy, Legendaries.WidowBleedPerSecond, false), Legendaries.WidowBleedSeconds);
            }
            if (lastCritical && enemy.IsBleeding && equipment.Wears(LegendaryId.BloodlettersGrips))
                enemy.ExtendBleed(Legendaries.BloodletterExtendSeconds);
            if (shot.Style == ShotStyle.Homing && equipment.Wears(LegendaryId.FalconersHood))
                focus.Gain(Legendaries.FalconerFocusPerHit);
            if (shot.ResetOnCrit && lastCritical)
            {
                shot.ResetOnCrit = false;
                for (var c = 0; c < skills.Length; c++)
                    if (skills[c] != null && skills[c].Kind == SkillKind.PierceShot)
                        cooldowns[c] = 0f;
            }
        }

        // The nearest living enemy to another, within a radius, not that one. Its own list: it runs while UpdateShots
        // walks the nearby list.
        readonly List<EnemyController> spreadNearby = new List<EnemyController>(16);

        EnemyController NearestOther(EnemyController from, float radius)
        {
            enemies.QueryEnemies(from.GroundPosition, radius + QueryMargin, spreadNearby);
            EnemyController best = null;
            var bestDistance = radius;
            for (var n = 0; n < spreadNearby.Count; n++)
            {
                var enemy = spreadNearby[n];
                if (enemy == from || !enemy.IsAlive)
                    continue;
                var distance = Vector2.Distance(from.GroundPosition, enemy.GroundPosition);
                if (distance > bestDistance)
                    continue;
                best = enemy;
                bestDistance = distance;
            }
            return best;
        }

        // A fork: two arrows from the struck enemy at 45 degrees either side, each at half the damage, which keep every
        // other trait but do not fork again and do not strike the enemy that split them.
        void Fork(Shot parent, EnemyController struck, Vector2 direction)
        {
            var speed = parent.Velocity.magnitude;
            var left = parent.MaxDistance - parent.Travelled;
            if (left <= 0.5f)
                return;
            for (var k = 0; k < 2; k++)
            {
                var side = k == 0 ? 1f : -1f;
                var arrow = spareArrows.Count > 0 ? spareArrows.Pop() : CreateArrow();
                arrow.gameObject.SetActive(true);
                var velocity = Rotate(direction, side * Legendaries.ForkDegrees) * speed;
                PlaceArrow(arrow, parent.Position, velocity);
                var fork = parent;
                fork.Transform = arrow;
                fork.Velocity = velocity;
                fork.Travelled = 0f;
                fork.MaxDistance = left;
                fork.Multiplier = parent.Multiplier * Legendaries.ForkShare;
                fork.IsFork = true;
                fork.CanFork = false;
                fork.Bloodied = false;
                fork.Pierced = sparePierceSets.Count > 0 ? sparePierceSets.Pop() : new HashSet<EnemyController>();
                fork.Pierced.Add(struck);
                shots.Add(fork);
            }
            fx.Sparks(IsoMath.GroundToWorld(parent.Position), IsoMath.GroundToWorld(direction), ArrowHeight, parent.Glow, 4);
        }

        // Burn and chill on a hit, by the arrow's chances (Docs/03: burn is 60 percent weapon damage over 3 s a stack).
        void Afflict(EnemyController enemy, Shot shot)
        {
            if (!enemy.IsAlive)
                return;
            var world = IsoMath.GroundToWorld(enemy.GroundPosition);
            if (shot.IgniteChance > 0f && Random.value < shot.IgniteChance)
            {
                enemy.ApplyBurn(Damage(enemy, BurnShare / BurnStacks.Seconds, false) * BurnFactor);
                fx.Sparks(world, Vector2.up, ArrowHeight, BurnColor, 3);
            }
            if (shot.ChillChance > 0f && Random.value < shot.ChillChance)
            {
                enemy.ApplyChill();
                fx.Sparks(world, Vector2.up, ArrowHeight, ChillColor, 3);
            }
        }

        const float BurnShare = 0.6f;

        // Affix 93: burns, and burning ground, deal more.
        float BurnFactor => 1f + session.Equipment.AffixTotal(AffixId.BurnDamage) / 100f;
        static readonly Color BurnColor = new Color(1f, 0.5f, 0.1f, 1f);
        static readonly Color ChillColor = new Color(0.6f, 0.9f, 1f, 1f);

        // Returns whether the shot is spent: a ricochet (Magpie's Nest) turns it toward the nearest enemy in sight and
        // keeps it flying, a little harder; an explosive arrow bursts; anything else sparks and sticks in the wall.
        bool HitWall(ref Shot shot, Vector2 direction)
        {
            if (shot.Style == ShotStyle.Explosive)
            {
                Explode(shot.Position, shot);
                return true;
            }
            if (shot.Style == ShotStyle.Axe)
                return true;
            var world = IsoMath.GroundToWorld(shot.Position);
            var screenDirection = IsoMath.GroundToWorld(direction);
            if (shot.Ricochets > 0)
            {
                var target = NearestInSight(shot.Position, RetargetRadius, shot.Pierced);
                if (target != null)
                {
                    shot.Ricochets--;
                    shot.Velocity = (target.GroundPosition - shot.Position).normalized * shot.Velocity.magnitude;
                    shot.Multiplier *= 1f + Legendaries.MagpieRicochetBonus;
                    shot.MaxDistance = shot.Travelled + RetargetRadius + ArrowOvershoot;
                    if (shot.Style == ShotStyle.Homing)
                        shot.Chasing = target;
                    fx.Sparks(world, screenDirection, ArrowHeight, shot.Glow, 5);
                    return false;
                }
            }
            fx.Sparks(world, screenDirection, ArrowHeight, shot.Glow, shot.Style == ShotStyle.Basic ? 3 : 6);
            fx.Stuck(world, screenDirection, ArrowHeight, ArrowArt.Arrow);
            return true;
        }

        // Everything within the burst is hit once (the enemy the arrow struck included); a flash and embers show it.
        // Ember-Tongue: burning enemies take 60 percent more from it.
        void Explode(Vector2 at, Shot shot)
        {
            enemies.QueryEnemies(at, shot.BurstRadius + QueryMargin, nearby);
            var emberTongue = session.Equipment.Wears(LegendaryId.EmberTongue);
            var hits = 0;
            for (var n = 0; n < nearby.Count; n++)
            {
                var enemy = nearby[n];
                if (!enemy.IsAlive || Vector2.Distance(enemy.GroundPosition, at) > shot.BurstRadius + enemy.Definition.BodyRadius)
                    continue;
                hits++;
                var multiplier = shot.Multiplier * (emberTongue && enemy.IsBurning ? 1f + Legendaries.EmberTongueBurstBonus : 1f);
                if (Strike(enemy, multiplier, projectile: true))
                    fx.Blood(IsoMath.GroundToWorld(enemy.GroundPosition), IsoMath.GroundToWorld((enemy.GroundPosition - at).normalized), ArrowHeight, 1.4f);
                else
                    Afflict(enemy, shot);
            }
            if (hits > 0)
            {
                HealOnHit(hits);
                focus.MarkCombat();
            }
            Sfx.Play(SoundId.Explosion, 0.9f);
            fx.Burst(IsoMath.GroundToWorld(at), shot.BurstRadius, shot.Glow);
            // Cinder-Stitched Jerkin: the burst leaves burning ground that burns and slows.
            if (session.Equipment.Wears(LegendaryId.CinderStitchedJerkin))
            {
                var radius = shot.BurstRadius * 0.8f;
                marks.Add(new GroundMark { Center = at, Radius = radius, Left = Legendaries.CinderGroundSeconds, Burning = true });
                fx.Fire(IsoMath.GroundToWorld(at), radius * 2f, Legendaries.CinderGroundSeconds);
            }
        }

        // Knockback Shot: the enemy struck and every enemy within the radius around it are thrown back along the arrow's
        // flight (each from where it stands, away from the impact for those beside it), walls respected.
        readonly List<EnemyController> knocked = new List<EnemyController>(16);

        void KnockBack(EnemyController struck, Vector2 direction, Shot shot)
        {
            var impact = struck.GroundPosition;
            enemies.QueryEnemies(impact, shot.KnockRadius + 1f, knocked);
            for (var i = 0; i < knocked.Count; i++)
            {
                var enemy = knocked[i];
                if (!enemy.IsAlive || (enemy != struck && Vector2.Distance(enemy.GroundPosition, impact) > shot.KnockRadius + enemy.Definition.BodyRadius))
                    continue;
                var away = enemy == struck ? direction : (enemy.GroundPosition - impact + direction * 0.5f).normalized;
                enemy.Push(away * shot.KnockDistance);
            }
            fx.Burst(IsoMath.GroundToWorld(impact), shot.KnockRadius, shot.Glow);
            Sfx.Play(SoundId.GroundBreaker, 0.6f);
        }

        // Barrage: one arrow every interval at an enemy in reach and in sight (the basic target first, then the others in
        // turn), while she keeps moving and shooting her basic arrows.
        void UpdateBarrage(Vector2 origin, float deltaTime)
        {
            barrageTimer -= deltaTime;
            if (barrageTimer > 0f)
                return;
            var skill = barrageSkill;
            barrageTimer = skill.DurationSeconds / Mathf.Max(1, skill.ProjectileCount);
            barrageLeft--;
            EnemyController target = null;
            var pick = barrageLeft;
            for (var tries = 0; tries < inReach.Count && target == null; tries++)
            {
                var enemy = inReach[(pick + tries) % inReach.Count];
                if (enemy.IsAlive && Vector2.Distance(origin, enemy.GroundPosition) <= skill.Range &&
                    enemies.Nav.HasLineOfSight(origin, enemy.GroundPosition))
                    target = enemy;
            }
            if (target == null)
                target = Target;
            if (target == null)
                return;
            var direction = Rotate((target.GroundPosition - origin).normalized, Random.Range(-skill.SpreadDegrees, skill.SpreadDegrees) * 0.5f);
            RollStray();
            Sfx.Play(SoundId.ArrowShot, 0.4f);
            shots.Add(NewArrow(origin, direction * skill.Speed, skill.Range + ArrowOvershoot, DamageOf(skill, skill.DamageMultiplier),
                ShotStyle.Barrage, Glow(skill)));
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
                var refund = session.Talents.Bonuses.BullRushRefund;
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
        bool Strike(EnemyController enemy, float multiplier, bool movementSkill = false, bool projectile = false, bool forceCrit = false)
        {
            var tree = session.Talents.Bonuses;
            var attributes = session.AttributeBonuses;
            var critical = forceCrit || Random.value < (GameSession.Current.Equipment.CriticalChancePercent + tree.CriticalChance + attributes.CriticalChance) / 100f;
            lastCritical = critical;
            var damage = Damage(enemy, multiplier, critical, movementSkill, projectile);

            var world = IsoMath.GroundToWorld(enemy.GroundPosition);
            DamageNumbers.Current?.Show(new Vector3(world.x, world.y, 0f), damage, critical, isDamageToPlayer: false);

            if (enemy.TakeDamage(damage))
            {
                Kills++;
                if (tree.RageOnKill > 0f)
                    focus.Gain(tree.RageOnKill);
                if (HitStopRules.OnKill(enemy.Definition.Rank, Time.unscaledTime - lastHitStop))
                {
                    lastHitStop = Time.unscaledTime;
                    HitStop.Instance?.Trigger(HitStopOnKillSeconds);
                }
                Sfx.Play(SoundId.Kill);
                return true;
            }
            Sfx.Play(critical ? SoundId.Crit : SoundId.Hit);
            return false;
        }

        /// <summary>A skill's damage multiplier: its own, raised by its "Improved" talent (2026-10-07; skill points and
        /// levels are gone).</summary>
        float DamageOf(SkillDefinition skill, float levelOneMultiplier) =>
            levelOneMultiplier * (1f + session.Talents.SkillDamage(skill.name));

        /// <summary>The hit formula for this character against an enemy: the gear's modifiers, Stillness and Battle
        /// Roar as increased damage.</summary>
        float Damage(EnemyController enemy, float multiplier, bool critical, bool movementSkill = false, bool projectile = false)
        {
            var equipment = GameSession.Current.Equipment;
            var tree = session.Talents.Bonuses;
            var increased = equipment.IncreasedDamagePercent / 100f + player.Stance.IncreasedDamage + (damageBuffTimer > 0f ? damageBuff : 0f);
            // Strength, on every arrow (Docs/02, the Wild Arrow's attributes).
            var attributes = session.AttributeBonuses;
            increased += attributes.IncreasedDamage;
            // The passive tree (Docs/02, proposed numbers): its flat increase, Bloodied Edge against the wounded, Hatred
            // for Rage held, Battering Ram for the movement skills, and Berserker below half life.
            increased += tree.IncreasedDamage + tree.DamagePerTenRage * Mathf.Floor(focus.Current / 10f);
            // The Vigil's blessing (2026-10-08): +5 percent damage for each ember of the Drowned Watch and the First Watchman.
            increased += session.Quests.Boons.DamagePercent;
            if (enemy.MaxLife > 0f && enemy.Life / enemy.MaxLife < 0.5f)
                increased += tree.DamageVsWounded;
            if (movementSkill)
                increased += tree.MovementSkillDamage;
            // Affixes 95 and 96: arrows hit harder far away or point blank.
            if (projectile && player != null)
            {
                var distance = Vector2.Distance(IsoMath.WorldToGround(player.transform.position), enemy.GroundPosition);
                if (distance > FarDistance)
                    increased += equipment.AffixTotal(AffixId.FarDamage) / 100f;
                else if (distance <= NearDistance)
                    increased += equipment.AffixTotal(AffixId.NearDamage) / 100f;
            }
            if (tree.Berserker && health != null && health.Fraction < 0.5f)
                increased += 0.3f;
            // Butcher: critical hits on a bleeding enemy hit harder.
            var criticalDamage = equipment.CriticalDamagePercent / 100f + tree.CriticalDamage / 100f + attributes.CriticalDamage / 100f +
                                 (critical && enemy.IsBleeding ? tree.CritDamageVsBleeding : 0f);
            return CombatFormulas.HitDamage(
                WeaponDamage, multiplier, equipment.FlatWeaponDamageBonus, increased, projectile ? ArrowMore(enemy) : 1f,
                critical, criticalDamage, enemy.Definition.Armor, enemy.Level);
        }

        // The legendaries' "more" damage from arrows on some enemies (Docs/03): The Long Silence past 7 units, the Hollow
        // Hound's marked enemies, Bloodletter's Grips on the bleeding. They multiply.
        float ArrowMore(EnemyController enemy)
        {
            var equipment = session.Equipment;
            var more = 1f;
            if (equipment.Wears(LegendaryId.TheLongSilence) && player != null &&
                Vector2.Distance(IsoMath.WorldToGround(player.transform.position), enemy.GroundPosition) > Legendaries.LongSilenceFar)
                more *= 1f + Legendaries.LongSilenceBonus;
            if (enemy.IsMarked && equipment.Wears(LegendaryId.QuiverOfTheHollowHound))
                more *= 1f + Legendaries.HoundMarkBonus;
            if (enemy.IsBleeding && equipment.Wears(LegendaryId.BloodlettersGrips))
                more *= 1f + Legendaries.BloodletterBonus;
            return more;
        }

        // Docs/03, affixes 95 and 96.
        const float FarDistance = 6f;
        const float NearDistance = 3f;

        void HealOnHit(int hits)
        {
            var tree = session.Talents.Bonuses;
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
