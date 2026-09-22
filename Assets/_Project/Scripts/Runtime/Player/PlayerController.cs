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

        Rigidbody2D body;
        Vector2 groundVelocity;
        readonly SlowDebuff slow = new SlowDebuff();

        /// <summary>Current velocity on the ground plane, in ground units per second.</summary>
        public Vector2 GroundVelocity => groundVelocity;

        public float MoveSpeed => moveSpeed;

        public bool IsMoving => groundVelocity.sqrMagnitude > 0.0001f;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            if (input == null)
                input = FindAnyObjectByType<FloatingStickInput>();
        }

        /// <summary>Slows movement to <paramref name="multiplier"/> of normal speed for this many seconds, such as
        /// an elite's Frozen modifier. See <see cref="SlowDebuff"/> for how overlapping applications combine.</summary>
        public void ApplySlow(float multiplier, float seconds) => slow.Apply(multiplier, seconds);

        void FixedUpdate()
        {
            slow.Tick(Time.fixedDeltaTime);
            var effectiveMoveSpeed = moveSpeed * slow.Multiplier;

            var stick = input != null ? input.Value : Vector2.zero;
            var target = IsoMath.StickToGround(stick) * effectiveMoveSpeed;

            var maxChange = effectiveMoveSpeed / accelerationTime * Time.fixedDeltaTime;
            groundVelocity = Vector2.MoveTowards(groundVelocity, target, maxChange);

            body.linearVelocity = IsoMath.GroundToWorld(groundVelocity);
        }
    }
}
