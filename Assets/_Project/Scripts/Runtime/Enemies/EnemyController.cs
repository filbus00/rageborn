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

        // How much the enemy swells over its wind-up. Placeholder tell.
        const float WindupScale = 0.2f;

        // A hit makes the enemy swell briefly, and death shrinks it while it fades. Placeholder feedback.
        const float PunchSeconds = 0.1f;
        const float PunchScale = 0.2f;
        const float DeathEndScale = 0.6f;

        EnemyDefinition definition;
        EliteModifiers modifiers;
        EnemyPack pack;
        EnemyManager manager;
        SpriteRenderer[] renderers;
        SpriteRenderer bodyRenderer;
        Sprite defaultBodySprite;
        SpriteRenderer[] modifierIcons;
        Vector2 ground;
        Vector2 home;
        float leashTimer;
        float stateTimer;
        float life;
        float punchTimer;
        float deathTimer;

        public EnemyState State { get; private set; }

        public bool IsAlive => State != EnemyState.Dead;

        public float Life => life;

        public float MaxLife => definition != null ? definition.MaxLife : 0f;

        public EnemyDefinition Definition => definition;

        /// <summary>The modifiers this elite rolled at spawn. Always None for a Normal or Champion.</summary>
        public EliteModifiers Modifiers => modifiers;

        public bool HasModifier(EliteModifiers modifier) => (modifiers & modifier) != 0;

        /// <summary>Docs: Hasted is plus 30 percent move and attack speed.</summary>
        float EffectiveMoveSpeed => definition.MoveSpeed * (HasModifier(EliteModifiers.Hasted) ? HastedMoveSpeedMultiplier : 1f);

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

            var body = transform.Find("Body");
            if (body != null)
            {
                bodyRenderer = body.GetComponent<SpriteRenderer>();
                defaultBodySprite = bodyRenderer.sprite;
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
        internal void Activate(EnemyDefinition data, Vector2 groundPosition, bool aggroed, EnemyPack owner, EnemyManager owningManager, EliteModifiers rolledModifiers = EliteModifiers.None)
        {
            definition = data;
            modifiers = rolledModifiers;
            pack = owner;
            manager = owningManager;
            ground = groundPosition;
            home = groundPosition;
            leashTimer = 0f;
            life = data.MaxLife;
            punchTimer = 0f;
            deathTimer = 0f;
            State = aggroed ? EnemyState.Approach : EnemyState.Idle;
            // The renderer is pooled and reused across definitions, so a Normal spawn must reset it explicitly:
            // otherwise it would keep showing whatever a previous occupant (say, an Elite) last set it to.
            if (bodyRenderer != null)
                bodyRenderer.sprite = data.BodySprite != null ? data.BodySprite : defaultBodySprite;
            SetVisuals(1f, 1f);
            UpdateModifierIcons();
            SyncTransform();
            gameObject.SetActive(true);
        }

        internal void Deactivate() => gameObject.SetActive(false);

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

            var playerGround = world.PlayerGround;
            var distance = Vector2.Distance(ground, playerGround);

            switch (State)
            {
                case EnemyState.Idle:
                    if (distance > definition.AggroRange)
                        return;
                    State = EnemyState.Approach;
                    leashTimer = 0f;
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

                    if (CanAttack(world, distance))
                    {
                        State = EnemyState.Attack;
                        stateTimer = EffectiveAttackWindupSeconds;
                        return;
                    }
                    break;

                case EnemyState.Attack:
                    stateTimer -= deltaTime;
                    SetVisuals(1f, 1f + WindupScale * (1f - Mathf.Clamp01(stateTimer / Mathf.Max(EffectiveAttackWindupSeconds, 1e-4f))));
                    if (stateTimer <= 0f)
                    {
                        LandAttack(world, distance);
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
            if (life <= 0f)
            {
                life = 0f;
                State = EnemyState.Dead;
                deathTimer = definition.DeathSeconds;
                HideModifierIcons();
                pack?.NotifyDeath(this);
                manager?.NotifyKilled(this);
                return true;
            }

            punchTimer = PunchSeconds;

            // A hit wakes an idle or returning enemy. One that is mid-attack keeps attacking.
            if (State == EnemyState.Idle || State == EnemyState.Return)
            {
                State = EnemyState.Approach;
                leashTimer = 0f;
            }
            return false;
        }

        /// <summary>Advances the death fade. Returns true once the enemy is finished and can return to the pool.</summary>
        internal bool TickDeath(float deltaTime)
        {
            deathTimer -= deltaTime;
            var duration = Mathf.Max(definition.DeathSeconds, 1e-4f);
            var remaining = Mathf.Clamp01(deathTimer / duration);
            SetVisuals(remaining, Mathf.Lerp(DeathEndScale, 1f, remaining));
            return deathTimer <= 0f;
        }

        void UpdatePunch(float deltaTime)
        {
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
            var scale = definition.VisualScale * scaleMultiplier;
            transform.localScale = new Vector3(scale, scale, 1f);
            var color = new Color(1f, 1f, 1f, alpha);
            for (var i = 0; i < renderers.Length; i++)
                renderers[i].color = color;
        }

        bool CanAttack(EnemyManager world, float distance) =>
            world.Player != null && world.Player.IsAlive && distance <= definition.AttackRange;

        void LandAttack(EnemyManager world, float distance)
        {
            if (world.Player == null || !world.Player.IsAlive || distance > definition.AttackRange + AttackForgiveness)
                return;

            var damage = CombatFormulas.EnemyHitDamage(definition.Level) * definition.DamageMultiplier;
            var armorIgnorePercent = definition.Rank == EnemyRank.Elite ? EliteArmorIgnorePercent : 0f;
            world.Player.TakeHit(damage, definition.Level, armorIgnorePercent);

            if (HasModifier(EliteModifiers.Vampiric))
                life = Mathf.Min(definition.MaxLife, life + damage * VampiricHealFraction);
            if (HasModifier(EliteModifiers.Frozen))
                world.PlayerController?.ApplySlow(FrozenSlowMultiplier, FrozenSlowSeconds);
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
