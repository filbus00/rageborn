using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Fixed orthographic camera that follows the player. The player sits low on screen and the view leads
    /// the movement direction. See Docs/01-core-gameplay.md, Camera and perspective. In a fight it leans toward the
    /// enemies after her instead (<see cref="CameraRules.ThreatLead"/>; the owner, 2026-10-19: kiting away from enemies
    /// on one side panned the view the other way and hid them).
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
        EnemyManager enemies;
        float nextSearch;
        readonly System.Collections.Generic.List<Vector2> threats = new System.Collections.Generic.List<Vector2>(32);

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
            var targetLead = Vector2.zero;
            if (!SettingsDirector.Current.reduceMotion)
            {
                if (GatherThreats())
                    targetLead = IsoMath.GroundToWorld(CameraRules.ThreatLead(threats));
                else if (player.MoveSpeed > 0f)
                    targetLead = IsoMath.GroundToWorld(Vector2.ClampMagnitude(player.GroundVelocity / player.MoveSpeed, 1f) * leadDistance);
            }
            lead = Vector2.SmoothDamp(lead, targetLead, ref leadVelocity, leadSmoothTime);

            Apply();
        }

        // The awake enemies near her, as offsets from her on the ground; whether there are any.
        bool GatherThreats()
        {
            threats.Clear();
            // Looked for at most once a second (the town has none).
            if (enemies == null && Time.unscaledTime >= nextSearch)
            {
                nextSearch = Time.unscaledTime + 1f;
                enemies = FindAnyObjectByType<EnemyManager>();
            }
            if (enemies == null)
                return false;
            var here = IsoMath.WorldToGround(player.transform.position);
            var active = enemies.Active;
            for (var i = 0; i < active.Count; i++)
            {
                var e = active[i];
                if (e == null || !e.IsAlive || e.State == EnemyState.Idle || e.State == EnemyState.Return)
                    continue;
                var offset = e.GroundPosition - here;
                if (offset.sqrMagnitude <= CameraRules.ThreatRadius * CameraRules.ThreatRadius)
                    threats.Add(offset);
            }
            return threats.Count > 0;
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

    /// <summary>Where the camera leans in a fight (pure, tested).</summary>
    public static class CameraRules
    {
        /// <summary>Awake enemies this near her (ground units) are the fight the view keeps in sight.</summary>
        public const float ThreatRadius = 10f;

        /// <summary>The share of the way toward their middle the view leans, and the most it leans (ground units).</summary>
        public const float ThreatShare = 0.45f, MaxThreatLead = 2f;

        /// <summary>The lean toward the middle of the enemies after her: none when they surround her evenly, the most
        /// when they all come from one side, whichever way she runs.</summary>
        public static Vector2 ThreatLead(System.Collections.Generic.IReadOnlyList<Vector2> offsets)
        {
            if (offsets == null || offsets.Count == 0)
                return Vector2.zero;
            var sum = Vector2.zero;
            for (var i = 0; i < offsets.Count; i++)
                sum += offsets[i];
            return Vector2.ClampMagnitude(sum / offsets.Count * ThreatShare, MaxThreatLead);
        }
    }
}
