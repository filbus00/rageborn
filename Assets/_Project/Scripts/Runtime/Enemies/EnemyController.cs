using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The enemy states from Docs/01-core-gameplay.md that exist so far. Attack, Recover and Death arrive with combat.
    /// </summary>
    public enum EnemyState
    {
        Idle,
        Approach,
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

        EnemyDefinition definition;
        Vector2 ground;
        float leashTimer;

        public EnemyState State { get; private set; }

        public EnemyDefinition Definition => definition;

        /// <summary>Position on the ground plane, in ground units.</summary>
        public Vector2 GroundPosition => ground;

        /// <summary>This enemy's id in the manager's spatial hash for the current frame.</summary>
        internal int HashId { get; set; }

        internal void Activate(EnemyDefinition data, Vector2 groundPosition, bool aggroed)
        {
            definition = data;
            ground = groundPosition;
            leashTimer = 0f;
            State = aggroed ? EnemyState.Approach : EnemyState.Idle;
            SyncTransform();
            gameObject.SetActive(true);
        }

        internal void Deactivate() => gameObject.SetActive(false);

        /// <summary>Advances the enemy by one frame. Returns false when it gave up and should return to the pool.</summary>
        internal bool Tick(float deltaTime, EnemyManager world)
        {
            var playerGround = world.PlayerGround;
            var distance = Vector2.Distance(ground, playerGround);

            if (distance > definition.LeashRange)
            {
                leashTimer += deltaTime;
                if (leashTimer >= definition.LeashSeconds)
                    return false;
            }
            else
            {
                leashTimer = 0f;
            }

            if (State == EnemyState.Idle)
            {
                if (distance > definition.AggroRange)
                    return true;
                State = EnemyState.Approach;
            }

            // Close enough to stop closing in; the pushes below still run so a crowd spreads around the player.
            var desired = distance > definition.StopDistance ? world.ChaseDirection(ground, playerGround) : Vector2.zero;
            desired += Separation(world);

            // The player is solid to enemies. Without this the crowd behind squeezes the front row through the player.
            desired += PushAway(playerGround, definition.StopDistance) * PlayerPushWeight;

            if (desired.sqrMagnitude > 1f)
                desired.Normalize();

            Move(desired * (definition.MoveSpeed * deltaTime), world);
            return true;
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
            if (step == Vector2.zero)
                return;

            var next = ground + step;
            if (!world.Nav.IsWalkable(IsoMath.GroundToCell(next)))
                return;

            ground = next;
            SyncTransform();
        }

        void SyncTransform()
        {
            var world = IsoMath.GroundToWorld(ground);
            transform.position = new Vector3(world.x, world.y, 0f);
        }
    }
}
