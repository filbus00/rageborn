using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Fixed orthographic camera that follows the player. The player sits low on screen and the view leads
    /// the movement direction. See Docs/01-core-gameplay.md, Camera and perspective. While she shoots at an enemy, the
    /// lead is smaller and toward that enemy rather than along her run (the owner, 2026-10-19: kiting away from enemies
    /// panned the view away from them; leaning toward every enemy near her "feels wierd", so this is the milder fix).
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
        PlayerCombat combat;

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

            // Reduce motion turns the lead off (Docs/06, accessibility).
            // The lead is capped at its distance: Bull Rush runs at four times the move speed, and an uncapped lead shot the
            // view 6 units ahead and back again, so the camera seemed to lose the character (the owner, 2026-09-29).
            if (combat == null)
                combat = FindAnyObjectByType<PlayerCombat>();
            var target = combat != null ? combat.Target : null;
            var fighting = target != null && target.IsAlive;
            var targetLead = Vector2.zero;
            if (!SettingsDirector.Current.reduceMotion)
            {
                if (fighting)
                    targetLead = IsoMath.GroundToWorld(CameraRules.AimLead(target.GroundPosition - IsoMath.WorldToGround(player.transform.position)));
                else if (player.MoveSpeed > 0f)
                    targetLead = IsoMath.GroundToWorld(Vector2.ClampMagnitude(player.GroundVelocity / player.MoveSpeed, 1f) * leadDistance);
            }
            // Softer while fighting, so a change of target does not swing the view.
            lead = Vector2.SmoothDamp(lead, targetLead, ref leadVelocity, fighting ? CameraRules.FightSmoothSeconds : leadSmoothTime);

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

    /// <summary>The camera's lead while she shoots (pure, tested).</summary>
    public static class CameraRules
    {
        /// <summary>The most the view leads toward her target, in ground units (1.5 along her run otherwise).</summary>
        public const float AimLeadDistance = 0.8f;

        /// <summary>The lead's smoothing while fighting, in seconds (0.25 otherwise).</summary>
        public const float FightSmoothSeconds = 0.5f;

        /// <summary>Toward the target, a third of the way there, at most <see cref="AimLeadDistance"/>.</summary>
        public static Vector2 AimLead(Vector2 toTarget) => Vector2.ClampMagnitude(toTarget / 3f, AimLeadDistance);
    }
}
