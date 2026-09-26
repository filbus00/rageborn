using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Moves the player from the floating stick. Movement happens in ground space (equal speed in every
    /// direction) and is projected to the squashed isometric world for the physics body.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Tooltip("Left empty, the first FloatingStickInput in the scene is used.")]
        [SerializeField] FloatingStickInput input;

        [Tooltip("Ground units per second at full stick deflection. Tuning value, not yet in the design docs.")]
        [SerializeField] float moveSpeed = 4f;

        [Tooltip("Seconds to go between standing and full speed. Docs/01-core-gameplay.md gives 80 ms for stopping.")]
        [SerializeField] float accelerationTime = 0.08f;

        // Below this speed, in ground units per second, the character counts as standing for Stillness and Momentum.
        const float MovingThreshold = 0.5f;

        Rigidbody2D body;
        Vector2 groundVelocity;
        readonly SlowDebuff slow = new SlowDebuff();
        readonly StanceStacks stance = new StanceStacks();
        Vector2 dashVelocity;
        float dashSeconds;
        SpriteEffects effects;

        /// <summary>Current velocity on the ground plane, in ground units per second.</summary>
        public Vector2 GroundVelocity => groundVelocity;

        public float MoveSpeed => moveSpeed;

        public bool IsMoving => groundVelocity.sqrMagnitude > 0.0001f;

        /// <summary>Stillness and Momentum (Docs/01-core-gameplay.md), ticked with the physics step.</summary>
        public StanceStacks Stance => stance;

        /// <summary>Whether a skill's dash (Bull Rush) is carrying the character; the stick is ignored meanwhile.</summary>
        public bool IsDashing => dashSeconds > 0f;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            if (input == null)
                input = FindAnyObjectByType<FloatingStickInput>();
            var bodySprite = transform.Find("Body");
            if (bodySprite != null && bodySprite.TryGetComponent<SpriteRenderer>(out var bodyRenderer))
                effects = new SpriteEffects(new[] { bodyRenderer });
        }

        /// <summary>Flashes the character's body a color for a moment, such as red when hit.</summary>
        public void Flash(Color color, float seconds) => effects?.Flash(color, seconds);

        void Update() => effects?.Tick(Time.deltaTime);

        /// <summary>Slows movement to <paramref name="multiplier"/> of normal speed for this many seconds, such as
        /// an elite's Frozen modifier. See <see cref="SlowDebuff"/> for how overlapping applications combine.</summary>
        public void ApplySlow(float multiplier, float seconds) => slow.Apply(multiplier, seconds);

        /// <summary>Carries the character along a ground direction for a distance at a speed, whatever the stick says.
        /// Walls still stop it, since the physics body moves.</summary>
        public void Dash(Vector2 direction, float distance, float speed)
        {
            if (speed <= 0f || distance <= 0f || direction.sqrMagnitude < 1e-6f)
                return;
            dashVelocity = direction.normalized * speed;
            dashSeconds = distance / speed;
        }

        void FixedUpdate()
        {
            var deltaTime = Time.fixedDeltaTime;
            slow.Tick(deltaTime);

            if (dashSeconds > 0f)
            {
                dashSeconds -= deltaTime;
                groundVelocity = dashVelocity;
                body.linearVelocity = IsoMath.GroundToWorld(groundVelocity);
                stance.Tick(deltaTime, true);
                if (dashSeconds <= 0f)
                    groundVelocity = dashVelocity.normalized * moveSpeed;
                return;
            }

            // Momentum is worth its speed: Docs/01, 5 percent movement speed per stack.
            var effectiveMoveSpeed = moveSpeed * slow.Multiplier * stance.MoveSpeedMultiplier;

            var stick = input != null ? input.Value : Vector2.zero;
            var target = IsoMath.StickToGround(stick) * effectiveMoveSpeed;

            var maxChange = effectiveMoveSpeed / accelerationTime * Time.fixedDeltaTime;
            groundVelocity = Vector2.MoveTowards(groundVelocity, target, maxChange);

            body.linearVelocity = IsoMath.GroundToWorld(groundVelocity);

            // The actual speed decides, not the stick: pushing into a wall is standing.
            var actual = IsoMath.WorldToGround(body.linearVelocity);
            stance.Tick(deltaTime, actual.magnitude > MovingThreshold && target.sqrMagnitude > 0f);
        }
    }
}
