using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The enemy states from Docs/01-core-gameplay.md.
    /// </summary>
    public enum EnemyState
    {
        /// <summary>Standing at its home spot until the player comes within aggro range.</summary>
        Idle,

        /// <summary>Chasing the player.</summary>
        Approach,

        /// <summary>The leash broke: walking back to the home spot. Ignores the player until it arrives.</summary>
        Return,

        /// <summary>Winding up an attack. Stands still and swells as a tell; the hit lands when the wind-up ends.</summary>
        Attack,

        /// <summary>Standing still after an attack, before it can attack again.</summary>
        Recover,

        /// <summary>Killed. Plays a short fade and then leaves the level. Cannot be hit or targeted.</summary>
        Dead,
    }

    /// <summary>
    /// One enemy. It holds its own state and steering but has no Update: <see cref="EnemyManager"/> ticks every
    /// enemy in one loop so the shared spatial hash and flow field are built once per frame.
    /// The enemy lives in ground space and only its transform is projected to the isometric view.
    /// </summary>
    public class EnemyController : MonoBehaviour
    {
        // How hard the player pushes enemies out of its space, relative to separation between enemies. Tuning value.
        const float PlayerPushWeight = 3f;

        // An enemy counts as home when it is this close to its home spot, in ground units.
        const float ArriveDistance = 0.2f;

        // A blocked step is retried turned by this angle either way, so enemies slide along walls (60 degrees).
        const float SlideAngle = 1.0471976f;

        // A wall is one cell thick, which is 0.707 across on the ground. A step longer than that could jump over it
        // (a long frame at speed 3.6 covers over a unit), so steps are split into pieces no longer than this.
        const float MaxStepLength = 0.25f;

        // An attack lands if the player is still within this much of the attack range when the wind-up ends, so a step
        // back just as it lands is forgiven. Tuning value.
        const float AttackForgiveness = 0.3f;

        // Docs/03-itemization.md: elite (and boss, not built yet) damage ignores 15 percent of player armor.
        const float EliteArmorIgnorePercent = 0.15f;

        // Docs/01-core-gameplay.md gives no numbers for elite modifiers; these are tuning values.
        const float HastedMoveSpeedMultiplier = 1.3f;
        const float HastedAttackSpeedMultiplier = 1.3f;
        const float VampiricHealFraction = 0.5f;
        const float FrozenSlowMultiplier = 0.5f;
        const float FrozenSlowSeconds = 1.5f;

        /// <summary>Docs: an elite carries at most two modifiers. Matches how many icon child sprites the prefab
        /// gets (EnemySceneBuilder.BuildPrefab); a third curated modifier would need a third slot there too.</summary>
        public const int MaxModifierIcons = 2;

        // Local-space layout of the modifier icons above the head, tuned for how it looks on an Elite's own
        // VisualScale (the only rank that ever shows any): a fixed local offset scales with it automatically.
        const float ModifierIconLocalY = 0.95f;
        const float ModifierIconSpacingX = 0.18f;

        static readonly EliteModifiers[] ModifierIconOrder = { EliteModifiers.Hasted, EliteModifiers.Vampiric, EliteModifiers.Frozen };

        // Docs/01-core-gameplay.md: hard red outline, soft fill. The same red as the boss's telegraphs.
        static readonly Color TelegraphColor = new Color(1f, 0.22f, 0.12f, 0.95f);

        // An archer's aim line (Docs/01: a thin line in the aim direction) and how far past its range an arrow flies.
        const float AimLineWidth = 0.07f;
        const float ArrowOvershoot = 2f;

        // How much the enemy swells over its wind-up. Placeholder tell.
        const float WindupScale = 0.2f;

        // A hit makes the enemy swell briefly, and death shrinks it while it fades. Placeholder feedback.
        const float PunchSeconds = 0.1f;
        const float PunchScale = 0.2f;
        const float DeathEndScale = 0.85f;
        const float FlashSeconds = 0.1f;

        EnemyDefinition definition;
        EliteModifiers modifiers;
        EnemyPack pack;
        EnemyManager manager;
        SpriteRenderer[] renderers;
        SpriteRenderer bodyRenderer;
        Sprite defaultBodySprite;
        SpriteRenderer[] modifierIcons;
        SpriteRenderer shadowRenderer;
        SpriteEffects effects;
        Vector3 bodyDefaultPosition;
        Vector3 bodyDefaultScale = Vector3.one;

        // The baked animations of this enemy's type, or null for the placeholder body (EnemyAnimation.cs).
        EnemyAnimationSet animationSet;
        float animationTime;
        int animationRow;
        Vector2 lastGround;
        float hitAnimationTimer;
        Vector2 ground;
        Vector2 home;
        float leashTimer;
        float stateTimer;
        float life;
        float punchTimer;
        float deathTimer;

        // A brute's slam lands where it stood when the wind-up began; an archer shoots where it aimed then.
        Vector2 attackCenter;
        Vector2 attackAim;

        // A player skill's slow (Ground Breaker). Reset on every spawn.
        readonly SlowDebuff slow = new SlowDebuff();

        // A bleed (Rending Spin): damage a second for a time; a new one replaces it rather than stacking (Docs/02's
        // proposal). Dealt in pulses so the numbers do not flood.
        const float BleedPulseSeconds = 0.5f;
        float bleedPerSecond;
        float bleedTimer;
        float bleedPulse;
        float bleedPending;

        // Burn and chill (Docs/03, decided 2026-10-01). Burn is dealt in pulses like the bleed; a frozen enemy stands still.
        readonly BurnStacks burn = new BurnStacks();
        readonly ChillMeter chill = new ChillMeter();
        float burnPulse;
        float burnPending;
        static readonly Color FrozenColor = new Color(0.55f, 0.85f, 1f, 1f);

        // Made on first use and reused, one of each since a pooled instance can be any archetype.
        GroundMarker slamMarker;
        GroundMarker aimMarker;

        public EnemyState State { get; private set; }

        public bool IsAlive => State != EnemyState.Dead;

        public float Life => life;

        public float MaxLife => definition != null ? definition.MaxLifeAt(Level) : 0f;

        /// <summary>This enemy's level: the definition's own, unless whoever spawned it set another (a dungeon level
        /// sets it from its depth). Damage, life, armor, loot and XP all read this, not the definition.</summary>
        public int Level { get; private set; }

        public EnemyDefinition Definition => definition;

        /// <summary>
        /// A boss (or any enemy a fight script drives): its pack AI does not run, and the script moves it with
        /// <see cref="ScriptedMove"/> and attacks for it. Targeting, damage, death, loot and XP work as for any enemy.
        /// Cleared on every spawn, since instances are pooled.
        /// </summary>
        public bool Scripted { get; set; }

        /// <summary>Raised with the damage of every hit this enemy takes, the killing one included. Cleared on spawn.</summary>
        public event System.Action<float> Damaged;

        /// <summary>The modifiers this elite rolled at spawn. Always None for a Normal or Champion.</summary>
        public EliteModifiers Modifiers => modifiers;

        public bool HasModifier(EliteModifiers modifier) => (modifiers & modifier) != 0;

        /// <summary>Docs: Hasted is plus 30 percent move and attack speed.</summary>
        float EffectiveMoveSpeed => definition.MoveSpeed * slow.Multiplier * (HasModifier(EliteModifiers.Hasted) ? HastedMoveSpeedMultiplier : 1f);

        float EffectiveAttackWindupSeconds => definition.AttackWindupSeconds / (HasModifier(EliteModifiers.Hasted) ? HastedAttackSpeedMultiplier : 1f);

        float EffectiveAttackRecoverSeconds => definition.AttackRecoverSeconds / (HasModifier(EliteModifiers.Hasted) ? HastedAttackSpeedMultiplier : 1f);

        /// <summary>Position on the ground plane, in ground units.</summary>
        public Vector2 GroundPosition => ground;

        /// <summary>Where the enemy stands while idle and returns to after losing the player.</summary>
        public Vector2 HomePosition => home;

        /// <summary>This enemy's id in the manager's spatial hash for the current frame.</summary>
        internal int HashId { get; set; }

        /// <summary>Which slot of its pack the enemy fills, so a kill can be remembered across scene loads.</summary>
        internal int PackSlot { get; set; }

        void Awake()
        {
            // Modifier icons keep their own per-modifier color (UpdateModifierIcons), so they must not be in the
            // set SetVisuals resets to a flat white*alpha every windup, punch and death tick.
            var all = GetComponentsInChildren<SpriteRenderer>(true);
            var tinted = new System.Collections.Generic.List<SpriteRenderer>(all.Length);
            modifierIcons = new SpriteRenderer[MaxModifierIcons];
            foreach (var r in all)
            {
                var iconIndex = IconIndexFromName(r.gameObject.name);
                if (iconIndex >= 0)
                    modifierIcons[iconIndex] = r;
                else
                    tinted.Add(r);
            }
            renderers = tinted.ToArray();

            // The hit flash and the dissolve are for the body; the shadow only fades.
            var bodies = new System.Collections.Generic.List<SpriteRenderer>(tinted.Count);
            foreach (var r in tinted)
            {
                if (r.gameObject.name == "Shadow")
                    shadowRenderer = r;
                else
                    bodies.Add(r);
            }
            effects = new SpriteEffects(bodies.ToArray());

            var body = transform.Find("Body");
            if (body != null)
            {
                bodyRenderer = body.GetComponent<SpriteRenderer>();
                defaultBodySprite = bodyRenderer.sprite;
                bodyDefaultPosition = body.localPosition;
                bodyDefaultScale = body.localScale;
            }
        }

        static int IconIndexFromName(string name)
        {
            for (var i = 0; i < MaxModifierIcons; i++)
                if (name == $"Modifier Icon {i}")
                    return i;
            return -1;
        }

        /// <param name="groundPosition">Where the enemy appears. This becomes its home spot.</param>
        /// <param name="owner">The pack the enemy belongs to, used to find its way home. Can be null.</param>
        /// <param name="owningManager">The manager, told when the enemy dies.</param>
        /// <param name="rolledModifiers">Rolled by the caller (only for an Elite); None for everything else.</param>
        /// <param name="level">The enemy's level; 0 or less uses the definition's own.</param>
        internal void Activate(EnemyDefinition data, Vector2 groundPosition, bool aggroed, EnemyPack owner, EnemyManager owningManager, EliteModifiers rolledModifiers = EliteModifiers.None, int level = 0)
        {
            definition = data;
            Level = level > 0 ? level : data.Level;
            Scripted = false;
            Damaged = null;
            modifiers = rolledModifiers;
            pack = owner;
            manager = owningManager;
            ground = groundPosition;
            home = groundPosition;
            leashTimer = 0f;
            slow.Clear();
            bleedTimer = 0f;
            bleedPending = 0f;
            burn.Clear();
            chill.Clear();
            burnPending = 0f;
            burnPulse = 0f;
            markTimer = 0f;
            life = MaxLife;
            punchTimer = 0f;
            deathTimer = 0f;
            State = aggroed ? EnemyState.Approach : EnemyState.Idle;
            // The renderer is pooled and reused across definitions, so a Normal spawn must reset it explicitly:
            // otherwise it would keep showing whatever a previous occupant (say, an Elite) last set it to.
            if (bodyRenderer != null)
                bodyRenderer.sprite = data.BodySprite != null ? data.BodySprite : defaultBodySprite;
            // Baked sheets draw the body from its feet at their own size; the placeholder keeps the prefab's placement.
            animationSet = bodyRenderer != null ? EnemyAnimationSet.For(data.SpriteCharacter) : null;
            if (bodyRenderer != null)
            {
                bodyRenderer.transform.localPosition = animationSet != null ? Vector3.zero : bodyDefaultPosition;
                // A look baked at its in-game size cancels the VisualScale the whole enemy is drawn at (pixels stay whole).
                bodyRenderer.transform.localScale = animationSet != null ? Vector3.one / animationSet.BakedScale : bodyDefaultScale;
            }
            animationTime = 0f;
            animationRow = 0;
            lastGround = groundPosition;
            hitAnimationTimer = 0f;
            effects.Clear();
            SetVisuals(1f, 1f);
            UpdateModifierIcons();
            HideTelegraphs();
            SyncTransform();
            gameObject.SetActive(true);
        }

        internal void Deactivate()
        {
            HideTelegraphs();
            gameObject.SetActive(false);
        }

        void HideTelegraphs()
        {
            slamMarker?.Hide();
            aimMarker?.Hide();
        }

        /// <summary>Shows a small colored dot above the head for each active modifier (Docs/01-core-gameplay.md:
        /// no visual spec given, this is a placeholder tell), in a fixed order so the display is stable. Hidden
        /// entirely for a Normal or Champion, which never carry any.</summary>
        void UpdateModifierIcons()
        {
            if (modifierIcons == null)
                return;

            var activeCount = 0;
            foreach (var candidate in ModifierIconOrder)
                if (HasModifier(candidate))
                    activeCount++;

            var shown = 0;
            foreach (var candidate in ModifierIconOrder)
            {
                if (!HasModifier(candidate))
                    continue;

                var icon = modifierIcons[shown];
                if (icon != null)
                {
                    icon.gameObject.SetActive(true);
                    icon.color = ModifierColor(candidate);
                    var x = activeCount == 1 ? 0f : (shown == 0 ? -ModifierIconSpacingX : ModifierIconSpacingX);
                    icon.transform.localPosition = new Vector3(x, ModifierIconLocalY, 0f);
                }
                shown++;
            }

            for (; shown < modifierIcons.Length; shown++)
                if (modifierIcons[shown] != null)
                    modifierIcons[shown].gameObject.SetActive(false);
        }

        static Color ModifierColor(EliteModifiers modifier)
        {
            switch (modifier)
            {
                case EliteModifiers.Hasted: return new Color32(255, 230, 60, 255);
                // Not red: every enemy sprite in this project is already red or crimson (Swarmer, Elite's own
                // tint), so a red Vampiric dot barely reads against its own owner. Green stands apart from all of it.
                case EliteModifiers.Vampiric: return new Color32(60, 200, 110, 255);
                case EliteModifiers.Frozen: return new Color32(120, 220, 255, 255);
                default: return Color.white;
            }
        }

        void HideModifierIcons()
        {
            if (modifierIcons == null)
                return;
            foreach (var icon in modifierIcons)
                if (icon != null)
                    icon.gameObject.SetActive(false);
        }

        /// <summary>Advances the enemy by one frame.</summary>
        internal void Tick(float deltaTime, EnemyManager world)
        {
            UpdatePunch(deltaTime);
            slow.Tick(deltaTime);
            TickBleed(deltaTime);
            TickBurn(deltaTime);
            chill.Tick(deltaTime);
            markTimer = Mathf.Max(0f, markTimer - deltaTime);
            if (!IsAlive || Scripted || chill.IsFrozen)
                return;

            var playerGround = world.PlayerGround;
            var distance = Vector2.Distance(ground, playerGround);

            switch (State)
            {
                case EnemyState.Idle:
                    if (distance > definition.AggroRange)
                        return;
                    State = EnemyState.Approach;
                    leashTimer = 0f;
                    pack?.Alert();
                    break;

                case EnemyState.Approach:
                    if (distance > definition.LeashRange)
                    {
                        leashTimer += deltaTime;
                        if (leashTimer >= definition.LeashSeconds)
                        {
                            State = EnemyState.Return;
                            return;
                        }
                    }
                    else
                    {
                        leashTimer = 0f;
                    }

                    // Archers only shoot what they can see; the check is skipped for everyone else.
                    var lineOfSight = definition.Archetype != EnemyArchetype.Archer || world.Nav.HasLineOfSight(ground, playerGround);
                    if (CanAttack(world, distance) && lineOfSight)
                    {
                        BeginAttack(world, playerGround);
                        return;
                    }
                    if (definition.Archetype == EnemyArchetype.Archer)
                    {
                        Reposition(world, playerGround, distance, lineOfSight, deltaTime);
                        return;
                    }
                    break;

                case EnemyState.Attack:
                    stateTimer -= deltaTime;
                    SetVisuals(1f, 1f + WindupScale * (1f - Mathf.Clamp01(stateTimer / Mathf.Max(EffectiveAttackWindupSeconds, 1e-4f))));
                    slamMarker?.Advance(deltaTime);
                    aimMarker?.Advance(deltaTime);
                    if (stateTimer <= 0f)
                    {
                        LandAttack(world, distance);
                        HideTelegraphs();
                        State = EnemyState.Recover;
                        stateTimer = EffectiveAttackRecoverSeconds;
                        SetVisuals(1f, 1f);
                    }
                    Hold(world, playerGround, deltaTime);
                    return;

                case EnemyState.Recover:
                    stateTimer -= deltaTime;
                    if (stateTimer <= 0f)
                        State = EnemyState.Approach;
                    // An archer uses the time between shots to get back to its range.
                    if (definition.Archetype == EnemyArchetype.Archer)
                        Reposition(world, playerGround, distance, world.Nav.HasLineOfSight(ground, playerGround), deltaTime);
                    else
                        Hold(world, playerGround, deltaTime);
                    return;

                case EnemyState.Return:
                    if (Vector2.Distance(ground, home) <= ArriveDistance)
                    {
                        State = EnemyState.Idle;
                        return;
                    }
                    Move(HomeDirection(world) * (EffectiveMoveSpeed * deltaTime), world);
                    return;
            }

            // Close enough to stop closing in; the pushes below still run so a crowd spreads around the player.
            var desired = distance > definition.StopDistance ? world.ChaseDirection(ground, playerGround) : Vector2.zero;
            desired += Separation(world);

            // The player is solid to enemies. Without this the crowd behind squeezes the front row through the player.
            desired += PushAway(playerGround, definition.StopDistance) * PlayerPushWeight;

            if (desired.sqrMagnitude > 1f)
                desired.Normalize();

            Move(desired * (EffectiveMoveSpeed * deltaTime), world);
        }

        /// <summary>
        /// Deals damage that has already been through the hit formula. Returns true when this hit killed the enemy.
        /// A hit wakes an idle or returning enemy and sends it after the player.
        /// </summary>
        public bool TakeDamage(float amount)
        {
            if (!IsAlive)
                return false;

            life -= amount;
            Damaged?.Invoke(amount);
            effects.Flash(Color.white, FlashSeconds);
            if (life <= 0f)
            {
                life = 0f;
                State = EnemyState.Dead;
                deathTimer = definition.DeathSeconds;
                HideModifierIcons();
                HideTelegraphs();
                pack?.NotifyDeath(this);
                manager?.NotifyKilled(this);
                return true;
            }

            punchTimer = PunchSeconds;
            if (State != EnemyState.Attack && State != EnemyState.Recover)
                hitAnimationTimer = EnemyAnimationRules.HitSeconds;

            // A hit wakes an idle or returning enemy, and its pack with it. One that is mid-attack keeps attacking.
            if (State == EnemyState.Idle || State == EnemyState.Return)
            {
                State = EnemyState.Approach;
                leashTimer = 0f;
                pack?.Alert();
            }
            return false;
        }

        /// <summary>Starts or replaces a bleed: this much damage a second, already through the hit formula, for this long.</summary>
        public void ApplyBleed(float damagePerSecond, float seconds)
        {
            if (!IsAlive || damagePerSecond <= 0f || seconds <= 0f)
                return;
            bleedPerSecond = damagePerSecond;
            bleedTimer = seconds;
        }

        public bool IsBleeding => bleedTimer > 0f;

        /// <summary>The bleed's damage a second while it lasts, 0 when not bleeding (for a bleed passed on).</summary>
        public float BleedPerSecond => bleedTimer > 0f ? bleedPerSecond : 0f;

        /// <summary>Lengthens a bleed in progress (Bloodletter's Grips); nothing when not bleeding.</summary>
        public void ExtendBleed(float seconds)
        {
            if (IsAlive && bleedTimer > 0f && seconds > 0f)
                bleedTimer += seconds;
        }

        float markTimer;

        /// <summary>Marked by the pet's bite (Quiver of the Hollow Hound): arrows hit it harder and Homing Arrow seeks it first.</summary>
        public bool IsMarked => markTimer > 0f;

        public void Mark(float seconds)
        {
            if (IsAlive)
                markTimer = Mathf.Max(markTimer, seconds);
        }

        public bool IsBurning => burn.IsBurning;

        public bool IsFrozen => chill.IsFrozen;

        /// <summary>Adds a burn stack: this much damage a second, already through the hit formula, for 3 s (up to 3).</summary>
        public void ApplyBurn(float damagePerSecond)
        {
            if (!IsAlive)
                return;
            burn.Apply(damagePerSecond);
        }

        /// <summary>Chills: slowed a quarter for 2 s; four chills within 2 s freeze it for 1 s (never a boss).</summary>
        public void ApplyChill()
        {
            if (!IsAlive)
                return;
            slow.Apply(1f - ChillMeter.SlowFraction, ChillMeter.SlowSeconds);
            if (chill.Apply(definition.Rank != EnemyRank.Boss))
                effects.Flash(FrozenColor, ChillMeter.FreezeSeconds);
        }

        void TickBurn(float deltaTime)
        {
            if (!burn.IsBurning && burnPending <= 0f)
                return;
            burnPending += burn.Tick(deltaTime);
            burnPulse += deltaTime;
            if (burnPulse < BleedPulseSeconds && burn.IsBurning)
                return;
            burnPulse = 0f;
            var damage = burnPending;
            burnPending = 0f;
            if (damage <= 0f || !IsAlive)
                return;
            var world = IsoMath.GroundToWorld(ground);
            DamageNumbers.Current?.Show(new Vector3(world.x, world.y, 0f), damage, false, isDamageToPlayer: false);
            TakeDamage(damage);
        }

        void TickBleed(float deltaTime)
        {
            if (bleedTimer <= 0f)
                return;
            var step = Mathf.Min(deltaTime, bleedTimer);
            bleedTimer -= deltaTime;
            bleedPending += bleedPerSecond * step;
            bleedPulse += deltaTime;
            if (bleedPulse < BleedPulseSeconds && bleedTimer > 0f)
                return;
            bleedPulse = 0f;
            var damage = bleedPending;
            bleedPending = 0f;
            if (damage <= 0f)
                return;
            var world = IsoMath.GroundToWorld(ground);
            DamageNumbers.Current?.Show(new Vector3(world.x, world.y, 0f), damage, false, isDamageToPlayer: false);
            TakeDamage(damage);
        }

        /// <summary>Called by the pack when another member engaged: an idle member joins in. It does not alert the
        /// pack again.</summary>
        internal void Wake()
        {
            if (State != EnemyState.Idle)
                return;
            State = EnemyState.Approach;
            leashTimer = 0f;
        }

        /// <summary>Advances the death fade. Returns true once the enemy is finished and can return to the pool.</summary>
        internal bool TickDeath(float deltaTime)
        {
            deathTimer -= deltaTime;
            var duration = Mathf.Max(definition.DeathSeconds, 1e-4f);
            var remaining = Mathf.Clamp01(deathTimer / duration);
            // The body burns away (SpriteEffects) while the shadow fades under it, and falls if it has a death animation.
            SetVisuals(1f, Mathf.Lerp(DeathEndScale, 1f, remaining));
            if (animationSet?.Death != null)
                ShowFrame(animationSet.Death, EnemyAnimationRules.OneShotFrame(1f - remaining, animationSet.Death.Frames));
            if (shadowRenderer != null)
                shadowRenderer.color = new Color(1f, 1f, 1f, remaining);
            effects.Tick(deltaTime);
            effects.SetDissolve(1f - remaining);
            return deathTimer <= 0f;
        }

        void UpdatePunch(float deltaTime)
        {
            effects.Tick(deltaTime);
            if (punchTimer <= 0f)
                return;

            punchTimer = Mathf.Max(0f, punchTimer - deltaTime);
            SetVisuals(1f, 1f + PunchScale * (punchTimer / PunchSeconds));
        }

        // scaleMultiplier is the transient part (wind-up swell, hit punch, death shrink); the definition's own
        // VisualScale is the permanent part that makes a Champion or Elite read as bigger at a glance. Color comes
        // from the sprite itself (see EnemyDefinition.BodySprite), not a runtime tint, so this only fades alpha.
        void SetVisuals(float alpha, float scaleMultiplier)
        {
            // The swell and shrink are the placeholder's tells; baked art tells with its own wind-up and fall.
            var scale = definition.VisualScale * (animationSet != null ? 1f : scaleMultiplier);
            transform.localScale = new Vector3(scale, scale, 1f);
            var color = new Color(1f, 1f, 1f, alpha);
            for (var i = 0; i < renderers.Length; i++)
                renderers[i].color = color;
        }

        /// <summary>
        /// Shows this frame's sprite from the baked animations, if the type has them: the attack through its wind-up to
        /// the strike and on through the recovery, a hit reaction, the run at the speed its feet were recorded at, or the
        /// idle; facing the player while attacking, else the way it moves. Called by the manager after <see cref="Tick"/>.
        /// </summary>
        internal void Animate(float deltaTime, EnemyManager world)
        {
            if (animationSet == null)
                return;
            var velocity = deltaTime > 0f ? (ground - lastGround) / deltaTime : Vector2.zero;
            lastGround = ground;
            hitAnimationTimer = Mathf.Max(0f, hitAnimationTimer - deltaTime);
            animationTime += deltaTime;
            var speed = velocity.magnitude;
            var count = animationSet.DirectionCount;

            if ((State == EnemyState.Attack || State == EnemyState.Recover) && animationSet.Attack != null)
            {
                Face(State == EnemyState.Attack && definition.Archetype == EnemyArchetype.Archer ? attackAim : world.PlayerGround - ground, count);
                var recovering = State == EnemyState.Recover;
                var duration = recovering ? EffectiveAttackRecoverSeconds : EffectiveAttackWindupSeconds;
                var progress = EnemyAnimationRules.AttackProgress(recovering, duration - stateTimer, duration);
                ShowFrame(animationSet.Attack, EnemyAnimationRules.OneShotFrame(progress, animationSet.Attack.Frames));
                return;
            }
            if (hitAnimationTimer > 0f && animationSet.Hit != null)
            {
                var progress = 1f - hitAnimationTimer / EnemyAnimationRules.HitSeconds;
                ShowFrame(animationSet.Hit, EnemyAnimationRules.OneShotFrame(progress, animationSet.Hit.Frames));
                return;
            }
            if (speed > EnemyAnimationRules.MovingSpeed && animationSet.Run != null)
            {
                Face(velocity, count);
                // The run plays at the speed the enemy moves over the speed its feet were recorded at.
                var rate = LocomotionRules.PlaybackRate(speed, animationSet.RunRecordedSpeed);
                ShowFrame(animationSet.Run, EnemyAnimationRules.LoopFrame(animationTime * rate, animationSet.RunSeconds, animationSet.Run.Frames));
                return;
            }
            ShowFrame(animationSet.Idle, EnemyAnimationRules.LoopFrame(animationTime, animationSet.IdleSeconds, animationSet.Idle.Frames));
        }

        void Face(Vector2 direction, int count) => animationRow = LocomotionRules.ChooseRow(animationRow, direction, count);

        void ShowFrame(CharacterSheet sheet, int frame)
        {
            var row = Mathf.Clamp(animationRow, 0, sheet.Rows.Length - 1);
            bodyRenderer.sprite = sheet.Rows[row][frame];
        }

        bool CanAttack(EnemyManager world, float distance) =>
            world.Player != null && world.Player.IsAlive && distance <= definition.AttackRange;

        /// <summary>Starts the wind-up. A brute paints its slam circle where it stands and an archer its aim line toward
        /// the player; both fill for the length of the wind-up (Docs/01: 0.6 to 1.2 s for a ground shape, 0.4 s for a
        /// projectile line), and the attack lands on that spot or line even if the player has moved.</summary>
        void BeginAttack(EnemyManager world, Vector2 playerGround)
        {
            State = EnemyState.Attack;
            stateTimer = EffectiveAttackWindupSeconds;
            attackCenter = ground;

            switch (definition.Archetype)
            {
                case EnemyArchetype.Brute:
                    if (slamMarker == null)
                        slamMarker = GroundMarker.Circle(ground, definition.SlamRadius, stateTimer, TelegraphColor, world.transform);
                    else
                        slamMarker.RestartCircle(ground, definition.SlamRadius, stateTimer);
                    break;

                case EnemyArchetype.Archer:
                    var aim = playerGround - ground;
                    attackAim = aim.sqrMagnitude > 1e-4f ? aim.normalized : Vector2.down;
                    var end = ground + attackAim * (definition.AttackRange + ArrowOvershoot);
                    if (aimMarker == null)
                        aimMarker = GroundMarker.Line(ground, end, AimLineWidth, stateTimer, TelegraphColor, world.transform);
                    else
                        aimMarker.RestartLine(ground, end, AimLineWidth, stateTimer);
                    break;
            }
        }

        void LandAttack(EnemyManager world, float distance)
        {
            if (world.Player == null || !world.Player.IsAlive)
                return;

            var damage = CombatFormulas.EnemyHitDamage(Level) * definition.DamageMultiplier;
            var armorIgnorePercent = definition.Rank == EnemyRank.Elite || definition.Rank == EnemyRank.Boss ? EliteArmorIgnorePercent : 0f;

            switch (definition.Archetype)
            {
                case EnemyArchetype.Brute:
                    Sfx.Play(SoundId.EnemySlam, 0.8f);
                    // Only the circle counts: stepping out of it is the counterplay, however close the brute is.
                    if (Vector2.Distance(world.PlayerGround, attackCenter) > definition.SlamRadius)
                        return;
                    break;

                case EnemyArchetype.Archer:
                    // The arrow does the hitting, or a wall stops it.
                    Sfx.Play(SoundId.ArrowShot, 0.7f);
                    world.Projectiles.Fire(this, ground + attackAim * definition.BodyRadius, attackAim * definition.ProjectileSpeed,
                        definition.AttackRange + ArrowOvershoot, damage, Level, armorIgnorePercent, definition.ProjectileColor);
                    return;

                default:
                    if (distance > definition.AttackRange + AttackForgiveness)
                        return;
                    // A pet beside this enemy may take the blow instead (Docs/02, Pets: it draws aggro).
                    if (PetController.Current != null && PetController.Current.TryTakeHit(this, damage))
                        return;
                    break;
            }

            // Docs/01: a brute's slam is a ground shape, avoided only by moving, so Momentum's dodge does not apply.
            var hpBefore = world.Player.Life;
            world.Player.TakeHit(damage, Level, armorIgnorePercent, dodgeable: definition.Archetype != EnemyArchetype.Brute);
            if (world.Player.Life < hpBefore)
                ApplyOnHitEffects(world, damage);
        }

        /// <summary>An elite's Vampiric and Frozen modifiers, after one of its hits landed (an arrow's too).</summary>
        internal void ApplyOnHitEffects(EnemyManager world, float damage)
        {
            if (HasModifier(EliteModifiers.Vampiric))
                life = Mathf.Min(MaxLife, life + damage * VampiricHealFraction);
            if (HasModifier(EliteModifiers.Frozen))
                world.PlayerController?.ApplySlow(FrozenSlowMultiplier, FrozenSlowSeconds);
        }

        /// <summary>An archer between shots: closes in until it can see the player in range, backs off when the player
        /// comes too close, otherwise holds (<see cref="ArcherSteering"/>). Still spreads from the crowd.</summary>
        void Reposition(EnemyManager world, Vector2 playerGround, float distance, bool lineOfSight, float deltaTime)
        {
            var desired = Vector2.zero;
            switch (ArcherSteering.Intent(distance, lineOfSight, definition.PreferredRange, definition.AttackRange))
            {
                case 1:
                    desired = world.ChaseDirection(ground, playerGround);
                    break;
                case -1:
                    desired = distance > 1e-4f ? (ground - playerGround) / distance : Vector2.zero;
                    break;
            }
            desired += Separation(world) + PushAway(playerGround, definition.StopDistance) * PlayerPushWeight;
            if (desired.sqrMagnitude > 1f)
                desired.Normalize();

            Move(desired * (EffectiveMoveSpeed * deltaTime), world);
        }

        // Stands its ground while attacking or recovering, but is still pushed apart from the crowd and the player.
        void Hold(EnemyManager world, Vector2 playerGround, float deltaTime)
        {
            var desired = Separation(world) + PushAway(playerGround, definition.StopDistance) * PlayerPushWeight;
            if (desired.sqrMagnitude > 1f)
                desired.Normalize();

            Move(desired * (EffectiveMoveSpeed * deltaTime), world);
        }

        Vector2 HomeDirection(EnemyManager world)
        {
            // With a clear line the straight route home is best; otherwise follow the pack's field to its anchor.
            if (!world.Nav.HasLineOfSight(ground, home) && pack != null && pack.TryGetHomeDirection(ground, out var direction))
                return direction;

            return (home - ground).normalized;
        }

        Vector2 Separation(EnemyManager world)
        {
            var neighbours = world.NeighbourBuffer;
            world.Hash.Query(ground, definition.SeparationRadius, neighbours);

            var push = Vector2.zero;
            for (var i = 0; i < neighbours.Count; i++)
                if (neighbours[i] != HashId)
                    push += PushAway(world.Hash.PointAt(neighbours[i]), definition.SeparationRadius);
            return push;
        }

        /// <summary>A push away from a point, growing from 0 at the radius to 1 at the point itself. Zero beyond the radius.</summary>
        Vector2 PushAway(Vector2 point, float radius)
        {
            var offset = ground - point;
            var distance = offset.magnitude;
            if (distance >= radius)
                return Vector2.zero;

            if (distance < 1e-4f)
            {
                // Exactly on the same spot: pick a stable direction, different for each enemy.
                var angle = HashId * 2.399963f;
                return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }

            return offset / distance * (1f - distance / radius);
        }

        /// <summary>Slows the enemy's movement to <paramref name="multiplier"/> for a while (a player skill).</summary>
        public void ApplySlow(float multiplier, float seconds) => slow.Apply(multiplier, seconds);

        /// <summary>Knocks the enemy along a ground offset, walls respected. A scripted boss is not moved.</summary>
        public void Push(Vector2 offset)
        {
            if (manager != null && IsAlive && !Scripted)
                Move(offset, manager);
        }

        /// <summary>Moves a <see cref="Scripted"/> enemy by a ground step, walls respected (long steps go in pieces).</summary>
        public void ScriptedMove(Vector2 step)
        {
            if (manager != null && IsAlive)
                Move(step, manager);
        }

        void Move(Vector2 step, EnemyManager world)
        {
            var length = step.magnitude;
            if (length <= 0f)
                return;

            // Normal frames are one piece. A hitch produces a long step that is walked in pieces, so a wall still stops it.
            var pieces = Mathf.CeilToInt(length / MaxStepLength);
            var piece = step / pieces;
            for (var i = 0; i < pieces; i++)
                MovePiece(piece, world);
        }

        void MovePiece(Vector2 step, EnemyManager world)
        {
            if (TryStep(step, world) || TryStep(Rotate(step, SlideAngle), world))
                return;
            TryStep(Rotate(step, -SlideAngle), world);
        }

        bool TryStep(Vector2 step, EnemyManager world)
        {
            var next = ground + step;
            if (!world.Nav.IsWalkable(IsoMath.GroundToCell(next)))
                return false;

            ground = next;
            SyncTransform();
            return true;
        }

        static Vector2 Rotate(Vector2 vector, float radians)
        {
            var cos = Mathf.Cos(radians);
            var sin = Mathf.Sin(radians);
            return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
        }

        void SyncTransform()
        {
            var world = IsoMath.GroundToWorld(ground);
            transform.position = new Vector3(world.x, world.y, 0f);
        }
    }
}
