using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Fixed orthographic camera that follows the player. The player sits low on screen and the view leads
    /// the movement direction. See Docs/01-core-gameplay.md, Camera and perspective.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FollowCamera : MonoBehaviour
    {
        [Tooltip("Left empty, the first PlayerController in the scene is used.")]
        [SerializeField] PlayerController player;

        [Tooltip("Where the player stands, as a fraction of screen height from the bottom.")]
        [SerializeField, Range(0.2f, 0.8f)] float playerScreenHeight = 0.45f;

        [Tooltip("How far the view leads the movement direction at full speed, in ground units.")]
        [SerializeField] float leadDistance = 1.5f;

        [Tooltip("Smoothing time of the lead, in seconds.")]
        [SerializeField] float leadSmoothTime = 0.25f;

        Camera cam;
        Vector2 lead;
        Vector2 leadVelocity;

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
        }

        void Start()
        {
            Apply();
        }

        void LateUpdate()
        {
            if (player == null)
                return;

            var targetLead = player.MoveSpeed > 0f
                ? IsoMath.GroundToWorld(player.GroundVelocity / player.MoveSpeed * leadDistance)
                : Vector2.zero;
            lead = Vector2.SmoothDamp(lead, targetLead, ref leadVelocity, leadSmoothTime);

            Apply();
        }

        void Apply()
        {
            if (player == null)
                return;

            // Centre of the screen is 50 percent; the player stands below it, so the camera sits above the player.
            var framing = (0.5f - playerScreenHeight) * 2f * cam.orthographicSize;

            var position = player.transform.position;
            transform.position = new Vector3(position.x + lead.x, position.y + framing + lead.y, transform.position.z);
        }
    }
}
