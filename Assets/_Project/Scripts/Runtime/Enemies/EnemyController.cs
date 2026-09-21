using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The enemy states from Docs/01-core-gameplay.md that exist so far. Attack and Recover arrive with enemy attacks.
    /// </summary>
    public enum EnemyState
    {
        /// <summary>Standing at its home spot until the player comes within aggro range.</summary>
        Idle,

        /// <summary>Chasing the player.</summary>
        Approach,

        /// <summary>The leash broke: walking back to the home spot. Ignores the player until it arrives.</summary>
        Return,

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

        // A hit makes the enemy swell briefly, and death shrinks it while it fades. Placeholder feedback.
        const float PunchSeconds = 0.1f;
        const float PunchScale = 0.2f;
        const float DeathEndScale = 0.6f;

        EnemyDefinition definition;
        EnemyPack pack;
        SpriteRenderer[] renderers;
        Vector2 ground;
        Vector2 home;
        float leashTimer;
        float life;
        float punchTimer;
        float deathTimer;

        public EnemyState State { get; private set; }

        public bool IsAlive => State != EnemyState.Dead;

        public float Life => life;

        public float MaxLife => definition != null ? definition.MaxLife : 0f;

        public EnemyDefinition Definition => definition;

        /// <summary>Position on the ground plane, in ground units.</summary>
        public Vector2 GroundPosition => ground;

        /// <summary>Where the enemy stands while idle and returns to after losing the player.</summary>
        public Vector2 HomePosition => home;

        /// <summary>This enemy's id in the manager's spatial hash for the current frame.</summary>
        internal int HashId { get; set; }

        void Awake() => renderers = GetComponentsInChildren<SpriteRenderer>(true);

        /// <param name="groundPosition">Where the enemy appears. This becomes its home spot.</param>
        /// <param name="owner">The pack the enemy belongs to, used to find its way home. Can be null.</param>
        internal void Activate(EnemyDefinition data, Vector2 groundPosition, bool aggroed, EnemyPack owner)
        {
            definition = data;
            pack = owner;
            ground = groundPosition;
            home = groundPosition;
            leashTimer = 0f;
            life = data.MaxLife;
            punchTimer = 0f;
            deathTimer = 0f;
            State = aggroed ? EnemyState.Approach : EnemyState.Idle;
            SetVisuals(1f, 1f);
            SyncTransform();
            gameObject.SetActive(true);
        }

        internal void Deactivate() => gameObject.SetActive(false);

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
                    break;

                case EnemyState.Return:
                    if (Vector2.Distance(ground, home) <= ArriveDistance)
                    {
                        State = EnemyState.Idle;
                        return;
                    }
                    Move(HomeDirection(world) * (definition.MoveSpeed * deltaTime), world);
                    return;
            }

            // Close enough to stop closing in; the pushes below still run so a crowd spreads around the player.
            var desired = distance > definition.StopDistance ? world.ChaseDirection(ground, playerGround) : Vector2.zero;
            desired += Separation(world);

            // The player is solid to enemies. Without this the crowd behind squeezes the front row through the player.
            desired += PushAway(playerGround, definition.StopDistance) * PlayerPushWeight;

            if (desired.sqrMagnitude > 1f)
                desired.Normalize();

            Move(desired * (definition.MoveSpeed * deltaTime), world);
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
                pack?.NotifyDeath(this);
                return true;
            }

            punchTimer = PunchSeconds;
            if (State != EnemyState.Approach)
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

        void SetVisuals(float alpha, float scale)
        {
            transform.localScale = new Vector3(scale, scale, 1f);
            var color = new Color(1f, 1f, 1f, alpha);
            for (var i = 0; i < renderers.Length; i++)
                renderers[i].color = color;
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
