using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// She walks herself a short way (2026-11-10, arriving on a forest level out of its way in): the stick steered one
    /// way until she is near the end point or the time runs out, then handed back. Touching the stick does nothing
    /// meanwhile; it is under a second.
    /// </summary>
    public class TrailWalk : MonoBehaviour
    {
        FloatingStickInput stick;
        Vector2 steer;
        Vector2 end;
        float secondsLeft;

        public static void Begin(PlayerController player, Vector2 steer, Vector2 end, float seconds)
        {
            var stick = FindAnyObjectByType<FloatingStickInput>();
            if (stick == null)
                return;
            var walk = player.gameObject.AddComponent<TrailWalk>();
            walk.stick = stick;
            walk.steer = steer;
            walk.end = end;
            walk.secondsLeft = seconds;
            stick.Steered = steer;
        }

        void Update()
        {
            secondsLeft -= Time.deltaTime;
            // Stopped once she is past the end along the way she walks, or out of time.
            var past = Vector2.Dot((Vector2)transform.position - end, steer) >= 0f;
            if (past || secondsLeft <= 0f || stick == null)
            {
                Destroy(this);
                return;
            }
            stick.Steered = steer;
        }

        void OnDestroy()
        {
            if (stick != null)
                stick.Steered = null;
        }
    }
}
