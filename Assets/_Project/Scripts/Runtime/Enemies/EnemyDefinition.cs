using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Data for one enemy type. Ranges are in ground units. Values marked as tuning are starting points that
    /// Docs/01-core-gameplay.md does not fix yet.
    /// </summary>
    [CreateAssetMenu(menuName = "ARPG/Enemy Definition", fileName = "Enemy")]
    public class EnemyDefinition : ScriptableObject
    {
        [Tooltip("Ground units per second. Tuning value: swarmers are fast, but this stays under the player's 4 so they can be kited.")]
        [SerializeField, Min(0f)] float moveSpeed = 3.6f;

        [Tooltip("An idle enemy starts chasing when the player comes this close. Docs: 7 for normal enemies, 10 for elites.")]
        [SerializeField, Min(0f)] float aggroRange = 7f;

        [Tooltip("Beyond this distance the enemy starts losing track of the player. Docs: 20.")]
        [SerializeField, Min(0f)] float leashRange = 20f;

        [Tooltip("Seconds beyond the leash range before the enemy gives up. Docs: 4.")]
        [SerializeField, Min(0f)] float leashSeconds = 4f;

        [Tooltip("The enemy stops closing in at this distance from the player. Tuning value, becomes the melee reach later.")]
        [SerializeField, Min(0f)] float stopDistance = 0.7f;

        [Tooltip("Enemies closer than this push each other apart, which gives a swarm its loose cluster. Tuning value.")]
        [SerializeField, Min(0.1f)] float separationRadius = 0.8f;

        public float MoveSpeed => moveSpeed;
        public float AggroRange => aggroRange;
        public float LeashRange => leashRange;
        public float LeashSeconds => leashSeconds;
        public float StopDistance => stopDistance;
        public float SeparationRadius => separationRadius;
    }
}
